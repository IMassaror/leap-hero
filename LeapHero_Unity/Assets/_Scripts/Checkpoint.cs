using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool activated = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !activated)
        {
            activated = true;
            LevelManager.instance.SetCheckpoint(transform.position);
            
            // Mude a cor ou toque um som para mostrar que ativou
            GetComponent<SpriteRenderer>().color = Color.green; 
        }
    }
}