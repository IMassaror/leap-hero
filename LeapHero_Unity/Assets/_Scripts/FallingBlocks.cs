using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FallingBlocks : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask blockLayer;

    [Header("State")]
    public bool activated = false;
    public bool isInCluster = false;

    void Awake()
    {
        int layer = LayerMask.NameToLayer("FallingBlocks");
        gameObject.layer = layer;
        blockLayer = 1 << layer;

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 🔒 tiles em cluster não reagem mais
        if (isInCluster) return;

        if (!activated && collision.gameObject.tag == "Player")
        {
            activated = true;

            List<FallingBlocks> group = GetConnectedTiles();
            StartCoroutine(CreateCluster(group));
        }
    }

    List<FallingBlocks> GetConnectedTiles()
    {
        List<FallingBlocks> result = new();
        Queue<FallingBlocks> queue = new();

        queue.Enqueue(this);
        result.Add(this);

        while (queue.Count > 0)
        {
            FallingBlocks current = queue.Dequeue();

            CheckNeighbor(current, Vector2.right, result, queue);
            CheckNeighbor(current, Vector2.left,  result, queue);
            CheckNeighbor(current, Vector2.up,    result, queue);
            CheckNeighbor(current, Vector2.down,  result, queue);
        }

        return result;
    }

    void CheckNeighbor(
        FallingBlocks current,
        Vector2 dir,
        List<FallingBlocks> result,
        Queue<FallingBlocks> queue
    )
    {
        Collider2D col = current.GetComponent<Collider2D>();
        Vector2 half = col.bounds.extents;

        Vector2 checkPos = (Vector2)col.bounds.center + dir * half * 2f;

        Collider2D hit = Physics2D.OverlapBox(
            checkPos,
            half * 0.9f,
            0f,
            blockLayer
        );

        if (!hit) return;

        FallingBlocks tile = hit.GetComponent<FallingBlocks>();
        if (tile == null || result.Contains(tile)) return;

        result.Add(tile);
        queue.Enqueue(tile);
    }

    IEnumerator CreateCluster(List<FallingBlocks> group)
    {
        // 🔹 cria o ROOT
        GameObject root = new("FallingBlockCluster");
        root.transform.position = transform.position;

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        BoxCollider2D clusterCollider = root.AddComponent<BoxCollider2D>();

        // 🔹 marca tiles e reparent
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        foreach (var tile in group)
        {
            tile.isInCluster = true;
            tile.activated = true;

            tile.transform.SetParent(root.transform);
            bounds.Encapsulate(tile.GetComponent<Collider2D>().bounds);
        }

        // 🔹 ajusta collider do cluster
        clusterCollider.offset = root.transform.InverseTransformPoint(bounds.center);
        clusterCollider.size = bounds.size;

        // 🔹 TREMOR COLETIVO
        yield return StartCoroutine(Shake(root.transform));

        // 🔹 ATIVA FÍSICA
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    IEnumerator Shake(Transform root)
    {
        float duration = 0.75f;
        float strength = 0.05f;

        Vector3 start = root.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            Vector2 offset =
                Random.insideUnitCircle *
                strength *
                Mathf.Sin(t * 50f);

            root.position = start + (Vector3)offset;
            yield return null;
        }

        root.position = start;
    }
}
