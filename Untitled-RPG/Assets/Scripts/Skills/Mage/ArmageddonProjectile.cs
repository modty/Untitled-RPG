﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmageddonProjectile : MonoBehaviour
{
    public float speed;
    public DamageInfo damageInfo;

    public ParticleSystem hitParticles;

    public AudioClip[] explosionSounds;

    List<Enemy> enemiesHit = new List<Enemy>();

    void Start() {
        GetComponent<Rigidbody>().AddForce(Vector3.down * speed, ForceMode.Impulse);
        Destroy(gameObject, 2);
    }

    void OnTriggerEnter(Collider other) {
        Enemy en = other.transform.GetComponentInParent<Enemy>();
        if (other.isTrigger || other.CompareTag("Player") || en == null)
            return;
        
        if (!enemiesHit.Contains(en)) {
            en.GetHit(damageInfo, "Armageddon", false, false, HitType.Knockdown);
            enemiesHit.Add(en);
        }

        hitParticles.Play();
        PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().CameraShake(0.2f, 2*(1+damageInfo.damage/2000), 0.2f, transform.position);
        GetComponent<AudioSource>().clip = explosionSounds[Random.Range(0, explosionSounds.Length)];
        GetComponent<AudioSource>().Play();
    }
}
