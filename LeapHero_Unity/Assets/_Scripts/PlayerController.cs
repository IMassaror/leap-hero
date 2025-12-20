using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    #region Enums & States
    // --- ESTADOS DO JOGADOR ---
    public enum PlayerState { Warrior, Frog }
    public PlayerState currentState;

    // --- ESTADOS DA LÍNGUA ---
    public enum TongueState { Ready, Shooting, Pulling, Retracting }
    public TongueState currentTongueState = TongueState.Ready;
    #endregion

    #region Inspector Variables

    [Header("Dependencies")]
    public ExcalibroController excalibroController; // Referência ao script da espada
    public LineRenderer lineRenderer; // Linha visual da língua

    [Header("General Settings")]
    public LayerMask groundLayer; // Camada do chão/paredes

    [Header("Warrior Settings")]
    public float warriorSpeed = 4f;
    public float warriorJumpForce = 12f;
    public Color warriorColor = Color.white;
    
    [Header("Warrior - Combat")]
    public KeyCode attackKey = KeyCode.J;
    public Transform attackPoint;       // Ponto de origem do ataque frontal
    public float attackRadius = 0.8f;   // Tamanho da área de dano
    public LayerMask enemyLayer;        // O que o jogador pode bater
    public float airStallDuration = 0.3f; // Tempo que ele para no ar ao bater
    public float attackCooldown = 0.4f;
    
    [Header("Warrior - Ground Pound")] // --- ADICIONADO PARA O ATAQUE DE ESMAGAR ---
    public float groundPoundSpeed = 20f;
    public float groundPoundRadius = 0.8f;
    // Vamos usar o pogoPoint se não tiver um especifico, ou crie um novo transform nos pés
    public Transform pogoPoint; 

    [Header("Frog Settings")]
    public float frogSpeed = 7f;
    public float frogJumpForce = 15f;
    public Color frogColor = Color.green;

    [Header("Frog - Wall Mechanics")]
    public float wallJumpForceX = 12f;
    public float wallJumpForceY = 16f;
    public float wallSlideSpeed = 2f;        // Velocidade deslizando
    public float wallClimbSpeed = 3f;        // Velocidade subindo/descendo (ajuste)
    public float wallStickTime = 1.5f;       // Tempo que aguenta grudado
    public float wallJumpBlockDuration = 0.2f; // Tempo sem controle após walljump
    public float wallDetachDelay = 0.2f;     // Tempo segurando contra a parede pra desgrudar

    [Header("Frog - Tongue Mechanics")]
    public KeyCode tongueKey = KeyCode.L;
    public float tongueRange = 8f;
    public float tongueShootSpeed = 25f;     // Velocidade da ponta indo
    public float tongueRetractSpeed = 35f;   // Velocidade da ponta voltando (errou)
    public float tonguePullSpeed = 18f;      // Velocidade do corpo sendo puxado
    public float tongueCooldown = 0.5f;
    public Vector3 mouthOffset;              // Posição da boca em relação ao centro

    #endregion

    #region Private Variables
    // Componentes Internos
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D boxCollider;
    private Animator anim;

    // Inputs e Controle
    private float moveInputX;
    private float moveInputY;
    private bool jumpInputPressed;
    private bool isFacingRight = true;
    private bool isAttacking = false;

    // Timers
    private float nextAttackTime;
    private float wallStickTimer;
    private float wallJumpBlockTimer;
    private float wallDetachTimer;
    private float nextTongueTime;
    private float tongueStartTime; // Segurança para não travar na língua

    // Controle de Parede
    private int wallDirection = 0; // 1 = Direita, -1 = Esquerda, 0 = Nenhuma
    private bool wasTouchingWall; // Para detectar entrada na parede

    // Controle da Língua
    private Vector2 tongueTargetPos;
    private Vector2 currentTongueTipPos;
    private bool hasHitWall;

    // Flags de Estado
    public bool isGrounded;
    public bool isTouchingWall;
    private bool isDead = false;
    private bool canDoubleJump = false; // Controle da Excalibro
    #endregion

    #region Unity Callbacks
    void Start()
    {
        // Pega as referências automaticamente
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        if (lineRenderer != null) lineRenderer.enabled = false;

        // Inicia como Guerreiro por padrão
        SwitchState(PlayerState.Warrior);
        
        // Pega o script de vida para mudar a cara do sapo depois
        if (GetComponent<PlayerHealth>() == null) Debug.LogError("PlayerHealth not found");
        
        // Configura respawn inicial se tiver LevelManager
        if (LevelManager.instance != null && LevelManager.instance.currentRespawnPoint == Vector3.zero)
            LevelManager.instance.currentRespawnPoint = transform.position;
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();
        HandleStateSwitch();
        HandleActions();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        CheckCollisions();
        HandleGroundLogic();

        // Prioridade para a física da língua
        if (currentTongueState != TongueState.Ready)
        {
            HandleTonguePhysics();
            return;
        }

        if (isAttacking) return; // Se ataca, não move

        // Seleciona velocidade baseada na forma atual
        float currentSpeed = (currentState == PlayerState.Warrior) ? warriorSpeed : frogSpeed;

        // Lógica de movimento
        if (currentState == PlayerState.Frog && isTouchingWall && !isGrounded)
        {
            HandleFrogWallLogic();
        }
        else
        {
            HandleNormalMovement(currentSpeed);
        }

        // Processa pulo (se houve input no Update)
        if (jumpInputPressed)
        {
            jumpInputPressed = false;
            HandleJump();
        }
    }

    void LateUpdate()
    {
        // Desenha a linha da língua DEPOIS de tudo calculado (evita atraso visual)
        DrawTongue();
    }

    // --- CORREÇÃO DE BUG (SOFTLOCK DA LÍNGUA) ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Se bater em parede sólida enquanto é puxado -> Cancela para não travar
        if (currentTongueState == TongueState.Pulling)
        {
            if (((1 << collision.gameObject.layer) & groundLayer) != 0)
            {
                Debug.Log("Collision during tongue pull. Resetting.");
                StopTongue();
            }
        }
    }
    
    // --- COLETA DE ITENS ---
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            if (CoinManager.instance != null) CoinManager.instance.AddCoins(1);
            Destroy(collision.gameObject);
        }
    }
    #endregion

    #region Input & Actions
    void HandleInput()
    {
        // --- NOVO: BACKFLIP (Cancelar Língua ao Pular) ---
        if (currentTongueState == TongueState.Pulling && Input.GetButtonDown("Jump"))
        {
            StopTongue(); // Para a língua imediatamente
            
            // Calcula direção: para trás de onde o sapo está olhando
            float backflipX = isFacingRight ? -6f : 6f; 
            float backflipY = 10f; // Força para cima
            
            rb.linearVelocity = Vector2.zero; // Zera a inércia do puxão
            rb.AddForce(new Vector2(backflipX, backflipY), ForceMode2D.Impulse);
            
            GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Jump);
            
            return; // Impede de tentar pular normal no mesmo frame
        }
        // ----------------------------------------

        // Se estiver atacando, trava movimento horizontal
        if (isAttacking) moveInputX = 0;
        else moveInputX = Input.GetAxisRaw("Horizontal");

        moveInputY = Input.GetAxisRaw("Vertical");

        // Timer do bloqueio de walljump
        if (wallJumpBlockTimer > 0) wallJumpBlockTimer -= Time.deltaTime;

        // Input de Pulo
        if (currentTongueState == TongueState.Ready && Input.GetButtonDown("Jump") && !isAttacking)
        {
            // Pulo normal (chão)
            if (isGrounded)
            {
                jumpInputPressed = true;
            }
            // Pulo na parede (Sapo)
            else if (currentState == PlayerState.Frog && isTouchingWall && !isGrounded)
            {
                jumpInputPressed = true;
            }
            // Pulo duplo (Excalibro)
            else if (currentState == PlayerState.Frog && canDoubleJump && excalibroController != null)
            {
                PerformDoubleJump();
            }
        }
    }

    void HandleActions()
    {
        // Input da Língua (Sapo)
        if (currentState == PlayerState.Frog && currentTongueState == TongueState.Ready && Input.GetKeyDown(tongueKey) && Time.time > nextTongueTime)
        {
            // Corrige a direção antes de atirar
            if (moveInputX > 0 && !isFacingRight) Flip();
            else if (moveInputX < 0 && isFacingRight) Flip();
            
            StartTongue();
        }

        // Input de Ataque (Guerreiro)
        if (currentState == PlayerState.Warrior && Input.GetKeyDown(attackKey) && Time.time > nextAttackTime && !isAttacking)
        {
            // --- GROUND POUND (NOVO) ---
            // Se estiver no ar E segurando pra baixo -> Ground Pound
            if (!isGrounded && moveInputY < -0.1f) 
            {
                StartCoroutine(PerformGroundPound());
            }
            else 
            {
                StartCoroutine(PerformNormalAttack());
            }
        }

        // Virar o personagem (Flip)
        if (currentTongueState == TongueState.Ready && !isTouchingWall && wallJumpBlockTimer <= 0 && !isAttacking)
        {
            if (moveInputX > 0 && !isFacingRight) Flip();
            else if (moveInputX < 0 && isFacingRight) Flip();
        }
    }

    void HandleStateSwitch()
    {
        if (Input.GetKeyDown(KeyCode.C) && !isAttacking)
        {
            SwitchState(currentState == PlayerState.Warrior ? PlayerState.Frog : PlayerState.Warrior);
        }
    }
    #endregion

    #region Movement Logic
    void HandleNormalMovement(float speed)
    {
        rb.gravityScale = 4; // Garante gravidade normal
        wallStickTimer = wallStickTime;
        wallDetachTimer = 0;

        if (wallJumpBlockTimer <= 0)
        {
            // Unity 6 usa linearVelocity (substituindo o antigo velocity)
            rb.linearVelocity = new Vector2(moveInputX * speed, rb.linearVelocity.y);
        }
    }

    void HandleFrogWallLogic()
    {
        // Zera o timer se detectar que parou na parede (Física)
        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f) wallJumpBlockTimer = 0;
        
        if (wallJumpBlockTimer > 0) return;

        // Lógica para desgrudar da parede (segurando para o lado oposto)
        bool tryingToDetach = (moveInputX != 0 && moveInputX != wallDirection);
        if (tryingToDetach) wallDetachTimer += Time.deltaTime;
        else wallDetachTimer = 0;

        if (tryingToDetach && wallDetachTimer > wallDetachDelay)
        {
            // Desgruda
            rb.gravityScale = 4;
            rb.linearVelocity = new Vector2(moveInputX * frogSpeed, rb.linearVelocity.y);
            return;
        }

        // Lógica de deslizar/grudar
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Trava horizontal

        if (wallStickTimer > 0)
        {
            rb.gravityScale = 0; // Remove gravidade para ficar parado
            
            // Permite deslizar devagar se pressionar para baixo
            if (moveInputY < 0) rb.linearVelocity = new Vector2(0, -wallClimbSpeed);
            else rb.linearVelocity = Vector2.zero;

            wallStickTimer -= Time.deltaTime;

            // Garante que o sapo olhe para a parede oposta
            if (wallDirection == 1 && isFacingRight) Flip();
            else if (wallDirection == -1 && !isFacingRight) Flip();

            // Rosto do sapo
            if (moveInputY < 0) GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.WallSlide);
            else GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Idle);
        }
        else
        {
            // Começa a cair deslizando
            rb.gravityScale = 4;
            float slideVelocity = (moveInputY < 0) ? wallClimbSpeed : wallSlideSpeed;
            rb.linearVelocity = new Vector2(0, Mathf.Clamp(rb.linearVelocity.y, -slideVelocity, float.MaxValue));

            if (rb.linearVelocity.y < 0)
            {
                GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.WallSlide);
            }
        }
    }

    void HandleJump()
    {
        if (currentState == PlayerState.Frog && isTouchingWall && !isGrounded)
        {
            PerformWallJump();
        }
        else
        {
            float force = (currentState == PlayerState.Warrior) ? warriorJumpForce : frogJumpForce;
            PerformJump(force);
        }
    }

    void PerformJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reseta velocidade vertical antes de pular
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        if (currentState == PlayerState.Frog)// Se for sapo, faz cara de pulo
        {
            GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Jump);
        }
    }

    void PerformDoubleJump()
    {
        canDoubleJump = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * frogJumpForce, ForceMode2D.Impulse);
        
        if (excalibroController != null) excalibroController.UseJump(); 
        Debug.Log("Double Jump executed!");
    }

    void PerformWallJump()
    {
        // Reseta timers
        wallStickTimer = wallStickTime;
        wallJumpBlockTimer = wallJumpBlockDuration;
        wallDetachTimer = 0;

        // Pula para o lado oposto da parede
        int jumpDirection = -wallDirection;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(jumpDirection * wallJumpForceX, wallJumpForceY), ForceMode2D.Impulse);
        GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Jump);

        // Vira o personagem
        if (jumpDirection == 1 && !isFacingRight) Flip();
        else if (jumpDirection == -1 && isFacingRight) Flip();
    }
    #endregion

    #region Combat Logic (Warrior)
    IEnumerator PerformNormalAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        
        // Trava no ar brevemente
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetTrigger("Attack");

        // Detecta inimigos
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            Destroy(enemy.gameObject); // Substituir por lógica de dano real depois
        }

        yield return new WaitForSeconds(airStallDuration);

        // Restaura física
        rb.gravityScale = originalGravity;
        isAttacking = false;
    }

    // --- GROUND POUND (Novo Pogo) ---
    IEnumerator PerformGroundPound()
    {
        isAttacking = true;
        // Não define nextAttackTime aqui para permitir spammar se errar, ou defina se quiser cooldown

        // 1. Inicia descida
        rb.linearVelocity = new Vector2(0, -groundPoundSpeed); // Desce rápido
        if (anim != null) anim.SetTrigger("AttackDown"); // Use animação de cair atacando

        // 2. Loop enquanto não toca o chão
        bool hitSomething = false;
        while (!isGrounded)
        {
            // Mantém velocidade constante para baixo
            rb.linearVelocity = new Vector2(0, -groundPoundSpeed);

            // Verifica colisão com inimigos ou quebráveis
            // Usa pogoPoint (se tiver) ou transform.position
            Vector2 checkPos = pogoPoint != null ? pogoPoint.position : transform.position;
            Collider2D[] hitObjects = Physics2D.OverlapCircleAll(checkPos, groundPoundRadius, enemyLayer | groundLayer);

            foreach (Collider2D obj in hitObjects)
            {
                // Se for Inimigo
                if (obj.CompareTag("Enemy")) 
                {
                    // obj.GetComponent<EnemyHealth>()?.TakeDamage(2); 
                    Destroy(obj.gameObject); 
                    hitSomething = true;
                }
                
                // Se for Quebrável ou Botão
                if (obj.CompareTag("Breakable"))
                {
                    Destroy(obj.gameObject);
                    hitSomething = true;
                }
                
                if (obj.CompareTag("Button"))
                {
                    // obj.GetComponent<ButtonScript>()?.Activate();
                    hitSomething = true;
                }
            }

            // Se bateu em algo, sai do loop
            if (hitSomething) break;

            yield return null; // Espera próximo frame
        }

        // 3. Impacto no Chão
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
        nextAttackTime = Time.time + 0.2f; // Cooldown curto
    }
    #endregion

    #region Tongue Mechanics (Frog)
    void StartTongue()
    {
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        currentTongueState = TongueState.Shooting;
        
        if (anim != null) anim.SetTrigger("TongueShoot");

        GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Tongue);

        // Para o jogador no ar
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        // Timer de segurança
        tongueStartTime = Time.time;

        // Calcula origem e destino
        Vector3 realOffset = isFacingRight ? mouthOffset : new Vector3(-mouthOffset.x, mouthOffset.y, 0);
        Vector3 startPos = transform.position + realOffset;
        currentTongueTipPos = startPos;

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, tongueRange, groundLayer);
        
        if (hit.collider != null)
        {
            tongueTargetPos = hit.point;
            hasHitWall = true;
        }
        else
        {
            // SE NÃO BATER, AINDA TEM ALVO (MAX RANGE)
            tongueTargetPos = (Vector2)startPos + (direction * tongueRange);
            hasHitWall = false;
        }
    }

    void HandleTonguePhysics()
    {
        // SEGURANÇA: Se passar 1.5s travado, corta
        if (Time.time > tongueStartTime + 1.5f)
        {
            StopTongue();
            return;
        }

        switch (currentTongueState)
        {
            case TongueState.Shooting:
                rb.linearVelocity = Vector2.zero;
                // Move a ponta até o alvo
                currentTongueTipPos = Vector2.MoveTowards(currentTongueTipPos, tongueTargetPos, tongueShootSpeed * Time.deltaTime);
                
                // --- CORREÇÃO: Garante que muda de estado ao chegar ---
                if (Vector2.Distance(currentTongueTipPos, tongueTargetPos) < 0.1f)
                {
                    // Se bateu na parede, puxa. Se não bateu (Miss), recolhe (Retracting).
                    currentTongueState = hasHitWall ? TongueState.Pulling : TongueState.Retracting;
                }
                break;

            case TongueState.Pulling:
                rb.linearVelocity = Vector2.zero;
                // Move o SAPO até a ponta
                transform.position = Vector2.MoveTowards(transform.position, currentTongueTipPos, tonguePullSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, tongueTargetPos) < 0.5f)
                {
                    StopTongue();
                }
                break;

            case TongueState.Retracting:
                if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero;
                
                // Traz a ponta de volta para a boca
                Vector3 realOffset = isFacingRight ? mouthOffset : new Vector3(-mouthOffset.x, mouthOffset.y, 0);
                Vector3 retractTarget = transform.position + realOffset;
                
                currentTongueTipPos = Vector2.MoveTowards(currentTongueTipPos, retractTarget, tongueRetractSpeed * Time.deltaTime);
                
                if (Vector2.Distance(currentTongueTipPos, retractTarget) < 0.1f)
                {
                    StopTongue();
                }
                break;
        }
    }

    void StopTongue()
    {
        currentTongueState = TongueState.Ready;
        rb.gravityScale = 4; // Restaura gravidade
        rb.linearVelocity = Vector2.zero;
        nextTongueTime = Time.time + tongueCooldown;
        GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Idle); 
    }

    void DrawTongue()
    {
        if (lineRenderer == null) return;

        if (currentTongueState != TongueState.Ready)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            Vector3 realOffset = isFacingRight ? mouthOffset : new Vector3(-mouthOffset.x, mouthOffset.y, 0);
            lineRenderer.SetPosition(0, transform.position + realOffset);
            lineRenderer.SetPosition(1, currentTongueTipPos);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
    #endregion

    #region Utils & Core Logic
    void CheckCollisions()
    {
        float margin = 0.05f;
        Bounds b = boxCollider.bounds;

        // Verifica chão
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margin, groundLayer);

        // Verifica paredes
        Vector2 wallSensorSize = new Vector2(b.size.x, b.size.y * 0.9f);
        bool wallRight = Physics2D.BoxCast(b.center, wallSensorSize, 0f, Vector2.right, margin, groundLayer);
        bool wallLeft = Physics2D.BoxCast(b.center, wallSensorSize, 0f, Vector2.left, margin, groundLayer);

        if (wallRight) { isTouchingWall = true; wallDirection = 1; }
        else if (wallLeft) { isTouchingWall = true; wallDirection = -1; }
        else { isTouchingWall = false; wallDirection = 0; }
        
        // CORREÇÃO: Reseta timers ao tocar na parede nova
        if (!wasTouchingWall && isTouchingWall && currentState == PlayerState.Frog) 
        { 
            wallStickTimer = wallStickTime; 
            wallJumpBlockTimer = 0; 
        }
        wasTouchingWall = isTouchingWall;
    }
    
    void HandleGroundLogic()
    {
        if (isGrounded && currentState == PlayerState.Frog && currentTongueState == TongueState.Ready)
        {
            GetComponent<PlayerHealth>()?.SetFrogFace(PlayerHealth.FrogFaceState.Idle);
        }

        if (isGrounded && !canDoubleJump)
        {
            canDoubleJump = true;
            if (excalibroController != null && currentState == PlayerState.Frog)
            {
                excalibroController.Recharge(); 
            }
        }
    }
    
    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void SwitchState(PlayerState newState)
    {
        currentState = newState;
        sr.color = (currentState == PlayerState.Warrior) ? warriorColor : frogColor;

        if (anim != null) anim.SetBool("IsSapo", currentState == PlayerState.Frog);

        if (excalibroController != null)
        {
            if (newState == PlayerState.Frog) excalibroController.Appear();
            else excalibroController.Vanish();
        }

        rb.gravityScale = 4;
        if (newState == PlayerState.Warrior) wallStickTimer = 0;
        PlayerHealth healthScript = GetComponent<PlayerHealth>();
        if (healthScript != null) healthScript.OnStateChanged();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1;
        if (anim != null) anim.SetTrigger("Die");
        if (excalibroController != null) excalibroController.Vanish();
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1f);

        if (LevelManager.instance != null)
        {
            LevelManager.instance.Respawn(this.gameObject);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        isDead = false;
        isAttacking = false;
        rb.gravityScale = 4;
        SwitchState(PlayerState.Warrior);
        if (anim != null) anim.Play("idle_h");
    }

    void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", Mathf.Abs(moveInputX));
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsSapo", currentState == PlayerState.Frog);
        
        bool isSliding = (currentState == PlayerState.Frog && isTouchingWall && !isGrounded && rb.linearVelocity.y < 0);
        anim.SetBool("IsWallSliding", isSliding);
        anim.SetBool("IsGrappling", currentTongueState != TongueState.Ready);
    }

    void OnDrawGizmos()
    {
        if (boxCollider == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCollider.bounds.center + Vector3.down * 0.05f, boxCollider.bounds.size);

        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        if (pogoPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pogoPoint.position, groundPoundRadius);
        }
    }
    #endregion
}