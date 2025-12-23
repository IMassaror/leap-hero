using UnityEngine;

public class SimpleAnimator : MonoBehaviour
{
    [Header("Animation")]
    public Sprite[] frames;
    public float frameRate = 12f;

    private SpriteRenderer sr;
    private int currentFrame;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (frames.Length <= 1) return;

        timer += Time.deltaTime;

        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            sr.sprite = frames[currentFrame];
        }
    }
}
