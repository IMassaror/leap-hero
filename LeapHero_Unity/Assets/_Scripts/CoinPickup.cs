using UnityEngine;
using ChristinaCreatesGames.Animations;

public class CoinPickup : MonoBehaviour
{
    private SquashAndStretch stretch;
    [SerializeField] private SpriteParticles particlePrefab;
    [SerializeField] private Transform particleSpawnPoint;
    public bool isFacingRight; 
    private bool collected;

    [Header("Settings")]
    public int value = 1; // Quanto vale essa moeda

    void SpawnParticle(string pAnim)
    {
        if (particlePrefab == null || particleSpawnPoint == null) return;
        SpriteParticles p = Instantiate(particlePrefab, particleSpawnPoint.position, Quaternion.identity);
        p.Play(pAnim, isFacingRight);
    }

    void Collected()
    {
        Invoke(nameof(ConfirmCollection), 0.25f);
        stretch.PlaySquashAndStretch();
    }

    void ConfirmCollection()
    {
        // Adiciona a moeda no gerente
        CoinManager.instance.AddCoins(value);
        SpawnParticle("smallExplosion");
        // Toca som (se tiver) ou efeito visual aqui no futuro...
            
        // Destroi a moeda
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se foi o Player que encostou
        if (collision.CompareTag("Player") && !collected)
        {
            stretch = GetComponent<SquashAndStretch>();
            collected = true;
            Collected();
        }
    }
}