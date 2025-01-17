using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_CraftingResourceSlot : UI_InventorySlot, IDropHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    [System.NonSerialized] public int availableAmount;

    protected override void DisplayItem () {
        slotIcon.sprite = itemInSlot.itemIcon;
        itemAmountText.text = $"{availableAmount}/{itemAmount}";
        slotIcon.color = availableAmount >= itemAmount ? Color.white : Color.grey;
        itemAmountText.color = availableAmount >= itemAmount ? Color.white : Color.Lerp(Color.red, Color.white, 0.3f);

        CheckForSpecialFrame();
    }

    public void UpdateResourceDisplay () {
        DisplayItem();
    }
    public override void ValidateSlot() {
        //do nothing
    }

    protected override void Start () {
        //do nothing
    }
    public override void SaveSlot () {
        //do nothing
    }
    public override void LoadSlot () {
        //do nothing
    }
    public override void OnBeginDrag (PointerEventData pointerData) {
        //do nothing
    }
    public override void OnDrag (PointerEventData pointerData) {
        //do nothing
    }
    public override void OnEndDrag (PointerEventData pointerData) {
        //do nothing
    }
    public override void OnDrop (PointerEventData pointerData) {
        //do nothing
    }
    public override void UseItem()
    {
        //do nothing
    }
}
