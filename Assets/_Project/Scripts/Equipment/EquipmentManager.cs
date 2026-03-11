using System;
using UnityEngine;

/// <summary>
/// Manages equipped items on the player. Provides stat bonuses and updates
/// character visuals when equipment changes. Attach to the Player GameObject.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    private const int SLOT_COUNT = 7;

    private EquipmentData[] equippedItems;

    // Component references
    private PlayerAppearance playerAppearance;
    private CombatController combatController;

    /// <summary>
    /// Fires when any equipment slot changes. Args: slot, new item (null if unequipped).
    /// </summary>
    public event Action<EquipmentSlotType, EquipmentData> OnEquipmentChanged;

    private void Awake()
    {
        equippedItems = new EquipmentData[SLOT_COUNT];
        playerAppearance = GetComponent<PlayerAppearance>();
        combatController = GetComponent<CombatController>();
    }

    /// <summary>
    /// Returns the equipment in the given slot, or null.
    /// </summary>
    public EquipmentData GetEquipped(EquipmentSlotType slot)
    {
        int idx = (int)slot;
        if (idx < 0 || idx >= SLOT_COUNT) return null;
        return equippedItems[idx];
    }

    /// <summary>
    /// Equips an item to its designated slot, replacing any existing item.
    /// Returns the previously equipped item (or null if the slot was empty).
    /// </summary>
    public EquipmentData Equip(EquipmentData item)
    {
        if (item == null) return null;

        int idx = (int)item.slotType;
        if (idx < 0 || idx >= SLOT_COUNT) return null;

        var previous = equippedItems[idx];
        equippedItems[idx] = item;
        ApplyEquipmentEffects(item.slotType, item);
        OnEquipmentChanged?.Invoke(item.slotType, item);

        Debug.Log($"[EquipmentManager] Equipped {item.displayName} in {item.slotType}");
        return previous;
    }

    /// <summary>
    /// Removes the item from the given slot.
    /// </summary>
    public void Unequip(EquipmentSlotType slot)
    {
        int idx = (int)slot;
        if (idx < 0 || idx >= SLOT_COUNT) return;

        var previous = equippedItems[idx];
        equippedItems[idx] = null;
        ApplyEquipmentEffects(slot, null);
        OnEquipmentChanged?.Invoke(slot, null);

        if (previous != null)
            Debug.Log($"[EquipmentManager] Unequipped {previous.displayName} from {slot}");
    }

    /// <summary>
    /// Returns the total STR bonus from all equipped items.
    /// </summary>
    public int GetTotalBonusSTR()
    {
        int total = 0;
        for (int i = 0; i < SLOT_COUNT; i++)
            if (equippedItems[i] != null) total += equippedItems[i].bonusSTR;
        return total;
    }

    /// <summary>
    /// Returns the total INT bonus from all equipped items.
    /// </summary>
    public int GetTotalBonusINT()
    {
        int total = 0;
        for (int i = 0; i < SLOT_COUNT; i++)
            if (equippedItems[i] != null) total += equippedItems[i].bonusINT;
        return total;
    }

    /// <summary>
    /// Returns the total AGI bonus from all equipped items.
    /// </summary>
    public int GetTotalBonusAGI()
    {
        int total = 0;
        for (int i = 0; i < SLOT_COUNT; i++)
            if (equippedItems[i] != null) total += equippedItems[i].bonusAGI;
        return total;
    }

    /// <summary>
    /// Returns all equipped item IDs for save purposes. Null entries become empty strings.
    /// </summary>
    public string[] GetEquippedIds()
    {
        var ids = new string[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
            ids[i] = equippedItems[i] != null ? equippedItems[i].equipmentId : "";
        return ids;
    }

    /// <summary>
    /// Loads equipment from saved IDs. Resolves each ID via Resources.
    /// </summary>
    public void LoadFromIds(string[] ids)
    {
        if (ids == null) return;

        for (int i = 0; i < SLOT_COUNT && i < ids.Length; i++)
        {
            if (string.IsNullOrEmpty(ids[i]))
            {
                equippedItems[i] = null;
                continue;
            }

            var item = Resources.Load<EquipmentData>($"Equipment/{ids[i]}");
            if (item != null)
            {
                equippedItems[i] = item;
                ApplyEquipmentEffects((EquipmentSlotType)i, item);
            }
            else
            {
                Debug.LogWarning($"[EquipmentManager] Could not resolve equipment ID: {ids[i]}");
                equippedItems[i] = null;
            }
        }
    }

    /// <summary>
    /// Equips all starter gear from the given job class data.
    /// Falls back to loading from Resources if starterEquipment array is empty.
    /// </summary>
    public void EquipStarterGear(JobClassData jobData)
    {
        if (jobData == null) return;

        if (jobData.starterEquipment != null && jobData.starterEquipment.Length > 0)
        {
            foreach (var item in jobData.starterEquipment)
            {
                if (item != null)
                    Equip(item);
            }
            return;
        }

        // Fallback: try loading from Resources by class ID convention
        string classId = jobData.jobId?.ToLower();
        if (string.IsNullOrEmpty(classId)) return;

        TryLoadAndEquip($"{classId}_sword");
        TryLoadAndEquip($"{classId}_staff");
        TryLoadAndEquip($"{classId}_dagger");
        TryLoadAndEquip($"{classId}_chainmail");
        TryLoadAndEquip($"{classId}_robe");
        TryLoadAndEquip($"{classId}_vest");
        TryLoadAndEquip($"{classId}_greaves");
        TryLoadAndEquip($"{classId}_shoes");
        TryLoadAndEquip($"{classId}_boots");
    }

    private void TryLoadAndEquip(string equipmentId)
    {
        var item = Resources.Load<EquipmentData>($"Equipment/{equipmentId}");
        if (item != null)
            Equip(item);
    }

    private void ApplyEquipmentEffects(EquipmentSlotType slot, EquipmentData item)
    {
        // Update character visuals
        if (playerAppearance == null)
            playerAppearance = GetComponent<PlayerAppearance>();

        if (playerAppearance != null)
        {
            switch (slot)
            {
                case EquipmentSlotType.Armor:
                    playerAppearance.SetPart(BodyPartSlot.Torso, item?.visualPart);
                    break;
                case EquipmentSlotType.Legs:
                    playerAppearance.SetPart(BodyPartSlot.Legs, item?.visualPart);
                    break;
                case EquipmentSlotType.Feet:
                    playerAppearance.SetPart(BodyPartSlot.Feet, item?.visualPart);
                    break;
                case EquipmentSlotType.Weapon:
                    playerAppearance.SetPart(BodyPartSlot.WeaponFront, item?.visualPart);
                    break;
                case EquipmentSlotType.Head:
                    playerAppearance.SetPart(BodyPartSlot.Hat, item?.visualPart);
                    break;
                case EquipmentSlotType.Hands:
                    playerAppearance.SetPart(BodyPartSlot.Gloves, item?.visualPart);
                    break;
            }
        }

        // Update combat weapon
        if (slot == EquipmentSlotType.Weapon)
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();

            if (combatController != null && item?.weaponData != null)
                combatController.EquipWeapon(item.weaponData);
        }
    }
}
