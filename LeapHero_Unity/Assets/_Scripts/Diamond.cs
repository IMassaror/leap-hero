using UnityEngine;

public class Diamond : MonoBehaviour
{
    [Header("Identidade")]
    public string diamondID; // Lembre de dar um nome ÚNICO para cada diamante (Ex: Diamond_01)
    
    [Header("Visual")]
    public Color collectedColor = Color.gray; 
    public float followSpeed = 5f;

    private bool isFollowing = false;
    private bool isCollected = false; // Já pegou nesta sessão?
    private bool isPermamentlyCollected = false; // Já está salvo no disco?

    private Transform playerTransform;
    private PlayerController playerController;
    private SpriteRenderer sr;
    private Collider2D col;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Verifica se já pegamos esse diamante antes (Salvo no PC)
        if (PlayerPrefs.GetInt(diamondID, 0) == 1)
        {
            SetAsGhost();
        }
    }

    void Update()
    {
        // Se já pegou, não faz nada
        if (isCollected || isPermamentlyCollected) return;

        // Lógica de Seguir
        if (isFollowing && playerTransform != null)
        {
            // O diamante flutua suavemente atrás do player
            Vector3 targetPos = playerTransform.position + new Vector3(0, 1.5f, 0);
            transform.position = Vector2.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

            // --- A CORREÇÃO ESTÁ AQUI ---
            // Só coleta se:
            // 1. O jogador está no chão (isGrounded)
            // 2. E NÃO está tocando na parede (!isTouchingWall)
            if (playerController.isGrounded && !playerController.isTouchingWall)
            {
                ConfirmCollection();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Se o player tocou e o diamante ainda não foi pego permanentemente
        if (collision.CompareTag("Player") && !isFollowing && !isPermamentlyCollected)
        {
            isFollowing = true;
            playerTransform = collision.transform;
            playerController = collision.GetComponent<PlayerController>();
        }
    }

    void ConfirmCollection()
    {
        isFollowing = false;
        isCollected = true;
        
        // Salva no sistema
        PlayerPrefs.SetInt(diamondID, 1);
        PlayerPrefs.Save();

        // Avisa o contador (se tiver)
        if (CoinManager.instance != null) 
        {
            // CoinManager.instance.AddDiamond(); // Descomente quando criar a UI de Diamantes
        }

        Debug.Log("Diamante " + diamondID + " coletado e SALVO!");
        
        // Some com o objeto
        Destroy(gameObject);
    }

    void SetAsGhost()
    {
        // Estado "Já coletado anteriormente"
        isPermamentlyCollected = true;
        sr.color = collectedColor; // Fica cinza
        // col.enabled = false; // Se quiser que ele seja intangível, descomente
    }
}