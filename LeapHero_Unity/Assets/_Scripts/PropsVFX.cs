using UnityEngine;
using ChristinaCreatesGames.Animations;

public class PropsVFX : MonoBehaviour
{
    public SquashAndStretch stretch;
    [SerializeField] private SpriteParticles particlePrefab;
    [SerializeField] private Transform particleSpawnPoint;
    public bool isFacingRight;
    
    void Start()
    {
        stretch = GetComponent<SquashAndStretch>();
    }

    public void SpawnParticle(string pAnim)
    {
        if (particlePrefab == null || particleSpawnPoint == null) return;
        SpriteParticles p = Instantiate(particlePrefab, particleSpawnPoint.position, Quaternion.identity);
        p.Play(pAnim, isFacingRight);
    }
}
