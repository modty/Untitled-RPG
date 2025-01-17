﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using NaughtyAttributes;

public class CanvasScript : MonoBehaviour
{
    public static CanvasScript instance;

    [Header("Player")]
    public Image healthBar;
    public Image staminaBar;

    public GameObject buffs;
    public GameObject castingBar;

    [Header("Overall UI")]
    public TextMeshProUGUI warningText;
    public TextMeshProUGUI rowNumberLeftLabel;
    public TextMeshProUGUI rowNumberRightLabel;

    [Header("Enemy")]
    public Image enemyHealthBar;
    public TextMeshProUGUI enemyName;
    public TextMeshProUGUI enemyHealth;

    [Header("Skillpanel")]
    public CanvasGroup skillpanel;
    public UI_MiddleSkillPanelButtons LMB;
    public UI_MiddleSkillPanelButtons RMB;
    public Sprite cancelSprite;

    [Header("Quick access menu")]
    [DisplayWithoutEdit] public bool isQuickAccessMenuOpen;
    public QuickAccessMenu quickAccessMenu;
    float quickAccessMenuTimer;

    Characteristics characteristics;

    public bool ShowSkillpanel {
        get {
            return skillpanel.alpha > 0.9f;
        }
        set {
            if (value) FadeinSkillpanel();
            else FadeoutSkillpanel();
        }
    }


    void Awake() {
        if (instance == null) 
            instance = this;
    }

    void Start() {
        characteristics = Characteristics.instance;
    }

    void Update() {
        DisplayHPandStamina();

        if (Input.GetKeyDown(KeybindsManager.instance.currentKeyBinds["Quick access menu"]))
            OpenQuickAccessMenu();
        else if (Input.GetKeyUp(KeybindsManager.instance.currentKeyBinds["Quick access menu"]))
            CloseQuickAccessMenu();

        if (Input.GetKeyDown(KeybindsManager.instance.currentKeyBinds["Switch skill rows"])) {
            Combat.instanace.SwitchSkillRows();
        }
    }

    float staminaColorLerp;
    float staminaFillAmount;
    void DisplayHPandStamina () {
        healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, (float)characteristics.health/characteristics.maxHealth, Time.deltaTime * 10);
        healthBar.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = characteristics.health.ToString();

        if (!PlayerControlls.instance.isMounted) DisplayPlayerStamina();
        else DisplayMountStamina();
    }
    void DisplayPlayerStamina () {
        staminaFillAmount = characteristics.maxStamina == 0 ? 0 : Mathf.Lerp(staminaFillAmount, (float)characteristics.stamina/characteristics.maxStamina, Time.deltaTime * 10);
        StaminaBarPosition();

        if (!Characteristics.instance.canUseStamina) {
            if (staminaColorLerp < 1)
                staminaColorLerp += Time.deltaTime * 10;
        } else {
            if (staminaColorLerp > 0)
                staminaColorLerp -= Time.deltaTime * 10;
        }
        staminaColorLerp = Mathf.Clamp01(staminaColorLerp);
        staminaFillAmount = Mathf.Clamp01(staminaFillAmount);
        
        staminaBar.material.SetFloat("_ColorLerp", staminaColorLerp);
        staminaBar.material.SetFloat("_FillAmount", staminaFillAmount);
    }
    void DisplayMountStamina () {
        MountController mountController = PlayerControlls.instance.rider.MountStored.Animal.GetComponent<MountController>();
        
        staminaFillAmount = mountController.totalMaxStamina == 0 ? 0 : Mathf.Lerp(staminaFillAmount, (float)mountController.currentStamina/mountController.totalMaxStamina, Time.deltaTime * 10);
        StaminaBarMountedPosition();

        if (!PlayerControlls.instance.rider.MountStored.Animal.UseSprint) {
            if (staminaColorLerp < 1)
                staminaColorLerp += Time.deltaTime * 10;
        } else {
            if (staminaColorLerp > 0)
                staminaColorLerp -= Time.deltaTime * 10;
        }
        staminaColorLerp = Mathf.Clamp01(staminaColorLerp);
        staminaFillAmount = Mathf.Clamp01(staminaFillAmount);
        
        staminaBar.material.SetFloat("_ColorLerp", staminaColorLerp);
        staminaBar.material.SetFloat("_FillAmount", staminaFillAmount);
    }

    void StaminaBarPosition () {
        Vector3 currentPos = staminaBar.transform.GetComponent<RectTransform>().anchoredPosition;
        Vector2 desPos = Camera.main.WorldToScreenPoint(PlayerControlls.instance.transform.position + PlayerControlls.instance.playerCamera.transform.right * 0.5f + Vector3.up * 1.2f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), desPos, PeaceCanvas.instance.UICamera, out desPos);
        staminaBar.transform.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(currentPos, desPos, 10 * Time.deltaTime);
    }
    void StaminaBarMountedPosition () {
        Vector3 currentPos = staminaBar.transform.GetComponent<RectTransform>().anchoredPosition;
        Vector2 desPos = Camera.main.WorldToScreenPoint(PlayerControlls.instance.rider.MountStored.transform.position + PlayerControlls.instance.rider.MountStored.transform.right * 0.7f + Vector3.up * 1.2f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), desPos, PeaceCanvas.instance.UICamera, out desPos);
        staminaBar.transform.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(currentPos, desPos, 10 * Time.deltaTime);
    }

    public void HideStamina () {
        staminaBar.transform.gameObject.SetActive(false);
    }
    public void ShowStamina () {
        staminaBar.transform.gameObject.SetActive(true);
    }

    public void DisplayEnemyInfo (string name, float healthFillAmount, int health) {
        if (!enemyHealthBar.gameObject.activeInHierarchy) {
            enemyHealthBar.gameObject.SetActive(true);
        } else if (!enemyName.gameObject.activeInHierarchy) {
            enemyName.gameObject.SetActive(true);
        }
        enemyHealthBar.transform.GetChild(0).GetComponent<Image>().fillAmount = Mathf.Lerp(enemyHealthBar.transform.GetChild(0).GetComponent<Image>().fillAmount, healthFillAmount, 0.1f);
        enemyName.text = name;
        if (enemyName.text != "Training Dummy")
            enemyHealth.text = health.ToString();
        else 
            enemyHealth.text = "";
    }
    public void StopDisplayEnemyInfo () {
        if (enemyHealthBar.gameObject.activeInHierarchy) {
            enemyHealthBar.gameObject.SetActive(false);
        } else if (enemyName.gameObject.activeInHierarchy) {
            enemyName.gameObject.SetActive(false);
        }
    }
    float warningTextAlpha;
    Coroutine warning;
    public void DisplayWarning(string text) {
        if (warning != null) StopCoroutine(warning);
        warning = StartCoroutine(WarningTextIenum(text));;
    }
    IEnumerator WarningTextIenum (string text){
        warningText.text = text;
        warningText.gameObject.SetActive(true);
        while (warningText.color.a <= 1) {
            warningTextAlpha += Time.deltaTime * 5;
            warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, warningTextAlpha);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        yield return new WaitForSeconds(2);
        while (warningText.color.a >= 0) {
            warningTextAlpha -= Time.deltaTime * 5;
            warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, warningTextAlpha);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        warningText.text = null;
        warningText.gameObject.SetActive(false);
        warning = null;
    }

    public void DisplayCastingBar (float castingNormalizedAnimationTime) {
        StartCoroutine(DisplayCastinBarIenum(castingNormalizedAnimationTime));
    }
    IEnumerator DisplayCastinBarIenum (float castEndNormalizedTime) {
        castingBar.SetActive(true);
        Image bar = castingBar.transform.GetChild(0).GetComponent<Image>();
        bar.fillAmount = 0;
        while (bar.fillAmount != 1) {
            if (PlayerControlls.instance.castInterrupted) {
                break;
            }
            AnimatorStateInfo cc = PlayerControlls.instance.animator.GetCurrentAnimatorStateInfo(PlayerControlls.instance.animator.GetLayerIndex("Attacks"));
            if (cc.IsName("Empty"))
                cc = PlayerControlls.instance.animator.GetNextAnimatorStateInfo(PlayerControlls.instance.animator.GetLayerIndex("Attacks")); 

            bar.fillAmount = cc.normalizedTime % 1 / castEndNormalizedTime;
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        castingBar.SetActive(false);
    }

    public void DisplayProgressBar (bool prewarm, float progress = 0, in bool done = false) {
        if (prewarm){
            castingBar.SetActive(true);
            castingBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 0;
        }
        castingBar.transform.GetChild(0).GetComponent<Image>().fillAmount = progress;
        if (done)
            castingBar.SetActive(false);
    }
    public void DisplayProgressBar (float duration) {
        StartCoroutine(DisplayAutomaricProgressBar(duration));
    }

    IEnumerator DisplayAutomaricProgressBar(float duration) {
        castingBar.SetActive(true);
        Image bar = castingBar.transform.GetChild(0).GetComponent<Image>();
        bar.fillAmount = 0;
        float startTime = Time.time; 
        while (Time.time - startTime < duration) {
            bar.fillAmount = (Time.time - startTime) / duration;
            yield return null;
        }
        bar.fillAmount = 1;
        yield return new WaitForSeconds(0.25f);
        castingBar.SetActive(false);
    }

    public void PickAreaForSkill (Skill skill) {
        LMB.areaPickerSkill = skill;
        RMB.areaPickerSkill = skill;
    }

    void OpenQuickAccessMenu(){
        if (PeaceCanvas.instance.anyPanelOpen || Time.time - quickAccessMenuTimer < 0.5f || isQuickAccessMenuOpen)
            return;

        quickAccessMenuTimer = Time.time;
        quickAccessMenu.gameObject.SetActive(true);
        PlayerControlls.instance.cameraControl.stopInput = true;
        isQuickAccessMenuOpen = true;
    }
    public void CloseQuickAccessMenu () {
        if (!isQuickAccessMenuOpen)
            return;
            
        PlayerControlls.instance.cameraControl.stopInput = false;
        isQuickAccessMenuOpen = false;
        quickAccessMenu.Close();
    }

    public void ChangeRowNumberLabel (){
        if (Combat.instanace.numberOfSkillSlotsRows == 1) {
            rowNumberLeftLabel.transform.parent.gameObject.SetActive(false);
            rowNumberRightLabel.transform.parent.gameObject.SetActive(false);
        } else {
            rowNumberLeftLabel.transform.parent.gameObject.SetActive(true);
            rowNumberRightLabel.transform.parent.gameObject.SetActive(true);
        }
        rowNumberLeftLabel.text = (Combat.instanace.currentSkillSlotsRow + 1).ToString();
        rowNumberRightLabel.text = (Combat.instanace.currentSkillSlotsRow + 1).ToString();
    }

    void FadeoutSkillpanel (float duration = 1) {
        if (duration <= 0) skillpanel.alpha = 0;
        else skillpanel.DOFade(0, duration);
    }
    void FadeinSkillpanel (float duration = 1) {
        if (duration <= 0) skillpanel.alpha = 1;
        else skillpanel.DOFade(1, duration);
    }
}
