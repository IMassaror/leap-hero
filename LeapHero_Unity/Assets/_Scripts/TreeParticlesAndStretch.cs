using UnityEngine;
using ChristinaCreatesGames.Animations;

public class TreeParticlesAndStretch : MonoBehaviour
{
    public ParticleSystem leafParticles;
    private PropWiggle PropWiggleR;
    private bool isWigglingB;
    private SquashAndStretch stretch;

    void Start()
    {
        stretch = GetComponent<SquashAndStretch>();
        leafParticles = GetComponentInChildren<ParticleSystem>();
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
             stretch.PlaySquashAndStretch();
        }
    }
}
