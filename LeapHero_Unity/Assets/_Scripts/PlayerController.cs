using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Criamos os "Estados" possíveis do personagem
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;

    [Header("Configuração Guerreiro")]
    public float velocidadeGuerreiro = 4f;
    public float puloGuerreiro = 12f;
    public Color corGuerreiro = Color.white; // Branco (Armadura)

    [Header("Configuração Sapo")]
    public float velocidadeSapo = 7f;
    public float puloSapo = 16f;
    public Color corSapo = Color.green;      // Verde (Sapo)

    [Header("Verificação de Chão")]
    public Transform peDoPersonagem;
    public LayerMask oQueEChao;
    public bool estaNoChao;

    // Variáveis internas
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float inputHorizontal;
    private float raioDoChao = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // Para mudar a cor
        
        // Começa como Guerreiro
        TrocarEstado(Estado.Guerreiro);
    }

    void Update()
    {
        // 1. INPUT DE MOVIMENTO
        inputHorizontal = Input.GetAxisRaw("Horizontal");

        // 2. DETECTAR PULO (Usa a força do estado atual)
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            float forcaPuloAtual = (estadoAtual == Estado.Guerreiro) ? puloGuerreiro : puloSapo;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPuloAtual);
        }

        // 3. TRANSFORMAÇÃO (Tecla C)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (estadoAtual == Estado.Guerreiro)
                TrocarEstado(Estado.Sapo);
            else
                TrocarEstado(Estado.Guerreiro);
        }
    }

    void FixedUpdate()
    {
        // Define a velocidade baseada no estado atual
        float velocidadeAtual = (estadoAtual == Estado.Guerreiro) ? velocidadeGuerreiro : velocidadeSapo;
        
        rb.linearVelocity = new Vector2(inputHorizontal * velocidadeAtual, rb.linearVelocity.y);

        // Verifica chão
        estaNoChao = Physics2D.OverlapCircle(peDoPersonagem.position, raioDoChao, oQueEChao);
    }

    // Função dedicada para organizar a troca (State Pattern básico)
    void TrocarEstado(Estado novoEstado)
    {
        estadoAtual = novoEstado;

        if (estadoAtual == Estado.Guerreiro)
        {
            spriteRenderer.color = corGuerreiro;
            // Aqui vamos ativar a espada e desativar a lingua no futuro
            Debug.Log("Transformou em GUERREIRO!");
        }
        else if (estadoAtual == Estado.Sapo)
        {
            spriteRenderer.color = corSapo;
            // Aqui vamos ativar a lingua e o pulo duplo no futuro
            Debug.Log("Transformou em SAPO!");
        }
    }
}