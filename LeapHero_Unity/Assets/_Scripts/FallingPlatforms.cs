using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FallingPlatforms : MonoBehaviour
{
   Rigidbody2D rb;
   Vector2 defaultPos;
   Collider2D col;
   SpriteRenderer sr;
   public bool falling;
   public bool landed;
   private PropsVFX vfx;

    void Start()
    {
        defaultPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        vfx = GetComponent<PropsVFX>();
        rb = gameObject.AddComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Static;
        col.usedByEffector = true;
        col.isTrigger = false;

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Solido") || collision.gameObject.layer == LayerMask.NameToLayer("FallingBlocks"))
        {
            if(!landed)
            {
                landed = true;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;
                sr.enabled = false;
                col.isTrigger = true;
                vfx.SpawnParticle("smallExplosion");
            }
        }
        
        if (!collision.collider.CompareTag("Player")) return;
        
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f && !falling)
            {
                falling = true;
                StartCoroutine(Fall());
                StartCoroutine(ResetTimer());
                break;
            }
        }
    }

    IEnumerator Fall()
    {
        yield return StartCoroutine(Shake(transform));

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    IEnumerator Shake(Transform plat)
    {
        float duration = 0.75f;
        float strength = 0.05f;

        Vector3 start = plat.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            Vector2 offset =
                Random.insideUnitCircle *
                strength *
                Mathf.Sin(t * 50f);

            plat.position = start + (Vector3)offset;
            yield return null;
        }

        plat.position = start;
    }

    IEnumerator ResetTimer()
    {
        yield return new WaitForSeconds(3f);
        Reset();
    }

    private void Reset()
    {
        rb.bodyType = RigidbodyType2D.Static;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        sr.enabled = true;
        col.isTrigger = false;

        vfx.stretch.PlaySquashAndStretch();

        transform.position = defaultPos;
        landed = false;
        falling = false;
    }
}
