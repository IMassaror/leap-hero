using UnityEngine;

public class ExcalibroController : MonoBehaviour
{
    #region Dependencies
    [Header("Dependencies")]
    public PlayerController playerController; // Referência ao script do Player
    private SpriteRenderer sr;
    #endregion

    #region Settings
    [Header("Movement Smoothing")]
    // Tempo que leva para alcançar o alvo (menor = mais rápido/rígido)
    public float followSmoothTime = 0.15f;      
    public float positioningSmoothTime = 0.05f; 

    [Header("Position Offsets")]
    // X deve ser positivo, o script inverte sozinho dependendo do lado
    public Vector3 offsetBack = new Vector3(0.8f, 0.8f, 0); // Nas costas
    public Vector3 offsetWall = new Vector3(0.5f, 0.5f, 0); // Na parede
    public Vector3 offsetFoot = new Vector3(0, -1.0f, 0);   // No pé (pulo duplo)

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color exhaustedColor = Color.gray; // Cor quando gasta o pulo
    public float floatAmplitude = 0.1f;       // Altura da flutuação
    public float floatFrequency = 3f;         // Velocidade da flutuação
    #endregion

    #region Internal State
    private bool isActive = false;
    private bool isExhausted = false;
    private Vector3 currentVelocity = Vector3.zero; // Variável auxiliar para o SmoothDamp
    #endregion

    #region Unity Callbacks
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        
        // Tenta encontrar o player automaticamente se esqueceu de arrastar
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        Vanish(); // Começa invisível
    }

    // LateUpdate é CRUCIAL para evitar tremor (roda depois que o player se moveu)
    void LateUpdate()
    {
        if (!isActive || playerController == null) return;

        HandleVisuals();
        HandleMovement();
    }
    #endregion

    #region Core Logic
    void HandleVisuals()
    {
        // Lerp suave para mudar a cor se gastou o pulo
        sr.color = Color.Lerp(sr.color, isExhausted ? exhaustedColor : normalColor, Time.deltaTime * 10f);
        
        // Garante que a espada olhe para o mesmo lado do player
        float playerDirection = Mathf.Sign(playerController.transform.localScale.x);
        transform.localScale = new Vector3(playerDirection, 1, 1);
    }

    void HandleMovement()
    {
        Vector3 targetPosition = Vector3.zero;
        float currentSmoothTime = followSmoothTime;
        float playerDirection = Mathf.Sign(playerController.transform.localScale.x);

        // --- Lógica de Estados da Espada ---

        // 1. Estado: Sapo na Parede
        if (playerController.isTouchingWall && !playerController.isGrounded)
        {
            // Fica na frente do player (para não entrar no muro)
            Vector3 frontPos = new Vector3(playerDirection * offsetWall.x, offsetWall.y, 0);
            targetPosition = playerController.transform.position + frontPos;
            currentSmoothTime = followSmoothTime;
        }
        // 2. Estado: No Ar e Pronta (Preparando Pulo)
        else if (!playerController.isGrounded && !isExhausted)
        {
            // Vai rápido para debaixo do pé
            targetPosition = playerController.transform.position + offsetFoot;
            currentSmoothTime = positioningSmoothTime; 
        }
        // 3. Estado: No Chão ou Pulo Gasto (Idle nas Costas)
        else
        {
            // Calcula flutuação suave
            float floatY = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            // Inverte X (-playerDirection) para ficar nas costas
            Vector3 backPos = new Vector3(-playerDirection * offsetBack.x, offsetBack.y + floatY, 0);
            
            targetPosition = playerController.transform.position + backPos;
            currentSmoothTime = followSmoothTime;
        }

        // Aplica o movimento "Manteiga" (SmoothDamp)
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, currentSmoothTime);
    }

    void ApplyImpulseFeedback()
    {
        // Empurrazinho visual para baixo ao pular
        transform.position += Vector3.down * 0.5f;
    }
    #endregion

    #region Public Methods
    // Métodos chamados pelo PlayerController

    public void Appear()
    {
        isActive = true;
        sr.enabled = true;
        isExhausted = false; // Reseta estado
        
        // Teleporta para perto para não vir voando do além
        if(playerController != null) 
        {
            transform.position = playerController.transform.position;
            currentVelocity = Vector3.zero; // Zera inércia
        }
    }

    public void Vanish()
    {
        isActive = false;
        sr.enabled = false;
    }

    public void UseJump()
    {
        isExhausted = true;
        ApplyImpulseFeedback();
    }

    public void Recharge()
    {
        isExhausted = false;
    }
    #endregion
}