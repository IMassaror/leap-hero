using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class propTileReplacer : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;

    [Header("Prop Prefabs")]
    public GameObject propPrefab;

    [Header("Prop Tiles")]
    public List<TileBase> propTiles;

    [Header("Wiggling Props")]
    public List<string> wiggleNames;

    [Header("Hurting Props")]
    public List<string> hurtNames;

    [Header("Falling Blocks")]
    public List<string> fallNames;

    [Header("Falling Platforms")]
    public List<string> fallPNames;

    [Header("Jump Through Props")]
    public List<string> jumpTNames;

    [Header("Pivot Offset")]
    public float yOffset = -0.5f;

    void Start()
    {
        ReplacePropTiles();
    }

    void ReplacePropTiles()
    {
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);

            if (tile != null && propTiles.Contains(tile))
            {
                string tileName = tile.name;

                // Posição do centro da célula + offset vertical
                Vector3 worldPos = tilemap.GetCellCenterWorld(pos);
                worldPos += new Vector3(0f, yOffset, 0f);

                // Instancia o objeto no mundo
                GameObject obj = Instantiate(propPrefab, worldPos, Quaternion.identity);

                // Aplica o sprite/frame correto
                ApplyFrame(obj, tileName);

                // SE o tile estiver marcado como "wiggle", adiciona o componente
                if (wiggleNames.Contains(tileName))
                {
                    if (obj.GetComponent<PropWiggle>() == null)
                        obj.AddComponent<PropWiggle>();
                }

                // SE o tile estiver marcado como "hurt", adiciona o componente
                if (hurtNames.Contains(tileName))
                {
                    if (obj.GetComponent<PropHurt>() == null)
                        obj.AddComponent<PropHurt>();
                }

                // SE o tile estiver marcado como "fall", adiciona o componente
                if (fallNames.Contains(tileName))
                {
                    if (obj.GetComponent<FallingBlocks>() == null)
                        obj.AddComponent<FallingBlocks>();
                }

                // SE o tile estiver marcado como "fallP", adiciona o componente
                if (fallPNames.Contains(tileName))
                {
                    if (obj.GetComponent<FallingPlatforms>() == null)
                        obj.AddComponent<FallingPlatforms>();
                }

                // SE o tile estiver marcado como "JumpThrough", adiciona o componente
                if (jumpTNames.Contains(tileName))
                {
                    if(obj.GetComponent<JumpThrough>() == null)
                       obj.AddComponent<JumpThrough>();
                }

                // Remove tile original
                tilemap.SetTile(pos, null);
            }
        }
    }

    void ApplyFrame(GameObject obj, string tileName)
    {
        Animator anim = obj.GetComponent<Animator>();

        if (anim != null)
        {
            anim.Play(tileName, 0, 0f);
            return;
        }

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("propSpritesheet");

            foreach (Sprite sp in sprites)
            {
                if (sp.name == tileName)
                {
                    sr.sprite = sp;
                    return;
                }
            }
        }
    }
}