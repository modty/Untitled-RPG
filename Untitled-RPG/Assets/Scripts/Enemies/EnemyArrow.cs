using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyArrow : MonoBehaviour
{    
    public float lifeTime = 20;
    public float gravityScale = 0.4f;
    public float gravityDelay = 0.2f;
    public TrailRenderer trail;
    public GameObject mesh;
    public HitType hitType = HitType.Normal;
    public AudioClip[] hitSounds;

    protected float timeShot;
    protected bool shot = false;
    protected bool applyGravity;

    protected Rigidbody rb;
    protected DamageInfo damageInfo;
    protected AudioSource audioSource;

    protected virtual void Start() {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rb.useGravity = false;
    }

    protected virtual void Update() {
        if (shot && rb.velocity.magnitude > 0)
            transform.rotation = Quaternion.LookRotation(rb.velocity, Vector3.up);
        
        if (shot && Time.time - timeShot >= lifeTime) {
            Destroy(gameObject);
        } else if (shot && Time.time - timeShot >= gravityDelay) {
            applyGravity = true;
        }
    }

    protected virtual void FixedUpdate() {
        if (applyGravity)
            rb.AddForce(Physics.gravity * gravityScale);
    }

    public virtual void Shoot (float _strength, Vector3 _shotPoint, DamageInfo _damageInfo) {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        damageInfo = _damageInfo;
        Vector3 direction = _shotPoint - transform.position; 

        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.AddForce(_strength * direction.normalized, ForceMode.Impulse);
        transform.SetParent(null);

        timeShot = Time.time;
        shot = true;
        trail.emitting = true;
    }

    protected virtual void OnTriggerEnter(Collider other) {
        if (!shot || other.isTrigger || other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            return;
        
        Collision(other.transform);

        if (other.CompareTag("Player") && other.GetType() == typeof(CapsuleCollider)) { 
            PlayerControlls.instance.GetComponent<Characteristics>().GetHit(damageInfo, hitType, 0.2f, 1.5f);
            mesh.gameObject.SetActive(false);
            Destroy(gameObject,1);
        }
    }

    protected virtual void Collision (Transform collisionObj) {
        if (!shot)
            return;
        transform.position -= rb.velocity * Time.fixedDeltaTime;
        transform.SetParent(collisionObj);
        
        rb.isKinematic = true;
        
        GetComponent<CapsuleCollider>().enabled = false;
        trail.emitting = false;
        audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length-1)]);
    }
}
