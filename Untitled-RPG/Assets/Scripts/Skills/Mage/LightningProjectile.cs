﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningProjectile : MonoBehaviour
{
    public int actualDamage;
    public int shots;

    void Start() {
        Invoke("EnableCollider", 0.1f);
        Destroy(gameObject, 1f);
    }

    int damage () {
        return Mathf.RoundToInt(Random.Range(actualDamage*0.85f, actualDamage*1.15f));
    }  

    void OnTriggerEnter(Collider other) {
        if (other.isTrigger || other.CompareTag("Player"))
            return;
        
        if (other.gameObject.GetComponent<Enemy>() != null) {
            other.GetComponent<Enemy>().GetHit(damage(), "Lightning", false, true, transform.position);
        }
    }

    void EnableCollider () {
        GetComponent<SphereCollider>().enabled = true;
        PlayerControlls.instance.playerCamera.GetComponent<CameraControll>().CameraShake(0.2f, 2*(1+actualDamage/2000), 0.1f, transform.position);
    }
}
