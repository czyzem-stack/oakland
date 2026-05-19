using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    public CharacterStats stats;
    
    [Header("Base Stats Text")]
    public TMP_Text brawnText;
    public TMP_Text finesseText;
    public TMP_Text witText;
    public TMP_Text gritText;

    [Header("Derived Stats Text")]
    public TMP_Text hpText;
    public TMP_Text mpText;
    public TMP_Text damageText;
    public TMP_Text defenseText;
    public TMP_Text critText;
    public Button closeButton;

    public void Refresh()
    {
        if (stats == null) return;

        EquipmentManager em = EquipmentManager.Instance;

        if (brawnText != null) 
        {
            int bonus = em != null ? em.GetBonus("Brawn") : 0;
            string bonusText = bonus != 0 ? $" (+{bonus})" : "";
            brawnText.text = $"Brawn: {stats.brawn}{bonusText}";
        }
        if (finesseText != null) 
        {
            int bonus = em != null ? em.GetBonus("Finesse") : 0;
            string bonusText = bonus != 0 ? $" (+{bonus})" : "";
            finesseText.text = $"Finesse: {stats.finesse}{bonusText}";
        }
        if (witText != null) 
        {
            int bonus = em != null ? em.GetBonus("Wit") : 0;
            string bonusText = bonus != 0 ? $" (+{bonus})" : "";
            witText.text = $"Wit: {stats.wit}{bonusText}";
        }
        if (gritText != null) 
        {
            int bonus = em != null ? em.GetBonus("Grit") : 0;
            string bonusText = bonus != 0 ? $" (+{bonus})" : "";
            gritText.text = $"Grit: {stats.grit}{bonusText}";
        }

        if (hpText != null) hpText.text = $"HP: {stats.currentHP:F0} / {stats.MaxHP}";
        if (mpText != null) 
        {
            mpText.text = $"Energy: {stats.currentMana:F0} / {stats.MaxMana}";
            mpText.color = Color.white;
        }
        if (damageText != null) damageText.text = $"Damage: {stats.MeleeDamage} (M) / {stats.RangedDamage} (R)";
        if (defenseText != null) defenseText.text = $"Defense: {stats.Defense}";
        if (critText != null) critText.text = $"Crit: {stats.critThreshold}+";
    }

    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void Start()
{
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    private void OnEnable()
    {
        Refresh();
    }
}
