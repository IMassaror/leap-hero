using UnityEngine;

public class PropHurt : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
          print("tocou");
    }
}