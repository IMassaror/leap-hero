using UnityEngine;
using ChristinaCreatesGames.Animations;

public class SquashStretchController : MonoBehaviour
{
    private SquashAndStretch squash;

    void Start()
    {
        squash = GetComponentInChildren<SquashAndStretch>();    
    }

    public void DoJump()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.Y, 
        0.2f,
        1f,
        1.3f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoDoubleJump()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.Y, 
        0.25f,
        1f,
        1.4f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoWallJump()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.X, 
        0.2f,
        1f,
        1.3f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoLand()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.Y, 
        0.2f,
        1f,
        0.75f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoTongue()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.Y, 
        0.15f,
        1f,
        0.8f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoWallGrab()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.X, 
        0.2f,
        1f,
        0.9f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoAttack()
    {
      squash.Setup(
        SquashAndStretch.SquashStretchAxis.Y, 
        0.15f,
        1f,
        0.8f, 
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        false, 
        0f, 
        true
      );
    }

    public void DoDash()
      {
        squash.Setup(
          SquashAndStretch.SquashStretchAxis.X, 
          0.2f,
          1f,
          1.3f, 
          AnimationCurve.EaseInOut(0, 0, 1, 1),
          false, 
          0f, 
          true
        );
      }
  }
