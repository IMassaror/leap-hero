using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // --- ENUMS ---
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;
    
    public enum EstadoLingua { Pronta, Atirando, PuxandoSapo, Retraindo }
    public EstadoLingua estadoLingua = EstadoLingua.Pronta;

    [Header("Conexões")]
    public ExcalibroController excalibroScript; 

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

    [Header("Guerreiro - Excalibur (Pogo)")]
    public float forcaPogo = 14f; 
    public Transform pontoPogo;   
    public float raioPogo = 0.6f;

    [Header("Sapo - Movimento")]
    public float velSapo = 7f;
    public float puloSapo = 15f;
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

    // Squash and Stretch Controller
    public SquashStretchController stretch;

    // INTERNAS
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D colisor;
    private Animator anim; 
    private float xInput;
    private float yInput;
    private bool jumpInputDown;
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
    
    // VARIÁVEIS DE SEGURANÇA (NOVO)
    private float tempoInicioPuxada; // Para evitar loops infinitos
    public bool prevGrounded;
    public bool isGrounded;
    public bool isTouchingWall;
    private bool isDead = false;

    // CONTROLE DE PULO DUPLO
    private bool canDoubleJump = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        colisor = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>(); 
        stretch = GetComponentInChildren<SquashStretchController>();

        if(lineRenderer != null) lineRenderer.enabled = false;
        
        TrocarEstado(Estado.Guerreiro);
    }

    void Update()
    {
        if (isDead) return;

        // 1. INPUTS
        if (estaAtacando) xInput = 0;
        else xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (wallJumpBlockTimer > 0) wallJumpBlockTimer -= Time.deltaTime;

        // 2. SISTEMA DE PULO
        if (estadoLingua == EstadoLingua.Pronta && Input.GetButtonDown("Jump") && !estaAtacando) 
        {
            if (isGrounded)
            {
                jumpInputDown = true;
            }
            else if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded)
            {
                jumpInputDown = true;
            }
            else if (estadoAtual == Estado.Sapo && canDoubleJump && excalibroScript != null)
            {
                ExecutarPuloDuplo();
            }
        }

        // 3. TROCA DE ESTADO
        if (Input.GetKeyDown(KeyCode.C) && !estaAtacando) 
            TrocarEstado(estadoAtual == Estado.Guerreiro ? Estado.Sapo : Estado.Guerreiro);

        // 4. LÍNGUA
        if (estadoAtual == Estado.Sapo && estadoLingua == EstadoLingua.Pronta && Input.GetKeyDown(teclaLingua) && Time.time > tempoParaProximaLingua) 
        {
            if (xInput > 0 && !viradoDireita) Flip();
            else if (xInput < 0 && viradoDireita) Flip();
            IniciarLingua();
        }

        // 5. COMBATE GUERREIRO
        if (estadoAtual == Estado.Guerreiro && Input.GetKeyDown(teclaAtaque) && Time.time > tempoProximoAtaque && !estaAtacando)
        {
            if (!isGrounded && yInput < -0.1f) StartCoroutine(RealizarPogo());
            else StartCoroutine(RealizarAtaque());
        }

        // 6. FLIP
        if (estadoLingua == EstadoLingua.Pronta && !isTouchingWall && wallJumpBlockTimer <= 0 && !estaAtacando)
        {
             if (xInput > 0 && !viradoDireita) Flip();
             else if (xInput < 0 && viradoDireita) Flip();
        }

        AtualizarAnimator();
    }

    // --- CORREÇÃO DE BUG: COLISÃO ENQUANTO PUXA ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Se bater em qualquer coisa sólida ENQUANTO está sendo puxado
        if (estadoLingua == EstadoLingua.PuxandoSapo)
        {
            // Verifica se o objeto colidido é da camada Sólida
            if (((1 << collision.gameObject.layer) & layerSolido) != 0)
            {
                Debug.Log("Colisão detectada durante a puxada! Cancelando língua.");
                FinalizarLingua();
            }
        }
    }

    public void Morrer()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1; 
        if (anim != null) anim.SetTrigger("Die");
        if (excalibroScript != null) excalibroScript.Sumir();
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
        anim.SetBool("IsGrappling", estadoLingua != EstadoLingua.Pronta);
    }

    IEnumerator RealizarPogo()
    {
        estaAtacando = true; 
        tempoProximoAtaque = Time.time + 0.2f; 
        if (anim != null) anim.SetTrigger("AttackDown"); 
        yield return new WaitForSeconds(0.05f);

        Collider2D[] acertos = Physics2D.OverlapCircleAll(pontoPogo.position, raioPogo, layerInimigos);
        bool acertouAlgo = false;
        foreach (Collider2D alvo in acertos)
        {
            Destroy(alvo.gameObject); 
            acertouAlgo = true;
        }

        if (acertouAlgo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * forcaPogo, ForceMode2D.Impulse);
        }
        yield return new WaitForSeconds(0.1f);
        estaAtacando = false;
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
        foreach (Collider2D inimigo in inimigos) Destroy(inimigo.gameObject);
        yield return new WaitForSeconds(duracaoTravamentoAereo);
        rb.gravityScale = gravidadeOriginal; 
        estaAtacando = false;
    }

    void LateUpdate()
    {
        if (lineRenderer != null && estadoLingua != EstadoLingua.Pronta)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2; 
            Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
            lineRenderer.SetPosition(0, transform.position + offsetReal);    
            lineRenderer.SetPosition(1, pontaLinguaAtual); 
        }
        else if(lineRenderer != null) lineRenderer.enabled = false;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        VerificarColisoes();
        
        if (isGrounded)
        {
            if(!canDoubleJump)
            {
                canDoubleJump = true; 
                if(excalibroScript != null && estadoAtual == Estado.Sapo) 
                    excalibroScript.Recarregar();
            }
        }

        if (estadoLingua != EstadoLingua.Pronta) { ProcessarFisicaLingua(); return; }
        if (estaAtacando) return; 

        float velAtual = (estadoAtual == Estado.Guerreiro) ? velGuerreiro : velSapo;
        
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) MecanicaDeParedeoSapo();
        else MovimentoNormal(velAtual);

        if (jumpInputDown) { jumpInputDown = false; isGrounded = false; ProcessarPulo(); }
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
        if(anim != null) anim.SetTrigger("TongueShoot");
        
        rb.gravityScale = 0; 
        rb.linearVelocity = Vector2.zero;
        
        // Timer de segurança (NOVO)
        tempoInicioPuxada = Time.time; 
        
        posicaoInicialTiro = transform.position;
        Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
        Vector3 origemTiro = transform.position + offsetReal; pontaLinguaAtual = origemTiro; 
        
        RaycastHit2D hit = Physics2D.Raycast(origemTiro, direcao, alcanceLingua, layerSolido);
        if (hit.collider != null) { destinoLingua = hit.point; acertouParede = true; }
        else { destinoLingua = (Vector2)origemTiro + (direcao * alcanceLingua); acertouParede = false; }
    }

    void ProcessarFisicaLingua() {
        
        // TRAVA DE SEGURANÇA 1: TEMPO LIMITE
        // Se ficar mais de 1.5s na língua (loop ou bug), corta
        if (Time.time > tempoInicioPuxada + 1.5f) {
            Debug.Log("Tempo limite da língua excedido. Cortando.");
            FinalizarLingua();
            return;
        }

        switch (estadoLingua) {
            case EstadoLingua.Atirando:
                rb.linearVelocity = Vector2.zero; 
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, destinoLingua, velocidadeLinguaIda * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, destinoLingua) < 0.1f) {
                    if (acertouParede) estadoLingua = EstadoLingua.PuxandoSapo; else estadoLingua = EstadoLingua.Retraindo;
                } break;

            case EstadoLingua.PuxandoSapo:
                // Move o player
                rb.linearVelocity = Vector2.zero; 
                transform.position = Vector2.MoveTowards(transform.position, pontaLinguaAtual, velocidadePuxadaCorpo * Time.deltaTime);
                
                // Se chegar no destino (a parede alvo)
                float distDestino = Vector2.Distance(transform.position, destinoLingua);
                if (distDestino < 0.5f) FinalizarLingua(); 
                break;

            case EstadoLingua.Retraindo:
                if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero;
                Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
                Vector3 alvoRetracao = transform.position + offsetReal;
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, alvoRetracao, velocidadeLinguaVolta * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, alvoRetracao) < 0.1f) FinalizarLingua(); break;
        }
    }

    void FinalizarLingua() { 
        estadoLingua = EstadoLingua.Pronta; 
        rb.gravityScale = 4; // Restaura a gravidade imediatamente
        rb.linearVelocity = Vector2.zero; // Zera inércia louca
        tempoParaProximaLingua = Time.time + delayEntreLinguadas;
    }
    
    void ProcessarPulo() {
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) 
        {
            RealizarWallJump();
        }
        else 
        {
            float forca = (estadoAtual == Estado.Guerreiro) ? puloGuerreiro : puloSapo;
            ExecutarPulo(forca);
        }
    }

    void ExecutarPuloDuplo()
    {
        canDoubleJump = false; 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * puloSapo, ForceMode2D.Impulse);
        if(excalibroScript != null) excalibroScript.UsarPulo();
        Debug.Log("Excalibro Jump!");
    }

    void ExecutarPulo(float forca) { 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        rb.AddForce(Vector2.up * forca, ForceMode2D.Impulse); 
        anim.SetBool("IsJumping", true);
        stretch.DoJump();
        }

    void VerificarColisoes() {
        float margem = 0.05f; Bounds b = colisor.bounds;
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margem, layerSolido);
        Vector2 tamanhoSensorParede = new Vector2(b.size.x, b.size.y * 0.9f);
        bool paredeDir = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.right, margem, layerSolido);
        bool paredeEsq = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.left, margem, layerSolido);
        if (paredeDir) { isTouchingWall = true; ladoParede = 1; } else if (paredeEsq) { isTouchingWall = true; ladoParede = -1; } else { isTouchingWall = false; ladoParede = 0; }
        if (isGrounded) anim.SetBool("IsJumping", !isGrounded);
        if (!prevGrounded && isGrounded){
            anim.SetTrigger("Land");
            stretch.DoLand();
        }
        prevGrounded = isGrounded;
    }

    void Flip() { viradoDireita = !viradoDireita; Vector3 escala = transform.localScale; escala.x *= -1; transform.localScale = escala; }

    void TrocarEstado(Estado novo) { 
        estadoAtual = novo; 
        sr.color = (estadoAtual == Estado.Guerreiro) ? corGuerreiro : corSapo; 
        if(anim != null) anim.SetBool("IsSapo", estadoAtual == Estado.Sapo);
        
        if(excalibroScript != null)
        {
            if(novo == Estado.Sapo) excalibroScript.Aparecer();
            else excalibroScript.Sumir();
        }

        rb.gravityScale = 4; 
        if(novo == Estado.Guerreiro) wallStickTimer = 0; 
    }
    
    void OnDrawGizmos() {
        if (colisor == null) return;
        Gizmos.color = Color.red; Gizmos.DrawWireCube(colisor.bounds.center + Vector3.down * 0.05f, colisor.bounds.size);
        if (pontoAtaque != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(pontoAtaque.position, raioAtaque); }
        if (pontoPogo != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(pontoPogo.position, raioPogo); }
    }
}