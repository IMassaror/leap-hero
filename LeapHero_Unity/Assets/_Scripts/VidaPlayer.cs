using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VidaPlayer : MonoBehaviour
{
    [Header("Configurações")]
    public int vidaGuerreiroMaxima = 3;
    public int vidaAtual;
    
    [Header("Invencibilidade")]
    public float tempoInvencivel = 1.0f;
    public SpriteRenderer spriteRenderer; // Arraste o Sprite do Player aqui

    private PlayerController playerController;
    private bool estaInvencivel = false;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        vidaAtual = vidaGuerreiroMaxima;
    }

    public void TomarDano(int dano)
    {
        if (estaInvencivel || vidaAtual <= 0) return;

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
        Debug.Log("GAME OVER - Reiniciando Fase");
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