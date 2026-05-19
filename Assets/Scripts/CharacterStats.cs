using UnityEngine;
using System.Collections;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int brawn = 14;
    public int finesse = 12;
    public int wit = 10;
    public int grit = 12;

    [Header("Combat Stats")]
    public int critThreshold = 11;

    [Header("Current State")]
    public float currentHP;
    public float currentMana; // Mana becomes Energy
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
    public float regenInterval = 5.0f; // Reduced from 15.0f for better flow
    private float regenTimer = 0f;

    public int maxXP = 100;
    public int maxStamina = 100;
    public int level = 1;
    public float amountPerKill = 5f; // New: amount per kill multiplier

    public float RegenTimeRemaining => (currentMana < cachedMaxMana) ? Mathf.Max(0, regenInterval - regenTimer) : 0;

    // Derived Stats (Cached)
    private int cachedMaxHP;
    private int cachedMaxMana;
    private int cachedMaxXP;

    public int MaxHP => cachedMaxHP;
    public int MaxMana => cachedMaxMana;
    public int MaxXP => cachedMaxXP;
    
    public int MaxStamina => maxStamina;
    public int MeleeDamage => brawn;
    public int RangedDamage => finesse;
    public int Defense => finesse / 2;

    private void RefreshCachedStats()
    {
        cachedMaxHP = brawn * 5 + 10;
        cachedMaxMana = grit * 3 + 10;
        cachedMaxXP = (int)(100 * Mathf.Pow(1.5f, level - 1));
    }

    public void AddXP(float amount)
    {
        if (isDead) return;
        currentXP += amount;
        Debug.Log($"[CharacterStats] Gained {amount} XP. Current: {currentXP}/{MaxXP}");
        
        CombatSystem.SpawnText(transform.position + Vector3.up * 2.5f, $"+{amount} XP", Color.cyan);

        while (currentXP >= MaxXP)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= MaxXP;
        level++;
        RefreshCachedStats();

        // Passive +1 to a random stat
        string[] stats = { "Brawn", "Finesse", "Wit", "Grit" };
        string randomStat = stats[UnityEngine.Random.Range(0, stats.Length)];
        ApplyStatUpgrade(randomStat, 1, false); 
        
        currentHP = MaxHP; // Heal on level up
        currentMana = MaxMana; // Refill Energy on level up
        Debug.Log($"[CharacterStats] LEVEL UP! Now Level {level}. Passive +1 {randomStat}. Next XP: {MaxXP}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 3.0f, "LEVEL UP!", Color.cyan);
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.2f, $"+1 {randomStat}!", Color.yellow);
        }

        ShowLevelUpPopup();
    }

    private void ShowLevelUpPopup()
    {
        // Pick 3 random stats to upgrade
        string[] stats = { "Brawn", "Finesse", "Wit", "Grit" };
        System.Collections.Generic.List<string> choices = new System.Collections.Generic.List<string>(stats);
        
        string s1 = choices[UnityEngine.Random.Range(0, choices.Count)];
        choices.Remove(s1);
        string s2 = choices[UnityEngine.Random.Range(0, choices.Count)];
        choices.Remove(s2);
        string s3 = choices[UnityEngine.Random.Range(0, choices.Count)];

        GenericPopup.Show(
            "LEVEL UP!",
            $"Choose a stat to upgrade for Level {level}:\n(You also gained a passive +1 in a random stat!)",
            $"+2 {s1}", $"+2 {s2}", $"+2 {s3}",
            () => ApplyStatUpgrade(s1, 2, true),
            () => ApplyStatUpgrade(s2, 2, true),
            () => ApplyStatUpgrade(s3, 2, true)
        );
    }

    public float CritChance => Mathf.Clamp01((13 - critThreshold) / 12f) * 100f;

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

        // Refresh UI
        var statsUI = UnityEngine.Object.FindAnyObjectByType<StatsUI>();
        if (statsUI != null) statsUI.Refresh();
        
        if (showFeedback)
        {
            CombatSystem.Instance?.SpawnDamageText(transform.position + Vector3.up * 2f, $"+{amount} {statName}!", Color.green);
        }
        
        // Ensure values are capped/refilled properly after stat change (e.g. MaxHP might have increased)
        currentHP = MaxHP;
        currentMana = MaxMana;
    }

    private void Start()
    {
        StartCoroutine(RegenRoutine());
    }

    private IEnumerator RegenRoutine()
    {
        while (!isDead)
        {
            if (currentMana < MaxMana)
            {
                yield return new WaitForSeconds(regenInterval);
                if (!isDead)
                {
                    RegenerateMana(manaRegenPerInterval);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }
    }

    public void ResetStats()
    {
        level = 1;
        currentHP = MaxHP;
        currentMana = MaxMana;
        currentStamina = MaxStamina;
        currentXP = 0; 
        isDead = false;
        pointsGainedThisRun = 0;
    }

    public void ConsumeMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        Debug.Log($"[CharacterStats] Consumed {amount} Energy. Current: {currentMana}/{MaxMana}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.5f, $"-{amount} Energy", new Color(0.6f, 0f, 1f));
        }
    }

    public void RegenerateMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, MaxMana);
        Debug.Log($"[CharacterStats] Regenerated {amount} Energy. Current: {currentMana}/{MaxMana}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.5f, $"+{amount} Energy", new Color(0.7f, 0.2f, 1f));
        }
    }

    public void AddGold(int amount)
    {
        coins += amount;
        Debug.Log($"[CharacterStats] Gained {amount} Gold. Total: {coins}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.0f, $"+{amount} Gold", new Color(1f, 0.84f, 0f));
        }
    }

    private void Awake()
{
        LoadPermStats();
        RefreshCachedStats();
        currentHP = MaxHP;
        currentMana = MaxMana;
        currentStamina = MaxStamina;
    }

    private void LoadPermStats()
    {
        brawnPerm = PlayerPrefs.GetInt("Perm_Brawn", 0);
        finessePerm = PlayerPrefs.GetInt("Perm_Finesse", 0);
        witPerm = PlayerPrefs.GetInt("Perm_Wit", 0);
        gritPerm = PlayerPrefs.GetInt("Perm_Grit", 0);

        brawn += brawnPerm;
        finesse += finessePerm;
        wit += witPerm;
        grit += gritPerm;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        // Stamina depletes alongside HP when taking damage
        currentStamina = Mathf.Max(0, currentStamina - amount);
        currentHP = Mathf.Max(0, currentHP - amount);
        
        if (currentHP <= 0) 
        {
            currentHP = 0;
            if (CombatSystem.Instance != null && this == CombatSystem.Instance.playerStats)
            {
                Die();
            }
            else
            {
                isDead = true;
            }
        }
}

    private void Die()
    {
        isDead = true;
        Debug.Log("[CharacterStats] Steve has died!");
        
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            CombatSystem.Instance.EndCombat(false);
        }

        // Pick 2 random stats for the permanent upgrade choice
string[] stats = { "Brawn", "Finesse", "Wit", "Grit" };
        System.Collections.Generic.List<string> choices = new System.Collections.Generic.List<string>(stats);
        
        string s1 = choices[UnityEngine.Random.Range(0, choices.Count)];
        choices.Remove(s1);
        string s2 = choices[UnityEngine.Random.Range(0, choices.Count)];

        int bonus = permPointsToAssign;

        GenericPopup.Show(
            "STEVE HAS FALLEN",
            $"You gained {pointsGainedThisRun} points this run.\nChoose a permanent enhancement for your next soul (+{bonus}):",
            s1, s2, null,
            () => ApplyPermUpgrade(s1, bonus),
            () => ApplyPermUpgrade(s2, bonus)
        );
    }

    private void ApplyPermUpgrade(string statName, int amount)
    {
        int current = PlayerPrefs.GetInt("Perm_" + statName, 0);
        PlayerPrefs.SetInt("Perm_" + statName, current + amount);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterStats] Permanent Upgrade: +{amount} {statName}. Persisting to next run.");
        
        // Reload scene to restart the game
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    }



