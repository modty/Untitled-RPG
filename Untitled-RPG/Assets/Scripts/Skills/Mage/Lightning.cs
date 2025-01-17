﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lightning : Skill
{
    [Header("Custom vars")]
    public float distance;
    public int maxShots;
    public GameObject lightningProjectile;
    public Sprite[] skillIcons;

    Transform[] hands;

    Vector3 shootPoint;
    int shots;
    float lastShotTime;
    bool continueShooting;

    public AudioClip[] sounds;
    public GameObject handsVFX;

    protected override void Start() {
        base.Start();
        hands = new Transform[2];
        hands[0] = PlayerControlls.instance.leftHandWeaponSlot;
        hands[1] = PlayerControlls.instance.rightHandWeaponSlot;
    }

    protected override void LocalUse () {
        playerControlls.InterruptCasting();
        //playerControlls.GetComponent<Characteristics>().UseOrRestoreStamina(staminaRequired);
        CustomUse();
        if (weaponOutRequired && !WeaponsController.instance.isWeaponOut)
                WeaponsController.instance.InstantUnsheathe();
    }
    
    protected override void Update() {
        if (PeaceCanvas.instance.isGamePaused)
            return;

        if (coolDownTimer >= 0) {
            coolDownTimer -= Time.deltaTime;
            isCoolingDown = true;
        } else {
            isCoolingDown = false;
        }

        if (shots >= maxShots)
            Reset();

        if(Time.time - lastShotTime >= 2 && shots != 0 && !isCoolingDown) {
            Reset();
        }

        if (Time.time - lastShotTime >= 0.4f && !continueShooting) {
            playerControlls.isAttacking = false;
            continueShooting = true;
        }

        icon = skillIcons[shots];
    }

    protected override void CustomUse () {
        if (Time.time - lastShotTime >= 0.4f)
            ShootLightning();
    }  
    
    void ShootLightning () {
        playerControlls.playerCamera.GetComponent<CameraControll>().isShortAiming = true;
        shots++;
        continueShooting = false;
        playerControlls.isAttacking = true;
        PlayAnimation();

        lastShotTime = Time.time;
        float actualDistance = distance + characteristics.skillDistanceIncrease;

        RaycastHit hit;
        if (Physics.Raycast(PlayerControlls.instance.playerCamera.transform.position, PlayerControlls.instance.playerCamera.transform.forward, out hit, actualDistance, ~LayerMask.GetMask("Player"))) {
            shootPoint = hit.point;        
        } else {
            shootPoint = PlayerControlls.instance.playerCamera.transform.forward * (actualDistance + PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().camDistance) + PlayerControlls.instance.playerCamera.transform.position;
        }

        GameObject go = Instantiate(lightningProjectile, shootPoint, Quaternion.identity);
        go.GetComponent<LightningProjectile>().damageInfo = CalculateDamage.damageInfo(damageType, Mathf.RoundToInt( baseDamagePercentage * (1+shots/10f)), skillName);
        go.GetComponent<AudioSource>().clip = sounds[shots-1];
        switch (shots) {
            case 1: go.GetComponent<AudioSource>().time = 0.8f; //1
                break;
            case 2: go.GetComponent<AudioSource>().time = 0.6f; //3
                break;
            case 3: go.GetComponent<AudioSource>().time = 0.5f; //4
                break;
            case 4: go.GetComponent<AudioSource>().time = 0.4f; //5
                break;
            case 5: go.GetComponent<AudioSource>().time = 0.5f; //6
                break;
        }
        PlayParticles();
        go.SetActive(true);
    }

    void Reset () {
        coolDownTimer = coolDown;
        shots = 0;
        icon = skillIcons[0];
        playerControlls.playerCamera.GetComponent<CameraControll>().isShortAiming = false;
    }

    void PlayAnimation () {
        if (!playerControlls.isFlying) {
            if (shots == 1 || shots == 3 || shots == 5) 
                animator.CrossFade("AttacksUpperBody.Mage.Lightning_right", 0.25f);
            else 
                animator.CrossFade("AttacksUpperBody.Mage.Lightning_left", 0.25f);
        } else {
            if (shots == 1 || shots == 3 || shots == 5) 
                animator.CrossFade("Attacks.Mage.Lightning_flying_right", 0.25f);
            else 
                animator.CrossFade("Attacks.Mage.Lightning_flying_left", 0.25f);
        }
    }

    void PlayParticles (){
        int hand = 0;
        if (shots == 1 ||shots == 3 || shots == 5)
            hand = 1;
        GameObject ps = Instantiate(handsVFX, hands[hand]);
        ps.SetActive(true);
        Destroy(ps, 1);
    }

    public override string getDescription()
    {
        DamageInfo dmg = CalculateDamage.damageInfo(damageType, baseDamagePercentage, skillName, 0, 0);
        return $"Shoot up to {maxShots} lightnings in short successions. Starting at {dmg.damage} {dmg.damageType} damage, each following lightning deals more damage.";
    }
}
