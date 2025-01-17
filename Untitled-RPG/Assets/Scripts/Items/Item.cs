﻿using UnityEngine;

[System.Serializable]
public struct CraftingObject {
    public Item resource;
    public int requiredAmount;
}

public abstract class Item : ScriptableObject
{
    public int ID;
    public string itemName;
    public string itemDesctription;
    public ItemRarity itemRarity;
    public bool isStackable;
    public int maxStackAmount = 100;
    public Sprite itemIcon;
    public GameObject lootPrefab;
    public Material specialFrameMat;
    [Space]
    public int itemBasePrice;
    public bool overridePrice;
    
    [Header("Crafting")]
    public bool isCraftable;
    public CraftingObject[] craftingRecipe;

    public abstract void Use ();
    public abstract void Use (UI_InventorySlot initialSlot);

    protected virtual void OnValidate() {
        craftingRecipe = isCraftable ? craftingRecipe : null;
        maxStackAmount = isStackable ? maxStackAmount : 1;

        if (craftingRecipe != null) {
            for (int i = 0; i < craftingRecipe.Length; i++){
                craftingRecipe[i].requiredAmount = Mathf.Clamp(craftingRecipe[i].requiredAmount, 1, 999999);
            }
        }
    }

    public virtual string getItemDescription() {
        return itemDesctription;
    }
    public virtual int getItemValue() {
        return 0;
    }
}
