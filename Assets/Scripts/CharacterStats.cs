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

    [Header("Settings")]
    public int manaRegenPerInterval = 1;
    public float regenInterval = 15.0f;
    private float regenTimer = 0f;

    public int maxXP = 100;
    public int maxStamina = 100;
    public int level = 1;
    public float amountPerKill = 5f; // New: amount per kill multiplier

    public float RegenTimeRemaining => (currentMana < MaxMana) ? Mathf.Max(0, regenInterval - regenTimer) : 0;

    // Derived Stats
    public int MaxHP => brawn * 5 + 10;
    public int MaxMana => grit * 3 + 10; // Max Energy
    public int MaxStamina => maxStamina;
    public int MaxXP => (int)(100 * Mathf.Pow(1.5f, level - 1)); // Exponential curve
    public int MeleeDamage => brawn;
    public int RangedDamage => finesse;
    public int Defense => finesse / 2;

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
        currentHP = MaxHP; // Heal on level up
        Debug.Log($"[CharacterStats] LEVEL UP! Now Level {level}. Next XP: {MaxXP}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 3.0f, "LEVEL UP!", Color.cyan);
        }
    }

    private void Awake()
    {
        ResetStats();
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
    }

    public void ConsumeMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        Debug.Log($"[CharacterStats] Consumed {amount} Mana. Current: {currentMana}/{MaxMana}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.5f, $"-{amount} Mana", new Color(0.2f, 0.5f, 1f));
        }
    }

    public void RegenerateMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, MaxMana);
        Debug.Log($"[CharacterStats] Regenerated {amount} Mana. Current: {currentMana}/{MaxMana}");
        
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(transform.position + Vector3.up * 2.5f, $"+{amount} Mana", new Color(0.4f, 0.8f, 1f));
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

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        // Stamina depletes alongside HP when taking damage
        currentStamina = Mathf.Max(0, currentStamina - amount);
        currentHP = Mathf.Max(0, currentHP - amount);
        
        if (currentHP <= 0) isDead = true;
    }
}



