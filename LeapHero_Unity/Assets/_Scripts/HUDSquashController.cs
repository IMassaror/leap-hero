using UnityEngine;
using UISquash;

public class HUDSquashController : MonoBehaviour
{
    private UIBounce bounce;

    private void Awake()
    {
        bounce = GetComponent<UIBounce>();
    }

    public void FrogHurt()
    {
        bounce.Play(
            UIBounce.Axis.X,
            0.15f,
            1f,
            1.2f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );
    }

    public void FrogJump()
    {
        bounce.Play(
            UIBounce.Axis.Y,
            0.25f,
            1f,
            1.2f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );
    }

    public void FrogIdle()
    {
        bounce.Play(
            UIBounce.Axis.X,
            0.2f,
            0.9f,
            1f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );
    }

    public void FrogSlide()
    {
        bounce.Play(
            UIBounce.Axis.X,
            0.2f,
            0.75f,
            1f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );
    }

    public void FrogDie()
    {
        bounce.Play(
            UIBounce.Axis.Y,
            0.1f,
            1f,
            0.7f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );
    }

    public void FrogTongue()
    {
        bounce.Play(
            UIBounce.Axis.Y,
            0.2f,
            1f,
            1.2f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );
    }
}
