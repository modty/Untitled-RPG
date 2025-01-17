﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimStates {upperBodyLayerAnimRest, layerAnimRest, treeAnimRest, upperBodyTreeAnimRest}

public class IsAttackingCheck : StateMachineBehaviour
{
    public bool Player;
    public bool Enemy;

    public AnimStates animState;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) { //This was Update, i dont remember why but i remember it was important
        if (Player){
            animator.gameObject.GetComponent<PlayerControlls>().isAttacking = false;
        } else {
            animator.gameObject.GetComponent<Enemy>().isAttacking = false;
        }
    }
    override public void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) {
        if (Player){
            SetAnimBool(false);
            
            if (!PlayerControlls.instance.isCasting && !PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().isAiming)
                PlayerControlls.instance.isAttacking = true;
        } else {
            animator.gameObject.GetComponent<Enemy>().isAttacking = true;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!Player)
            return;
            
        SetAnimBool(true);
    }

    public void SetAnimBool (bool _true) {
        switch (animState)
        {
            case AnimStates.upperBodyLayerAnimRest: PlayerControlls.instance.upperBodyLayerAnimRest = _true ? true : false;
                break;
            case AnimStates.layerAnimRest: PlayerControlls.instance.layerAnimRest = _true ? true : false;
                break;
            case AnimStates.treeAnimRest: PlayerControlls.instance.treeAnimRest = _true ? true : false;
                break;
            case AnimStates.upperBodyTreeAnimRest: PlayerControlls.instance.upperBodyTreeAnimRest = _true ? true : false;
                break;
        }
    }
}
