using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's inventory bag. Items are unequipped EquipmentData references
/// that can be swapped into equipment slots via the character menu.
/// Attach to the Player GameObject (like EquipmentManager).
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public const int MAX_CAPACITY = 24;

    private readonly List<EquipmentData> items = new List<EquipmentData>();

    public IReadOnlyList<EquipmentData> Items => items;
    public int Count => items.Count;
    public bool IsFull => items.Count >= MAX_CAPACITY;

    /// <summary>
    /// Fires whenever inventory contents change (add, remove, swap).
    /// </summary>
    public event Action OnInventoryChanged;

    /// <summary>
    /// Adds an item to the inventory. Returns false if full.
    /// </summary>
    public bool AddItem(EquipmentData item)
    {
        if (item == null || items.Count >= MAX_CAPACITY)
            return false;

        items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory by reference. Returns false if not found.
    /// </summary>
    public bool RemoveItem(EquipmentData item)
    {
        if (item == null)
            return false;

        bool removed = items.Remove(item);
        if (removed)
            OnInventoryChanged?.Invoke();
        return removed;
    }

    /// <summary>
    /// Removes the item at the given index. Returns the removed item, or null.
    /// </summary>
    public EquipmentData RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        var item = items[index];
        items.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return item;
    }

    /// <summary>
    /// Gets the item at the given index, or null.
    /// </summary>
    public EquipmentData GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;
        return items[index];
    }

    /// <summary>
    /// Equips an inventory item, returning the previously equipped item to inventory.
    /// Atomic swap: inventory item goes to slot, old slot item comes to inventory.
    /// Returns true on success.
    /// </summary>
    public bool EquipFromInventory(int inventoryIndex, EquipmentManager equipMgr)
    {
        if (equipMgr == null) return false;

        var item = GetItem(inventoryIndex);
        if (item == null) return false;

        // Get what's currently in that slot
        var previous = equipMgr.GetEquipped(item.slotType);

        // Remove from inventory first
        items.RemoveAt(inventoryIndex);

        // Equip the new item (this returns the previous via our modified Equip)
        equipMgr.Equip(item);

        // Return old item to inventory (if there was one)
        if (previous != null)
            items.Add(previous);

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Unequips an item from the given slot and places it in inventory.
    /// Returns false if inventory is full.
    /// </summary>
    public bool UnequipToInventory(EquipmentSlotType slot, EquipmentManager equipMgr)
    {
        if (equipMgr == null) return false;

        var equipped = equipMgr.GetEquipped(slot);
        if (equipped == null) return false;

        if (items.Count >= MAX_CAPACITY)
            return false;

        equipMgr.Unequip(slot);
        items.Add(equipped);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Returns all item IDs for save purposes.
    /// </summary>
    public string[] GetItemIds()
    {
        var ids = new string[items.Count];
        for (int i = 0; i < items.Count; i++)
            ids[i] = items[i] != null ? items[i].equipmentId : "";
        return ids;
    }

    /// <summary>
    /// Loads inventory from saved IDs. Resolves each ID via Resources.
    /// </summary>
    public void LoadFromIds(string[] ids)
    {
        items.Clear();
        if (ids == null) return;

        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;

            var item = Resources.Load<EquipmentData>($"Equipment/{id}");
            if (item != null)
                items.Add(item);
            else
                Debug.LogWarning($"[InventoryManager] Could not resolve item ID: {id}");
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void Clear()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
    }
}
