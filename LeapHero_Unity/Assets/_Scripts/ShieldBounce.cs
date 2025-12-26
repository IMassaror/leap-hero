using UnityEngine;
using System.Collections;
using UISquash;

public class ShieldBounce : MonoBehaviour
{
    [Header("Shields")]
    [SerializeField] private RectTransform[] shields;

    [Header("Bounce")]
    [SerializeField] private float bounceHeight = 20f;
    [SerializeField] private float bounceDuration = 0.15f;
    [SerializeField] private float waveDelay = 0.08f;

    private Vector2[] originalPositions;
    private UIBounce[] squashScripts;

    void Awake()
    {
        int count = shields.Length;

        originalPositions = new Vector2[count];
        squashScripts = new UIBounce[count];

        for (int i = 0; i < count; i++)
        {
            originalPositions[i] = shields[i].anchoredPosition;

            // pega o squash individual daquele coração
            squashScripts[i] = shields[i].GetComponent<UIBounce>();
        }
    }

    public void PlayWave()
    {
        StopAllCoroutines();
        StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        for (int i = 0; i < shields.Length; i++)
        {
            // toca squash + bounce juntos
            if (squashScripts[i] != null)
                squashScripts[i].Play();

            StartCoroutine(BounceShield(i));

            yield return new WaitForSeconds(waveDelay);
        }
    }

    IEnumerator BounceShield(int index)
    {
        RectTransform shield = shields[index];
        Vector2 startPos = originalPositions[index];
        Vector2 upPos = startPos + Vector2.up * bounceHeight;

        float t = 0f;

        // SUBIR
        while (t < bounceDuration)
        {
            t += Time.unscaledDeltaTime;
            shield.anchoredPosition = Vector2.Lerp(startPos, upPos, t / bounceDuration);
            yield return null;
        }

        t = 0f;

        // DESCER
        while (t < bounceDuration)
        {
            t += Time.unscaledDeltaTime;
            shield.anchoredPosition = Vector2.Lerp(upPos, startPos, t / bounceDuration);
            yield return null;
        }

        shield.anchoredPosition = startPos;
    }
}
