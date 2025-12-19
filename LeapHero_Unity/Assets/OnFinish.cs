using UnityEngine;

public class OnFinish : StateMachineBehaviour
{
    [SerializeField] private string animation;

    override public void OnStateEnter(Animator anim, AnimatorStateInfo stateInfo, int layerIndex)
    {
         anim.GetComponentInParent<PlayerController>().UpdateAnimations(animation, 0.2f, stateInfo.length);
    }
}
