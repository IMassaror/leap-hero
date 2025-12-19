using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    #region Settings
    [Header("Damage Settings")]
    public int damageAmount = 1;
    
    [Header("Instant Kill Options")]
    public bool killWarriorInstantly = false; // Mata o Guerreiro na hora?
    public bool killFrogInstantly = false;    // Mata o Sapo na hora?
    #endregion

    #region Unity Callbacks
    private void OnTriggerEnter2D(Collider2D collision) => DealDamage(collision.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => DealDamage(collision.gameObject);
    #endregion

    #region Internal Logic
    void DealDamage(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Passamos as duas opções novas para o PlayerHealth
                playerHealth.TakeDamage(damageAmount, killWarriorInstantly, killFrogInstantly);
            }
        }
    }
    #endregion
}