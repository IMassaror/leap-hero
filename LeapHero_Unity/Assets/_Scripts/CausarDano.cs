using UnityEngine;

public class CausarDano : MonoBehaviour
{
    public int dano = 1;

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<VidaPlayer>()?.TomarDano(dano);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<VidaPlayer>()?.TomarDano(dano);
        }
    }
}