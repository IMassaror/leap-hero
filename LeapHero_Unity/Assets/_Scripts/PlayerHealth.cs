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
    // TORNAR PÚBLICO PARA O CONTROLLER LER
    public FrogFaceState currentFrogFace = FrogFaceState.Idle;

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

    // --- NOVA FUNÇÃO PARA O CONTROLLER LER ---
    public bool IsFaceLocked()
    {
        // Se a cara for de MORTO ou STUNNED, o controller não pode mexer!
        return currentFrogFace == FrogFaceState.Dead || currentFrogFace == FrogFaceState.Stunned;
    }

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
                    SetFrogFace(FrogFaceState.Dead); // Força cara de morto
                    HandleDeath();
                }
            }
            else if (!isInvincible)
            {
                savedFrogHealth = 1;
                SetFrogFace(FrogFaceState.Stunned); // Força cara de stun
                
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

        // PRIORIDADE ABSOLUTA: Se já está morto, não muda
        if (currentFrogFace == FrogFaceState.Dead) return;

        // PRIORIDADE DO STUN: Só sai do Stun se for pra morrer ou se o tempo acabar
        if (currentFrogFace == FrogFaceState.Stunned && newState != FrogFaceState.Dead && newState != FrogFaceState.Stunned) 
            return;

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
        
        // Garante a cara visualmente
        currentFrogFace = FrogFaceState.Stunned;
        UpdateFrogUI();

        while (timer < frogRegenTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
        savedFrogHealth = 2;
        
        // Libera a cara
        currentFrogFace = FrogFaceState.Idle; 
        UpdateFrogUI();
    }

    void HandleDeath()
    {
        if(hasDied) return;
        hasDied = true;
        if(playerController != null) playerController.Die();
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

    // --- RESET PARA O LEVEL MANAGER ---
    public void ResetStatus()
    {
        hasDied = false;
        isInvincible = false;
        StopAllCoroutines(); 
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true; 
            spriteRenderer.color = Color.white;
        }

        savedWarriorHealth = maxWarriorHealth;
        savedFrogHealth = 2; 

        // O SEGREDO: Limpa a cara "suja" de morto
        currentFrogFace = FrogFaceState.Idle;
        UpdateStateFromController();
    }
}