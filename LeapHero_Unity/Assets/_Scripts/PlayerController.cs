using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    /* =================================================================================
     * PLAYER CONTROLLER - VERSÃO FINAL (STATE UPDATE FIX)
     * =================================================================================
     * CORREÇÃO DO TRAVAMENTO NO ATAQUE:
     * Agora, ao forçar uma animação (Ataque/Giro), atualizamos a variável 'currentAnimState'.
     * Isso garante que, ao terminar, o jogo saiba voltar para Idle corretamente.
     * =================================================================================
     */

    #region Enums & States
    public enum PlayerState { Warrior, Frog }
    public PlayerState currentState;
    public enum TongueState { Ready, Shooting, Pulling, Retracting }
    public TongueState currentTongueState = TongueState.Ready;
    public enum AnimState { Idle, Walk, Jump, Fall, Land, WallSlide, WallJump, Attack, Dash, TongueEnd}
    private AnimState currentAnimState = AnimState.Idle;
    #endregion

    #region Animation Hashes
    private static readonly int WarriorIdle   = Animator.StringToHash("idle_h");
    private static readonly int WarriorWalk   = Animator.StringToHash("walk_h");
    private static readonly int WarriorJump   = Animator.StringToHash("jump_h");
    private static readonly int WarriorFall   = Animator.StringToHash("fall_h");
    private static readonly int WarriorLand   = Animator.StringToHash("land_h");
    private static readonly int WarriorAttack = Animator.StringToHash("attack_h");

    private static readonly int FrogIdle      = Animator.StringToHash("idle_f");
    private static readonly int FrogWalk      = Animator.StringToHash("walk_f");
    private static readonly int FrogJump      = Animator.StringToHash("jump_f");
    private static readonly int FrogFall      = Animator.StringToHash("fall_f");
    private static readonly int FrogLand      = Animator.StringToHash("land_f");
    private static readonly int FrogAttack    = Animator.StringToHash("tongueStart_f");
    private static readonly int FrogWallSlide = Animator.StringToHash("wallslide_f");
    private static readonly int FrogWallJump  = Animator.StringToHash("walljump_f");
    private static readonly int FrogDash    = Animator.StringToHash("dash_f");
    private static readonly int FrogTongueEnd    = Animator.StringToHash("tongueEnd_f");

    private int currentAnimHash = 0;
    public float animationLockTime = 0f; 
    #endregion

    #region Inspector Variables
    [Header("Dependencies")]
    public ExcalibroController excalibroController;
    public LineRenderer lineRenderer;
    public GameObject pSprite;

    [Header("General Settings")]
    public LayerMask groundLayer;

    [Header("Warrior Settings")]
    public float warriorSpeed = 4f;
    public float warriorJumpForce = 12f;
    public Color warriorColor = Color.white;
    public KeyCode attackKey = KeyCode.J;
    public Transform attackPoint;
    public float attackRadius = 0.8f;
    public LayerMask enemyLayer;
    public float attackCooldown = 0.4f;
    public float attackDuration = 0.4f; 
    public float airStallDuration = 0.2f;

    [Header("Warrior - Pogo")]
    public float pogoForce = 14f;
    public Transform pogoPoint;
    public float pogoRadius = 0.6f;

    [Header("Frog Settings")]
    public float frogSpeed = 7f;
    public float frogJumpForce = 15f;
    public Color frogColor = Color.green;

    [Header("Frog - Wall Mechanics")]
    public float wallJumpForceX = 12f;
    public float wallJumpForceY = 16f;
    public float wallSlideSpeed = 2f;
    public float wallClimbSpeed = 3f;
    public float wallStickTime = 1.5f;
    public float wallJumpBlockDuration = 0.2f; 
    public float wallDetachDelay = 0.2f;
    public float wallJumpAnimDuration = 1.0f; 

    [Header("Frog - Tongue")]
    public KeyCode tongueKey = KeyCode.L;
    public float tongueRange = 8f;
    public float tongueShootSpeed = 25f;
    public float tongueRetractSpeed = 35f;
    public float tonguePullSpeed = 18f;
    public float tongueCooldown = 0.5f;
    public Vector3 mouthOffset;

    [SerializeField] private PlayerParticles particlePrefab;
    [SerializeField] private Transform particleSpawnPoint;

    #endregion

    #region Private Variables
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private SquashStretchController stretch;
    
    private float moveInputX;
    private float moveInputY;
    private bool jumpInputPressed;
    private bool isFacingRight = true;
    public bool isAttacking = false;
    private float nextAttackTime;
    
    private float wallStickTimer;
    private float wallJumpBlockTimer;
    private float wallDetachTimer;
    private float nextTongueTime;
    private float tongueStartTime;
    private int wallDirection = 0;
    private Vector2 tongueTargetPos;
    private Vector2 currentTongueTipPos;
    private bool hasHitWall;
    public bool isGrounded;
    public bool prevGrounded;
    public bool isTouchingWall;
    public bool isLanding = false;
    private bool isDead = false;
    private bool canDoubleJump = false;
    #endregion

    #region Unity Callbacks
    void Start()
    {
        stretch = GetComponentInChildren<SquashStretchController>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = pSprite.GetComponent<Animator>();
        if (lineRenderer != null) lineRenderer.enabled = false;
        SwitchState(PlayerState.Warrior);
    }

    void Update()
    {
        if (isDead) return;
        HandleInput();
        HandleStateSwitch();
        HandleActions();
        HandleAnimationStateMachine(); 
    }

    void FixedUpdate()
    {
        if (isDead) return;
        
        CheckCollisions(); 
        HandleGroundLogic();

        if (currentTongueState != TongueState.Ready) { HandleTonguePhysics(); return; }
        if (isAttacking) return;

        float currentSpeed = (currentState == PlayerState.Warrior) ? warriorSpeed : frogSpeed;
        if (currentState == PlayerState.Frog && isTouchingWall && !isGrounded) HandleFrogWallLogic();
        else HandleNormalMovement(currentSpeed);

        if (jumpInputPressed) { jumpInputPressed = false; HandleJump(); }
    }

    void LateUpdate() { DrawTongue(); }
    void OnCollisionEnter2D(Collision2D collision) {
        if (currentTongueState == TongueState.Pulling && ((1 << collision.gameObject.layer) & groundLayer) != 0) StopTongue();
    }
    #endregion

    void SpawnParticle(string pAnim)
    {
        PlayerParticles p = Instantiate(
            particlePrefab,
            particleSpawnPoint.position,
            Quaternion.identity
        );

        p.Play(pAnim, isFacingRight);
    }

    #region Movement
    void HandleNormalMovement(float speed)
    {
        rb.gravityScale = 4;
        wallStickTimer = wallStickTime;
        wallDetachTimer = 0;
        if (wallJumpBlockTimer <= 0) rb.linearVelocity = new Vector2(moveInputX * speed, rb.linearVelocity.y);
    }

    void HandleFrogWallLogic()
    {
        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f) wallJumpBlockTimer = 0;

        if (wallJumpBlockTimer > 0) return;

        bool tryingToDetach = (moveInputX != 0 && moveInputX != wallDirection);
        if (tryingToDetach) wallDetachTimer += Time.deltaTime; else wallDetachTimer = 0;

        if (tryingToDetach && wallDetachTimer > wallDetachDelay) {
            rb.gravityScale = 4;
            rb.linearVelocity = new Vector2(moveInputX * frogSpeed, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (wallStickTimer > 0) {
            rb.gravityScale = 0;
            if (moveInputY < 0) rb.linearVelocity = new Vector2(0, -wallClimbSpeed); else rb.linearVelocity = Vector2.zero;
            wallStickTimer -= Time.deltaTime;
            if (wallDirection == 1 && isFacingRight) Flip(); else if (wallDirection == -1 && !isFacingRight) Flip();
        } else {
            rb.gravityScale = 4;
            float slideVelocity = (moveInputY < 0) ? wallClimbSpeed : wallSlideSpeed;
            rb.linearVelocity = new Vector2(0, Mathf.Clamp(rb.linearVelocity.y, -slideVelocity, float.MaxValue));
        }
    }

    void HandleJump()
    {
        if (currentState == PlayerState.Frog && isTouchingWall && !isGrounded) PerformWallJump();
        else PerformJump((currentState == PlayerState.Warrior) ? warriorJumpForce : frogJumpForce);
    }

    void PerformJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        stretch.DoJump();
        SpawnParticle("jump_dust");
    }

    void PerformDoubleJump()
    {
        canDoubleJump = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * frogJumpForce, ForceMode2D.Impulse);
        if (excalibroController != null) excalibroController.UseJump(); 
        
        isLanding = false; 
        PlayAnimation(AnimState.WallJump); 
        currentAnimState = AnimState.WallJump; // <--- CORREÇÃO AQUI
        LockAnimation(wallJumpAnimDuration);
        stretch.DoDoubleJump(); 
        SpawnParticle("jump_dust");
    }

    void PerformWallJump()
    {
        wallStickTimer = wallStickTime;
        wallJumpBlockTimer = wallJumpBlockDuration; 
        wallDetachTimer = 0; 

        int jumpDirection = -wallDirection;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(jumpDirection * wallJumpForceX, wallJumpForceY), ForceMode2D.Impulse);
        if (jumpDirection == 1 && !isFacingRight) Flip(); else if (jumpDirection == -1 && isFacingRight) Flip();

        isLanding = false; 
        PlayAnimation(AnimState.WallJump);
        currentAnimState = AnimState.WallJump; // <--- CORREÇÃO AQUI
        LockAnimation(wallJumpAnimDuration);
        stretch.DoWallJump();
        SpawnParticle("walljump_dust");

    }
    #endregion

    #region Combat
    IEnumerator PerformNormalAttack() {
        isAttacking = true; nextAttackTime = Time.time + attackCooldown;
        float originalGravity = rb.gravityScale; rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;
        
        PlayAnimation(AnimState.Attack); 
        currentAnimState = AnimState.Attack; // <--- CORREÇÃO CRÍTICA AQUI
        stretch.DoAttack();
        
        LockAnimation(attackDuration);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies) Destroy(enemy.gameObject);
        
        yield return new WaitForSeconds(airStallDuration);
        
        rb.gravityScale = originalGravity;
        if (attackDuration > airStallDuration) yield return new WaitForSeconds(attackDuration - airStallDuration);
        
        isAttacking = false;
    }

    IEnumerator PerformPogoAttack() {
        isAttacking = true; nextAttackTime = Time.time + 0.2f;
        if (anim != null) anim.SetTrigger("AttackDown");
        yield return new WaitForSeconds(0.05f);
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(pogoPoint.position, pogoRadius, enemyLayer);
        bool hitSomething = false;
        foreach (Collider2D obj in hitObjects) { Destroy(obj.gameObject); hitSomething = true; }
        if (hitSomething) { rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); rb.AddForce(Vector2.up * pogoForce, ForceMode2D.Impulse); }
        yield return new WaitForSeconds(0.1f); isAttacking = false;
    }
    #endregion

    #region Tongue & Utils
    void StartTongue() {
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left; currentTongueState = TongueState.Shooting;

        LockAnimation(1f); 
        PlayAnimation(AnimState.Attack);
        currentAnimState = AnimState.Attack;
        isAttacking = true;
        stretch.DoTongue();

        rb.gravityScale = 0; rb.linearVelocity = Vector2.zero; tongueStartTime = Time.time;
        Vector3 realOffset = isFacingRight ? mouthOffset : new Vector3(-mouthOffset.x, mouthOffset.y, 0);
        Vector3 startPos = transform.position + realOffset; currentTongueTipPos = startPos;
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, tongueRange, groundLayer);
        if (hit.collider != null) { tongueTargetPos = hit.point; hasHitWall = true; } else { tongueTargetPos = (Vector2)startPos + (direction * tongueRange); hasHitWall = false; }
    }
    void HandleTonguePhysics() {
        if (Time.time > tongueStartTime + 1.5f) { StopTongue(); return; }
        switch (currentTongueState) {
            case TongueState.Shooting:
                rb.linearVelocity = Vector2.zero; currentTongueTipPos = Vector2.MoveTowards(currentTongueTipPos, tongueTargetPos, tongueShootSpeed * Time.deltaTime);
                if (Vector2.Distance(currentTongueTipPos, tongueTargetPos) < 0.1f) currentTongueState = hasHitWall ? TongueState.Pulling : TongueState.Retracting; break;
            case TongueState.Pulling:
                rb.linearVelocity = Vector2.zero; transform.position = Vector2.MoveTowards(transform.position, currentTongueTipPos, tonguePullSpeed * Time.deltaTime);

                LockAnimation(0.1f); 
                PlayAnimation(AnimState.Dash);
                stretch.DoDash();

                if (Vector2.Distance(transform.position, tongueTargetPos) < 0.5f) StopTongue(); break;
            case TongueState.Retracting:
                if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero;
                Vector3 realOffset = isFacingRight ? mouthOffset : new Vector3(-mouthOffset.x, mouthOffset.y, 0);
                Vector3 retractTarget = transform.position + realOffset;
                currentTongueTipPos = Vector2.MoveTowards(currentTongueTipPos, retractTarget, tongueRetractSpeed * Time.deltaTime);
                if (Vector2.Distance(currentTongueTipPos, retractTarget) < 0.1f) StopTongue(); break;
        }
    }
    void StopTongue() { currentTongueState = TongueState.Ready; rb.gravityScale = 4; rb.linearVelocity = Vector2.zero; nextTongueTime = Time.time + tongueCooldown; LockAnimation(0.2f); stretch.DoTongue(); PlayAnimation(AnimState.TongueEnd); currentAnimState = AnimState.TongueEnd; isAttacking = false;}
    void DrawTongue() { if (lineRenderer == null) return; if (currentTongueState != TongueState.Ready) { lineRenderer.enabled = true; lineRenderer.positionCount = 2; Vector3 realOffset = isFacingRight ? mouthOffset : new Vector3(-mouthOffset.x, mouthOffset.y, 0); lineRenderer.SetPosition(0, transform.position + realOffset); lineRenderer.SetPosition(1, currentTongueTipPos); } else lineRenderer.enabled = false; }
    void HandleInput() { if (isAttacking) moveInputX = 0; else moveInputX = Input.GetAxisRaw("Horizontal"); moveInputY = Input.GetAxisRaw("Vertical"); if (wallJumpBlockTimer > 0) wallJumpBlockTimer -= Time.deltaTime; if (currentTongueState == TongueState.Ready && Input.GetButtonDown("Jump") && !isAttacking) { if (isGrounded) jumpInputPressed = true; else if (currentState == PlayerState.Frog && isTouchingWall && !isGrounded) jumpInputPressed = true; else if (currentState == PlayerState.Frog && canDoubleJump && excalibroController != null) PerformDoubleJump(); } }
    void HandleActions() { if (currentState == PlayerState.Frog && currentTongueState == TongueState.Ready && Input.GetKeyDown(tongueKey) && Time.time > nextTongueTime) { if (moveInputX > 0 && !isFacingRight) Flip(); else if (moveInputX < 0 && isFacingRight) Flip(); StartTongue(); } if (currentState == PlayerState.Warrior && Input.GetKeyDown(attackKey) && Time.time > nextAttackTime && !isAttacking) { if (!isGrounded && moveInputY < -0.1f) StartCoroutine(PerformPogoAttack()); else StartCoroutine(PerformNormalAttack()); } if (currentTongueState == TongueState.Ready && !isTouchingWall && wallJumpBlockTimer <= 0 && !isAttacking) { if (moveInputX > 0 && !isFacingRight) Flip(); else if (moveInputX < 0 && isFacingRight) Flip(); } }
    void HandleStateSwitch() { if (Input.GetKeyDown(KeyCode.C) && !isAttacking) SwitchState(currentState == PlayerState.Warrior ? PlayerState.Frog : PlayerState.Warrior); }
    
    // --- COLLISION CHECK ---
    void CheckCollisions() {
        bool wasTouchingWall = isTouchingWall;
        Bounds b = boxCollider.bounds; float margin = 0.08f;
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margin, groundLayer);
        Vector2 wallSize = new Vector2(b.size.x, b.size.y * 0.9f);
        bool wR = Physics2D.BoxCast(b.center, wallSize, 0f, Vector2.right, margin, groundLayer);
        bool wL = Physics2D.BoxCast(b.center, wallSize, 0f, Vector2.left, margin, groundLayer);
        if (wR) { isTouchingWall = true; wallDirection = 1; } else if (wL) { isTouchingWall = true; wallDirection = -1; } else { isTouchingWall = false; wallDirection = 0; }
        
        if (!wasTouchingWall && isTouchingWall && currentState == PlayerState.Frog)
        {
            wallStickTimer = wallStickTime;
            wallJumpBlockTimer = 0;

            if (currentAnimState == AnimState.WallJump)
            {
                animationLockTime = 0f;
            }
        }

        if (!wasTouchingWall && isTouchingWall && currentState == PlayerState.Frog && !isGrounded)
        {
            stretch.DoWallGrab();
        }
    }

    void HandleGroundLogic() { if (isGrounded && !canDoubleJump) { canDoubleJump = true; if (excalibroController != null && currentState == PlayerState.Frog) excalibroController.Recharge(); } }
    void Flip() { isFacingRight = !isFacingRight; Vector3 scale = transform.localScale; scale.x *= -1; transform.localScale = scale; }

    void SwitchState(PlayerState newState) 
    { 
        currentState = newState; 
        
        if (excalibroController != null) 
        { 
            if (newState == PlayerState.Frog)
            {
                SpawnParticle("frogSwitch_particle"); 
                excalibroController.Appear(); 
            }

            else excalibroController.Vanish(); 

        } 

            rb.gravityScale = 4; 

        if (newState == PlayerState.Warrior) 
        {
            SpawnParticle("heroSwitch_particle"); 
            wallStickTimer = 0; 
        }
    }

    public void Die() { if (isDead) return; isDead = true; rb.linearVelocity = Vector2.zero; rb.gravityScale = 1; if (anim != null) anim.SetTrigger("Die"); if (excalibroController != null) excalibroController.Vanish(); }
    void OnDrawGizmos() { if (boxCollider == null) return; Gizmos.color = Color.red; Gizmos.DrawWireCube(boxCollider.bounds.center + Vector3.down * 0.05f, boxCollider.bounds.size); if (attackPoint != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(attackPoint.position, attackRadius); } if (pogoPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(pogoPoint.position, pogoRadius); } }
    #endregion

    // =================================================================================
    // MÁQUINA DE ESTADOS - VERSÃO CORRIGIDA
    // =================================================================================
    #region Animation Logic

    private void HandleAnimationStateMachine()
    {
        // 1. PRIORIDADE PAREDE (CORTE DE GIRO)
        if (
            currentState == PlayerState.Frog &&
            isTouchingWall &&
            !isGrounded &&
            !isLanding &&
            wallDetachTimer <= 0 &&
            currentAnimState != AnimState.WallJump
        )
        {
            animationLockTime = 0;
            PlayAnimation(AnimState.WallSlide);
            currentAnimState = AnimState.WallSlide;
            return;
        }

        if (isGrounded && currentAnimState == AnimState.WallJump)
        {
            animationLockTime = 0f;
            currentAnimState = AnimState.Idle;
        }

        if (isGrounded && !prevGrounded && rb.linearVelocity.y == 0)
        {
            prevGrounded = true;
            stretch.DoLand(); 
            SpawnParticle("land_dust");

            if (Mathf.Abs(moveInputX) < 0.1f)
            {
                isLanding = true;
                LockAnimation(0.15f);
                PlayAnimation(AnimState.Land);
                currentAnimState = AnimState.Land;
                return;
            }

            isLanding = false;
        }


        if (!isGrounded)
        {
            prevGrounded = false;
        }

        // 2. PRIORIDADE CHÃO (SÓ CORTA SE NÃO ESTIVER ATACANDO)
        // 3. Trava de tempo
        if (animationLockTime > 0)
        {
            animationLockTime -= Time.deltaTime;
            return; 
        }

        // 4. Estados normais
        AnimState newState = DetermineAnimState();
        if (newState != currentAnimState && animationLockTime <= 0) { PlayAnimation(newState); currentAnimState = newState; }
    }

    private AnimState DetermineAnimState()
    {
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f && wallJumpBlockTimer <= 0)
            {
                isLanding = false;
                return currentAnimState == AnimState.WallJump
                    ? AnimState.WallJump
                    : AnimState.Jump;
            }

            if (rb.linearVelocity.y < -0.1f) return AnimState.Fall;
                isLanding = false; 
            return (currentAnimState == AnimState.Jump) ? AnimState.Jump : AnimState.Fall;
        }

        if (Mathf.Abs(moveInputX) > 0.1f){
            isLanding = false;
            animationLockTime = 0;
        return AnimState.Walk;
        }  
        else{
            isLanding = false;
        }
        return AnimState.Idle;
    }

    private void PlayAnimation(AnimState state)
    {
        int targetHash = 0;
        if (currentState == PlayerState.Warrior) {
            switch (state) {
                case AnimState.Idle: targetHash = WarriorIdle; break;
                case AnimState.Walk: targetHash = WarriorWalk; break;
                case AnimState.Jump: targetHash = WarriorJump; break;
                case AnimState.Fall: targetHash = WarriorFall; break;
                case AnimState.Land: targetHash = WarriorLand; break;
                case AnimState.Attack: targetHash = WarriorAttack; break;
                default: targetHash = WarriorIdle; break;
            }
        } else { 
            switch (state) {
                case AnimState.Idle: targetHash = FrogIdle; break;
                case AnimState.Walk: targetHash = FrogWalk; break;
                case AnimState.Jump: targetHash = FrogJump; break;
                case AnimState.Fall: targetHash = FrogFall; break;
                case AnimState.Land: targetHash = FrogLand; break;
                case AnimState.Attack: targetHash = FrogAttack; break;
                case AnimState.WallSlide: targetHash = FrogWallSlide; break;
                case AnimState.WallJump: targetHash = FrogWallJump; break; 
                case AnimState.Dash: targetHash = FrogDash; break;
                case AnimState.TongueEnd: targetHash = FrogTongueEnd; break;
                default: targetHash = FrogIdle; break;
            }
        }
        if (targetHash != currentAnimHash && targetHash != 0) { currentAnimHash = targetHash; anim.CrossFade(targetHash, 0.1f); }
    }

    private void LockAnimation(float duration) { animationLockTime = duration; }
    #endregion
}