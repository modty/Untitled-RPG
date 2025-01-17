using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Golem : NavAgentBoss
{
    bool walkForward;

    float cooldown = 7;
    float defendingDuration = 3;
    float stunDuration = 5;
    float stunHitTime;
    float startedDefending;

    public GameObject rockShard;
    public ParticleSystem weakSpotHitPS;

    float kaiteDuration;
    float maxAllowedKaite = 7;

    [Space]
    public AudioClip weakSpotHitSound;
    public AudioClip hitGroundAttackSound;
    public AudioClip attack1Sound;
    public AudioClip attack2Sound;
    public AudioClip rockShardLoop;
    public AudioClip rockShardExplosion;
    public AudioClip jumpSound;

    bool forceRigidbodyControl;

    protected override void Update() {
        base.Update();

        if (isKnockedDown || isDead || PlayerControlls.instance == null) { //Player instance is null when level is only loading.
            if (navAgent.enabled) navAgent.isStopped = true;
            return;
        }

        if (distanceToPlayer >= 20) {
            attackRange = 20;
            kaiteDuration = 0;
        }

        CheckForKaite();

        isStunned = Time.time - stunHitTime < stunDuration;
        isDefending = (Time.time - startedDefending < defendingDuration) && !isStunned;
        walkForward = !isDefending && !isStunned && (currentState == EnemyState.Approaching || currentState == EnemyState.Returning);

        animator.SetBool("Defend", isDefending);
        animator.SetBool("isStunned", isStunned);
        animator.SetBool("Walk Forward", walkForward);
    }

    void CheckForKaite () {
        if (!agr) {
            kaiteDuration = 0;
            return;
        }
        if (distanceToPlayer < 20 && distanceToPlayer > 4.5) {
            kaiteDuration += Time.deltaTime;
        }
        if (kaiteDuration > maxAllowedKaite) {
            attackRange = 20;
            kaiteDuration = 0;
        }
    }

    protected override void Start()
    {
        base.Start();

        //Doing this cause otherwise it will trigger at start;
        stunHitTime -= stunDuration;
        startedDefending -= defendingDuration;
    }

    protected override void AttackTarget () {
        isAttacking = true;
        
        if (plannedAttack.attackName == "HitGroundAttack") StartCoroutine(HitGroundAttack());
        else if (plannedAttack.attackName == "PunchAttack") StartCoroutine(PunchAttack());
        else if (plannedAttack.attackName == "DoublePunchAttack") StartCoroutine(DoublePunchAttack());
        else if (plannedAttack.attackName == "CastSpellAttack") StartCoroutine(CastSpellAttack());
        else if (plannedAttack.attackName == "JumpAttack") StartCoroutine(JumpAttack());
        UseAttack(plannedAttack);
    }
    IEnumerator JumpAttack() {
        forceRigidbodyControl = true;
        animSwitched = false;
        PlaySound(jumpSound, 0, 1, 0.6f);
        yield return new WaitForSeconds(1);
        enemyController.jump = true;
        jumpDir = (PlayerControlls.instance.transform.position + PlayerControlls.instance.rb.velocity * 0.5f - transform.position).normalized;
        moveToPlayer = true;
        yield return new WaitForSeconds(0.1f);
        enemyController.jump = false;
        while (!enemyController.isGrounded) {
            yield return null;
        }
        moveToPlayer = false;
        HitPlayerOnLanding();
        PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().CameraShake(0.2f, 5f, 0.15f, transform.position);
        PlaySound(attack1Sound);
        yield return new WaitForSeconds(1f);
        forceRigidbodyControl = false;
        isAttacking = false;
    }
    IEnumerator CastSpellAttack() {
        yield return new WaitForSeconds(0.4f);
        ShootRockShard();
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }
    IEnumerator HitGroundAttack() {
        yield return new WaitForSeconds(0.4f);
        PlaySound(hitGroundAttackSound);
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }
    IEnumerator PunchAttack() {
        yield return new WaitForSeconds(0.15f);
        PlaySound(attack1Sound);
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }
    IEnumerator DoublePunchAttack() {
        yield return new WaitForSeconds(0.15f);
        PlaySound(attack1Sound);
        yield return new WaitForSeconds(0.4f);
        PlaySound(attack2Sound);
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    void HitPlayerOnLanding() {
        LayerMask player = LayerMask.GetMask("Player");
        Collider[] findPlayer = Physics.OverlapBox(transform.position + Vector3.up, Vector3.one * 2, transform.rotation, player);
        for (int i = 0; i < findPlayer.Length; i++) {
            if (findPlayer[i] is CapsuleCollider) {
                Hit();
            }
        }
    }

    bool moveToPlayer;
    bool animSwitched;
    Vector3 jumpDir;
    void FixedUpdate() {
        if (moveToPlayer) {
            enemyController.movement.Move(jumpDir * distanceToPlayer * 2, 20, true);
            if (distanceToPlayer <= 10 && !animSwitched) {
                animator.SetTrigger("Land");
                animSwitched = true;
            }
        }
    }

    protected override void ApplyEnemyControllerSettings()
    {
        enemyController.useRootMotion = !forceRigidbodyControl && (isAttacking || isGettingInterrupted);       
        enemyController.useRootMotionRotation = !forceRigidbodyControl && (isAttacking || isGettingInterrupted);       
        enemyController.speed = enemyController.useRootMotion ? baseControllerSpeed * 50 : baseControllerSpeed;
    }

    public void ShootRockShard(){
        Vector3 adjPlayerPos = target.transform.position + PlayerControlls.instance.rb.velocity * 0.5f;
        Quaternion rot = Quaternion.LookRotation(adjPlayerPos - transform.position, Vector3.up);
        GolemRockShard r = Instantiate(rockShard, transform.position, rot, null).GetComponent<GolemRockShard>();
        float dot = Vector3.Dot(PlayerControlls.instance.rb.velocity, transform.forward) * 0.7f;
        r.Init(distanceToPlayer + dot, this);
    }

    protected override void ApproachTarget () {
        base.ApproachTarget();

        navAgent.isStopped = isGettingInterrupted || isDefending || isStunned || isAttacking ? true : false;
        navAgent.destination = target.position;
    }

    protected override bool blockFaceTarget()
    {
        return isAttacking || isStunned || isDefending;
    }

    public override void FootStep()
    {
        base.FootStep();
        PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().CameraShake(0.15f, 2.5f, 0.1f, transform.position);
    }

    public override void Hit () {
        DamageInfo enemyDamageInfo = CalculateDamage.enemyDamageInfo(damage, enemyName);
        PlayerControlls.instance.GetComponent<Characteristics>().GetHit(enemyDamageInfo, hitType, 0.2f, 2.5f);
    }

    public override void GetHit(DamageInfo damageInfo, bool stopHit = false, bool cameraShake = false, HitType hitType = HitType.Normal, Vector3 damageTextPos = default, float kickBackStrength = 50)
    {
        DamageInfo adjForDefenseDI = damageInfo;
        adjForDefenseDI.damage = Mathf.RoundToInt(adjForDefenseDI.damage * (isDefending ? 0.05f : 1));
        base.GetHit(adjForDefenseDI, stopHit, cameraShake, hitType, damageTextPos, kickBackStrength);
        if (!isStunned) startedDefending = Time.time;
    }

    public override void OnWeakSpotHit()
    {
        base.OnWeakSpotHit();
        if (isStunned) return;

        PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().CameraShake(0.15f, 1.5f, 0.1f, PlayerControlls.instance.transform.position);
        StartCoroutine(HitStop(true));
        PlaySound(weakSpotHitSound);
        weakSpotHitPS.Play();
        stunHitTime = Time.time;
    }

    public void ShakeCamera () {
        PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().CameraShake(0.15f, 2.5f, 0.1f, transform.position);
    }

    protected override void SyncAnimator() {
       
    }
}
