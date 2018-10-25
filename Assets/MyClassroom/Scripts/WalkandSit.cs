using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkandSit : NPCBaseFSM {

    GameObject [] chairs;

    void Awake()
    {
        chairs = GameObject.FindGameObjectsWithTag("chair");
    }

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (chairs.Length == 0) return;
        if (Vector3.Distance(chairs[currentChair].transform.position, NPC.transform.position) < accuracy)
        {

            currentChair++;
            if (currentChair >= chairs.Length)
            {
                currentChair = 0;
            }

        }

        //rotate toward target
        var direction = chairs[currentChair].transform.position - NPC.transform.position;
        NPC.transform.rotation = Quaternion.Slerp(NPC.transform.rotation, Quaternion.LookRotation(direction), rotSpeed * Time.deltaTime);
        NPC.transform.Translate(0, 0, Time.deltaTime * speed);
    }

    
    

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	
	}

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
}
