using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FallingBlocks : MonoBehaviour
{
    public LayerMask blockLayer;

    private bool activated = false;

    void Awake()
    {
        int layer = LayerMask.NameToLayer("FallingBlocks");

        if (layer == -1)
        {
            Debug.LogError("Layer 'FallingBlocks' NÃO EXISTE!");
            return;
        }

        gameObject.layer = layer;
        blockLayer = 1 << layer;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (activated) return;

        if (collision.CompareTag("Player"))
        {
            activated = true;

            List<FallingBlocks> group = GetConnectedTiles();
            StartCoroutine(TriggerGroup(group));
        }
    }

    // =========================
    // CLUSTER DETECTION
    // =========================

    List<FallingBlocks> GetConnectedTiles()
    {
        List<FallingBlocks> result = new List<FallingBlocks>();
        Queue<FallingBlocks> queue = new Queue<FallingBlocks>();

        queue.Enqueue(this);
        result.Add(this);

        while (queue.Count > 0)
        {
            FallingBlocks current = queue.Dequeue();
            Collider2D col = current.GetComponent<Collider2D>();

            CheckOverlap(current, Vector2.right, result, queue);
            CheckOverlap(current, Vector2.left,  result, queue);
            CheckOverlap(current, Vector2.up,    result, queue);
            CheckOverlap(current, Vector2.down,  result, queue);
        }

        return result;
    }

    void CheckOverlap(
        FallingBlocks current,
        Vector2 dir,
        List<FallingBlocks> result,
        Queue<FallingBlocks> queue
    )
    {
        Collider2D col = current.GetComponent<Collider2D>();
        Vector2 halfSize = col.bounds.extents;

        Vector2 offset = Vector2.zero;

        if (dir == Vector2.right) offset = new Vector2(halfSize.x * 2f, 0);
        if (dir == Vector2.left)  offset = new Vector2(-halfSize.x * 2f, 0);
        if (dir == Vector2.up)    offset = new Vector2(0, halfSize.y * 2f);
        if (dir == Vector2.down)  offset = new Vector2(0, -halfSize.y * 2f);

        Vector2 checkPos = (Vector2)col.bounds.center + offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            checkPos,
            halfSize * 0.9f,
            0f,
            blockLayer
        );

        foreach (var hit in hits)
        {
            FallingBlocks tile = hit.GetComponent<FallingBlocks>();

            if (tile == null || result.Contains(tile))
                continue;

            result.Add(tile);
            queue.Enqueue(tile);
        }
    }

    // =========================
    // GROUP SHAKE + FALL
    // =========================

    IEnumerator TriggerGroup(List<FallingBlocks> group)
    {
        float shakeDuration = 0.4f;
        float shakeStrength = 0.05f;

        // Salva posições originais
        Dictionary<FallingBlocks, Vector3> originalPositions =
            new Dictionary<FallingBlocks, Vector3>();

        foreach (var tile in group)
            originalPositions[tile] = tile.transform.position;

        float t = 0f;

        // 🔥 TREMOR COLETIVO
        while (t < shakeDuration)
        {
            t += Time.deltaTime;

            Vector2 offset =
                Random.insideUnitCircle.normalized *
                shakeStrength *
                Mathf.Sin(t * 50f);

            foreach (var tile in group)
                tile.transform.position = originalPositions[tile] + (Vector3)offset;

            yield return null;
        }

        // Reset final
        foreach (var tile in group)
            tile.transform.position = originalPositions[tile];

        // Pequena pausa dramática
        yield return new WaitForSeconds(0.05f);

        // 💥 QUEDA
        foreach (var tile in group)
            tile.Fall();
    }

    // =========================
    // FALL
    // =========================

    public void Fall()
    {
        if (!TryGetComponent(out Rigidbody2D rb))
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
        }

        Destroy(gameObject, 2f);
    }
}
