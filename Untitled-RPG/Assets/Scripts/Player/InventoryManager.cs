﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public struct ItemAmountPair {public Item item1; public int amount1;}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    public int currentGold;
    [Space]
    public TextMeshProUGUI currentGoldLabel;
    public Dropdown sortDropDown;
    [Space]
    public GameObject slots;
    UI_InventorySlot[] allSlots;

    //DEBUG 
    int itemAmountToAdd = 1;
    Item itemToAdd;
    
    protected string savefilePath = "saves/currency.txt";

    public InventoryManager () {
        if (instance == null)
            instance = this;
    }

    public void Init() {
        if (instance == null)
            instance = this;
        
        PeaceCanvas.saveGame += Save;

        Load();

        allSlots = slots.GetComponentsInChildren<UI_InventorySlot>();
    }

    void Update() {
        if (itemToAdd is Equipment)
            itemAmountToAdd = 1;

        currentGoldLabel.text = currentGold.ToString();
        currentGoldLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(currentGoldLabel.GetComponent<TextMeshProUGUI>().textBounds.size.x, 20);
    }

    void Save () {
        ES3.Save<int>("currentGold", currentGold, savefilePath);
    }
    void Load () {
        if (ES3.FileExists(savefilePath))
            currentGold = ES3.Load<int>("currentGold", savefilePath);
    }

    public void AddItemToInventory (Item item, int amount, UI_InventorySlot slotToExclude = null) {
        for (int i = 0; i < allSlots.Length; i++) {
            if (slotToExclude != null && slotToExclude == allSlots[i]) {
                continue; 
            }
            if (allSlots[i].itemInSlot == item && item.isStackable && allSlots[i].itemAmount + amount <= item.maxStackAmount) {
                allSlots[i].AddItem(item, amount, null);
                break;
            }

            if (allSlots[i].itemInSlot == null) {
                allSlots[i].AddItem(item, amount, null);
                break;
            }
        }
    }

    public void AddGold(int amount) {
        currentGold += amount;
    }
    public void RemoveGold (int amount) {
        currentGold -= amount;
    }

    public int getNumberOfEmptySlots () {
        int i = 0;
        foreach (UI_InventorySlot slot in allSlots) {
            if (slot.itemInSlot == null)
                i++;
        }
        return i;
    }

    public int getItemAmountInInventory (Item item){
        int amount = 0;
        for (int i = 0; i < allSlots.Length; i++) {
            if (allSlots[i].itemInSlot == item)
                amount += allSlots[i].itemAmount;
        }
        return amount;
    }

    public void RemoveItemFromInventory (Item item, int amount) {
        int amountLeftToRemove = amount;
        for (int i = 0; i < allSlots.Length; i++) {
            if (allSlots[i].itemInSlot == item) {
                int amountInThisSlot = allSlots[i].itemAmount;
                allSlots[i].itemAmount -= amountLeftToRemove;
                amountLeftToRemove -= amountInThisSlot;
                allSlots[i].ValidateSlot();
            }
            if (amountLeftToRemove <= 0)
                break;
        }
    }

    public void Sort () {
        List<ItemAmountPair> allItemsAmounts = new List<ItemAmountPair>();
        ItemAmountPair x;

        foreach (UI_InventorySlot slot in allSlots) {
            if (slot.itemInSlot != null) {
                x.item1 = slot.itemInSlot;
                x.amount1 = slot.itemAmount;
                allItemsAmounts.Add(x);
                slot.ClearSlot();
            }
        }

        if (sortDropDown.value == 0) { //type
            List<ItemAmountPair> Weapons = new List<ItemAmountPair>();
            List<ItemAmountPair> Armor = new List<ItemAmountPair>();
            List<ItemAmountPair> Skillbooks = new List<ItemAmountPair>();
            List<ItemAmountPair> Consumables = new List<ItemAmountPair>();
            
            for (int i = 0; i < allItemsAmounts.Count; i++) { //Consumables last
                if (allItemsAmounts[i].item1 is Weapon) { 
                    Weapons.Add(allItemsAmounts[i]);
                } else if (allItemsAmounts[i].item1 is Armor) { 
                    Armor.Add(allItemsAmounts[i]);
                } else if (allItemsAmounts[i].item1 is Skillbook) { 
                    Skillbooks.Add(allItemsAmounts[i]);
                } else if (allItemsAmounts[i].item1 is Consumable) { 
                    Consumables.Add(allItemsAmounts[i]);
                }
            }

            Weapons.Sort((p2,p1)=>p1.item1.itemBasePrice.CompareTo(p2.item1.itemBasePrice));
            Armor.Sort((p2,p1)=>p1.item1.itemBasePrice.CompareTo(p2.item1.itemBasePrice));
            Skillbooks.Sort((p2,p1)=>p1.item1.itemBasePrice.CompareTo(p2.item1.itemBasePrice));
            Consumables.Sort((p2,p1)=>p1.item1.itemBasePrice.CompareTo(p2.item1.itemBasePrice));

            allItemsAmounts.Clear();
            allItemsAmounts.AddRange(Weapons);
            allItemsAmounts.AddRange(Armor);
            allItemsAmounts.AddRange(Skillbooks);
            allItemsAmounts.AddRange(Consumables);
        } else if (sortDropDown.value == 1) { //Price
            allItemsAmounts.Sort((p2,p1)=>p1.item1.itemBasePrice.CompareTo(p2.item1.itemBasePrice));
        } else if (sortDropDown.value == 2) { //Rarity
            allItemsAmounts.Sort((p2,p1)=>p1.item1.itemRarity.CompareTo(p2.item1.itemRarity));
        } else if (sortDropDown.value == 3) { //ID
            allItemsAmounts.Sort((p1,p2)=>p1.item1.ID.CompareTo(p2.item1.ID));
        }

        for (int i = 0; i < allItemsAmounts.Count; i++) {
            AddItemToInventory(allItemsAmounts[i].item1, allItemsAmounts[i].amount1, null);
        }

        UIAudioManager.instance.PlayUISound(UIAudioManager.instance.UI_Select);
    }
}
