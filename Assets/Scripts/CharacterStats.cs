using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int brawn = 14;
    public int finesse = 12;
    public int wit = 10;
    public int grit = 12;

    [Header("Current State")]
    public float currentHP;
    public float currentMana;

    [Header("Settings")]
    public int manaRegenPerInterval = 1;
    public float regenInterval = 15.0f;
    private float regenTimer = 0f;

    public float RegenTimeRemaining => (currentMana < MaxMana) ? Mathf.Max(0, regenInterval - regenTimer) : 0;

    // Derived Stats
    public int MaxHP => brawn * 5 + 10;
    public int MaxMana => grit * 3 + 10; 
    public int MeleeDamage => brawn;
    public int RangedDamage => finesse;
    public int Defense => finesse / 2;

    private void Awake()
    {
        ResetStats();
    }

    private void Update()
    {
        // Periodic Regeneration
        if (currentMana < MaxMana)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenInterval)
            {
                RegenerateMana(manaRegenPerInterval);
                regenTimer = 0f;
            }
        }
        else
        {
            regenTimer = 0f;
        }
    }

    public void ResetStats()
    {
        currentHP = MaxHP;
        currentMana = MaxMana;
    }

    public void ConsumeMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        Debug.Log($"[CharacterStats] Consumed {amount} Mana. Current: {currentMana}/{MaxMana}");
    }

    public void RegenerateMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, MaxMana);
        Debug.Log($"[CharacterStats] Regenerated {amount} Mana. Current: {currentMana}/{MaxMana}");
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
    }
}



