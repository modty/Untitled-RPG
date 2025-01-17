﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firewall : Skill
{
    [Header("Custom vars")]
    public float duration = 20;
    public GameObject firewall;
    public AudioClip castingSound;
    Vector3 spawnPos;

    Transform[] hands;
    public ParticleSystem handsEffect;
    List<ParticleSystem> spawnedPS = new List<ParticleSystem>();

    protected override void Start() {
        base.Start();
        hands = new Transform[2];
        hands[0] = PlayerControlls.instance.leftHandWeaponSlot;
        hands[1] = PlayerControlls.instance.rightHandWeaponSlot;
    }

    protected override void CustomUse() {
        if (playerControlls.isFlying)
            animator.CrossFade("Attacks.Mage.Firewall_flying", 0.25f);
        else
            animator.CrossFade("Attacks.Mage.Firewall", 0.25f);
        
        PlaySound(castingSound, 0, characteristics.castingSpeed.x);

        Invoke("Spawn", 1.6f * 0.5f * characteristics.attackSpeed.y);

        for (int i = 0; i < hands.Length; i++) {
            ParticleSystem ps = Instantiate(handsEffect, hands[i]);
            ps.gameObject.SetActive(true);
            spawnedPS.Add(ps);
        }
    }

    void Spawn () {
        playerControlls.playerCamera.GetComponent<CameraControll>().CameraShake(0.2f, 0.15f);
        
        spawnPos = playerControlls.transform.position + playerControlls.transform.forward * 2 + Vector3.up - playerControlls.transform.right * 4.5f;
        
        RaycastHit hit;
        Vector3 origin = spawnPos + playerControlls.transform.right * 4.5f;
        if(Physics.Raycast(origin, Vector3.down, out hit, 3)) {
            spawnPos.y = hit.point.y + 0.7f;
        } 

        FirewallWall fw = Instantiate(firewall, spawnPos, Quaternion.LookRotation(playerControlls.transform.forward, Vector3.up)).GetComponent<FirewallWall>();
        fw.duration = duration;
        fw.skill = this;
        fw.gameObject.SetActive(true);

        DeleteEffects();
    }

    void DeleteEffects () {
        int x = spawnedPS.Count;
        for(int i = 0; i < x; i++) {
            spawnedPS[0].Stop();
            Destroy(spawnedPS[0].gameObject, 1f);
            spawnedPS.RemoveAt(0);
        }
    }

    public override string getDescription()
    {
        DamageInfo dmg = CalculateDamage.damageInfo(damageType, baseDamagePercentage, skillName, 0, 0);
        return $"Create a wall of fire in front, blocking enemies and dealing {dmg.damage} {dmg.damageType} damage.";
    }
}
