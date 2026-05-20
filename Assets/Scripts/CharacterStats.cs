using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int brawn = 16;
    public int finesse = 12;
public int wit = 10;
    public int grit = 12;

    [Header("Combat Stats")]
    public int critThreshold = 11;

    [Header("Current State")]
    public float currentHP;
    public float currentMana; 
    public float currentStamina;
    public float currentXP;
    public int coins;
    public bool isDead = false;

    [Header("Run Persistence")]
    public int pointsGainedThisRun = 0;
    public int permPointsToAssign = 0;

    [Header("Permanent Bonus Stats")]
    public int brawnPerm = 0;
    public int finessePerm = 0;
    public int witPerm = 0;
    public int gritPerm = 0;

    [Header("Settings")]
    public int manaRegenPerInterval = 1;
    public float regenInterval = 5.0f;
    private float regenTimer = 0f;

    public int maxXP = 100;
    public int maxStamina = 100;
    public int level = 1;
    public float amountPerKill = 5f;

    public float RegenTimeRemaining => (currentMana < MaxMana) ? Mathf.Max(0, regenInterval - regenTimer) : 0;

    private int cachedMaxHP;
    private int cachedMaxMana;
    private int cachedMaxXP;

    public int MaxHP => cachedMaxHP;
    public int MaxMana => cachedMaxMana;
    public int MaxXP => cachedMaxXP;
    public int MaxStamina => maxStamina;
    
    public int MeleeDamage 
    {
        get 
        {
            int effectiveBrawn = brawn + (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Brawn") : 0);
            int atkBonus = (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Attack") : 0);
            return effectiveBrawn + atkBonus;
        }
    }

    public int RangedDamage 
    {
        get 
        {
            int effectiveFinesse = finesse + (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Finesse") : 0);
            int atkBonus = (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Attack") : 0);
            return effectiveFinesse + atkBonus;
        }
    }

    public int Defense 
    {
        get 
        {
            int effectiveFinesse = finesse + (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Finesse") : 0);
            return effectiveFinesse / 2;
        }
    }

    public float CritChance => Mathf.Clamp01((13 - critThreshold) / 12f) * 100f;

    public void RefreshCachedStats()
    {
        int effectiveBrawn = brawn + (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Brawn") : 0);
        int effectiveGrit = grit + (EquipmentManager.Instance != null ? EquipmentManager.Instance.GetBonus("Grit") : 0);
        
        // Ensure minimum values for health and mana calculation
        effectiveBrawn = Mathf.Max(1, effectiveBrawn);
        effectiveGrit = Mathf.Max(1, effectiveGrit);

        cachedMaxHP = effectiveBrawn * 5 + 10;
        cachedMaxMana = effectiveGrit * 3 + 10;
        cachedMaxXP = (int)(100 * Mathf.Pow(1.5f, level - 1));

        float scale = 1.0f + (level - 1) * 0.1f; 
        transform.localScale = Vector3.one * scale;
        Debug.Log($"[CharacterStats] {name} Refreshed. Level: {level}, Scale: {scale}, MaxHP: {cachedMaxHP}, MaxMana: {cachedMaxMana}");
    }

    public void AddXP(float amount)
    {
        if (isDead) 
        {
            Debug.LogWarning($"[CharacterStats] {name} is dead. Ignoring XP.");
            return;
        }
        currentXP += amount;
        CombatSystem.SpawnText(transform.position + Vector3.up * 2.5f, $"+{amount} XP", Color.cyan);
        while (currentXP >= MaxXP) LevelUp();
    }

    private void LevelUp()
    {
        currentXP -= MaxXP;
        level++;
        RefreshCachedStats();

        string[] statsArr = { "Brawn", "Finesse", "Wit", "Grit" };
        string randomStat = statsArr[UnityEngine.Random.Range(0, statsArr.Length)];
        ApplyStatUpgrade(randomStat, 1, false); 
        
        currentHP = MaxHP;
        currentMana = MaxMana;
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 3.0f, "LEVEL UP!", Color.cyan);
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.2f, $"+1 {randomStat}!", Color.yellow);
        }
        ShowLevelUpPopup();
    }

    private void ShowLevelUpPopup()
    {
        string[] statsArr = { "Brawn", "Finesse", "Wit", "Grit" };
        List<string> choices = new List<string>(statsArr);
        string s1 = choices[Random.Range(0, choices.Count)]; choices.Remove(s1);
        string s2 = choices[Random.Range(0, choices.Count)]; choices.Remove(s2);
        string s3 = choices[Random.Range(0, choices.Count)];

        GenericPopup.Show("LEVEL UP!", $"Choose a stat to upgrade for Level {level}:", 
            $"+2 {s1}", $"+2 {s2}", $"+2 {s3}",
            () => ApplyStatUpgrade(s1, 2, true),
            () => ApplyStatUpgrade(s2, 2, true),
            () => ApplyStatUpgrade(s3, 2, true));
    }

    public void ApplyStatUpgrade(string statName, int amount, bool showFeedback)
    {
        switch (statName)
        {
            case "Brawn": brawn += amount; break;
            case "Finesse": finesse += amount; break;
            case "Wit": wit += amount; break;
            case "Grit": grit += amount; break;
        }
        pointsGainedThisRun += amount;
        permPointsToAssign = pointsGainedThisRun / 2;
        RefreshCachedStats();

        var statsUI = Object.FindAnyObjectByType<StatsUI>();
        if (statsUI != null) statsUI.Refresh();
        
        if (showFeedback) CombatSystem.Instance?.SpawnDamageText(transform.position + Vector3.up * 2f, $"+{amount} {statName}!", Color.green);
        
        currentHP = MaxHP;
        currentMana = MaxMana;
    }

    private void Start() { StartCoroutine(RegenRoutine()); }

    private IEnumerator RegenRoutine()
    {
        while (true)
        {
            if (!isDead && currentMana < MaxMana) RegenerateMana(manaRegenPerInterval);
            yield return new WaitForSeconds(regenInterval);
        }
    }

    public void ResetStats()
    {
        level = 1;
        isDead = false;
        pointsGainedThisRun = 0;
        RefreshCachedStats();
        currentHP = MaxHP;
        currentMana = MaxMana;
        currentStamina = MaxStamina;
        currentXP = 0; 
    }

    public void ConsumeMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        CombatSystem.Instance?.SpawnDamageText(transform.position + Vector3.up * 2.5f, $"-{amount} Energy", new Color(0.6f, 0f, 1f));
    }

    public void RegenerateMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, MaxMana);
    }

    public void AddGold(int amount)
    {
        coins += amount;
        CombatSystem.Instance?.SpawnDamageText(transform.position + Vector3.up * 2.0f, $"+{amount} Gold", new Color(1f, 0.84f, 0f));
    }

    private void Awake()
    {
        isDead = false;
        LoadPermStats();
        RefreshCachedStats();
        currentHP = MaxHP;
        currentMana = MaxMana;
        currentStamina = MaxStamina;

        // Assign layers for silhouette effect and occlusion
        int layer = GetComponent<EquipmentManager>() != null ? 7 : 8;
        SetLayerRecursive(gameObject, layer);
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    private void LoadPermStats()
    {
        brawnPerm = PlayerPrefs.GetInt("Perm_Brawn", 0);
        finessePerm = PlayerPrefs.GetInt("Perm_Finesse", 0);
        witPerm = PlayerPrefs.GetInt("Perm_Wit", 0);
        gritPerm = PlayerPrefs.GetInt("Perm_Grit", 0);
        brawn += brawnPerm; finesse += finessePerm; wit += witPerm; grit += gritPerm;

        // Visual growth based on perm upgrades
        int totalPerm = brawnPerm + finessePerm + witPerm + gritPerm;
        if (totalPerm > 0)
        {
            transform.localScale += Vector3.one * (totalPerm * 0.02f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        bool isPlayer = GetComponent<EquipmentManager>() != null;
        
        var ftue = FTUEManager.Instance;
        if (ftue == null) ftue = Object.FindAnyObjectByType<FTUEManager>();
        bool isFTUE = (ftue != null && ftue.isFTUEActive);

        // Apply defense reduction
float reducedAmount = Mathf.Max(1, amount - Defense);
        
        Debug.Log($"[CharacterStats] {name} taking damage. Amount: {amount}, Defense: {Defense}, Reduced: {reducedAmount}, HP: {currentHP} -> {currentHP - reducedAmount}");

        currentStamina = Mathf.Max(0, currentStamina - reducedAmount);
        currentHP -= reducedAmount;
        // Second Wind / FTUE Plot Armor: Only trigger if the manual setting is ON
        HeroSettings settings = GetComponent<HeroSettings>();
        bool secondWindEnabled = settings != null && settings.secondWind;

        if (currentHP <= 0 && secondWindEnabled && isPlayer)
        {
            currentHP = 1;
            CombatSystem.Instance?.SpawnDamageText(transform.position + Vector3.up * 2.5f, "SECOND WIND!", Color.green);
            return;
        }

        if (currentHP <= 0) { currentHP = 0; Die(); }
    }

    private void Die()
    {
        isDead = true;

        if (GetComponent<EquipmentManager>() != null)
        {
            // If the player dies during the tutorial, mark it as completed 
            // so they don't have to restart it on reload.
            PlayerPrefs.SetInt("FTUE_Completed_v14", 1);
            PlayerPrefs.Save();
            if (FTUEManager.Instance != null) FTUEManager.Instance.isFTUEActive = false;
        }
        
        Animator anim = GetComponent<Animator>();
if (anim != null) anim.SafeSetTrigger("Die");

        var nav = GetComponent<HeroNavigation>();
        if (nav != null) nav.StopMoving("Death");

        // Only trigger the death flow if this is Steve (the player)
        if (GetComponent<EquipmentManager>() != null)
        {
            // If in combat, let CombatSystem handle the sequence/popup
            // Otherwise, we'll need to show it here (future-proofing)
            if (CombatSystem.Instance == null || !CombatSystem.Instance.isInCombat)
            {
                StartCoroutine(DelayedDeathPopup());
            }
        }
        else
        {
            // If an enemy dies, the CombatSystem handles it via EndCombat(true) 
            // in the PlayerAttackSequence or OnReachedPOI.
            Debug.Log($"[CharacterStats] {name} (Enemy/Object) has been defeated.");
        }
    }

    private IEnumerator DelayedDeathPopup()
    {
        yield return new WaitForSeconds(2.0f);
        ShowDeathPopup();
    }

    public void ShowDeathPopup()
    {
        string[] statsArr = { "Brawn", "Finesse", "Wit", "Grit" };
        List<string> choices = new List<string>(statsArr);
        string s1 = choices[Random.Range(0, choices.Count)]; choices.Remove(s1);
        string s2 = choices[Random.Range(0, choices.Count)];
        int bonus = permPointsToAssign;

        GenericPopup.Show("STEVE HAS FALLEN", $"Choose a permanent enhancement for your next soul (+{bonus}):", 
            s1, s2, null,
            () => ApplyPermUpgrade(s1, bonus),
            () => ApplyPermUpgrade(s2, bonus));
    }

    private void ApplyPermUpgrade(string statName, int amount)
    {
        int current = PlayerPrefs.GetInt("Perm_" + statName, 0);
        PlayerPrefs.SetInt("Perm_" + statName, current + amount);
        PlayerPrefs.Save();
        GenericPopup.ResetForSceneLoad();
        EquipmentLootPopup.ResetForSceneLoad();
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
