using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    #region Enums
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
    public Sprite faceWallSlide; 

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

    public void TakeDamage(int damage, bool killWarrior = false, bool killFrog = false)
    {
        if (hasDied) return;
        
        // Lógica do Sapo
        if (playerController.currentState == PlayerController.PlayerState.Frog)
        {
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
                savedFrogHealth = 1;
                SetFrogFace(FrogFaceState.Stunned);
                
                if (frogRegenCoroutine != null) StopCoroutine(frogRegenCoroutine);
                frogRegenCoroutine = StartCoroutine(FrogStunRoutine());
            }
            return;
        }

        // Lógica do Guerreiro
        if (isInvincible) return;

        if (killWarrior) savedWarriorHealth = 0; 
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

        if (currentFrogFace == FrogFaceState.Dead) return;
        if (currentFrogFace == FrogFaceState.Stunned && newState != FrogFaceState.Dead && newState != FrogFaceState.Stunned) return;
        if (currentFrogFace == FrogFaceState.Tongue && newState != FrogFaceState.Dead && newState != FrogFaceState.Stunned && newState != FrogFaceState.Idle) {}

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
            case FrogFaceState.WallSlide: frogFaceImage.sprite = faceWallSlide; break; 
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
        
        // Avisa o PlayerController que morreu.
        // O PlayerController vai chamar a animação de morte e depois chamar o LevelManager para o respawn.
        if(playerController != null) playerController.Die();

        // --- CORREÇÃO IMPORTANTE ---
        // Removi o RestartLevelRoutine(). 
        // Não queremos recarregar a cena, queremos usar o Checkpoint do LevelManager.
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

    // =========================================================================
    // FUNÇÃO CORRIGIDA PARA O RESPAWN (Sem erros de variável)
    // =========================================================================
    public void ResetStatus()
    {
        // 1. Reseta lógica
        hasDied = false;
        isInvincible = false;

        // 2. Para de piscar imediatamente
        StopAllCoroutines(); 
        
        // 3. Garante que o Sprite fique visível
        // Isso corrige o bug de nascer invisível se morreu enquanto piscava
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true; 
            spriteRenderer.color = Color.white;
        }

        // 4. Enche a vida de novo
        savedWarriorHealth = maxWarriorHealth; // Aqui estava o erro (era maxHealth)
        savedFrogHealth = 2; // Sapo volta inteiro

        // 5. Atualiza a UI visualmente
        UpdateStateFromController();
        
        Debug.Log("Status do Jogador Resetado!");
    }
}