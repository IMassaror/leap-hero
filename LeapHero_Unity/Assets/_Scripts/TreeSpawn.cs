using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("Renderers")]
    public SpriteRenderer trunkRenderer;
    public SpriteRenderer leafRenderer;

    [Header("Sprites")]
    public Sprite[] trunkFrames;
    public Sprite[] leafFrames;

    [Header("Config")]
    [Min(0)]
    public int treeIndex;

    void OnValidate()
    {
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (trunkFrames == null || leafFrames == null) return;
        if (trunkFrames.Length == 0 || leafFrames.Length == 0) return;

        treeIndex = Mathf.Clamp(treeIndex, 0, trunkFrames.Length - 1);

        trunkRenderer.sprite = trunkFrames[treeIndex];
        leafRenderer.sprite = leafFrames[treeIndex];
    }
}