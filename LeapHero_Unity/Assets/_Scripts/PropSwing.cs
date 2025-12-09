using UnityEngine;

public class PropWiggle : MonoBehaviour
{
    [Header("wiggle settings")]
    public float maxAngle = 12f;
    public float wiggleDuration = 0.5f;
    public float decayRate = 3f;

    private float wiggleAmount = 0f;
    private float wiggleDirection = 1f;
    private float wiggleTimer = 0f;

    private bool isWiggling = false;   // <- impede repetição
    private float resetThreshold = 0.05f;

    void Update()
    {
        // Se ainda está balançando
        if (wiggleAmount > resetThreshold)
        {
            wiggleTimer += Time.deltaTime;

            float angle = Mathf.Sin(wiggleTimer * (Mathf.PI * 2f) / wiggleDuration)
                        * maxAngle
                        * wiggleAmount
                        * wiggleDirection;

            transform.localRotation = Quaternion.Euler(0, 0, angle);

            wiggleAmount = Mathf.Lerp(wiggleAmount, 0f, Time.deltaTime * decayRate);
        }
        else
        {
            // Quando parar completamente, libera nova ativação
            if (isWiggling)
            {
                isWiggling = false;
                transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            TriggerWiggleOnce(other.transform.position);
    }

    private void TriggerWiggleOnce(Vector3 playerPos)
    {
        if (isWiggling) return; // Já está balançando → ignora

        // Define direção pelo lado do player
        wiggleDirection = (playerPos.x < transform.position.x) ? -1f : 1f;

        // Começa o balanço
        wiggleAmount = 1f;
        wiggleTimer = 0f;
        isWiggling = true;
    }
}