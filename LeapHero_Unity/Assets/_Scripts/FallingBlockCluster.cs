using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FallingBlockCluster : MonoBehaviour
{
    Rigidbody2D rb;
    bool landed = false;

    public static void Create(List<FallingBlocks> tiles)
    {
        GameObject root = new("FallingBlockCluster");
        FallingBlockCluster cluster = root.AddComponent<FallingBlockCluster>();

        cluster.Setup(tiles);
    }

    void Setup(List<FallingBlocks> tiles)
    {
        // posição média
        Vector3 center = Vector3.zero;
        foreach (var t in tiles) center += t.transform.position;
        center /= tiles.Count;

        transform.position = center;

        // Rigidbody ÚNICO
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Collider ÚNICO
        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = false;

        Bounds bounds = new Bounds(tiles[0].transform.position, Vector3.zero);
        foreach (var t in tiles)
            bounds.Encapsulate(t.GetComponent<Collider2D>().bounds);

        box.size = bounds.size;
        box.offset = bounds.center - transform.position;

        // reparent tiles
        foreach (var t in tiles)
        {
            t.transform.SetParent(transform);
            t.GetComponent<Collider2D>().enabled = false;
        }

        StartCoroutine(ShakeAndFall());
    }

    IEnumerator ShakeAndFall()
    {
        float time = 0.75f;
        float strength = 0.05f;
        Vector3 start = transform.position;

        while (time > 0f)
        {
            time -= Time.deltaTime;
            transform.position = start + (Vector3)Random.insideUnitCircle * strength;
            yield return null;
        }

        transform.position = start;

        yield return new WaitForFixedUpdate();

        rb.gravityScale = 3f;
        rb.linearVelocity = Vector2.down * 0.1f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (landed) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Solido") || collision.gameObject.layer == LayerMask.NameToLayer("FallingBlocks"))
        {
            landed = true;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }
}
