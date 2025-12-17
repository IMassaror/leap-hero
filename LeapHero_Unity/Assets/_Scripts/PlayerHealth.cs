using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // NECESSÁRIO PARA MEXER NO CANVAS
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    #region Settings
    [Header("Health Settings")]
    public int maxWarriorHealth = 3;
    public int currentHealth;
    public float restartDelay = 2.0f;
    
    [Header("UI Settings")] // --- NOVIDADE ---
    public Image[] hearts;       // Arraste as 3 imagens aqui
    public Sprite fullHeart;     // Arraste o sprite cheio
    public Sprite emptyHeart;    // Arraste o sprite vazio (ou transparente)

    [Header("Invincibility")]
    public float invincibilityDuration = 1.0f;
    public SpriteRenderer spriteRenderer; 
    #endregion

    #region Internal State
    private PlayerController playerController;
    private bool isInvincible = false;
    private bool hasDied = false;
    #endregion

    #region Unity Callbacks
    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentHealth = maxWarriorHealth;
        UpdateHealthUI(); // Atualiza a tela logo no começo
    }
    #endregion

    #region Public Methods
    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0 || hasDied) return;

        // REGRA: Sapo morre direto
        if (playerController.currentState == PlayerController.PlayerState.Frog)
        {
            HandleDeath();
            return;
        }

        // REGRA: Guerreiro perde vida
        currentHealth -= damage;
        UpdateHealthUI(); // --- NOVIDADE: Atualiza a tela quando toma dano

        if (currentHealth <= 0)
        {
            HandleDeath();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }
    #endregion

    #region Internal Logic
    // --- MÉTODO NOVO: Controla os desenhos ---
    void UpdateHealthUI()
    {
        // Se esqueceu de arrastar as imagens, evita erro
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < currentHealth)
            {
                hearts[i].sprite = fullHeart; // Vida cheia
                hearts[i].enabled = true;     // Garante que aparece
            }
            else
            {
                // Se tiver sprite de coração vazio, usa ele. Se não, some com a imagem.
                if (emptyHeart != null)
                {
                    hearts[i].sprite = emptyHeart;
                    hearts[i].enabled = true;
                }
                else
                {
                    hearts[i].enabled = false;
                }
            }
        }
    }

    void HandleDeath()
    {
        if(hasDied) return;
        hasDied = true;
        
        // Zera visualmente a vida
        currentHealth = 0;
        UpdateHealthUI();

        if(playerController != null) playerController.Die();

        StartCoroutine(RestartLevelRoutine());
    }

    IEnumerator RestartLevelRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        for (float i = 0; i < invincibilityDuration; i += 0.1f)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        isInvincible = false;
    }
    #endregion
}