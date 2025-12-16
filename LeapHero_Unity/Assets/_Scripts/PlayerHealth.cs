using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    #region Settings
    [Header("Health Settings")]
    public int maxWarriorHealth = 3;
    public int currentHealth;
    public float restartDelay = 2.0f; // Tempo para ver a animação de morte antes de reiniciar
    
    [Header("Invincibility")]
    public float invincibilityDuration = 1.0f;
    public SpriteRenderer spriteRenderer; 
    #endregion

    #region Internal State
    private PlayerController playerController;
    private bool isInvincible = false;
    private bool hasDied = false; // Evita chamadas duplicadas de morte
    #endregion

    #region Unity Callbacks
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentHealth = maxWarriorHealth;
    }
    #endregion

    #region Public Methods
    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0 || hasDied) return;

        // REGRA: Sapo morre com 1 hit (Glass Cannon)
        // AQUI MUDOU: Conversando com as variáveis novas em Inglês do PlayerController
        if (playerController.currentState == PlayerController.PlayerState.Frog)
        {
            Debug.Log("Frog hit! Instant Death.");
            HandleDeath();
            return;
        }

        // REGRA: Guerreiro tanka o dano
        currentHealth -= damage;
        Debug.Log($"Warrior hit! Health remaining: {currentHealth}");

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
    void HandleDeath()
    {
        if(hasDied) return;
        hasDied = true;

        // 1. Avisa o PlayerController para travar movimento e tocar animação
        if(playerController != null)
        {
            // AQUI MUDOU: Chama o método Die() em inglês
            playerController.Die();
        }

        Debug.Log("GAME OVER - Waiting for animation to restart...");
        
        // 2. Espera um pouco antes de resetar a cena
        StartCoroutine(RestartLevelRoutine());
    }

    IEnumerator RestartLevelRoutine()
    {
        // Espera o tempo da animação (configurável no Inspector)
        yield return new WaitForSeconds(restartDelay);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        
        // Pisca o personagem
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