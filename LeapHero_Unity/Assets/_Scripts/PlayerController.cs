using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;

    // NOVO: Estados exclusivos para controlar o fluxo da língua
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
    public float forcaWallJumpX = 10f;
    public float forcaWallJumpY = 14f;
    public float velocidadeDeslizar = 2f; 
    public float tempoGrudadoNaParede = 1.5f; 

    [Header("Sapo - Língua (Grapple)")]
    public float alcanceLingua = 8f;
    public float velocidadeLinguaIda = 20f;   // Velocidade da ponta da língua saindo
    public float velocidadeLinguaVolta = 30f; // Velocidade da língua voltando (se errar)
    public float velocidadePuxadaCorpo = 15f; // Velocidade do sapo indo até a parede
    public KeyCode teclaLingua = KeyCode.L; 
    public LineRenderer lineRenderer; 

    // --- VARIÁVEIS INTERNAS ---
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D colisor;
    
    private float xInput;
    private float yInput;
    private bool jumpInputDown;
    private int pulosExtras;
    private bool viradoDireita = true;

    // Timer da Parede
    private float wallStickTimer; 
    
    // Variáveis da Lógica da Língua
    private Vector2 destinoLingua;      // Onde a língua quer chegar (Parede ou Ar)
    private Vector2 pontaLinguaAtual;   // Onde a ponta da língua está AGORA
    private bool acertouParede;         // Flag para saber se puxamos o corpo ou retraímos a língua

    // Sensores
    public bool isGrounded;
    public bool isTouchingWall;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        colisor = GetComponent<BoxCollider2D>();
        
        if(lineRenderer != null) 
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2; // Garante que tem 2 pontos (Boca e Ponta)
        }

        TrocarEstado(Estado.Guerreiro);
    }

    void Update()
    {
        // 1. Inputs
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");
        if (Input.GetButtonDown("Jump")) jumpInputDown = true;
        if (Input.GetKeyDown(KeyCode.C)) TrocarEstado(estadoAtual == Estado.Guerreiro ? Estado.Sapo : Estado.Guerreiro);

        // 2. DISPARO DA LÍNGUA (Inicia o processo)
        if (estadoAtual == Estado.Sapo && estadoLingua == EstadoLingua.Pronta && Input.GetKeyDown(teclaLingua))
        {
            IniciarLingua();
        }

        // 3. FLIP (Bloqueado se estiver usando a língua)
        if (estadoLingua == EstadoLingua.Pronta && (!isTouchingWall || isGrounded))
        {
             if (xInput > 0 && !viradoDireita) Flip();
             else if (xInput < 0 && viradoDireita) Flip();
        }

        // 4. ATUALIZAR VISUAL DA LÍNGUA
        if (estadoLingua != EstadoLingua.Pronta && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position); // Ponto 0: No Sapo
            lineRenderer.SetPosition(1, pontaLinguaAtual);   // Ponto 1: Na ponta viajando
        }
    }

    void FixedUpdate()
    {
        VerificarColisoes();

        // --- PRIORIDADE MÁXIMA: LÍNGUA ---
        // Se a língua não estiver "Pronta", ela está em uso. O Sapo congela.
        if (estadoLingua != EstadoLingua.Pronta)
        {
            ProcessarFisicaLingua();
            return; // Impede qualquer outro movimento
        }

        // --- MOVIMENTO NORMAL ---
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

    // --- NOVA LÓGICA DA LÍNGUA ---

    void IniciarLingua()
    {
        // 1. Configura direção
        Vector2 direcao = viradoDireita ? Vector2.right : Vector2.left;

        // 2. Trava o Sapo imediatamente
        estadoLingua = EstadoLingua.Atirando;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        pontaLinguaAtual = transform.position; // Língua nasce no corpo

        // 3. Raycast para decidir o destino
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direcao, alcanceLingua, layerSolido);

        if (hit.collider != null)
        {
            destinoLingua = hit.point;
            acertouParede = true;
        }
        else
        {
            // Se não bater, destino é o ar no limite do alcance
            destinoLingua = (Vector2)transform.position + (direcao * alcanceLingua);
            acertouParede = false;
        }

        // 4. Liga visual
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    void ProcessarFisicaLingua()
    {
        // Trava o corpo enquanto a língua opera (Redundância de segurança)
        rb.linearVelocity = Vector2.zero; 

        switch (estadoLingua)
        {
            case EstadoLingua.Atirando:
                // Move a ponta da língua até o destino
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, destinoLingua, velocidadeLinguaIda * Time.deltaTime);

                // Chegou no destino?
                if (Vector2.Distance(pontaLinguaAtual, destinoLingua) < 0.1f)
                {
                    if (acertouParede)
                    {
                        // Se bateu na parede, agora puxa o corpo
                        estadoLingua = EstadoLingua.PuxandoSapo;
                    }
                    else
                    {
                        // Se bateu no ar, retrai a língua de volta
                        estadoLingua = EstadoLingua.Retraindo;
                    }
                }
                break;

            case EstadoLingua.PuxandoSapo:
                // Move o CORPO do sapo até a ponta da língua (que está na parede)
                transform.position = Vector2.MoveTowards(transform.position, pontaLinguaAtual, velocidadePuxadaCorpo * Time.deltaTime);

                // Se chegou na parede
                if (Vector2.Distance(transform.position, pontaLinguaAtual) < 0.5f)
                {
                    FinalizarLingua();
                }
                break;

            case EstadoLingua.Retraindo:
                // A ponta da língua volta para o corpo do sapo (que está parado no ar)
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, transform.position, velocidadeLinguaVolta * Time.deltaTime);

                // Se a língua voltou pra boca
                if (Vector2.Distance(pontaLinguaAtual, transform.position) < 0.1f)
                {
                    FinalizarLingua();
                }
                break;
        }

        // CANCELAMENTO MANUAL (Pulo cancela tudo)
        if (jumpInputDown)
        {
            FinalizarLingua();
            // Dá um pulinho pra não cair seco
            rb.gravityScale = 4;
            rb.AddForce(Vector2.up * puloSapo * 0.5f, ForceMode2D.Impulse);
            jumpInputDown = false;
        }
    }

    void FinalizarLingua()
    {
        estadoLingua = EstadoLingua.Pronta;
        rb.gravityScale = 4; // Devolve a gravidade
        
        if (lineRenderer != null) lineRenderer.enabled = false;
    }


    // --- LÓGICAS ANTIGAS ---

    void MovimentoNormal(float velAtual)
    {
        rb.gravityScale = 4; 
        wallStickTimer = tempoGrudadoNaParede; 
        rb.linearVelocity = new Vector2(xInput * velAtual, rb.linearVelocity.y);
    }

    void MecanicaDeParedeoSapo()
    {
        int direcaoParede = viradoDireita ? 1 : -1;

        if (xInput != 0 && xInput != direcaoParede) // Sair da parede
        {
            rb.gravityScale = 4;
            rb.linearVelocity = new Vector2(xInput * velSapo, rb.linearVelocity.y);
            Flip(); 
            return;
        }

        if (yInput < 0) wallStickTimer = 0; // Deslizar manual

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Cola horizontalmente

        if (wallStickTimer > 0) // Travado
        {
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero; 
            wallStickTimer -= Time.deltaTime;
        }
        else // Deslizando
        {
            rb.gravityScale = 4; 
            float velocidadeY = Mathf.Clamp(rb.linearVelocity.y, -velocidadeDeslizar, float.MaxValue);
            rb.linearVelocity = new Vector2(0, velocidadeY);
        }
    }

    void ProcessarPulo()
    {
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded)
        {
            RealizarWallJump();
        }
        else if (isGrounded)
        {
            ExecutarPulo((estadoAtual == Estado.Guerreiro) ? puloGuerreiro : puloSapo);
        }
        else if (estadoAtual == Estado.Sapo && pulosExtras > 0)
        {
            ExecutarPulo(puloSapo);
            pulosExtras--;
        }
    }

    void ExecutarPulo(float forca)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        rb.AddForce(Vector2.up * forca, ForceMode2D.Impulse);
    }

    void RealizarWallJump()
    {
        wallStickTimer = tempoGrudadoNaParede; 
        int dir = viradoDireita ? -1 : 1;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dir * forcaWallJumpX, forcaWallJumpY), ForceMode2D.Impulse);
        Flip();
    }

    void VerificarColisoes()
    {
        float margem = 0.05f; 
        Bounds b = colisor.bounds;
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margem, layerSolido);
        Vector2 dir = viradoDireita ? Vector2.right : Vector2.left;
        isTouchingWall = Physics2D.BoxCast(b.center, b.size, 0f, dir, margem, layerSolido);
    }

    void Flip()
    {
        viradoDireita = !viradoDireita;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    void TrocarEstado(Estado novo)
    {
        estadoAtual = novo;
        sr.color = (estadoAtual == Estado.Guerreiro) ? corGuerreiro : corSapo;
        pulosExtras = (estadoAtual == Estado.Sapo) ? totalPulosSapo : 0;
        rb.gravityScale = 4;
        if(novo == Estado.Guerreiro) wallStickTimer = 0; 
    }
    
    void OnDrawGizmos()
    {
        if (colisor == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(colisor.bounds.center + Vector3.down * 0.05f, colisor.bounds.size);
        Gizmos.color = Color.yellow;
        Vector3 dir = viradoDireita ? Vector3.right : Vector3.left;
        Gizmos.DrawWireCube(colisor.bounds.center + dir * 0.05f, colisor.bounds.size);
    }
}