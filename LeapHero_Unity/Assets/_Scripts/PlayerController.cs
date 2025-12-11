using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // --- ENUMS ---
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;
    
    public enum EstadoLingua { Pronta, Atirando, PuxandoSapo, Retraindo }
    public EstadoLingua estadoLingua = EstadoLingua.Pronta;

    [Header("Geral")]
    public LayerMask layerSolido; 

    [Header("Guerreiro - Movimento")]
    public float velGuerreiro = 4f;
    public float puloGuerreiro = 12f;
    public Color corGuerreiro = Color.white;

    [Header("Guerreiro - Combate")]
    public KeyCode teclaAtaque = KeyCode.J;
    public Transform pontoAtaque; 
    public float raioAtaque = 0.8f; 
    public LayerMask layerInimigos; 
    public float duracaoTravamentoAereo = 0.3f; 
    public float cooldownAtaque = 0.4f; 
    private float tempoProximoAtaque;
    private bool estaAtacando = false; 

    [Header("Sapo - Movimento")]
    public float velSapo = 7f;
    public float puloSapo = 15f;
    public int totalPulosSapo = 1;
    public Color corSapo = Color.green;

    [Header("Sapo - Wall Mechanics")]
    public float forcaWallJumpX = 12f; 
    public float forcaWallJumpY = 16f;
    public float velocidadeDeslizar = 2f; 
    public float velocidadeAjusteVertical = 3f; 
    public float tempoGrudadoNaParede = 1.5f; 
    public float tempoBloqueioWallJump = 0.2f; 
    public float tempoResistenciaParede = 0.2f; 

    [Header("Sapo - Língua")]
    public float alcanceLingua = 8f;
    public float velocidadeLinguaIda = 25f;    
    public float velocidadeLinguaVolta = 35f; 
    public float velocidadePuxadaCorpo = 18f; 
    public float delayEntreLinguadas = 0.5f; 
    public KeyCode teclaLingua = KeyCode.L; 
    public LineRenderer lineRenderer;
    public Vector3 offsetBoca; 

    // INTERNAS
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D colisor;
    private Animator anim; 
    private float xInput;
    private float yInput;
    private bool jumpInputDown;
    private int pulosExtras;
    private bool viradoDireita = true;
    private float wallStickTimer; 
    private float wallJumpBlockTimer; 
    private float timerTentativaSair; 
    private int ladoParede = 0;
    private Vector2 destinoLingua;       
    private Vector2 pontaLinguaAtual;    
    private bool acertouParede;          
    private float tempoParaProximaLingua;
    private Vector2 posicaoInicialTiro; 
    public bool isGrounded;
    public bool isTouchingWall;
    
    // NOVO: Controle de Morte
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        colisor = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>(); 

        if(lineRenderer != null) lineRenderer.enabled = false;
        TrocarEstado(Estado.Guerreiro);
    }

    void Update()
    {
        // 1. CHECAGEM DE MORTE (NOVO!)
        if (isDead) return; // Se morreu, para tudo aqui. Não lê input nenhum.

        // 2. INPUTS
        if (estaAtacando) xInput = 0;
        else xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (wallJumpBlockTimer > 0) wallJumpBlockTimer -= Time.deltaTime;

        // 3. PULO
        if (estadoLingua == EstadoLingua.Pronta && Input.GetButtonDown("Jump") && !estaAtacando) 
            jumpInputDown = true;

        // 4. TROCA DE ESTADO
        if (Input.GetKeyDown(KeyCode.C) && !estaAtacando) 
            TrocarEstado(estadoAtual == Estado.Guerreiro ? Estado.Sapo : Estado.Guerreiro);

        // 5. HABILIDADES
        if (estadoAtual == Estado.Sapo && estadoLingua == EstadoLingua.Pronta && Input.GetKeyDown(teclaLingua) && Time.time > tempoParaProximaLingua) 
        {
            if (xInput > 0 && !viradoDireita) Flip();
            else if (xInput < 0 && viradoDireita) Flip();
            IniciarLingua();
        }

        if (estadoAtual == Estado.Guerreiro && Input.GetKeyDown(teclaAtaque) && Time.time > tempoProximoAtaque && !estaAtacando)
        {
            StartCoroutine(RealizarAtaque());
        }

        // 6. FLIP
        if (estadoLingua == EstadoLingua.Pronta && !isTouchingWall && wallJumpBlockTimer <= 0 && !estaAtacando)
        {
             if (xInput > 0 && !viradoDireita) Flip();
             else if (xInput < 0 && viradoDireita) Flip();
        }

        // 7. ATUALIZA ANIMATOR
        AtualizarAnimator();
    }

    // --- FUNÇÃO PÚBLICA PARA O SCRIPT DE VIDA CHAMAR (NOVO!) ---
    public void Morrer()
    {
        if (isDead) return; // Já está morto
        isDead = true;
        
        // Para a física
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1; // Deixa cair no chão se estiver voando
        
        // Toca animação
        if (anim != null) anim.SetTrigger("Die");
        
        // Desativa colisores se quiser que ele atravesse o chão (opcional)
        // colisor.enabled = false; 
        
        Debug.Log("O Player Morreu!");
    }

    void AtualizarAnimator()
    {
        if (anim == null) return;

        anim.SetFloat("Speed", Mathf.Abs(xInput));
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsSapo", estadoAtual == Estado.Sapo);
        
        bool deslizando = (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded && rb.linearVelocity.y < 0);
        anim.SetBool("IsWallSliding", deslizando);

        // IsGrappling mantém a pose da língua esticada enquanto ela não volta
        anim.SetBool("IsGrappling", estadoLingua != EstadoLingua.Pronta);
    }

    IEnumerator RealizarAtaque()
    {
        estaAtacando = true;
        tempoProximoAtaque = Time.time + cooldownAtaque;

        float gravidadeOriginal = rb.gravityScale;
        rb.gravityScale = 0; 
        rb.linearVelocity = Vector2.zero; 

        if (anim != null) anim.SetTrigger("Attack"); 

        Collider2D[] inimigos = Physics2D.OverlapCircleAll(pontoAtaque.position, raioAtaque, layerInimigos);
        foreach (Collider2D inimigo in inimigos)
        {
            // Aqui você chamaria o script de vida do inimigo
            Destroy(inimigo.gameObject);
        }

        yield return new WaitForSeconds(duracaoTravamentoAereo);

        rb.gravityScale = gravidadeOriginal; 
        estaAtacando = false;
    }

    void LateUpdate()
    {
        if (lineRenderer != null)
        {
            if (estadoLingua != EstadoLingua.Pronta)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2; 
                Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
                lineRenderer.SetPosition(0, transform.position + offsetReal);    
                lineRenderer.SetPosition(1, pontaLinguaAtual); 
            }
            else lineRenderer.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return; // (NOVO!) Morto não processa física de movimento

        VerificarColisoes();

        if (estadoLingua != EstadoLingua.Pronta) { ProcessarFisicaLingua(); return; }
        if (estaAtacando) return; 

        float velAtual = (estadoAtual == Estado.Guerreiro) ? velGuerreiro : velSapo;
        
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) MecanicaDeParedeoSapo();
        else MovimentoNormal(velAtual);

        if (jumpInputDown) { jumpInputDown = false; ProcessarPulo(); }
        if (isGrounded) pulosExtras = totalPulosSapo;
    }

    void MovimentoNormal(float velAtual) {
        rb.gravityScale = 4; wallStickTimer = tempoGrudadoNaParede; timerTentativaSair = 0;
        if (wallJumpBlockTimer <= 0) rb.linearVelocity = new Vector2(xInput * velAtual, rb.linearVelocity.y);
    }

    void MecanicaDeParedeoSapo() {
        if (wallJumpBlockTimer > 0) return;
        bool tentandoSair = (xInput != 0 && xInput != ladoParede);
        if (tentandoSair) timerTentativaSair += Time.deltaTime; else timerTentativaSair = 0;

        if (tentandoSair && timerTentativaSair > tempoResistenciaParede) {
            rb.gravityScale = 4; rb.linearVelocity = new Vector2(xInput * velSapo, rb.linearVelocity.y); return;
        }
        
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
        if (wallStickTimer > 0) {
            rb.gravityScale = 0;
            if (yInput < 0) rb.linearVelocity = new Vector2(0, -velocidadeAjusteVertical); else rb.linearVelocity = Vector2.zero; 
            wallStickTimer -= Time.deltaTime;
            if (ladoParede == 1 && viradoDireita) Flip(); else if (ladoParede == -1 && !viradoDireita) Flip();
        } else {
            rb.gravityScale = 4; 
            float velQueda = (yInput < 0) ? velocidadeAjusteVertical : velocidadeDeslizar;
            rb.linearVelocity = new Vector2(0, Mathf.Clamp(rb.linearVelocity.y, -velQueda, float.MaxValue));
        }
    }

    void RealizarWallJump() {
        wallStickTimer = tempoGrudadoNaParede; wallJumpBlockTimer = tempoBloqueioWallJump; timerTentativaSair = 0; 
        int dirPulo = -ladoParede; rb.linearVelocity = Vector2.zero; 
        rb.AddForce(new Vector2(dirPulo * forcaWallJumpX, forcaWallJumpY), ForceMode2D.Impulse);
        if (dirPulo == 1 && !viradoDireita) Flip(); else if (dirPulo == -1 && viradoDireita) Flip();
    }

    void IniciarLingua() {
        Vector2 direcao = viradoDireita ? Vector2.right : Vector2.left;
        estadoLingua = EstadoLingua.Atirando; 
        
        // (NOVO!) Dispara o Trigger da animação de cuspir
        if(anim != null) anim.SetTrigger("TongueShoot");

        rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;
        posicaoInicialTiro = transform.position;
        Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
        Vector3 origemTiro = transform.position + offsetReal; pontaLinguaAtual = origemTiro; 
        RaycastHit2D hit = Physics2D.Raycast(origemTiro, direcao, alcanceLingua, layerSolido);
        if (hit.collider != null) { destinoLingua = hit.point; acertouParede = true; }
        else { destinoLingua = (Vector2)origemTiro + (direcao * alcanceLingua); acertouParede = false; }
    }

    void ProcessarFisicaLingua() {
        switch (estadoLingua) {
            case EstadoLingua.Atirando:
                rb.linearVelocity = Vector2.zero; 
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, destinoLingua, velocidadeLinguaIda * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, destinoLingua) < 0.1f) {
                    if (acertouParede) estadoLingua = EstadoLingua.PuxandoSapo; else estadoLingua = EstadoLingua.Retraindo;
                } break;
            case EstadoLingua.PuxandoSapo:
                float distOrigem = Vector2.Distance(transform.position, posicaoInicialTiro);
                float distDestino = Vector2.Distance(transform.position, destinoLingua);
                if (distOrigem > 0.5f) {
                    Collider2D col = Physics2D.OverlapCircle(transform.position, 0.5f, layerSolido);
                    if (col != null && distDestino > 1.0f) { estadoLingua = EstadoLingua.Retraindo; rb.gravityScale = 4; rb.linearVelocity = Vector2.zero; break; }
                }
                rb.linearVelocity = Vector2.zero; transform.position = Vector2.MoveTowards(transform.position, pontaLinguaAtual, velocidadePuxadaCorpo * Time.deltaTime);
                if (distDestino < 0.5f) FinalizarLingua(); break;
            case EstadoLingua.Retraindo:
                if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero;
                Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
                Vector3 alvoRetracao = transform.position + offsetReal;
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, alvoRetracao, velocidadeLinguaVolta * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, alvoRetracao) < 0.1f) FinalizarLingua(); break;
        }
    }

    void FinalizarLingua() { estadoLingua = EstadoLingua.Pronta; rb.gravityScale = 4; tempoParaProximaLingua = Time.time + delayEntreLinguadas; }
    
    void ProcessarPulo() {
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) RealizarWallJump();
        else if (isGrounded) ExecutarPulo((estadoAtual == Estado.Guerreiro) ? puloGuerreiro : puloSapo);
        else if (estadoAtual == Estado.Sapo && pulosExtras > 0) { ExecutarPulo(puloSapo); pulosExtras--; }
    }

    void ExecutarPulo(float forca) { rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); rb.AddForce(Vector2.up * forca, ForceMode2D.Impulse); }

    void VerificarColisoes() {
        float margem = 0.05f; Bounds b = colisor.bounds;
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margem, layerSolido);
        Vector2 tamanhoSensorParede = new Vector2(b.size.x, b.size.y * 0.9f);
        bool paredeDir = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.right, margem, layerSolido);
        bool paredeEsq = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.left, margem, layerSolido);
        if (paredeDir) { isTouchingWall = true; ladoParede = 1; } else if (paredeEsq) { isTouchingWall = true; ladoParede = -1; } else { isTouchingWall = false; ladoParede = 0; }
    }

    void Flip() { viradoDireita = !viradoDireita; Vector3 escala = transform.localScale; escala.x *= -1; transform.localScale = escala; }

    void TrocarEstado(Estado novo) { 
        estadoAtual = novo; 
        sr.color = (estadoAtual == Estado.Guerreiro) ? corGuerreiro : corSapo; 
        
        if(anim != null) anim.SetBool("IsSapo", estadoAtual == Estado.Sapo);

        pulosExtras = (estadoAtual == Estado.Sapo) ? totalPulosSapo : 0; 
        rb.gravityScale = 4; 
        if(novo == Estado.Guerreiro) wallStickTimer = 0; 
    }
    
    void OnDrawGizmos() {
        if (colisor == null) return;
        Gizmos.color = Color.red; Gizmos.DrawWireCube(colisor.bounds.center + Vector3.down * 0.05f, colisor.bounds.size);
        if (pontoAtaque != null) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pontoAtaque.position, raioAtaque);
        }
    }
}