using UnityEngine;

public class TreeParticlesAndStretch : MonoBehaviour
{
    public SquashStretchController stretch;
    public ParticleSystem leafParticles;
    private PropWiggle PropWiggleR;
    private bool isWigglingB;

    void Start()
    {
        leafParticles = GetComponentInChildren<ParticleSystem>();
        stretch = GetComponentInChildren<SquashStretchController>();
        PropWiggleR = GetComponentInChildren<PropWiggle>();
    }

    void Update()
    {
        isWigglingB = PropWiggleR.isWiggling;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isWigglingB)
        {
             leafParticles.Play();
             stretch.DoLand();
        }
    }
}
