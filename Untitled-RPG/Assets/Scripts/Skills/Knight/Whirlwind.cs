﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Whirlwind : Skill
{
    List<IDamagable> damagablesInTrigger = new List<IDamagable>();

    [Header("CustomVars")]
    public float duration;
    public AudioClip start;
    public AudioClip loop;

    protected override void CustomUse() {
        StartCoroutine(Using());
    }

    protected override void Update() {
        base.Update();
        ClearTrigger();
    }
    bool wasFlying;
    IEnumerator Using () {
        Combat.instanace.blockSkills = true;

        if (WeaponsController.instance.bothHandsStatus == BothHandsStatus.TwoHanded)
            animator.CrossFade("Attacks.Knight.Whirlwind Two handed", 0.25f);
        else if (WeaponsController.instance.bothHandsStatus == BothHandsStatus.DualOneHanded) 
            animator.CrossFade("Attacks.Knight.Whirlwind Dual swords", 0.25f);
        else
            animator.CrossFade("Attacks.Knight.Whirlwind Dual swords", 0.25f);

        PlaySound(start, 0, characteristics.attackSpeed.x, 0.1f);

        if (WeaponsController.instance.bothHandsStatus == BothHandsStatus.TwoHanded) {
            while (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Attacks")). IsName("Whirlwind_loop Two handed")) {
                yield return null;
            }
        } else if (WeaponsController.instance.bothHandsStatus == BothHandsStatus.DualOneHanded) {
            while (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Attacks")). IsName("Whirlwind_loop Dual swords")) {
                yield return null;
            }
        } else {
            while (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Attacks")). IsName("Whirlwind_loop Dual swords")) {
                yield return null;
            }
        }        

        if (!playerControlls.isFlying) {
            playerControlls.forceRigidbodyMovement = true;
            //I was setting speed/movement here before shifring to rigidbody
        } else {
            wasFlying = true;
        }

        PlaySound(loop);

        Characteristics.instance.immuneToDamage = true;
        Characteristics.instance.immuneToInterrupt = true;
        
        float timer = duration;
        float hitTimer = 0;
        while (timer > 0) {
            timer -= Time.fixedDeltaTime;
            playerControlls.characterController.speedMultiplier = 1;
            if (hitTimer <= 0) {
                hitTimer = 0.2f * characteristics.attackSpeed.y;
                Hit();
            } else {
                hitTimer -= Time.fixedDeltaTime;
            }
            //if was flying but the flight is over midway
            if (!playerControlls.isFlying && wasFlying == true) {
                //I was setting speed/movement here before shifring to rigidbody
                wasFlying = false;
            }
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        
        if (WeaponsController.instance.bothHandsStatus == BothHandsStatus.TwoHanded)
            animator.CrossFade("Attacks.Knight.Whirlwind_end Two handed", 0.25f);
        else if (WeaponsController.instance.bothHandsStatus == BothHandsStatus.DualOneHanded) 
            animator.CrossFade("Attacks.Knight.Whirlwind_end Dual swords", 0.25f);
        else
            animator.CrossFade("Attacks.Knight.Whirlwind_end Dual swords", 0.25f);

        playerControlls.forceRigidbodyMovement = false;
        Characteristics.instance.immuneToDamage = false;
        Characteristics.instance.immuneToInterrupt = false;
        Combat.instanace.blockSkills = false;

        audioSource.Stop();
    }   

    void OnTriggerStay(Collider other) {
        IDamagable en = other.transform.GetComponentInParent<IDamagable>();
        if (en == null || other.isTrigger)
            return;

        if (!damagablesInTrigger.Contains(en)) damagablesInTrigger.Add(en);
    }
    void OnTriggerExit(Collider other) {
        IDamagable en = other.transform.GetComponentInParent<IDamagable>();
        if (en == null || other.isTrigger)
            return;

        if (damagablesInTrigger.Contains(en)) damagablesInTrigger.Remove(en);
    }

    public void Hit () {
        for (int i = 0; i < damagablesInTrigger.Count; i++) {
            damagablesInTrigger[i].GetHit(CalculateDamage.damageInfo(damageType, baseDamagePercentage, skillName), false, true, HitType.Interrupt);
        }
    }

    void ClearTrigger () {
        for (int i = 0; i < damagablesInTrigger.Count; i++) {
            if (damagablesInTrigger[i] == null) {
                damagablesInTrigger.RemoveAt(i);
            }
        }
    }

    public override string getDescription()
    {
        DamageInfo dmg = CalculateDamage.damageInfo(damageType, baseDamagePercentage, skillName, 0, 0);
        return $"Spin your sword for {duration} seconds, dealing {dmg.damage} {dmg.damageType} damage to everyone around.";
    }

}
