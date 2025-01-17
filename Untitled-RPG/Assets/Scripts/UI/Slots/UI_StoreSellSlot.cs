using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_StoreSellSlot : UI_InventorySlot, IDropHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    protected override void Start () {
        //do nothing
    }
    public override void SaveSlot () {
        //do nothing
    }
    public override void LoadSlot()
    {
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
        InventoryManager.instance.AddItemToInventory(itemInSlot, itemAmount);
        ClearSlot();
        UIAudioManager.instance.PlayUISound(UIAudioManager.instance.DropItem);
    }
}
