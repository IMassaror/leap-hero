using UnityEngine;

public class JumpThrough : MonoBehaviour
{
    Collider2D col;

    void Start()
    {
        col = GetComponent<Collider2D>();
        col.usedByEffector = true;
        col.isTrigger = false;
    }
}
