using UnityEngine;
using ChristinaCreatesGames.Animations;

public class SquashStretchController : MonoBehaviour
{
    [Header("Visual Target (Objeto que tem o SquashAndStretch)")]
    public SquashAndStretch squashAndStretch;

    [Header("Configurações por Ação")]

    public AnimationCurve jumpCurve;
    public float jumpDuration = 0.15f;
    public float jumpZero = 1f;
    public float jumpOne = 1.25f;

    public AnimationCurve landCurve;
    public float landDuration = 0.20f;
    public float landZero = 1.2f;
    public float landOne = 0.8f;

    public AnimationCurve dashCurve;
    public float dashDuration = 0.12f;
    public float dashZero = 1f;
    public float dashOne = 1.35f;

    public AnimationCurve wallJumpCurve;
    public float wallJumpDuration = 0.18f;
    public float wallJumpZero = 1f;
    public float wallJumpOne = 1.20f;

    public AnimationCurve crouchCurve;
    public float crouchDuration = 0.25f;
    public float crouchZero = 1f;
    public float crouchOne = 0.7f;

    private void Reset()
    {
        squashAndStretch = GetComponentInChildren<SquashAndStretch>();
    }

    // ------------------------------
    // AÇÕES QUE O PLAYER CHAMA
    // ------------------------------

    public void DoJump()
    {
        squashAndStretch.Setup(
            SquashAndStretch.SquashStretchAxis.Y,
            jumpDuration,
            jumpZero,
            jumpOne,
            jumpCurve,
            false,
            0f,
            true
        );
    }

    public void DoLand()
    {
        squashAndStretch.Setup(
            SquashAndStretch.SquashStretchAxis.Y,
            landDuration,
            landZero,
            landOne,
            landCurve,
            false,
            0f,
            true
        );
    }

    public void DoDash()
    {
        squashAndStretch.Setup(
            SquashAndStretch.SquashStretchAxis.X,
            dashDuration,
            dashZero,
            dashOne,
            dashCurve,
            false,
            0f,
            true
        );
    }

    public void DoWallJump()
    {
        squashAndStretch.Setup(
            SquashAndStretch.SquashStretchAxis.Y,
            wallJumpDuration,
            wallJumpZero,
            wallJumpOne,
            wallJumpCurve,
            false,
            0f,
            true
        );
    }

    public void DoCrouch()
    {
        squashAndStretch.Setup(
            SquashAndStretch.SquashStretchAxis.Y,
            crouchDuration,
            crouchZero,
            crouchOne,
            crouchCurve,
            false,
            0f,
            true
        );
    }
}