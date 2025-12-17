using UnityEngine;

public class TreeSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float maxAngle = 12f;
    public float returnForce = 8f;
    public float damping = 4f;

    [Header("Randomization")]
    public Vector2 maxAngleRange = new Vector2(8f, 15f);

    private float angle;
    private float velocity;

    void Start()
    {
        // Aleatoriedade por árvore
        maxAngle = Random.Range(maxAngleRange.x, maxAngleRange.y);
    }

    void Update()
    {
        // Simula retorno ao centro (pêndulo)
        float force = -angle * returnForce;
        velocity += force * Time.deltaTime;

        // Amortecimento
        velocity *= Mathf.Exp(-damping * Time.deltaTime);

        angle += velocity * Time.deltaTime;
        angle = Mathf.Clamp(angle, -maxAngle, maxAngle);

        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void AddImpulse(float force)
    {
        velocity += force;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
             float dir = other.transform.position.x > transform.position.x ? -1f : 1f;
             AddImpulse(dir * Random.Range(6f, 10f));
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
             float dir = other.transform.position.x > transform.position.x ? -1f : 1f;
             AddImpulse(dir * Random.Range(2f, 4f) * Time.deltaTime);
        }
    }
}
