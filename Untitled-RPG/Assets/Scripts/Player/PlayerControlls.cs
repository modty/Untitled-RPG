﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using ECM.Controllers;
using UnityEngine.SceneManagement;
using MalbersAnimations.HAP;
using NaughtyAttributes;

public class PlayerControlls : MonoBehaviour, ISavable
{
    public static PlayerControlls instance;

    [Header("Movement")]
    public float walkSpeed;
    public bool toggleRunning = false;
    int staminaReqToRoll = 50;
    int staminaReqToSprint = 1;

    [Foldout("States"), ReadOnly] public bool isIdle;
    [Foldout("States"), ReadOnly] public bool isRunning;
    [Foldout("States"), ReadOnly] public bool isSprinting;
    [Foldout("States"), ReadOnly] public bool isCrouch;
    [Foldout("States"), ReadOnly] public bool isJumping;
    [Foldout("States"), ReadOnly] public bool isRolling;
    [Foldout("States"), ReadOnly] public bool isMounted;
    [Foldout("States"), ReadOnly] public bool isAttacking;
    [Foldout("States"), ReadOnly] public bool isGettingHit;
    [Foldout("States"), ReadOnly] public bool isGrounded;
    [Foldout("States"), ReadOnly] public bool isCasting;
    [Foldout("States"), ReadOnly] public bool isFlying;
    [Foldout("States"), ReadOnly] public bool isPickingArea;
    [Foldout("States"), ReadOnly] public bool isAimingSkill;

    [Space]
    public bool isSitting;
    [Header("Battle")]
    public bool inBattle;
    public float inBattleExitTime = 7; //How long after exisiting the battle will the player remain "in battle"
    float inBattleTimer;
    public bool attackedByEnemies;

    [Space]
    public bool castInterrupted;
    public bool disableControl {
        get {
            return disableControlRequests > 0;
        }
    }
    [Space]
    public int disableControlRequests;
    [SerializeField, DisplayWithoutEdit] bool overridePosRot;
    [SerializeField, ReadOnly, ShowIf("overridePosRot")] Vector3 overridePos;
    [SerializeField, ReadOnly, ShowIf("overridePosRot")] float overrideRotAngle;
    Transform overrideTransform;

    float sprintingDirection;
    float desiredSprintingDirection;

    [System.NonSerialized] public float desiredLookDirection; //Accessed by M_Rider fix rotation when on animal
    [System.NonSerialized] public float lookDirection; 

    float rollDirection;
    float desiredRollDirection;


    [System.NonSerialized] public MRider rider;
    [System.NonSerialized] public Camera playerCamera;
    [System.NonSerialized] public Rigidbody rb;
    [System.NonSerialized] public CameraControll cameraControl;
    [System.NonSerialized] public CinemachineFreeLook CM_Camera;
    [System.NonSerialized] public PlayerAudioController audioController;
    [System.NonSerialized] public PlayerCharacterController characterController;

    [Space]
    public bool forceRigidbodyMovement; //when TRUE rootAnimations will be ignored;

    [Header("Bodypart roots")]
    public Transform leftFootRoot;
    public Transform rightFootRoot;
    public Transform leftHandRoot;
    public Transform rightHandRoot;
    public Transform leftHandWeaponSlot;
    public Transform rightHandWeaponSlot;
    public Transform chestTransform;
    public ParticleSystem sprintTrails;
    public SkinnedMeshRenderer skinnedMesh;
    [System.NonSerialized] public Animator animator;

    SittingSpot currentSittingSpot;

    [Foldout("Animation states")] public bool upperBodyLayerAnimRest;
    [Foldout("Animation states")] public bool layerAnimRest;
    [Foldout("Animation states")] public bool treeAnimRest;
    [Foldout("Animation states")] public bool upperBodyTreeAnimRest;

    float startedHoldingKeyTime;
    bool holdingButton;

    void Awake() {
        if (instance == null) 
            instance = this;
        
        rider = GetComponent<MRider>();
        animator = GetComponent<Animator>();
        audioController = GetComponent<PlayerAudioController>();
        characterController = GetComponent<PlayerCharacterController>();
        rb = GetComponent<Rigidbody>();
        
        playerCamera = Camera.main;
        cameraControl = playerCamera.GetComponent<CameraControll>();
        CM_Camera = playerCamera.GetComponent<CameraControll>().CM_cam;
    }

    void Start()
    {
        walkSpeed = Characteristics.instance.walkSpeed;
        SaveManager.instance.saveObjects.Add(this);
    }

    void Update()
    {
        isGrounded = characterController.isGrounded; //IsGrounded(); LEGACY
        isJumping = characterController.isJumping;
        isMounted = rider.Mounted;

        if (!isMounted && !isFlying && !disableControl) {
            GroundMovement();
            GroundAnimations();
        } else if (isFlying && !disableControl) {
            FlyingAnimations();
        }
        Sprinting();
        SetAnimatorVarialbes();
        InputMagnitudeFunc();
        CheckIsAttacking();
        InBattleCheck();
        PushEnemies();

        if (Input.GetKeyDown(KeybindsManager.instance.currentKeyBinds["Toggle walk"]))
            toggleRunning = !toggleRunning;

        MountingInput();

        if ( (!Input.GetButton("Horizontal") && !Input.GetButton("Vertical") && !isAttacking) || isSitting) {
            isIdle = true;
        } else {
            isIdle = false;
        }

        //Smooth rotation after rolling
        if (Mathf.Abs(rollDirection - desiredRollDirection) > 1) {
            rollDirection = Mathf.Lerp(rollDirection, desiredRollDirection, Time.deltaTime * 15);
        } else {
            rollDirection = desiredRollDirection;
        }

        //Smooth rotation sprinting
        //LerpAngle since need to go from -179 to 179 smoothely
        if (Mathf.Abs(sprintingDirection - desiredSprintingDirection) > 1) {
            sprintingDirection = Mathf.LerpAngle(sprintingDirection, desiredSprintingDirection, Time.deltaTime * 15);
        } else {
            sprintingDirection = desiredSprintingDirection;
        }

        //Smooth rotation after idle
        if (Mathf.Abs(lookDirection - desiredLookDirection) > 1) {
            lookDirection = Mathf.LerpAngle(lookDirection, desiredLookDirection, Time.deltaTime * 15);
        } else {
            lookDirection = desiredLookDirection;
        }

    }
    
    void MountingInput () {
        if (isFlying) return;
        if (Input.GetKeyDown(KeybindsManager.instance.currentKeyBinds["Interact"])) {
            if (!isMounted) {
                holdingButton = false;
                rider.MountAnimal();
                SprintOff();
            } else {
                startedHoldingKeyTime = Time.time;
                holdingButton = true; 
                PeaceCanvas.instance.ShowKeySuggestion(KeyCodeDictionary.keys[KeybindsManager.instance.currentKeyBinds["Interact"]], InterractionIcons.Horse);
            }
        }
        
        if (!isMounted) return;

        if (Input.GetKeyUp(KeybindsManager.instance.currentKeyBinds["Interact"])){
            holdingButton = false;
            PeaceCanvas.instance.HideKeySuggestion();
        }
        if (Time.time - startedHoldingKeyTime > 0.5f && isMounted && holdingButton) {
            rider.DismountAnimal();
            holdingButton = false;
            PeaceCanvas.instance.HideKeySuggestion();
        }
        if (holdingButton) PeaceCanvas.instance.UpdateKeySuggestionProgress( (Time.time - startedHoldingKeyTime) / 0.5f );
    }

#region Ground movement
    float runningStaminaTimer;
    void GroundMovement()
    {
        //if (Input.GetButtonDown("Jump"))  //JUMPING IS NOW FONE THROUGH "BASE CHARACTER CONTROLLER"
            //Jump();

        if (Input.GetKeyDown(KeybindsManager.instance.currentKeyBinds["Crouch"]))
            Crouch();

        if (Input.GetKeyDown(KeybindsManager.instance.currentKeyBinds["Roll"]))
            Roll();

        if (!isIdle) {
            if (Input.GetKey(KeybindsManager.instance.currentKeyBinds["Run"]))
            {
                if (!toggleRunning)
                    isRunning = true;
                else if (!isCrouch && !isAttacking && !isRolling && !isSitting)
                    isSprinting = true; //Start sprinting
            } else if (Input.GetKeyUp(KeybindsManager.instance.currentKeyBinds["Run"]))
            {
                if (toggleRunning && !isRolling) {
                    isSprinting = false;
                } else {
                    isRunning = false;
                }
            } else {
                if (!isRolling) {
                    isSprinting = false;
                }
                if (toggleRunning){
                    isRunning = true;
                } else {
                    isRunning = false;
                }
            }
        } else {
            if (!isRolling)
                isSprinting = false;
        }

        if( (Characteristics.instance.stamina <= 0 || !Characteristics.instance.canUseStamina) && !isRolling) {
            isSprinting = false;
        }

        if (isSprinting) {
            if (InputDirection > 0) {
                //sprintingDirection = InputDirection;
                desiredSprintingDirection = InputDirection;
            } else {
                //sprintingDirection = 360 + InputDirection;
                desiredSprintingDirection =  InputDirection;
            }

            if (Time.time - runningStaminaTimer >= 0.02f) {
                Characteristics.instance.UseOrRestoreStamina(staminaReqToSprint);
                runningStaminaTimer = Time.time;
            }
        } else {
            // sprintingDirection = 0;
            desiredSprintingDirection = 0;
        }

        if (isAttacking || isSitting) {
            SprintOff();
        }
    }

    void FixedUpdate() {
        UpdateRotation();
    }

    void UpdateRotation () {
        
        if (rider.IsMountingDismounting) {
            desiredLookDirection = transform.eulerAngles.y;
            return;
        } 

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, lookDirection + sprintingDirection + rollDirection, transform.eulerAngles.z);
        
        if ( (!PeaceCanvas.instance.anyPanelOpen && !isIdle) || cameraControl.isAiming || cameraControl.isShortAiming) { 
            desiredLookDirection = CM_Camera.m_XAxis.Value; //rotate player with camera, unless idle, aiming, or in inventory
        }

        if (overridePosRot) SetOverridePosRot();
    }

    void SetOverridePosRot(bool instant = false) {
        desiredSprintingDirection = 0;
        desiredRollDirection = 0;
        if (!instant) {
            transform.position =  Vector3.MoveTowards(transform.position, overrideTransform ? overrideTransform.position : overridePos, Time.deltaTime * 7f);
            desiredLookDirection = overrideTransform ? overrideTransform.rotation.eulerAngles.y : overrideRotAngle;
        } else {
            transform.position = overrideTransform ? overrideTransform.position : overridePos;
            lookDirection = overrideTransform ? overrideTransform.rotation.eulerAngles.y : overrideRotAngle;
        }
    }

    public void OverridePosRot (bool _enableOverride) {
        OverridePosRot(_enableOverride, transform.position, transform.rotation.eulerAngles.y);
    }
    public void OverridePosRot (Vector3 _overridePos) {
        OverridePosRot(true, _overridePos, transform.rotation.eulerAngles.y);
    }
    public void OverridePosRot (float _overrideYRot) {
        OverridePosRot(true, transform.position, _overrideYRot);
    }
    public void OverridePosRot (Vector3 _overridePos, float _overrideYRot) {
        OverridePosRot(true, _overridePos, _overrideYRot);
    }
    public void OverridePosRot (Transform posRotTransform) {
        OverridePosRot(true, posRotTransform);
    }
    
    void OverridePosRot (bool _enableOverride, Vector3 _overridePos, float _overrideYRot) {
        overridePosRot = _enableOverride;
        overridePos = _overridePos;
        overrideRotAngle = _overrideYRot;
        overrideTransform = null;
    }
    void OverridePosRot (bool _enableOverride, Transform _overrideTransform) {
        overridePosRot = _enableOverride;
        overridePos = Vector3.zero;
        overrideRotAngle = 0;
        overrideTransform = _overrideTransform;
    }

    public void InstantOverridePosRot (Transform _overrideTransform) {
        overridePos = Vector3.zero;
        overrideRotAngle = 0;
        overrideTransform = _overrideTransform;
        SetOverridePosRot(true);
    }

    //Animation variables
    float InputDirection;
    float lastInputDirection;
    float InputMagnitude;
    float isRunningFloat;
    float isSprintingFloat;
    
    void GroundAnimations() {
        if ( (InputDirection == 90 || InputDirection == -90) && isRunning && !isSprinting)
            walkSpeed = Characteristics.instance.walkSpeed * 1.2f;
        else 
            walkSpeed = Characteristics.instance.walkSpeed;

        if (!isIdle) {
            InputDirection = Mathf.Atan2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Mathf.Rad2Deg;
            lastInputDirection = InputDirection;

            CheckWhichFootIsUp();

        } else {
            InputDirection = lastInputDirection;
        }


        if (isRunning) {
            isRunningFloat += Time.deltaTime * 5;
        } else {
            isRunningFloat -= Time.deltaTime * 5;
        }
        isRunningFloat = Mathf.Clamp01(isRunningFloat);

        if (isSprinting) {
            isSprintingFloat += Time.deltaTime * 10; //1;
        } else {
            isSprintingFloat -= Time.deltaTime * 10; //0;
        }
        isSprintingFloat = Mathf.Clamp01(isSprintingFloat);
    }

    void InputMagnitudeFunc () {
        if (disableControl){
            InputMagnitude = 0;
            return;
        }

        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical")) {
            InputMagnitude = 1;
            InterruptCasting();
        } else {
            InputMagnitude = 0;
        }
    }

    void CheckWhichFootIsUp() {
        if (leftFootRoot.position.y < rightFootRoot.position.y) {
            animator.SetBool("IsRightLegUp", true);
        } else {
            animator.SetBool("IsRightLegUp", false);
        }
    }
    
    float jumpDis;
    public bool canJump() {
        if (!isGrounded || isFlying || isCrouch || isRolling || isAttacking || isGettingHit || isJumping || PeaceCanvas.instance.anyPanelOpen) {
            return false;
        } else {
            return true;
        }
    }
    public void Jump ()
    {
        audioController.PlayJumpRollSound();
        animator.SetTrigger("Jump");
        
        InterruptCasting();
    }
    void Crouch() {
        if (isGrounded)
            isCrouch = !isCrouch;
        
        InterruptCasting();
    }

    void Roll() {
        if (isAttacking || isRolling || isFlying || isSitting) {
            return;
        }
        if (!Characteristics.instance.canUseStamina) {
            CanvasScript.instance.DisplayWarning("Not enough stamina!");
            return;
        }

        GetComponent<Characteristics>().UseOrRestoreStamina(staminaReqToRoll);
        isRolling = true;
        if (InputDirection > 0 && !isSprinting && !isIdle) {
            desiredRollDirection = InputDirection;
        } else if (!isSprinting && !isIdle) {
            desiredRollDirection = InputDirection;
        } 
        UpdateRotation();
        animator.SetTrigger("Roll");
        audioController.PlayJumpRollSound();

        InterruptCasting();
    }
    public void StopRoll() { //Called from rolling animation
        isRolling = false;
        desiredRollDirection = 0;
    }

    void Sprinting () {
        if (disableControl)
            isSprinting = false;

        if (isSprinting && !sprintTrails.isPlaying) {
            PlayerAudioController.instance.PlayPlayerSound(PlayerAudioController.instance.sprint, 0.05f, 1.7f);
            sprintTrails.Play();
        } else if (!isSprinting && sprintTrails.isPlaying)  {
            sprintTrails.Stop();
        }
        
    }
    public void SprintOff () {
        isSprinting = false;
    }


#endregion

#region Flying movement

    void FlyingMovement() {
        UpdateRotation();
    }

    void FlyingAnimations () {
        if (!isIdle && !PeaceCanvas.instance.anyPanelOpen) {
            InputDirection = Mathf.Atan2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Mathf.Rad2Deg;
            lastInputDirection = InputDirection;
        } else {
            InputDirection = lastInputDirection;
        }
    }

    public void TakeOff () {
        isCrouch = false;
        desiredSprintingDirection = 0;
        animator.CrossFade("Locomotion.Flying.Takeoff", 0.1f);
        characterController.SwitchRootAnimation(false);
        isFlying = true;
    }
    public void LandFromFlying () {
        animator.CrossFade("Locomotion.Flying.Land", 0.1f);
    }
    public void FlyUp () { //Called from take off animation
        characterController.allowVerticalMovement = true;
        characterController.movement.DisableGroundDetection();
        characterController.movement.ApplyVerticalImpulse(10);
        SprintOff();
    }
    public void LandDown () { //Called from landing animation
        characterController.allowVerticalMovement = false;
        characterController.movement.EnableGroundDetection();
        characterController.SwitchRootAnimation(true);
        isFlying = false;
    }


#endregion
    
    void SetAnimatorVarialbes () {
        animator.SetFloat("InputMagnitude", InputMagnitude);    
        animator.SetFloat("InputDirection", InputDirection);
        animator.SetFloat("isSprinting", isSprintingFloat);
        animator.SetFloat("isRunning", isRunningFloat);
        animator.SetFloat("WalkSpeed", walkSpeed);
        animator.SetFloat("AttackSpeed", Characteristics.instance.attackSpeed.x);
        animator.SetFloat("CastingSpeed", Characteristics.instance.castingSpeed.x);
        
        animator.SetBool("Idle", isIdle);
        animator.SetBool("isCrouch", isCrouch);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isSitting", isSitting);
    }
    
    public void InterruptCasting () {
        if (!isCasting)
            return;
        animator.CrossFade("Attacks.Empty", 0.15f);
        castInterrupted = true;
        isCasting = false;
    }

    void CheckIsAttacking () {
        if (layerAnimRest && upperBodyLayerAnimRest)
            isAttacking = false;
        
        if (upperBodyLayerAnimRest && treeAnimRest)
            isAttacking = false;

        if (layerAnimRest && upperBodyTreeAnimRest)
            isAttacking = false;

        if (treeAnimRest && upperBodyTreeAnimRest)
            isAttacking = false;
    }

    public void TriggerBattle() {
        inBattle = true;
        inBattleTimer = Time.time;
    }
    void InBattleCheck () {
        if (isAttacking || isGettingHit || attackedByEnemies || cameraControl.isAiming || cameraControl.isShortAiming) {
            TriggerBattle();
        } else if (Time.time - inBattleTimer >= inBattleExitTime && !WeaponsController.instance.sheathingUnsheathing) {
            if (WeaponsController.instance.isWeaponOut || WeaponsController.instance.isBowOut) {
                StartCoroutine(WeaponsController.instance.Sheathe());
            }
            inBattle = false;
        }
    }

    public void PlayGeneralAnimation (int animationID, bool blockExit = false, float blockDuration = 0, bool upperBody = false) {
        animator.SetInteger("GeneralID", animationID);
        animator.SetTrigger(upperBody ? "GeneralUpperBodyTrigger" : "GeneralTrigger");

        animator.SetBool("blockGeneralExit", blockExit);
        if (blockDuration > 0) Invoke("UnlockGeneralAnimationExit", blockDuration);
    }
    public void ExitGeneralAnimation () {
        animator.CrossFade("General.empty", 0.15f);
        animator.CrossFade("GeneralUpperBody.empty", 0.15f);
        UnlockGeneralAnimationExit();
    }
    void UnlockGeneralAnimationExit () {
        animator.SetBool("blockGeneralExit", false);
    }

    public bool isDoingAnything () {
        if (isCrouch || isJumping || isRolling || isMounted || isAttacking || isGettingHit || isCasting || isFlying || isPickingArea || isAimingSkill)
            return true;
        else 
            return false;
    }

    float timeSat;

    public void Sit (SittingSpot spot) {
        if (isDoingAnything() || Time.time-timeSat < 2)
            return;
        isSitting = true;
        animator.SetTrigger("Sit");
        spot.isTaken = true;
        timeSat = Time.time;
        currentSittingSpot = spot;
        OverridePosRot(currentSittingSpot.transform.position, currentSittingSpot.transform.eulerAngles.y);
    }
    public void Unsit(SittingSpot spot) {
        if (Time.time-timeSat < 2)
            return;
        isSitting = false;
        spot.isTaken = false;
        currentSittingSpot = null;
        OverridePosRot(false);
    }

    void PushEnemies() {
        foreach (Collider col in Physics.OverlapCapsule(transform.position, transform.position + Vector3.up * 1.7f, 0.5f, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Collide)) {
            if (col.TryGetComponent(out Rigidbody rb)) {
                if (!rb.isKinematic) {
                    rb.AddForce(transform.forward * 20 + transform.InverseTransformPoint(col.transform.position).x * transform.right * 20);
                }
            }
        }
    }

#region ISavable

    public LoadPriority loadPriority {
        get {
            return LoadPriority.Last;
        }
    }

    public void Save()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).name == "City") {
                ES3.Save<Vector3>("pos", transform.position, SaveManager.instance.getCurrentCharacterFolderPath("playerTrans"));
                return;
            }
        }
    }

    public void Load()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).name == "City") {
                transform.position = ES3.Load("pos", SaveManager.instance.getCurrentCharacterFolderPath("playerTrans"), new Vector3(-560, 10, -120)); //default position at the City scene.
                return;
            }
        }
    }

#endregion
}
