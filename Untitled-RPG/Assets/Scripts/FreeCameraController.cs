﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;

public class FreeCameraController : MonoBehaviour
{
    public float speed = 1;
    Vector3 move;
    
    float xRotation = 0f;
    float yRotation = 0f;

    void OnEnable() {
        xRotation = transform.rotation.eulerAngles.x;
        yRotation = transform.rotation.eulerAngles.y;
    }

    void Update () {
        move = (Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("QE") * transform.up )* speed / 10f;
        transform.Translate(move, Space.World);

        float mouseX = Input.GetAxis("Mouse X") * SettingsManager.instance.mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * SettingsManager.instance.mouseSensitivity * Time.deltaTime * (SettingsManager.instance.invertY ? 1 : -1);

        xRotation += mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.LeftControl)){
            speed *= 4;
        } else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.LeftControl)) {
            speed *= 0.25f;
        }

        speed += Input.mouseScrollDelta.y * 0.5f;
        speed = Mathf.Clamp(speed, 0, Mathf.Infinity);
    }
}
