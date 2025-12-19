using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    #region Enums
    // ADICIONADO: WallSlide
    public enum FrogFaceState { Idle, Jump, Tongue, Stunned, Dead, WallSlide }
    #endregion

    #region Settings
    [Header("Health Settings")]
    public int maxWarriorHealth = 3;
    public float restartDelay = 2.0f;
    public float frogRegenTime = 3.0f;

    [Header("UI References")]
    public GameObject knightHUDObject;
    public GameObject frogHUDObject;
    public Image[] knightHearts;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;
    
    [Header("Frog 'Doom' Face")]
    public Image frogFaceImage;
    public Sprite faceIdle;
    public Sprite faceJump;
    public Sprite faceTongue;
    public Sprite faceStunned;
    public Sprite faceDead;
    public Sprite faceWallSlide; // --- NOVO SPRITE ---

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer; 
    #endregion

    #region Internal State
    private PlayerController playerController;
    private bool isInvincible = false;
    private bool hasDied = false;
    private FrogFaceState currentFrogFace = FrogFaceState.Idle;

    private int savedWarriorHealth;
    private int savedFrogHealth; 
    private int currentHealth; 

    private Coroutine frogRegenCoroutine;
    #endregion

    #region Unity Callbacks
    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        savedWarriorHealth = maxWarriorHealth;
        savedFrogHealth = 2; 

        UpdateStateFromController();
    }
    #endregion

    #region Public Methods

    // --- MUDANÇA AQUI: Agora aceita dois booleans ---
    public void TakeDamage(int damage, bool killWarrior = false, bool killFrog = false)
    {
        if (hasDied) return;
        
        // Lógica do Sapo
        if (playerController.currentState == PlayerController.PlayerState.Frog)
        {
            // Mata se for KillFrog marcado OU se já estiver sem escudo (vida 1)
            if (killFrog || savedFrogHealth <= 1)
            {
                if(!hasDied) 
                {
                    savedFrogHealth = 0;
                    SetFrogFace(FrogFaceState.Dead);
                    HandleDeath();
                }
            }
            else if (!isInvincible)
            {
                // Dano Não Letal
                savedFrogHealth = 1;
                SetFrogFace(FrogFaceState.Stunned);
                
                if (frogRegenCoroutine != null) StopCoroutine(frogRegenCoroutine);
                frogRegenCoroutine = StartCoroutine(FrogStunRoutine());
            }
            return;
        }

        // Lógica do Guerreiro
        if (isInvincible) return;

        if (killWarrior) savedWarriorHealth = 0; // Mata só se killWarrior estiver true
        else savedWarriorHealth -= damage;

        currentHealth = savedWarriorHealth;
        UpdateKnightUI();

        Debug.Log($"Warrior hit! HP: {savedWarriorHealth}");

        if (savedWarriorHealth <= 0) HandleDeath();
        else StartCoroutine(InvincibilityRoutine());
    }

    public void OnStateChanged()
    {
        UpdateStateFromController();
    }

    public void SetFrogFace(FrogFaceState newState)
    {
        if (playerController.currentState != PlayerController.PlayerState.Frog || hasDied) return;

        // --- SISTEMA DE PRIORIDADE ATUALIZADO ---
        if (currentFrogFace == FrogFaceState.Dead) return;

        // Stun ganha de tudo
        if (currentFrogFace == FrogFaceState.Stunned && newState != FrogFaceState.Dead && newState != FrogFaceState.Stunned) 
            return;

        // Língua ganha de movimento
        if (currentFrogFace == FrogFaceState.Tongue && newState != FrogFaceState.Dead && newState != FrogFaceState.Stunned && newState != FrogFaceState.Idle)
        {
             // Impede trocar lingua por pulo/slide, mas deixa voltar pra idle
        }

        currentFrogFace = newState;
        UpdateFrogUI();
    }
    #endregion

    #region Internal Logic

    void UpdateStateFromController()
    {
        if (playerController.currentState == PlayerController.PlayerState.Warrior)
        {
            currentHealth = savedWarriorHealth;
            knightHUDObject.SetActive(true);
            frogHUDObject.SetActive(false);
            UpdateKnightUI();
        }
        else
        {
            currentHealth = savedFrogHealth;
            knightHUDObject.SetActive(false);
            frogHUDObject.SetActive(true);
            
            if (savedFrogHealth <= 1) SetFrogFace(FrogFaceState.Stunned);
            else SetFrogFace(FrogFaceState.Idle);
        }
    }

    void UpdateKnightUI()
    {
        if (knightHearts == null) return;
        for (int i = 0; i < knightHearts.Length; i++)
        {
            if (i < currentHealth) knightHearts[i].sprite = fullHeartSprite;
            else knightHearts[i].sprite = emptyHeartSprite != null ? emptyHeartSprite : fullHeartSprite;
            
            knightHearts[i].enabled = (emptyHeartSprite != null || i < currentHealth);
        }
    }

    void UpdateFrogUI()
    {
        if (frogFaceImage == null) return;
        switch (currentFrogFace)
        {
            case FrogFaceState.Idle: frogFaceImage.sprite = faceIdle; break;
            case FrogFaceState.Jump: frogFaceImage.sprite = faceJump; break;
            case FrogFaceState.Tongue: frogFaceImage.sprite = faceTongue; break;
            case FrogFaceState.Stunned: frogFaceImage.sprite = faceStunned; break;
            case FrogFaceState.Dead: frogFaceImage.sprite = faceDead; break;
            case FrogFaceState.WallSlide: frogFaceImage.sprite = faceWallSlide; break; // Adicionado
        }
    }

    IEnumerator FrogStunRoutine()
    {
        isInvincible = true;
        float timer = 0;
        while (timer < frogRegenTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
        savedFrogHealth = 2;
        
        currentFrogFace = FrogFaceState.Idle; 
        SetFrogFace(FrogFaceState.Idle); 
    }

    void HandleDeath()
    {
        if(hasDied) return;
        hasDied = true;
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
        for (float i = 0; i < 1.0f; i += 0.1f)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        isInvincible = false;
    }
    #endregion
}