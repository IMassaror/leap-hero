using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para reiniciar a cena

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public Vector3 currentRespawnPoint;
    private bool hasCheckpointSaved = false; // Protege o checkpoint ao mudar de cena

    void Awake()
    {
        // Singleton Blindado: Garante que só existe um LevelManager e ele sobrevive entre cenas
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Só define a posição inicial se NÃO tivermos um checkpoint salvo ainda
        if (!hasCheckpointSaved)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                currentRespawnPoint = player.transform.position;
            }
        }
    }

    // --- LÓGICA DE RESET (MÉTODO 1) ---
    void Update()
    {
        // Aperte F1 para apagar todo o progresso e reiniciar
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ResetAllProgress();
        }
    }

    public void ResetAllProgress()
    {
        // 1. Apaga a memória permanente (Diamantes, Moedas, etc)
        PlayerPrefs.DeleteAll();
        Debug.Log("⚠️ SAVE DELETADO! Tudo resetado.");

        // 2. Reseta o estado interno do LevelManager
        hasCheckpointSaved = false;

        // 3. Reinicia a cena atual
        // Nota: Como este objeto tem DontDestroyOnLoad, precisamos ter cuidado.
        // O jeito mais limpo de resetar TOTALMENTE é se destruir para o novo LevelManager nascer limpo.
        Destroy(gameObject); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    // ----------------------------------

    public void SetCheckpoint(Vector3 position)
    {
        currentRespawnPoint = position;
        hasCheckpointSaved = true;
        Debug.Log("Checkpoint Salvo!");
    }

    public void Respawn(GameObject player)
    {
        // Zera a velocidade para não nascer voando
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if(rb != null) rb.linearVelocity = Vector2.zero;

        // Move para o último checkpoint
        player.transform.position = currentRespawnPoint;
        
        // Reseta a vida e visual do player (cura o bug de piscar)
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetStatus();
        }
    }
}