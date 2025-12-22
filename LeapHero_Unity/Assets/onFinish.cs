using UnityEngine;

public class onFinish : StateMachineBehaviour
{
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      ExcalibroController excalibro =
            animator.GetComponent<ExcalibroController>();

        if (excalibro == null) return;

        excalibro.isSpining = false;
        excalibro.isExhausted = true;
    }
}
