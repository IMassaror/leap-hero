using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // --- ENUMS ---
    public enum Estado { Guerreiro, Sapo }
    public Estado estadoAtual;
    
    public enum EstadoLingua { Pronta, Atirando, PuxandoSapo, Retraindo }
    public EstadoLingua estadoLingua = EstadoLingua.Pronta;

    [Header("Conexões")]
    public ExcalibroController excalibroScript; // ARRASTE O EXCALIBRO AQUI

    [Header("Visual")]
    public Transform visualRoot; // ARRASTE O OBJETO "Visual" AQUI
    private Animator animVisual;
    private SpriteRenderer srVisual;
    private string animAtual = "";

    [Header("Geral")]
    public LayerMask layerSolido; 

    [Header("Guerreiro - Movimento")]
    public float velGuerreiro = 4f;
    public float puloGuerreiro = 12f;
    public Color corGuerreiro = Color.white;

    [Header("Guerreiro - Combate")]
    public KeyCode teclaAtaque = KeyCode.J;
    public Transform pontoAtaque; 
    public float raioAtaque = 0.8f; 
    public LayerMask layerInimigos; 
    public float duracaoTravamentoAereo = 0.3f; 
    public float cooldownAtaque = 0.4f; 
    private float tempoProximoAtaque;
    private bool estaAtacando = false; 

    [Header("Guerreiro - Excalibur (Pogo)")]
    public float forcaPogo = 14f; 
    public Transform pontoPogo;   
    public float raioPogo = 0.6f;

    [Header("Sapo - Movimento")]
    public float velSapo = 7f;
    public float puloSapo = 15f;
    public Color corSapo = Color.green;

    [Header("Sapo - Wall Mechanics")]
    public float forcaWallJumpX = 12f; 
    public float forcaWallJumpY = 16f;
    public float velocidadeDeslizar = 2f; 
    public float velocidadeAjusteVertical = 3f; 
    public float tempoGrudadoNaParede = 1.5f; 
    public float tempoBloqueioWallJump = 0.2f; 
    public float tempoResistenciaParede = 0.2f; 

    [Header("Sapo - Língua")]
    public float alcanceLingua = 8f;
    public float velocidadeLinguaIda = 25f;    
    public float velocidadeLinguaVolta = 35f; 
    public float velocidadePuxadaCorpo = 18f; 
    public float delayEntreLinguadas = 0.5f; 
    public KeyCode teclaLingua = KeyCode.L; 
    public LineRenderer lineRenderer;
    public Vector3 offsetBoca; 

    [Header("Squash & Stretch (Visual)")]
    public SquashStretchController squashController; // arraste o controller do visual ou será buscado automaticamente

    // INTERNAS
    private Rigidbody2D rb;
    private BoxCollider2D colisor;
    private float xInput;
    private float yInput;
    private bool jumpInputDown;
    private bool viradoDireita = true;
    private float wallStickTimer; 
    private float wallJumpBlockTimer; 
    private float timerTentativaSair; 
    private int ladoParede = 0;
    private Vector2 destinoLingua;       
    private Vector2 pontaLinguaAtual;    
    private bool acertouParede;          
    private float tempoParaProximaLingua;
    private Vector2 posicaoInicialTiro; 
    public bool isGrounded;
    public bool isTouchingWall;
    private bool isDead = false;

    // CONTROLE DE PULO DUPLO
    private bool canDoubleJump = false; 

    // terreno: para detectar aterrissagem
    private bool prevGrounded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<BoxCollider2D>();

        if (visualRoot != null)
        {
            animVisual = visualRoot.GetComponent<Animator>();
            srVisual = visualRoot.GetComponent<SpriteRenderer>();
        }

        // tenta buscar squash automaticamente se não arrastado
        if (squashController == null)
            squashController = GetComponentInChildren<SquashStretchController>();

        if (lineRenderer != null) lineRenderer.enabled = false;
        
        TrocarEstado(Estado.Guerreiro);

        prevGrounded = isGrounded;
    }

    // ----------------------------
    // SISTEMA DE SUFIXOS
    // ----------------------------
    string Sufixo => estadoAtual == Estado.Guerreiro ? "_h" : "_f";

    bool EhTrigger(string nome)
    {
        // Lista das animações que NÃO devem receber sufixo (são triggers ou chamadas diretas)
        return nome == "Attack" ||
               nome == "AttackDown" ||
               nome == "tongueStart" ||
               nome == "Die" ||
               nome == "Dash";
    }

    void PlayAnim(string nomeBase)
    {
        if (animVisual == null) return;

        // triggers: usar SetTrigger sem sufixo
        if (EhTrigger(nomeBase))
        {
            // evita retriggerar continuamente: ainda assim deixamos SetTrigger para ser chamado
            animVisual.SetTrigger(nomeBase);
            animAtual = nomeBase; // guarda esse estado pra evitar Play repetido em outras chamadas
            return;
        }

        // animações que usam sufixo
        string nome = nomeBase + Sufixo;

        if (animAtual == nome) return; // evita reiniciar a animação repetidamente

        animAtual = nome;
        animVisual.Play(nome);
    }
    // ----------------------------

    void Update()
    {
        if (isDead) return;

        // 1. INPUTS
        if (estaAtacando) xInput = 0;
        else xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (wallJumpBlockTimer > 0) wallJumpBlockTimer -= Time.deltaTime;

        // 2. SISTEMA DE PULO UNIFICADO
        if (estadoLingua == EstadoLingua.Pronta && Input.GetButtonDown("Jump") && !estaAtacando) 
        {
            // Prioridade 1: Pulo do Chão
            if (isGrounded)
            {
                jumpInputDown = true;
            }
            // Prioridade 2: Wall Jump (Sapo na Parede e Fora do Chão)
            else if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded)
            {
                jumpInputDown = true;
            }
            // Prioridade 3: Pulo Duplo (No Ar, Sapo, Tem Carga e NÃO está na parede)
            else if (estadoAtual == Estado.Sapo && canDoubleJump && excalibroScript != null)
            {
                ExecutarPuloDuplo();
            }
        }

        // 3. TROCA DE ESTADO
        if (Input.GetKeyDown(KeyCode.C) && !estaAtacando) 
            TrocarEstado(estadoAtual == Estado.Guerreiro ? Estado.Sapo : Estado.Guerreiro);

        // 4. LÍNGUA
        if (estadoAtual == Estado.Sapo && estadoLingua == EstadoLingua.Pronta && Input.GetKeyDown(teclaLingua) && Time.time > tempoParaProximaLingua) 
        {
            if (xInput > 0 && !viradoDireita) Flip();
            else if (xInput < 0 && viradoDireita) Flip();
            IniciarLingua();
        }

        // 5. COMBATE GUERREIRO (Pogo)
        if (estadoAtual == Estado.Guerreiro && Input.GetKeyDown(teclaAtaque) && Time.time > tempoProximoAtaque && !estaAtacando)
        {
            if (!isGrounded && yInput < -0.1f) StartCoroutine(RealizarPogo());
            else StartCoroutine(RealizarAtaque());
        }

        // 6. FLIP 
        if (estadoLingua == EstadoLingua.Pronta && !isTouchingWall && wallJumpBlockTimer <= 0 && !estaAtacando)
        {
             if (xInput > 0 && !viradoDireita) Flip();
             else if (xInput < 0 && viradoDireita) Flip();
        }

        // 7. Crouch trigger for squash
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (squashController != null) squashController.DoCrouch();
            // also play crouch anim (Crouch usa sufixo)
            PlayAnim("Crouch");
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            // when released, fallback to Idle/Run handled in AtualizarAnimator
        }

        AtualizarAnimator();
    }

    public void Morrer()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1; 
        if (animVisual != null) animVisual.SetTrigger("Die");
        if (excalibroScript != null) excalibroScript.Sumir();
    }

    void AtualizarAnimator()
    {
        // Se estiver atacando ou usando língua, não trocar animação
        if (estadoLingua != EstadoLingua.Pronta)
        {
             PlayAnim("tongueStart");
             return;
        }

        if (!isGrounded)
        {
             if (rb.linearVelocity.y > 0.1f)
             {
                PlayAnim("jump");
             }
             else if (rb.linearVelocity.y < -0.2f)
             {
                 PlayAnim("fall");
             }
             return;
        }

        // Estado em solo
        if (Mathf.Abs(xInput) > 0.1f)
                PlayAnim("walk");
        else
                PlayAnim("idle");

        // Crouch handled via input (kept above)
    }

    IEnumerator RealizarPogo()
    {
        estaAtacando = true; 
        tempoProximoAtaque = Time.time + 0.2f; 
        if (animVisual != null) animVisual.SetTrigger("AttackDown"); 
        yield return new WaitForSeconds(0.05f);

        Collider2D[] acertos = Physics2D.OverlapCircleAll(pontoPogo.position, raioPogo, layerInimigos);
        bool acertouAlgo = false;
        foreach (Collider2D alvo in acertos)
        {
            Destroy(alvo.gameObject); 
            acertouAlgo = true;
        }

        if (acertouAlgo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * forcaPogo, ForceMode2D.Impulse);
            // jump visual
            if (squashController != null) squashController.DoJump();
        }
        yield return new WaitForSeconds(0.1f);
        estaAtacando = false;
    }

    IEnumerator RealizarAtaque()
    {
        estaAtacando = true;
        tempoProximoAtaque = Time.time + cooldownAtaque;
        float gravidadeOriginal = rb.gravityScale;
        rb.gravityScale = 0; 
        rb.linearVelocity = Vector2.zero; 
        if (animVisual != null) PlayAnim("attack");
        Collider2D[] inimigos = Physics2D.OverlapCircleAll(pontoAtaque.position, raioAtaque, layerInimigos);
        foreach (Collider2D inimigo in inimigos) Destroy(inimigo.gameObject);
        yield return new WaitForSeconds(duracaoTravamentoAereo);
        rb.gravityScale = gravidadeOriginal; 
        estaAtacando = false;
    }

    void LateUpdate()
    {
        if (lineRenderer != null && estadoLingua != EstadoLingua.Pronta)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2; 
            Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
            lineRenderer.SetPosition(0, transform.position + offsetReal);    
            // pontaLinguaAtual é Vector2 — convertemos para Vector3 aqui
            lineRenderer.SetPosition(1, new Vector3(pontaLinguaAtual.x, pontaLinguaAtual.y, 0f));
        }
        else if(lineRenderer != null) lineRenderer.enabled = false;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        VerificarColisoes();
        
        // detect landing event
        if (!prevGrounded && isGrounded)
        {
            // landed
            if (squashController != null) squashController.DoLand();
            PlayAnim("Land"); // Land usa sufixo (Land_h / Land_f)
        }

        // RECARREGA PULO DUPLO NO CHÃO OU NA PAREDE
        if (isGrounded || isTouchingWall)
        {
            if(!canDoubleJump)
            {
                canDoubleJump = true; // Recarrega
                if(excalibroScript != null && estadoAtual == Estado.Sapo) 
                    excalibroScript.Recarregar(); // Fica colorida dnv
            }
        }

        if (estadoLingua != EstadoLingua.Pronta) { ProcessarFisicaLingua(); prevGrounded = isGrounded; return; }
        if (estaAtacando) { prevGrounded = isGrounded; return; }

        float velAtual = (estadoAtual == Estado.Guerreiro) ? velGuerreiro : velSapo;
        
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) MecanicaDeParedeoSapo();
        else MovimentoNormal(velAtual);

        if (jumpInputDown) { jumpInputDown = false; ProcessarPulo(); }

        prevGrounded = isGrounded;
    }

    void MovimentoNormal(float velAtual) {
        rb.gravityScale = 4; wallStickTimer = tempoGrudadoNaParede; timerTentativaSair = 0;
        if (wallJumpBlockTimer <= 0) rb.linearVelocity = new Vector2(xInput * velAtual, rb.linearVelocity.y);
    }

    void MecanicaDeParedeoSapo() {
        if (wallJumpBlockTimer > 0) return;
        bool tentandoSair = (xInput != 0 && xInput != ladoParede);
        if (tentandoSair) timerTentativaSair += Time.deltaTime; else timerTentativaSair = 0;

        if (tentandoSair && timerTentativaSair > tempoResistenciaParede) {
            rb.gravityScale = 4; rb.linearVelocity = new Vector2(xInput * velSapo, rb.linearVelocity.y); return;
        }
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
        if (wallStickTimer > 0) {
            rb.gravityScale = 0;
            if (yInput < 0) rb.linearVelocity = new Vector2(0, -velocidadeAjusteVertical); else rb.linearVelocity = Vector2.zero; 
            wallStickTimer -= Time.deltaTime;
            if (ladoParede == 1 && viradoDireita) Flip(); else if (ladoParede == -1 && !viradoDireita) Flip();
        } else {
            rb.gravityScale = 4; 
            float velQueda = (yInput < 0) ? velocidadeAjusteVertical : velocidadeDeslizar;
            rb.linearVelocity = new Vector2(0, Mathf.Clamp(rb.linearVelocity.y, -velQueda, float.MaxValue));
        }
    }

    void RealizarWallJump() {
        wallStickTimer = tempoGrudadoNaParede; wallJumpBlockTimer = tempoBloqueioWallJump; timerTentativaSair = 0; 
        int dirPulo = -ladoParede; rb.linearVelocity = Vector2.zero; 
        rb.AddForce(new Vector2(dirPulo * forcaWallJumpX, forcaWallJumpY), ForceMode2D.Impulse);
        if (dirPulo == 1 && !viradoDireita) Flip(); else if (dirPulo == -1 && viradoDireita) Flip();
        
        // Wall Jump recarrega o pulo duplo
        canDoubleJump = true; 
        if(excalibroScript != null) excalibroScript.Recarregar();

        // squash visual
        if (squashController != null) squashController.DoWallJump();
    }

    void IniciarLingua() {
        Vector2 direcao = viradoDireita ? Vector2.right : Vector2.left;
        estadoLingua = EstadoLingua.Atirando; 
        if (animVisual != null) animVisual.SetTrigger("TongueShoot");
        rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;

        // posicaoInicialTiro é Vector2 — convertemos explicitamente
        posicaoInicialTiro = (Vector2)transform.position;

        Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
        Vector3 origemTiro = transform.position + offsetReal;
        pontaLinguaAtual = (Vector2)origemTiro; 

        RaycastHit2D hit = Physics2D.Raycast(origemTiro, direcao, alcanceLingua, layerSolido);
        if (hit.collider != null) { destinoLingua = hit.point; acertouParede = true; }
        else { destinoLingua = (Vector2)origemTiro + (direcao * alcanceLingua); acertouParede = false; }
    }

    void ProcessarFisicaLingua() {
        switch (estadoLingua) {
            case EstadoLingua.Atirando:
                rb.linearVelocity = Vector2.zero; 
                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, destinoLingua, velocidadeLinguaIda * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, destinoLingua) < 0.1f) {
                    if (acertouParede) {
                        estadoLingua = EstadoLingua.PuxandoSapo;
                        // quando virou para puxar, consideramos isso o "dash" — toca squash dash
                        if (squashController != null) squashController.DoDash();
                        PlayAnim("Dash"); // Dash é trigger (sem sufixo)
                    }
                    else { estadoLingua = EstadoLingua.Retraindo; }
                } break;
            case EstadoLingua.PuxandoSapo:
                // Converter transform.position para Vector2 explicitamente antes de usar Vector2.Distance / MoveTowards
                float distOrigem = Vector2.Distance((Vector2)transform.position, posicaoInicialTiro);
                float distDestino = Vector2.Distance((Vector2)transform.position, destinoLingua);
                if (distOrigem > 0.5f) {
                    Collider2D col = Physics2D.OverlapCircle(transform.position, 0.5f, layerSolido);
                    if (col != null && distDestino > 1.0f) { estadoLingua = EstadoLingua.Retraindo; rb.gravityScale = 4; rb.linearVelocity = Vector2.zero; break; }
                }

                rb.linearVelocity = Vector2.zero;

                // MoveTowards em Vector2 -> converter depois para Vector3 ao atribuir a transform.position
                Vector2 newPos2 = Vector2.MoveTowards((Vector2)transform.position, pontaLinguaAtual, velocidadePuxadaCorpo * Time.deltaTime);
                transform.position = new Vector3(newPos2.x, newPos2.y, transform.position.z);

                if (Vector2.Distance((Vector2)transform.position, destinoLingua) < 0.5f) FinalizarLingua();
                break;
            case EstadoLingua.Retraindo:
                if (rb.gravityScale == 0) rb.linearVelocity = Vector2.zero;
                Vector3 offsetReal = viradoDireita ? offsetBoca : new Vector3(-offsetBoca.x, offsetBoca.y, 0);
                Vector3 alvoRetracao = transform.position + offsetReal;

                // pontaLinguaAtual é Vector2, alvoRetracao é Vector3 -> convertemos alvo para Vector2
                Vector2 alvo2 = (Vector2)alvoRetracao;

                pontaLinguaAtual = Vector2.MoveTowards(pontaLinguaAtual, alvo2, velocidadeLinguaVolta * Time.deltaTime);
                if (Vector2.Distance(pontaLinguaAtual, alvo2) < 0.1f) FinalizarLingua();
                break;
        }
    }

    void FinalizarLingua() { 
        estadoLingua = EstadoLingua.Pronta; 
        rb.gravityScale = 4; 
        tempoParaProximaLingua = Time.time + delayEntreLinguadas;
        canDoubleJump = true; 
        if(excalibroScript != null) excalibroScript.Recarregar();
    }
    
    void ProcessarPulo() {
        if (estadoAtual == Estado.Sapo && isTouchingWall && !isGrounded) 
        {
            RealizarWallJump();
        }
        else 
        {
            float forca = (estadoAtual == Estado.Guerreiro) ? puloGuerreiro : puloSapo;
            ExecutarPulo(forca);
        }
    }

    void ExecutarPuloDuplo()
    {
        canDoubleJump = false; 
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * puloSapo, ForceMode2D.Impulse);

        // AVISA A ESPADA QUE GASTOU
        if(excalibroScript != null) excalibroScript.UsarPulo();

        // squash visual para double jump
        if (squashController != null) squashController.DoJump();

        Debug.Log("Excalibro Jump!");
    }

    void ExecutarPulo(float forca) { 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        rb.AddForce(Vector2.up * forca, ForceMode2D.Impulse);

        // squash visual no pulo
        if (squashController != null) squashController.DoJump();
    }

    void VerificarColisoes() {
        float margem = 0.05f; Bounds b = colisor.bounds;
        isGrounded = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, margem, layerSolido);
        Vector2 tamanhoSensorParede = new Vector2(b.size.x, b.size.y * 0.9f);
        bool paredeDir = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.right, margem, layerSolido);
        bool paredeEsq = Physics2D.BoxCast(b.center, tamanhoSensorParede, 0f, Vector2.left, margem, layerSolido);
        if (paredeDir) { isTouchingWall = true; ladoParede = 1; } else if (paredeEsq) { isTouchingWall = true; ladoParede = -1; } else { isTouchingWall = false; ladoParede = 0; }
    }

    void Flip() 
    {
        viradoDireita = !viradoDireita;

        if (visualRoot != null)
        {
            Vector3 escala = visualRoot.localScale;
            escala.x *= -1;
            visualRoot.localScale = escala;
        }
    }

    void TrocarEstado(Estado novo) { 
        estadoAtual = novo; 
         if (srVisual != null) srVisual.color = Color.white;
        
        if(excalibroScript != null)
        {
            if(novo == Estado.Sapo) excalibroScript.Aparecer();
            else excalibroScript.Sumir();
        }

        rb.gravityScale = 4; 
        if(novo == Estado.Guerreiro) wallStickTimer = 0; 
    }
    
    void OnDrawGizmos() {
        if (colisor == null) return;
        Gizmos.color = Color.red; Gizmos.DrawWireCube(colisor.bounds.center + Vector3.down * 0.05f, colisor.bounds.size);
        if (pontoAtaque != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(pontoAtaque.position, raioAtaque); }
        if (pontoPogo != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(pontoPogo.position, raioPogo); }
    }
}
