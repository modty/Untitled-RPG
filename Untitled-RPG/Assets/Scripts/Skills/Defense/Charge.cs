using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Charge : Skill
{
    [Header("Custom vars")]
    public float duraiton;
    public GameObject VFX;

    public ParticleSystem sprintingTrails;
    public ParticleSystem lastParticles;
    public ParticleSystem hitVFX;
    public AudioClip soundFX;
    
    bool charging;

    List<IDamagable> damagablesHit = new List<IDamagable>();
    Collider hitCollider;

    protected override void CustomUse()
    {
        hitCollider = GetComponent<Collider>();
        StartCoroutine(Using());
    }

    IEnumerator Using () {
        animator.CrossFade("Attacks.Defense.Charge", 0.4f);
        PlaySound(soundFX, 0, 1, 0, 1);
        PlayerAudioController.instance.PlayPlayerSound(PlayerAudioController.instance.sprint, 0.05f, 1.7f);
        
        damagablesHit.Clear();
        
        Combat.instanace.blockSkills = true;
        characteristics.immuneToDamage = true;
        characteristics.immuneToInterrupt = true;
        playerControlls.isAttacking = true;
        charging = true;
        hitCollider.enabled = true;
        playerControlls.characterController.speedMultiplier = 10;
        sprintingTrails.Play();

        VFX.SetActive(true);
        Material vfxMat = VFX.transform.GetChild(0).GetComponent<MeshRenderer>().material;
        vfxMat.SetFloat("Progress", 0);
        vfxMat.DOFloat(1, "Progress", 2);
        float timeStarted = Time.time;
        float cleanListTimer = Time.time;
        while (Time.time - timeStarted < duraiton) {
            if (Time.time - cleanListTimer > 1) {
                damagablesHit.Clear();
                cleanListTimer = Time.time;
            }
            yield return null;
        }
        playerControlls.characterController.speedMultiplier = 1;
        
        vfxMat.DOFloat(0, "Progress", 0.5f);
        animator.CrossFade("Attacks.Defense.Empty", 0.4f);
        
        sprintingTrails.Stop();
        characteristics.immuneToDamage = false;
        characteristics.immuneToInterrupt = false;
        charging = false;
        playerControlls.isAttacking = false;
        hitCollider.enabled = false;
        yield return new WaitForSeconds(0.5f);
        lastParticles.Play();
        Combat.instanace.blockSkills = false;
        yield return new WaitForSeconds(0.7f);
        VFX.SetActive(false);
    }

    void OnTriggerEnter(Collider other) {
        if(!charging)
            return;

        IDamagable en = other.GetComponentInParent<IDamagable>();
        if (en == null || damagablesHit.Contains(en))
            return;

        en.GetHit(CalculateDamage.damageInfo(damageType, baseDamagePercentage, skillName), false, false, HitType.Kickback, new Vector3(), 30);
        damagablesHit.Add(en);

        MonoBehaviour mb = en as MonoBehaviour;
        ParticleSystem ps = Instantiate(hitVFX, mb.transform.position + Vector3.up * 1.2f, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, 1);
    }

    public override bool skillActive()
    {
        if (WeaponsController.instance.leftHandStatus != SingleHandStatus.Shield)
            return false;
        return base.skillActive();
    }

    public override string getDescription()
    {
        DamageInfo dmg = CalculateDamage.damageInfo(damageType, baseDamagePercentage, skillName, 0, 0);
        return $"Charge through enemies for {duraiton} seconds, dealing {dmg.damage} {dmg.damageType} damage.\n\nSkill damage depends on your defense and you cannot recieve damage while charging.\n\nShield is required.";
    }
}
