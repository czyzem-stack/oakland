using UnityEngine;
using System.Collections.Generic;

public enum EquipmentSlot { Helmet, Weapon, Chest, Cloak, Gloves, Boots }

[System.Serializable]
public class EquipmentItem
{
    public string name;
    public EquipmentSlot slot;
    public int brawnBonus;
    public int finesseBonus;
    public int witBonus;
    public int gritBonus;
    public int attackBonus;
}

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [Header("Visual References")]
    public GameObject[] helmetObjects; // All available helmets (HeadArmor01-05)
    public GameObject[] bodyObjects; // All available armor models (Body01-05)
    public GameObject defaultBodyObject; // Base body (Body01)
    public GameObject[] cloakObjects; // All available cloaks (Cloak01-03)
    
    [Header("Weapon Visuals")]
    public GameObject[] weaponObjects; // All weapons in hierarchy
    public GameObject shieldObject;
    public Sprite[] helmetIcons;
    public Sprite[] chestIcons;
    public Sprite[] weaponIcons; // All generated/assigned icons
    public Sprite[] cloakIcons; // Corresponding icons for cloakObjects
    public Sprite[] gloveIcons; // Icons for stat-only gloves
    public Sprite[] bootIcons; // Icons for stat-only boots

    [Header("Weapon Animations")]
    public AnimationClip attackSingleSword;
    public AnimationClip attackTwoHandSword;
    public AnimationClip attackBow;
    public AnimationClip attackSpear;
    public AnimationClip attackMagicWand;

    [Header("Current Equipment")]
    public EquipmentItem currentHelmet;
    public EquipmentItem currentWeapon;
    public EquipmentItem currentChest;
    public EquipmentItem currentCloak;
    public EquipmentItem currentGloves;
    public EquipmentItem currentBoots;

    private Animator animator;
    private CharacterStats stats;
    private AnimatorOverrideController overrideController;

    private void Awake()
    {
        Instance = this;
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }
    }

    private void Start()
    {
        // Set initial state
        RefreshVisuals();
        RefreshAnimations();
        
        // Ensure starting state is visually clean
        if (string.IsNullOrEmpty(currentWeapon?.name))
        {
            if (shieldObject != null) shieldObject.SetActive(false);
            foreach (var w in weaponObjects) if (w != null) w.SetActive(false);
        }
    }

    public void Equip(EquipmentItem item)
    {
        if (item == null) return;
        Debug.Log($"[EquipmentManager] Equipping: {item.name} in slot {item.slot}");

        if (item.slot == EquipmentSlot.Helmet)
        {
            currentHelmet = item;
        }
        else if (item.slot == EquipmentSlot.Weapon)
        {
            currentWeapon = item;
        }
        else if (item.slot == EquipmentSlot.Chest)
        {
            currentChest = item;
        }
        else if (item.slot == EquipmentSlot.Cloak)
        {
            currentCloak = item;
        }
        else if (item.slot == EquipmentSlot.Gloves)
        {
            currentGloves = item;
        }
        else if (item.slot == EquipmentSlot.Boots)
        {
            currentBoots = item;
        }

        RefreshVisuals();
        RefreshAnimations();
        
        if (stats != null) 
        {
            stats.RefreshCachedStats();
            Debug.Log($"[EquipmentManager] Stats Refreshed. New MaxHP: {stats.MaxHP}, MeleeDamage: {stats.MeleeDamage}");
        }
        
        var statsUI = Object.FindAnyObjectByType<StatsUI>();
        if (statsUI != null) statsUI.Refresh();
    }

    public void RefreshAnimations()
    {
        if (overrideController == null) 
        {
            Debug.LogWarning("[EquipmentManager] No AnimatorOverrideController found!");
            return;
        }

        AnimationClip targetClip = attackSingleSword; // Default

        if (currentWeapon != null && !string.IsNullOrEmpty(currentWeapon.name))
        {
            string weaponName = currentWeapon.name.ToLower();
            
            if (weaponName.Contains("bow")) targetClip = attackBow;
            else if (weaponName.Contains("spear")) targetClip = attackSpear;
            else if (weaponName.Contains("wand")) targetClip = attackMagicWand;
            else if (weaponName.Contains("greatsword")) targetClip = attackTwoHandSword;
            
            Debug.Log($"[EquipmentManager] Animation Refreshed for {currentWeapon.name}. Target: {targetClip?.name}");
        }

        if (targetClip != null)
        {
            overrideController["Attack01_SwordAndShiled"] = targetClip;
        }
    }

    public void RefreshVisuals()
    {
        // Handle Helmet
        bool hasHelmet = currentHelmet != null && !string.IsNullOrEmpty(currentHelmet.name);
        string[] helmetNames = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
        
        for (int i = 0; i < helmetObjects.Length; i++)
        {
            if (helmetObjects[i] == null) continue;
            bool isActive = hasHelmet && currentHelmet.name == helmetNames[i];
            helmetObjects[i].SetActive(isActive);
        }

        // Handle Chest
        bool hasChest = currentChest != null && !string.IsNullOrEmpty(currentChest.name);
        string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
        
        bool anyArmorActive = false;
        for (int i = 0; i < bodyObjects.Length; i++)
        {
            if (bodyObjects[i] == null) continue;
            
            bool isActive = hasChest && currentChest.name == armorNames[i];
            bodyObjects[i].SetActive(isActive);
            if (isActive) anyArmorActive = true;
        }

        if (defaultBodyObject != null)
        {
            // The default body (Body01) should be active if:
            // 1. No armor is equipped
            // 2. The equipped armor IS "Padded Cloth" (which uses Body01)
            bool isPaddedCloth = hasChest && currentChest.name == "Padded Cloth";
            defaultBodyObject.SetActive(!anyArmorActive || isPaddedCloth);
        }

        // Handle Cloak
        bool hasCloak = currentCloak != null && !string.IsNullOrEmpty(currentCloak.name);
        string[] cloakNames = { "Traveler's Cloak", "Ranger Cape", "Royal Mantle" };

        for (int i = 0; i < cloakObjects.Length; i++)
        {
            if (cloakObjects[i] == null) continue;
            bool isActive = hasCloak && currentCloak.name == cloakNames[i];
            cloakObjects[i].SetActive(isActive);
        }

        // Handle Weapons
        bool hasWeapon = currentWeapon != null && !string.IsNullOrEmpty(currentWeapon.name);
        bool isTwoHanded = false;

        if (hasWeapon)
        {
            string weaponName = currentWeapon.name.ToLower();
            if (weaponName.Contains("bow") || weaponName.Contains("spear") || weaponName.Contains("greatsword")) 
                isTwoHanded = true;
        }

        int activeCount = 0;
        for (int i = 0; i < weaponObjects.Length; i++)
        {
            if (weaponObjects[i] == null) continue;
            
            bool isMatch = hasWeapon && IsWeaponMatch(currentWeapon.name, weaponObjects[i].name);
            bool isActive = false;

            if (isMatch)
            {
                string parentName = weaponObjects[i].transform.parent.name;
                
                if (currentWeapon.name.Contains("Bow"))
                {
                    // Bows are in weapon_l
                    if (parentName == "Bows") isActive = true;
                }
                else
                {
                    // Everything else (Sword, Axe, Spear, etc.) in weapon_r
                    if (parentName == "weapon_r") isActive = true;
                }
            }

            weaponObjects[i].SetActive(isActive);
            if (isActive) activeCount++;
        }

        if (shieldObject != null)
        {
            // Shield only active if we HAVE a weapon and it's NOT two-handed
            shieldObject.SetActive(hasWeapon && !isTwoHanded);
        }
        
        Debug.Log($"[EquipmentManager] Visuals Refreshed. Weapon: {currentWeapon?.name} (Active Models: {activeCount}), Armor: {currentChest?.name}, Helmet: {currentHelmet?.name}, Cloak: {currentCloak?.name}, Gloves: {currentGloves?.name}, Boots: {currentBoots?.name}");
        }

        private bool IsWeaponMatch(string itemName, string objectName)
        {
        // Direct matches for special items
        if (itemName == "Hunting Bow" && objectName == "Bow01") return true;
        if (itemName == "Iron Spear" && objectName == "Spear01") return true;
        if (itemName == "Stronger Stick" && objectName == "OHS01_Stick") return true;
        
        string lowerItem = itemName.ToLower();
        string lowerObject = objectName.ToLower();

        // Check for specific numbered items (e.g. "Sword 03")
        string[] parts = itemName.Split(' ');
        string lastPart = parts[parts.Length - 1];
        
        if (int.TryParse(lastPart, out _))
        {
             // Must match both the base type and the specific number
             bool typeMatch = false;
             if (lowerItem.Contains("sword") && lowerObject.Contains("sword")) typeMatch = true;
             else if (lowerItem.Contains("axe") && lowerObject.Contains("axe")) typeMatch = true;
             else if (lowerItem.Contains("hammer") && lowerObject.Contains("hammer")) typeMatch = true;
             else if (lowerItem.Contains("bow") && lowerObject.Contains("bow")) typeMatch = true;
             else if (lowerItem.Contains("spear") && lowerObject.Contains("spear")) typeMatch = true;
             else if (lowerItem.Contains("wand") && lowerObject.Contains("wand")) typeMatch = true;

             return typeMatch && lowerObject.Contains(lastPart);
        }

        // Fallback for categorized matching without explicit numbers
        if (lowerItem.Contains("sword") && lowerObject.Contains("sword") && lowerObject.Contains("03")) return true;
        if (lowerItem.Contains("axe") && lowerObject.Contains("axe") && lowerObject.Contains("10")) return true;
        if (lowerItem.Contains("hammer") && lowerObject.Contains("hammer") && lowerObject.Contains("11")) return true;
        if (lowerItem.Contains("wand") && lowerObject.Contains("wand") && lowerObject.Contains("01")) return true;

        return false;
        }

        public int GetBonus(string statName)
        {
            int total = 0;
            if (IsEquipped(currentHelmet))
            {
                if (statName == "Brawn") total += currentHelmet.brawnBonus;
                if (statName == "Finesse") total += currentHelmet.finesseBonus;
                if (statName == "Wit") total += currentHelmet.witBonus;
                if (statName == "Grit") total += currentHelmet.gritBonus;
                if (statName == "Attack") total += currentHelmet.attackBonus;
            }
            if (IsEquipped(currentWeapon))
            {
                if (statName == "Brawn") total += currentWeapon.brawnBonus;
                if (statName == "Finesse") total += currentWeapon.finesseBonus;
                if (statName == "Wit") total += currentWeapon.witBonus;
                if (statName == "Grit") total += currentWeapon.gritBonus;
                if (statName == "Attack") total += currentWeapon.attackBonus;
            }
            if (IsEquipped(currentChest))
            {
                if (statName == "Brawn") total += currentChest.brawnBonus;
                if (statName == "Finesse") total += currentChest.finesseBonus;
                if (statName == "Wit") total += currentChest.witBonus;
                if (statName == "Grit") total += currentChest.gritBonus;
                if (statName == "Attack") total += currentChest.attackBonus;
            }
            if (IsEquipped(currentCloak))
            {
                if (statName == "Brawn") total += currentCloak.brawnBonus;
                if (statName == "Finesse") total += currentCloak.finesseBonus;
                if (statName == "Wit") total += currentCloak.witBonus;
                if (statName == "Grit") total += currentCloak.gritBonus;
                if (statName == "Attack") total += currentCloak.attackBonus;
            }
            if (IsEquipped(currentGloves))
            {
                if (statName == "Brawn") total += currentGloves.brawnBonus;
                if (statName == "Finesse") total += currentGloves.finesseBonus;
                if (statName == "Wit") total += currentGloves.witBonus;
                if (statName == "Grit") total += currentGloves.gritBonus;
                if (statName == "Attack") total += currentGloves.attackBonus;
            }
            if (IsEquipped(currentBoots))
            {
                if (statName == "Brawn") total += currentBoots.brawnBonus;
                if (statName == "Finesse") total += currentBoots.finesseBonus;
                if (statName == "Wit") total += currentBoots.witBonus;
                if (statName == "Grit") total += currentBoots.gritBonus;
                if (statName == "Attack") total += currentBoots.attackBonus;
            }
            return total;
        }

        private bool IsEquipped(EquipmentItem item)
        {
            return item != null && !string.IsNullOrEmpty(item.name);
        }
        }
