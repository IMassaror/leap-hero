using UnityEngine;

public class ExcalibroController : MonoBehaviour
{
    [Header("Configurações de Conexão")]
    public PlayerController playerScript; // Arraste o Player aqui (que tem o script)

    [Header("Velocidades")]
    public float velocidadeSeguir = 5f;
    public float velocidadePosicionar = 15f; 

    [Header("Offsets (Distância do Player)")]
    // X deve ser positivo, o script inverte sozinho dependendo do lado
    public Vector3 offsetCostas = new Vector3(0.8f, 0.8f, 0); // Atrás do ombro
    public Vector3 offsetParede = new Vector3(0.5f, 0.5f, 0); // Na frente (para não entrar na parede)
    public Vector3 offsetPe = new Vector3(0, -1.0f, 0);       // Embaixo do pé

    [Header("Visual")]
    public Color corNormal = Color.white;
    public Color corEsgotada = Color.gray; // Cor quando gasta o pulo
    public float amplitudeFlr = 0.1f; 
    public float frequenciaFlr = 3f;

    private SpriteRenderer sr;
    private bool ativo = false;
    private bool estaEsgotada = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        
        // Tenta achar o script se não foi arrastado
        if (playerScript == null)
            playerScript = FindObjectOfType<PlayerController>();

        Sumir(); // Começa invisível
    }

    void Update()
    {
        if (!ativo || playerScript == null) return;

        // 1. LÓGICA DE COR (Feedback de Uso)
        // Lerp suave para mudar a cor
        sr.color = Color.Lerp(sr.color, estaEsgotada ? corEsgotada : corNormal, Time.deltaTime * 10f);

        // 2. LÓGICA DE POSICIONAMENTO
        Vector3 alvo = Vector3.zero;
        float velocidadeAtual = velocidadeSeguir;

        // Pega a direção que o player está olhando (Escala X positiva ou negativa)
        float direcaoPlayer = Mathf.Sign(playerScript.transform.localScale.x);

        // Lógica de Estados da Espada
        if (playerScript.isTouchingWall && !playerScript.isGrounded)
        {
            // SITUAÇÃO: NA PAREDE
            // Fica na FRENTE do player para não entrar no muro
            // (Direcao * Offset positivo coloca na frente)
            Vector3 posFrente = new Vector3(direcaoPlayer * offsetParede.x, offsetParede.y, 0);
            alvo = playerScript.transform.position + posFrente;
            velocidadeAtual = velocidadeSeguir;
        }
        else if (!playerScript.isGrounded && !estaEsgotada)
        {
            // SITUAÇÃO: NO AR E CARREGADA
            // Vai para debaixo do pé preparar o pulo
            alvo = playerScript.transform.position + offsetPe;
            velocidadeAtual = velocidadePosicionar;
        }
        else
        {
            // SITUAÇÃO: NO CHÃO OU JÁ GASTOU O PULO
            // Fica nas COSTAS (Atrás do ombro)
            // (Inverte a direção para ficar nas costas)
            float flutuacaoY = Mathf.Sin(Time.time * frequenciaFlr) * amplitudeFlr;
            Vector3 posCostas = new Vector3(-direcaoPlayer * offsetCostas.x, offsetCostas.y + flutuacaoY, 0);
            
            alvo = playerScript.transform.position + posCostas;
            velocidadeAtual = velocidadeSeguir;
        }

        // Aplica o movimento
        transform.position = Vector3.Lerp(transform.position, alvo, velocidadeAtual * Time.deltaTime);
    }

    public void Aparecer()
    {
        ativo = true;
        sr.enabled = true;
        estaEsgotada = false; // Nasce carregada
        
        // Teleporta para perto para não vir voando de longe
        if(playerScript != null) 
            transform.position = playerScript.transform.position;
    }

    public void Sumir()
    {
        ativo = false;
        sr.enabled = false;
    }

    public void UsarPulo()
    {
        estaEsgotada = true;
        FeedbackImpulso();
    }

    public void Recarregar()
    {
        estaEsgotada = false;
    }

    void FeedbackImpulso()
    {
        // Empurra a espada um pouco pra baixo pra dar impacto visual do "chute"
        transform.position += Vector3.down * 0.5f;
    }
}