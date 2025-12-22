using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    #region Settings
    [Header("Damage Settings")]
    public int damageAmount = 1; // Quantidade de dano (era 'dano')
    #endregion

    #region Unity Callbacks
    
    // Se for um Trigger (Objeto atravessável, tipo área de espinhos)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        DealDamage(collision.gameObject);
    }

    // Se for uma Colisão Física (Objeto sólido, tipo inimigo que empurra)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        DealDamage(collision.gameObject);
    }

    #endregion

    #region Internal Logic
    void DealDamage(GameObject target)
    {
        // Verifica se é o Player
        if (target.CompareTag("Player"))
        {
            // Busca o componente NOVO (PlayerHealth) em vez do antigo (VidaPlayer)
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                // Chama o método NOVO (TakeDamage) em vez do antigo (TomarDano)
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
    #endregion
}