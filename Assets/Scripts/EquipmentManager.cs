using UnityEngine;
using System.Collections.Generic;

public enum EquipmentSlot { Helmet, Weapon, Shield, Chest, Cloak, Gloves, Boots }

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
    public GameObject[] helmetObjects;
    public GameObject[] bodyObjects;
    public GameObject defaultBodyObject;
    public GameObject[] cloakObjects;
    public GameObject[] gloveObjects;
    public GameObject[] bootObjects;
    public GameObject[] shieldObjects;
    
    [Header("Weapon Visuals")]
    public GameObject[] weaponObjects;
    public GameObject shieldObject;
    public Sprite[] helmetIcons;
    public Sprite[] chestIcons;
    public Sprite[] weaponIcons;
    public Sprite[] shieldIcons;
    public Sprite[] cloakIcons;
    public Sprite[] gloveIcons;
    public Sprite[] bootIcons;

    [Header("Weapon Animations")]
    public AnimationClip attackSingleSword;
    public AnimationClip attackTwoHandSword;
    public AnimationClip attackBow;
    public AnimationClip attackSpear;
    public AnimationClip attackMagicWand;

    [Header("Locomotion Overrides")]
    public AnimationClip idleUnarmed;
    public AnimationClip walkUnarmed;
    public AnimationClip runUnarmed;
    public AnimationClip victoryUnarmed;
    public AnimationClip dieUnarmed;
    public AnimationClip getHitUnarmed;

    public AnimationClip idleOneHanded;
    public AnimationClip walkOneHanded;
    public AnimationClip runOneHanded;
    public AnimationClip victoryOneHanded;
    public AnimationClip dieOneHanded;
    public AnimationClip getHitOneHanded;

    [Header("Current Equipment")]
public EquipmentItem currentHelmet;
    public EquipmentItem currentWeapon;
    public EquipmentItem currentShield;
    public EquipmentItem currentChest;
    public EquipmentItem currentCloak;
    public EquipmentItem currentGloves;
    public EquipmentItem currentBoots;

    private Animator animator;
    private CharacterStats stats;
    private AnimatorOverrideController overrideController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }

        // Add a collider for selection if missing
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 1, 0);
            box.size = new Vector3(1, 2, 1);
        }

        // Steve starts naked per user requirements
        currentChest = null;
        currentHelmet = null;
        currentWeapon = null;
        currentShield = null;
        currentCloak = null;
        currentGloves = null;
        currentBoots = null;

        // Force an immediate refresh to hide everything
        RefreshVisuals();
    }

    private void Start()
    {
        RefreshAnimations();
    }

    private EquipmentItem previewHelmet;
    private EquipmentItem previewWeapon;
    private EquipmentItem previewShield;
    private EquipmentItem previewChest;
    private EquipmentItem previewCloak;
    private EquipmentItem previewGloves;
    private EquipmentItem previewBoots;

    public void Preview(EquipmentItem item)
    {
        if (item == null) { ClearPreview(); return; }
        
        // Clear all previews first to ensure we only preview ONE item at a time
        previewHelmet = null;
        previewWeapon = null;
        previewShield = null;
        previewChest = null;
        previewCloak = null;
        previewGloves = null;
        previewBoots = null;

        if (item.slot == EquipmentSlot.Helmet) previewHelmet = item;
        else if (item.slot == EquipmentSlot.Weapon) previewWeapon = item;
        else if (item.slot == EquipmentSlot.Shield) previewShield = item;
        else if (item.slot == EquipmentSlot.Chest) previewChest = item;
        else if (item.slot == EquipmentSlot.Cloak) previewCloak = item;
        else if (item.slot == EquipmentSlot.Gloves) previewGloves = item;
        else if (item.slot == EquipmentSlot.Boots) previewBoots = item;

        RefreshVisuals();
    }

    public void ClearPreview()
    {
        previewHelmet = null;
        previewWeapon = null;
        previewShield = null;
        previewChest = null;
        previewCloak = null;
        previewGloves = null;
        previewBoots = null;
        RefreshVisuals();
    }

    public void Equip(EquipmentItem item)
    {
        if (item == null) return;
        Debug.Log($"[EquipmentManager] Equipping: {item.name} in slot {item.slot}");

        bool isStatOnly = false;
        if (item.slot == EquipmentSlot.Gloves && (gloveObjects == null || gloveObjects.Length == 0)) isStatOnly = true;
        if (item.slot == EquipmentSlot.Boots && (bootObjects == null || bootObjects.Length == 0)) isStatOnly = true;

        if (item.slot == EquipmentSlot.Helmet) currentHelmet = item;
        else if (item.slot == EquipmentSlot.Weapon) currentWeapon = item;
        else if (item.slot == EquipmentSlot.Shield) currentShield = item;
        else if (item.slot == EquipmentSlot.Chest) currentChest = item;
        else if (item.slot == EquipmentSlot.Cloak) currentCloak = item;
        else if (item.slot == EquipmentSlot.Gloves) currentGloves = item;
        else if (item.slot == EquipmentSlot.Boots) currentBoots = item;

        // Clear preview of the same slot
        if (item.slot == EquipmentSlot.Helmet) previewHelmet = null;
        if (item.slot == EquipmentSlot.Weapon) previewWeapon = null;
        if (item.slot == EquipmentSlot.Shield) previewShield = null;
        if (item.slot == EquipmentSlot.Chest) previewChest = null;
        if (item.slot == EquipmentSlot.Cloak) previewCloak = null;
        if (item.slot == EquipmentSlot.Gloves) previewGloves = null;
        if (item.slot == EquipmentSlot.Boots) previewBoots = null;

        RefreshVisuals();
        RefreshAnimations();
        
        if (stats != null) stats.RefreshCachedStats();
        
        StartCoroutine(EquipPulseRoutine());

        // Visual Feedback
        string feedbackText = isStatOnly ? $"{item.name} Equipped! (Attributes Boosted)" : $"{item.name} Equipped!";
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.5f, feedbackText, Color.white);
        }

        var statsUI = Object.FindAnyObjectByType<StatsUI>();
        if (statsUI != null) statsUI.Refresh();
    }

    private System.Collections.IEnumerator EquipPulseRoutine()
    {
        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale * 1.15f;
        yield return new WaitForSeconds(0.15f);
        transform.localScale = baseScale;
    }

    public void RefreshAnimations()
    {
        if (overrideController == null)
        {
             // Re-initialize if lost
             if (animator != null && animator.runtimeAnimatorController != null)
             {
                 if (animator.runtimeAnimatorController is AnimatorOverrideController existing)
                     overrideController = existing;
                 else
                 {
                     overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                     animator.runtimeAnimatorController = overrideController;
                 }
             }
        }
        
        if (overrideController == null) return;

        // 1. Attack Animation
        AnimationClip targetAttack = attackSingleSword;
        if (IsEquipped(currentWeapon))
        {
            string weaponName = currentWeapon.name.ToLower();
            if (weaponName.Contains("bow")) targetAttack = attackBow;
            else if (weaponName.Contains("spear")) targetAttack = attackSpear;
            else if (weaponName.Contains("wand")) targetAttack = attackMagicWand;
            else if (weaponName.Contains("greatsword") || weaponName.Contains("hammer")) targetAttack = attackTwoHandSword;
        }
        overrideController["Attack01_SwordAndShiled"] = targetAttack;

        // 2. Locomotion & Aux Logic
        AnimationClip locomotionIdle = null;
        AnimationClip locomotionWalk = null;
        AnimationClip locomotionRun = null;
        AnimationClip locomotionVictory = null;
        AnimationClip locomotionDie = null;
        AnimationClip locomotionGetHit = null;

        if (IsEquipped(currentShield))
        {
            // Use base (Sword and Shield) - null in override means use original
        }
        else if (IsEquipped(currentWeapon))
        {
            // Single Sword / One Handed
            locomotionIdle = idleOneHanded;
            locomotionWalk = walkOneHanded;
            locomotionRun = runOneHanded;
            locomotionVictory = victoryOneHanded;
            locomotionDie = dieOneHanded;
            locomotionGetHit = getHitOneHanded;
        }
        else
        {
            // Unarmed
            locomotionIdle = idleUnarmed;
            locomotionWalk = walkUnarmed;
            locomotionRun = runUnarmed;
            locomotionVictory = victoryUnarmed;
            locomotionDie = dieUnarmed;
            locomotionGetHit = getHitUnarmed;
        }

        // Only override locomotion when all three blend clips are assigned (partial overrides break the blend tree)
        bool hasLocomotion = locomotionIdle != null && locomotionWalk != null && locomotionRun != null;
        overrideController["Idle_Normal_SwordAndShield"] = hasLocomotion ? locomotionIdle : null;
        overrideController["MoveFWD_Normal_InPlace_SwordAndShield"] = hasLocomotion ? locomotionWalk : null;
        overrideController["SprintFWD_Battle_InPlace_SwordAndShield"] = hasLocomotion ? locomotionRun : null;

        overrideController["Victory_Battle_SwordAndShield"] = locomotionVictory;
        overrideController["Die01_SwordAndShield"] = locomotionDie;
        overrideController["GetHit01_SwordAndShield"] = locomotionGetHit;
        
        Debug.Log($"[EquipmentManager] Refreshed animations. Mode: {(IsEquipped(currentShield) ? "Shield" : (IsEquipped(currentWeapon) ? "OneHanded" : "Unarmed"))}");
    }

    public void RefreshVisuals()
    {
        EquipmentItem helm = previewHelmet ?? currentHelmet;
        EquipmentItem weapon = previewWeapon ?? currentWeapon;
        EquipmentItem shieldItem = previewShield ?? currentShield;
        EquipmentItem chest = previewChest ?? currentChest;
        EquipmentItem cloak = previewCloak ?? currentCloak;
        EquipmentItem gloves = previewGloves ?? currentGloves;
        EquipmentItem boots = previewBoots ?? currentBoots;

        bool hasHelmet = IsEquipped(helm);

        // Comprehensive cleanup of Head children (Hats, Accessories, etc.)
        Transform head = transform.Find("root/pelvis/spine_01/spine_02/spine_03/neck_01/head");
        if (head != null)
        {
            foreach (Transform child in head)
            {
                // Hide all potential head gear
                if (child.name.StartsWith("Hat") || child.name.StartsWith("AC") || child.name.StartsWith("HeadArmor"))
                {
                    child.gameObject.SetActive(false);
                }
                
                // Hair logic: only show Hair01 if no helmet is on
                if (child.name.StartsWith("Hair"))
                {
                    child.gameObject.SetActive(!hasHelmet && child.name == "Hair01");
                }
            }
        }

        string[] helmetNames = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
        for (int i = 0; i < helmetObjects.Length; i++)
        {
            if (helmetObjects[i] == null) continue;
            helmetObjects[i].SetActive(hasHelmet && helm.name == helmetNames[i]);
        }

        bool hasChest = IsEquipped(chest);
        bool hasGloves = IsEquipped(gloves);
        bool hasBoots = IsEquipped(boots);

        string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
        string[] gloveNames = { "Leather Gloves", "Plate Gauntlets", "Silk Mitts" };
        string[] bootNames = { "Leather Boots", "Iron Greaves", "Swift Shoes" };
        
        // Ensure all body objects are hidden first
        if (defaultBodyObject != null) defaultBodyObject.SetActive(false);
        foreach (var b in bodyObjects) if (b != null) b.SetActive(false);
        if (gloveObjects != null) foreach (var g in gloveObjects) if (g != null) g.SetActive(false);
        if (bootObjects != null) foreach (var b in bootObjects) if (b != null) b.SetActive(false);

        bool armorApplied = false;
        if (hasChest)
        {
            for (int i = 0; i < armorNames.Length; i++)
            {
                if (chest.name == armorNames[i])
                {
                    if (i < bodyObjects.Length && bodyObjects[i] != null)
                    {
                        bodyObjects[i].SetActive(true);
                        armorApplied = true;
                    }
                    break;
                }
            }
        }

        // Glove Logic
        if (hasGloves && gloveObjects != null)
        {
            for (int i = 0; i < gloveNames.Length; i++)
            {
                if (gloves.name == gloveNames[i])
                {
                    if (i < gloveObjects.Length && gloveObjects[i] != null)
                        gloveObjects[i].SetActive(true);
                    break;
                }
            }
        }

        // Boot Logic
        if (hasBoots && bootObjects != null)
        {
            for (int i = 0; i < bootNames.Length; i++)
            {
                if (boots.name == bootNames[i])
                {
                    if (i < bootObjects.Length && bootObjects[i] != null)
                        bootObjects[i].SetActive(true);
                    break;
                }
            }
        }

        // Fallback to default if no armor matches or is equipped
        if (!armorApplied && defaultBodyObject != null)
        {
            defaultBodyObject.SetActive(true);
        }

        bool hasCloak = IsEquipped(cloak);
        string[] cloakNames = { "Traveler's Cloak", "Ranger Cape", "Royal Mantle" };
        for (int i = 0; i < cloakObjects.Length; i++)
        {
            if (cloakObjects[i] == null) continue;
            cloakObjects[i].SetActive(hasCloak && cloak.name == cloakNames[i]);
        }

        bool hasWeapon = IsEquipped(weapon);
        bool hasBow = hasWeapon && weapon.name.Contains("Bow");

        // Handle Bow/Arrow containers
        Transform bowsContainer = transform.Find("root/pelvis/spine_01/spine_02/spine_03/clavicle_l/upperarm_l/lowerarm_l/hand_l/weapon_l/Bows");
        Transform arrowsContainer = transform.Find("root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r/lowerarm_r/hand_r/weapon_r/Arrows");
        if (bowsContainer != null) bowsContainer.gameObject.SetActive(hasBow);
        if (arrowsContainer != null) arrowsContainer.gameObject.SetActive(hasBow);

        // Weapon Logic
        for (int i = 0; i < weaponObjects.Length; i++)
        {
            if (weaponObjects[i] == null) continue;
            bool isMatch = hasWeapon && IsWeaponMatch(weapon.name, weaponObjects[i].name);
            bool isActive = false;
            if (isMatch)
            {
                string parentName = weaponObjects[i].transform.parent.name;
                if (hasBow) 
                { 
                    if (parentName == "Bows") isActive = true; 
                }
                else 
                { 
                    if (parentName == "weapon_r") isActive = true; 
                }
                
                if (isActive) Debug.Log($"[EquipmentManager] Activated weapon visual: {weaponObjects[i].name} for item {weapon.name}");
            }
            weaponObjects[i].SetActive(isActive);
        }

        // Shield Logic
        bool hasShield = IsEquipped(shieldItem);
        string[] shieldNames = { "Log", "Iron Shield", "Steel Shield", "Magic Shield" }; 
        if (shieldObjects != null)
        {
            for (int i = 0; i < shieldObjects.Length; i++)
            {
                if (shieldObjects[i] == null) continue;
                bool isMatch = false;
                if (hasShield)
                {
                    for (int j = 0; j < shieldNames.Length; j++)
                    {
                        if (shieldItem.name == shieldNames[j])
                        {
                            if (shieldObjects[i].name == "Shield" + (j + 1).ToString("D2"))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                }
                shieldObjects[i].SetActive(isMatch);
            }
        }
        else if (shieldObject != null)
        {
            shieldObject.SetActive(hasShield && shieldItem.name == "Log");
        }

        // Handle child arrows (visuals on player)
        string currentArrowName = "";
        if (hasBow)
        {
            // Try to match arrow index to bow index (e.g. Bow01 -> Arrow01)
            string weaponName = weapon.name;
            if (weaponName == "Hunting Bow") currentArrowName = "Arrow01";
            else
            {
                string lastPart = weaponName.Split(' ')[weaponName.Split(' ').Length - 1];
                if (int.TryParse(lastPart, out int idx)) currentArrowName = "Arrow" + idx.ToString("D2");
                else currentArrowName = "Arrow01";
            }
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("Arrow")) 
            {
                if (child.parent == arrowsContainer || child == arrowsContainer)
                {
                    if (child == arrowsContainer) child.gameObject.SetActive(hasBow);
                    else child.gameObject.SetActive(hasBow && child.name == currentArrowName);
                }
            }
        }
        }

    public void BindSkinnedMesh(SkinnedMeshRenderer smr)
    {
        if (smr == null) return;
        
        Transform[] allBones = GetComponentsInChildren<Transform>();
        Transform[] newBones = new Transform[smr.bones.Length];
        
        for (int i = 0; i < smr.bones.Length; i++)
        {
            string boneName = smr.bones[i].name;
            foreach (Transform t in allBones)
            {
                if (t.name == boneName)
                {
                    newBones[i] = t;
                    break;
                }
            }
        }
        
        smr.bones = newBones;
        smr.rootBone = transform.Find("root"); // Assumption based on hierarchy dump
    }

    private bool IsWeaponMatch(string itemName, string objectName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        
        string lowerItem = itemName.ToLower().Trim();
        string lowerObject = objectName.ToLower().Trim();

        if (lowerItem == "hunting bow" && lowerObject == "bow01") return true;
        if (lowerItem == "iron spear" && lowerObject == "spear01") return true;
        if ((lowerItem == "stronger stick" || lowerItem == "wooden stick") && lowerObject == "ohs01_stick") return true;
        
        string[] parts = lowerItem.Split(' ');
        string lastPart = parts[parts.Length - 1];
        
        if (int.TryParse(lastPart, out _))
        {
             bool typeMatch = false;
             if (lowerItem.Contains("sword") && lowerObject.Contains("sword")) typeMatch = true;
             else if (lowerItem.Contains("axe") && lowerObject.Contains("axe")) typeMatch = true;
             else if (lowerItem.Contains("hammer") && lowerObject.Contains("hammer")) typeMatch = true;
             else if (lowerItem.Contains("bow") && lowerObject.Contains("bow")) typeMatch = true;
             else if (lowerItem.Contains("spear") && lowerObject.Contains("spear")) typeMatch = true;
             else if (lowerItem.Contains("wand") && lowerObject.Contains("wand")) typeMatch = true;
             return typeMatch && lowerObject.Contains(lastPart);
        }
        return false;
    }

    public int GetBonus(string statName)
    {
        int total = 0;
        EquipmentItem[] slots = { currentHelmet, currentWeapon, currentShield, currentChest, currentCloak, currentGloves, currentBoots };
        foreach (var item in slots)
        {
            if (IsEquipped(item))
            {
                if (statName == "Brawn") total += item.brawnBonus;
                else if (statName == "Finesse") total += item.finesseBonus;
                else if (statName == "Wit") total += item.witBonus;
                else if (statName == "Grit") total += item.gritBonus;
                else if (statName == "Attack") total += item.attackBonus;
            }
        }
        return total;
    }

    private bool IsEquipped(EquipmentItem item)
    {
        return item != null && !string.IsNullOrEmpty(item.name);
    }
}
