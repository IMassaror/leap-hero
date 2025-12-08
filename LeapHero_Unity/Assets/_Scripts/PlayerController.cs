using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;
    
    public enum EstadoLingua { Pronta, Atirando, PuxandoSapo, Retraindo }
    public EstadoLingua estadoLingua = EstadoLingua.Pronta;

    [Header("Configurações Gerais")]
    public LayerMask layerSolido; 

    [Header("Guerreiro")]
    public float velGuerreiro = 4f;
    public float puloGuerreiro = 12f;
    public Color corGuerreiro = Color.white;

    [Header("Sapo")]
    public float velSapo = 7f;
    public float puloSapo = 15f;
    public int totalPulosSapo = 1;
    public Color corSapo = Color.green;

    [Header("Sapo - Wall Mechanics")]
    public float forcaWallJumpX = 12f; 
    public float forcaWallJumpY = 16f;
    public float velocidadeDeslizar = 2f; // Velocidade caindo AUTOMATICAMENTE (após o tempo acabar)
    public float velocidadeAjusteVertical = 3f; // NOVO: Velocidade lenta para ajustar posição
    public float tempoGrudadoNaParede = 1.5f; 
    public float tempoBloqueioWallJump = 0.2f; 

    [Header("Sapo - Língua (Grapple)")]
    public float alcanceLingua = 8f;
    public float velocidadeLinguaIda = 25f;   
    public float velocidadeLinguaVolta = 35f; 
    public float velocidadePuxadaCorpo = 18f; 
    public float delayEntreLinguadas = 0.5f; 
    public KeyCode teclaLingua = KeyCode.L; 
    public LineRenderer lineRenderer;
    
    [Header("Ajustes Visuais")]
    public Vector3 offsetBoca; 

    // VARIÁVEIS INTERNAS
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D colisor;
    
    private float xInput;
    private float yInput;
    private bool jumpInputDown;
    private int pulosExtras;
    private bool viradoDireita = true;
    
    private float wallStickTimer; 
    private float wallJumpBlockTimer; 
    private int ladoParede = 0;
    
    private Vector2 destinoLingua;      
    private Vector2 pontaLinguaAtual;   
    private bool acertouParede;         
    private float tempoParaProximaLingua;
    private Vector2 posicaoInicialTiro; 

    public bool isGrounded;
    public bool isTouchingWall;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        colisor = GetComponent<BoxCollider2D>();
        
        if(lineRenderer != null) lineRenderer.enabled = false;
        TrocarEstado(Estado.Guerreiro);
    }

    void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (wallJumpBlockTimer > 0)
        {
            wallJumpBlockTimer -= Time.deltaTime;
        }

        if (estadoLingua == EstadoLingua.Pronta && Input.GetButtonDown("Jump")) 
        {
            jumpInputDown = true;
        }

        if (Input.GetKeyDown(KeyCode.C)) TrocarEstado(estadoAtual == Estado.Guerreiro ? Estado.Sapo : Estado.Guerreiro);

        if (estadoAtual == Estado.Sapo && 
            estadoLingua == EstadoLingua.Pronta && 
            Input.GetKeyDown(teclaLingua) && 
            Time.time > tempoParaProximaLingua) 
        {
            if (xInput > 0 && !viradoDireita) Flip();
            else if (xInput < 0 && viradoDireita) Flip();

            IniciarLingua();
        }

        if (estadoLingua == EstadoLingua.Pronta && !isTouchingWall && wallJumpBlockTimer <= 0)
        {
             if (xInput > 0 && !viradoDireita) Flip();
             else if (xInput < 0 && viradoDireita) Flip();
        }
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
            else
            {
                lineRenderer.enabled = false;
            }
        }
    }

    void FixedUpdate()
    {
        VerificarColisoes();

        if (estadoLingua != EstadoLingua.Pronta)
        {
            ProcessarFisicaLingua();
            return; 
        }

        float velAtual = (estadoAtual == Estado.Guerreiro) ? velGuerreiro : velSapo;
        
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded)
        {
            MecanicaDeParedeoSapo();
        }
        else
        {
            MovimentoNormal(velAtual);
        }

        if (jumpInputDown)
        {
            jumpInputDown = false;
            ProcessarPulo();
        }

        if (isGrounded) pulosExtras = totalPulosSapo;
    }

    void MovimentoNormal(float velAtual)
    {
        rb.gravityScale = 4; 
        wallStickTimer = tempoGrudadoNaParede; 

        if (wallJumpBlockTimer <= 0)
        {
            rb.linearVelocity = new Vector2(xInput * velAtual, rb.linearVelocity.y);
            
            if (xInput > 0 && !viradoDireita) Flip();
            else if (xInput < 0 && viradoDireita) Flip();
        }
    }

    void MecanicaDeParedeoSapo()
    {
        if (wallJumpBlockTimer > 0) return;

        if (xInput != 0 && xInput != ladoParede) 
        {
            rb.gravityScale = 4;
            rb.linearVelocity = new Vector2(xInput * velSapo, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 

        if (wallStickTimer > 0) 
        {
            // FASE 1: TRAVADO (COM AJUSTE DE POSIÇÃO)
            rb.gravityScale = 0;
            
            // Se apertar para baixo, desce devagar para ajustar
            if (yInput < 0)
            {
                rb.linearVelocity = new Vector2(0, -velocidadeAjusteVertical);
            }
            else
            {
                // Se soltar, trava instantaneamente
                rb.linearVelocity = Vector2.zero; 
            }

            wallStickTimer -= Time.deltaTime;
            
            if (ladoParede == 1 && viradoDireita) Flip();
            else if (ladoParede == -1 && !viradoDireita) Flip();
        }
        else 
        {
            // FASE 2: DESLIZANDO (Acabou o tempo)
            rb.gravityScale = 4; 
            
            // Se segurar pra baixo aqui, usa a velocidade rápida ou mantém a do slide?
            // Vamos manter a velocidadeAjusteVertical se o jogador estiver segurando, pra ter consistência
            float velQueda = (yInput < 0) ? velocidadeAjusteVertical : velocidadeDeslizar;
            
            float velocidadeY = Mathf.Clamp(rb.linearVelocity.y, -velQueda, float.MaxValue);
            rb.linearVelocity = new Vector2(0, velocidadeY);
        }
    }

    void RealizarWallJump()
    {
        wallStickTimer = tempoGrudadoNaParede; 
        wallJumpBlockTimer = tempoBloqueioWallJump;

        int dirPulo = -ladoParede; 
        
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(new Vector2(dirPulo * forcaWallJumpX, forcaWallJumpY), ForceMode2D.Impulse);
        
        if (dirPulo == 1 && !viradoDireita) Flip();
        else if (dirPulo == -1 && viradoDireita) Flip();
    }

    // --- FUNÇÕES DE LÍNGUA ---
    
    void IniciarLingua()
    {
        Vector2 direcao = viradoDireita ? Vector2.right : Vector2.left;
        estadoLingua = EstadoLingua.Atirando;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        posicaoInicialTiro = transform.position;

        Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
        Vector3 origemTiro = transform.position + offsetReal;
        pontaLinguaAtual = origemTiro; 

        RaycastHit2D hit = Physics2D.Raycast(origemTiro, direcao, alcanceLingua, layerSolido);

        if (hit.collider != null) { destinoLingua = hit.point; acertouParede = true; }
        else { destinoLingua = (Vector2)origemTiro + (direcao * alcanceLingua); acertouParede = false; }
    }

    void ProcessarFisicaLingua()
    {
        switch (estadoLingua)
        {
            case EstadoLingua.Atirando:
                rb.linearVelocity = Vector2.zero; 
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, destinoLingua, velocidadeLinguaIda * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, destinoLingua) < 0.1f)
                {
                    if (acertouParede) estadoLingua = EstadoLingua.PuxandoSapo;
                    else estadoLingua = EstadoLingua.Retraindo;
                }
                break;
            case EstadoLingua.PuxandoSapo:
                float distDaOrigem = Vector2.Distance(transform.position, posicaoInicialTiro);
                float distParaDestino = Vector2.Distance(transform.position, destinoLingua);
                if (distDaOrigem > 0.5f) {
                    Collider2D colisorNoCaminho = Physics2D.OverlapCircle(transform.position, 0.5f, layerSolido);
                    if (colisorNoCaminho != null && distParaDestino > 1.0f) {
                       estadoLingua = EstadoLingua.Retraindo; rb.gravityScale = 4; rb.linearVelocity = Vector2.zero; break;
                    }
                }
                rb.linearVelocity = Vector2.zero; 
                transform.position = Vector2.MoveTowards(transform.position, pontaLinguaAtual, velocidadePuxadaCorpo * Time.deltaTime);
                if (distParaDestino < 0.5f) FinalizarLingua();
                break;
            case EstadoLingua.Retraindo:
                if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero;
                Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
                Vector3 alvoRetracao = transform.position + offsetReal;
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, alvoRetracao, velocidadeLinguaVolta * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, alvoRetracao) < 0.1f) FinalizarLingua();
                break;
        }
    }

    void FinalizarLingua() { estadoLingua = EstadoLingua.Pronta; rb.gravityScale = 4; tempoParaProximaLingua = Time.time + delayEntreLinguadas; }
    
    void ProcessarPulo()
    {
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) RealizarWallJump();
        else if (isGrounded) ExecutarPulo((estadoAtual == Estado.Guerreiro) ? puloGuerreiro : puloSapo);
        else if (estadoAtual == Estado.Sapo && pulosExtras > 0) { ExecutarPulo(puloSapo); pulosExtras--; }
    }

    void ExecutarPulo(float forca) { rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); rb.AddForce(Vector2.up * forca, ForceMode2D.Impulse); }

    void VerificarColisoes()
    {
        float margem = 0.05f; Bounds b = colisor.bounds;
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margem, layerSolido);
        Vector2 tamanhoSensorParede = new Vector2(b.size.x, b.size.y * 0.9f);
        bool paredeDir = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.right, margem, layerSolido);
        bool paredeEsq = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.left, margem, layerSolido);
        if (paredeDir) { isTouchingWall = true; ladoParede = 1; }
        else if (paredeEsq) { isTouchingWall = true; ladoParede = -1; }
        else { isTouchingWall = false; ladoParede = 0; }
    }

    void Flip() { viradoDireita = !viradoDireita; Vector3 escala = transform.localScale; escala.x *= -1; transform.localScale = escala; }

    void TrocarEstado(Estado novo) { estadoAtual = novo; sr.color = (estadoAtual == Estado.Guerreiro) ? corGuerreiro : corSapo; pulosExtras = (estadoAtual == Estado.Sapo) ? totalPulosSapo : 0; rb.gravityScale = 4; if(novo == Estado.Guerreiro) wallStickTimer = 0; }
    
    void OnDrawGizmos()
    {
        if (colisor == null) return;
        Gizmos.color = Color.red; Gizmos.DrawWireCube(colisor.bounds.center + Vector3.down * 0.05f, colisor.bounds.size);
        Gizmos.color = Color.yellow; Vector2 tamanhoSensorParede = new Vector2(colisor.bounds.size.x, colisor.bounds.size.y * 0.9f);
        Gizmos.DrawWireCube(colisor.bounds.center + Vector3.right * 0.05f, tamanhoSensorParede);
        Gizmos.DrawWireCube(colisor.bounds.center + Vector3.left * 0.05f, tamanhoSensorParede);
    }
}