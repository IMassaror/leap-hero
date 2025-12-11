using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VidaPlayer : MonoBehaviour
{
    [Header("Configurações")]
    public int vidaGuerreiroMaxima = 3;
    public int vidaAtual;
    public float tempoParaReiniciar = 2.0f; // NOVO: Tempo para ver a animação de morte
    
    [Header("Invencibilidade")]
    public float tempoInvencivel = 1.0f;
    public SpriteRenderer spriteRenderer; 

    private PlayerController playerController;
    private bool estaInvencivel = false;
    private bool jaMorreu = false; // Para não morrer duas vezes seguidas

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        vidaAtual = vidaGuerreiroMaxima;
    }

    public void TomarDano(int dano)
    {
        if (estaInvencivel || vidaAtual <= 0 || jaMorreu) return;

        // REGRA: Sapo morre com 1 hit (Glass Cannon)
        if (playerController.estadoAtual == PlayerController.Estado.Sapo)
        {
            Debug.Log("Sapo atingido! Morte Instantânea.");
            Morrer();
            return;
        }

        // REGRA: Guerreiro tanka o dano
        vidaAtual -= dano;
        Debug.Log($"Guerreiro atingido! Vida restante: {vidaAtual}");

        if (vidaAtual <= 0)
        {
            Morrer();
        }
        else
        {
            StartCoroutine(Invencibilidade());
        }
    }

    void Morrer()
    {
        if(jaMorreu) return;
        jaMorreu = true;

        // 1. Avisa o PlayerController para travar movimento e tocar animação
        if(playerController != null)
        {
            playerController.Morrer();
        }

        Debug.Log("GAME OVER - Esperando animação para reiniciar...");
        
        // 2. Espera um pouco antes de resetar a cena
        StartCoroutine(ReiniciarFase());
    }

    IEnumerator ReiniciarFase()
    {
        // Espera o tempo da animação (configurável no Inspector)
        yield return new WaitForSeconds(tempoParaReiniciar);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator Invencibilidade()
    {
        estaInvencivel = true;
        // Pisca o personagem
        for (float i = 0; i < tempoInvencivel; i += 0.1f)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        estaInvencivel = false;
    }
}