using UnityEngine;

public class PlayerParticles : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;

    private float clipLength;

    /// <summary>
    /// Toca a animação e destrói o objeto quando ela acabar
    /// </summary>
    public void Play(string animationName, bool faceRight)
    {
        // 🔁 Flip automático
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1);
        transform.localScale = scale;

        // ▶️ Toca animação do início
        animator.Play(animationName, 0, 0f);

        // ⏱️ Descobre a duração real do clip
        clipLength = GetClipLength(animationName);

        // 🧹 Auto destroy
        Destroy(gameObject, clipLength);
    }

    /// <summary>
    /// Busca a duração do AnimationClip pelo nome
    /// </summary>
    private float GetClipLength(string clipName)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        // Fallback de segurança
        return 1f;
    }
}