using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;

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

    // --- VARIÁVEIS INTERNAS ---
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D colisor;
    
    private float xInput;
    private float yInput; // NOVO: Para detectar seta para baixo
    private bool jumpInputDown;
    private int pulosExtras;
    private bool viradoDireita = true;

    // Timer da Parede
    private float wallStickTimer; 
    
    // Sensores
    public bool isGrounded;
    public bool isTouchingWall;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        colisor = GetComponent<BoxCollider2D>();
        TrocarEstado(Estado.Guerreiro);
    }

    void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical"); // Lendo input vertical
        
        if (Input.GetButtonDown("Jump")) jumpInputDown = true;

        if (Input.GetKeyDown(KeyCode.C))
        {
            TrocarEstado(estadoAtual == Estado.Guerreiro ? Estado.Sapo : Estado.Guerreiro);
        }

        // FLIP: Só vira se NÃO estiver grudado na parede OU se estiver no chão
        if (!isTouchingWall || isGrounded)
        {
             if (xInput > 0 && !viradoDireita) Flip();
             else if (xInput < 0 && viradoDireita) Flip();
        }
    }

    void FixedUpdate()
    {
        VerificarColisoes();

        float velAtual = (estadoAtual == Estado.Guerreiro) ? velGuerreiro : velSapo;
        
        // --- LÓGICA DE MOVIMENTO ---
        
        // Se for Sapo, estiver na Parede e no Ar -> Mecânica de Parede
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded)
        {
            MecanicaDeParedeoSapo();
        }
        else
        {
            // Movimento Normal (Chão ou Ar livre)
            rb.gravityScale = 4; 
            wallStickTimer = tempoGrudadoNaParede; // Reseta o timer sempre que sai da parede
            
            rb.linearVelocity = new Vector2(xInput * velAtual, rb.linearVelocity.y);
            
            // Permite virar no ar se não estiver travado na parede
            if (xInput > 0 && !viradoDireita) Flip();
            else if (xInput < 0 && viradoDireita) Flip();
        }

        if (jumpInputDown)
        {
            jumpInputDown = false;
            ProcessarPulo();
        }

        if (isGrounded) pulosExtras = totalPulosSapo;
    }

    void MecanicaDeParedeoSapo()
    {
        int direcaoParede = viradoDireita ? 1 : -1;

        // 1. SAIR DA PAREDE: Se apertar para o lado OPOSTO -> Desgruda e cai
        if (xInput != 0 && xInput != direcaoParede)
        {
            rb.gravityScale = 4;
            rb.linearVelocity = new Vector2(xInput * velSapo, rb.linearVelocity.y);
            Flip(); 
            return;
        }

        // 2. FORÇAR DESLIZE: Se apertar para BAIXO -> Zera timer para deslizar
        if (yInput < 0)
        {
            wallStickTimer = 0;
        }

        // --- AUTO-STICK ---
        // Trava movimento horizontal para grudar
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (wallStickTimer > 0)
        {
            // FASE 1: TRAVADO (Grudado)
            // Zera a gravidade e a velocidade Y para ficar parado
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero; 
            wallStickTimer -= Time.deltaTime;
        }
        else
        {
            // FASE 2: DESLIZANDO (Slide)
            // A gravidade volta
            rb.gravityScale = 4; 
            
            // Limitamos a velocidade máxima de queda (Slide suave)
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
        // Ao pular da parede, resetamos o timer para permitir grudar na próxima parede (ou na mesma se voltar)
        wallStickTimer = tempoGrudadoNaParede; 
        
        int dir = viradoDireita ? -1 : 1;
        
        rb.linearVelocity = Vector2.zero;
        // Adiciona força diagonal
        rb.AddForce(new Vector2(dir * forcaWallJumpX, forcaWallJumpY), ForceMode2D.Impulse);
        
        Flip();
    }

    void VerificarColisoes()
    {
        float margem = 0.05f; 
        Bounds b = colisor.bounds;

        // Chão
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margem, layerSolido);

        // Parede
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