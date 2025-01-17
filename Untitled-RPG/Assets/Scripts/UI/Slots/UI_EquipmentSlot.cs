﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_EquipmentSlot : UI_InventorySlot
{
    [Header("Equipment slot")]
    public EquipmentSlotType equipmentSlotType;
    Image equipmentIcon;

    Color baseColor = new Color(1,1,1, 220f/255f);

     void Awake() {
        equipmentIcon = GetComponent<Image>();
    }

    protected override string savefilePath() {
        return SaveManager.instance.getCurrentCharacterFolderPath("equipmentSlots");
    } 

    public override void SaveSlot (){
        short ID;
        if (itemInSlot != null)
            ID = (short)itemInSlot.ID;
        else
            ID = -1; //Slot is empty

        ES3.Save<short>($"{slotID}_ID", ID, savefilePath());
    }
    public override void LoadSlot () {
        short ID = ES3.Load<short>($"{slotID}_ID", savefilePath(), -1);

        if (ID < 0) {
            ClearSlot();
            return; //Slot is empty
        }
        AddItem(AssetHolder.instance.getItem(ID), 1, null);
    }
    public override void AddItem (Item item, int amount, UI_InventorySlot initialSlot) {
        if (equipmentSlotType == EquipmentSlotType.MainHand) {
            MainHandAdd(item, amount, initialSlot);
        } else if (equipmentSlotType == EquipmentSlotType.SecondaryHand) {
            SecondaryHandAdd(item, amount, initialSlot);    
        } else if (equipmentSlotType == EquipmentSlotType.Bow) {
            BowAdd(item, amount, initialSlot);    
        } else if (equipmentSlotType == EquipmentSlotType.Helmet || equipmentSlotType == EquipmentSlotType.Chest || equipmentSlotType == EquipmentSlotType.Gloves || equipmentSlotType == EquipmentSlotType.Pants || equipmentSlotType == EquipmentSlotType.Boots || equipmentSlotType == EquipmentSlotType.Back) {
            ArmorAdd(item, amount, initialSlot);    
        } else if (equipmentSlotType == EquipmentSlotType.Necklace) {
            NecklaceAdd(item, amount, initialSlot);    
        } else if (equipmentSlotType == EquipmentSlotType.Ring) {
            RingAdd(item, amount, initialSlot);    
        } else {
            //if its not equipment, return it to initial slot. LATER IMPLEMENT EVERY BODY PART.
            initialSlot.AddItem(item, amount, null); 
            return;
        }
    }

    protected override void DisplayItem() {
        slotIcon.sprite = itemInSlot.itemIcon;
        slotIcon.color = Color.white;
        equipmentIcon.color = transparentColor;

        CheckForSpecialFrame();
    }

    void SharedAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if (initialSlot != null) { //at this point we are 100% equiping the item, so its safe to clear initial slot. Initial slot might be null if we drop item in a wrong area and it just returns back
            initialSlot.ClearSlot();
        } 
        if (itemInSlot != null) {
            initialSlot.AddItem(itemInSlot, itemAmount, null);
        }

        ClearSlot();
        itemInSlot = item;
        itemAmount = amount;
        DisplayItem();
        
        Equipment eq = (Equipment)item;
        if (eq.grantedSkill != null && !Combat.instanace.currentSkillsFromEquipment.Contains(AssetHolder.instance.getSkill(eq.grantedSkill.ID))) {
            Combat.instanace.currentSkillsFromEquipment.Add(AssetHolder.instance.getSkill(eq.grantedSkill.ID));
            Combat.instanace.ValidateSkillSlots();
        }

        if (item is Armor) {
            Armor a = (Armor)item;
            if (a.armorType == ArmorType.Necklace || a.armorType == ArmorType.Ring)
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.EquipJewlery);
            else 
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.EquipArmor);
        }
        else if (item is Weapon) {
            Weapon w = (Weapon)item;
            if (w.weaponCategory == WeaponCategory.Bow)
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.EquipBow);
            else if (w.weaponCategory == WeaponCategory.Shield)
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.EquipShield);
            else
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.EquipWeapon);
        }

        EquipmentManager.instance.CheckEquipmentBuffs();
    }

    void MainHandAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if ( !(item is Weapon) ) {  //if its not weapon, return it to initial slot;
            initialSlot.AddItem(item, amount, null); 
            return;
        }

        Weapon w = (Weapon)item;

        if (w.weaponHand == WeaponHand.BowHand || w.weaponHand == WeaponHand.SecondaryHand) { //if bow or secondary, return back.
            initialSlot.AddItem(item, amount, null);
            return;
        }

        if ( w.weaponHand == WeaponHand.TwoHanded && EquipmentManager.instance.secondaryHand.itemInSlot != null) { //if its two handed and second hand is busy, clear the second hand.
            InventoryManager.instance.AddItemToInventory(EquipmentManager.instance.secondaryHand.itemInSlot, EquipmentManager.instance.secondaryHand.itemAmount, initialSlot);
            EquipmentManager.instance.secondaryHand.ClearSlot();
        }

        SharedAdd(item, amount, initialSlot);
        EquipmentManager.instance.EquipWeaponPrefab(w);
    }

    void SecondaryHandAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if ( !(item is Weapon) ) {   //if its not weapon, return it to initial slot;
            initialSlot.AddItem(item, amount, null); 
            return;
        }
        
        Weapon w = (Weapon)item;

        if (w.weaponHand == WeaponHand.TwoHanded || w.weaponHand == WeaponHand.BowHand) {  //if weapon is two handed or a bow, return it to initial slot
            initialSlot.AddItem(item, amount, initialSlot); 
            return;
        }
        Weapon mw = (Weapon)EquipmentManager.instance.mainHand.itemInSlot;
        if (mw != null && (mw.weaponHand == WeaponHand.TwoHanded) ) { //if main hand is busy with a tho handed weapon, remove two handed weapon
            InventoryManager.instance.AddItemToInventory(EquipmentManager.instance.mainHand.itemInSlot, EquipmentManager.instance.mainHand.itemAmount, initialSlot);
            EquipmentManager.instance.mainHand.ClearSlot();
        }
        
        Weapon wInSlot = (Weapon)itemInSlot;
        if (wInSlot != null && wInSlot.weaponHand == WeaponHand.SecondaryHand) { // if slot already contains item that only for the secondary hand, return it initial slot or inventory
            InventoryManager.instance.AddItemToInventory(itemInSlot, itemAmount, initialSlot);
            ClearSlot();
        }

        SharedAdd(item, amount, initialSlot);
        EquipmentManager.instance.EquipWeaponPrefab(w, true);
    }

    void BowAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if (!(item is Weapon)) {    //If its not weapon, return to initial slot
            initialSlot.AddItem(item, amount, null);
            return;
        }

        Weapon w = (Weapon)item;

        if (w.weaponHand != WeaponHand.BowHand) {   //If its not a bow, return to initial slot
            initialSlot.AddItem(item, amount, null);
            return;
        }

        SharedAdd(item, amount, initialSlot);
        EquipmentManager.instance.EquipWeaponPrefab(w);
    }

    void ArmorAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if (!(item is Armor)) { //If its not armor, return to initial slot
            initialSlot.AddItem(item, amount, null);
            return;
        }
    
        Armor ar = (Armor)item;
        if (ar.armorType == ArmorType.Necklace || ar.armorType == ArmorType.Ring) { //if its necklace or ring, don't accept it.
            initialSlot.AddItem(item, amount, null);
            return;
        }

        if (equipmentSlotType == EquipmentSlotType.Helmet && ar.armorType != ArmorType.Helmet) {
            initialSlot.AddItem(item, amount, null);
            return;
        } else if (equipmentSlotType == EquipmentSlotType.Chest && ar.armorType != ArmorType.Chest) {
            initialSlot.AddItem(item, amount, null);
            return;
        } else if (equipmentSlotType == EquipmentSlotType.Gloves && ar.armorType != ArmorType.Gloves) {
            initialSlot.AddItem(item, amount, null);
            return;
        } else if (equipmentSlotType == EquipmentSlotType.Pants && ar.armorType != ArmorType.Pants) {
            initialSlot.AddItem(item, amount, null);
            return;
        } else if (equipmentSlotType == EquipmentSlotType.Boots && ar.armorType != ArmorType.Boots) {
            initialSlot.AddItem(item, amount, null);
            return;
        } else if (equipmentSlotType == EquipmentSlotType.Back && ar.armorType != ArmorType.Back) {
            initialSlot.AddItem(item, amount, null);
            return;
        }

        SharedAdd(item, amount, initialSlot);
        EquipmentManager.instance.EquipArmorVisual(ar);
    }

    void NecklaceAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if (!(item is Armor)) { // If its not armor, return to initial slot
            initialSlot.AddItem(item, amount, null);
            return;
        }

        Armor ar = (Armor)item;
        if (ar.armorType != ArmorType.Necklace) { //if its not necklace, return to initial slot.
            initialSlot.AddItem(item, amount, null);
            return;
        }

        SharedAdd(item, amount, initialSlot);
    }

    void RingAdd (Item item, int amount, UI_InventorySlot initialSlot) {
        if (!(item is Armor)) { // If its not armor, return to initial slot
            initialSlot.AddItem(item, amount, null);
            return;
        }

        Armor ar = (Armor)item;
        if (ar.armorType != ArmorType.Ring) { //if its not ring, return to initial slot.
            initialSlot.AddItem(item, amount, null);
            return;
        }

        SharedAdd(item, amount, initialSlot);
    }

    public override void ClearSlot()
    {
        if (itemInSlot is Weapon) {
            Weapon w = (Weapon)itemInSlot;
            if (w.weaponCategory == WeaponCategory.Bow) {
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.UnequipBow);
            } else if (w.weaponCategory == WeaponCategory.Shield) {
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.UnequipShield);
            } else {
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.UnequipWeapon);
            }
            EquipmentManager.instance.UnequipWeaponPrefab(w, equipmentSlotType == EquipmentSlotType.SecondaryHand ? true : false);
        } else if (itemInSlot is Armor) {
            if (equipmentSlotType != EquipmentSlotType.Necklace && equipmentSlotType != EquipmentSlotType.Ring)
                EquipmentManager.instance.UnequipArmorVisual((Armor)itemInSlot);
            
            Armor a = (Armor)itemInSlot;
            if (a.armorType == ArmorType.Necklace || a.armorType == ArmorType.Ring)
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.UnequipJewlery);
            else 
                UIAudioManager.instance.PlayUISound(UIAudioManager.instance.UnequipArmor);
        }
        if (itemInSlot is Equipment) {
            Equipment eq = (Equipment)itemInSlot;
            if (eq.grantedSkill != null && Combat.instanace.currentSkillsFromEquipment.Contains(AssetHolder.instance.getSkill(eq.grantedSkill.ID))) {
                Combat.instanace.currentSkillsFromEquipment.Remove(AssetHolder.instance.getSkill(eq.grantedSkill.ID));
                Combat.instanace.ValidateSkillSlots();
            }
        }

        if (equipmentIcon == null) equipmentIcon = GetComponent<Image>();
        equipmentIcon.color = baseColor;

        base.ClearSlot();

        EquipmentManager.instance.CheckEquipmentBuffs();
    }

    public override void OnBeginDrag (PointerEventData pointerData) { //overriding these to not play UI sounds when equiping / unequiping by drag
        if (itemInSlot == null || pointerData.button == PointerEventData.InputButton.Right)
            return;

        PeaceCanvas.instance.StartDraggingItem(GetComponent<RectTransform>().sizeDelta, itemInSlot, itemAmount, this);
        ClearSlot();
        //UIAudioManager.instance.PlayUISound(UIAudioManager.instance.GrabItem);
    }
    public override void OnDrop (PointerEventData pointerData) {
        if (PeaceCanvas.instance.itemBeingDragged != null) { //Dropping Item
            PeaceCanvas.instance.dragSuccess = true;
            AddItem(PeaceCanvas.instance.itemBeingDragged, PeaceCanvas.instance.amountOfDraggedItem, PeaceCanvas.instance.initialSlot);
            //UIAudioManager.instance.PlayUISound(UIAudioManager.instance.DropItem);
        }
    }
}
