using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [Header("Settings")]
    public int value = 1; // Quanto vale essa moeda

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se foi o Player que encostou
        if (collision.CompareTag("Player"))
        {
            // Adiciona a moeda no gerente
            CoinManager.instance.AddCoins(value);

            // Toca som (se tiver) ou efeito visual aqui no futuro...
            
            // Destroi a moeda
            Destroy(gameObject);
        }
    }
}