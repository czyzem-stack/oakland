using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    public CharacterStats stats;
    
    [Header("Base Stats Text")]
    public Text brawnText;
    public Text finesseText;
    public Text witText;
    public Text gritText;

    [Header("Derived Stats Text")]
    public Text hpText;
    public Text mpText;
    public Text damageText;
    public Text defenseText;
    public Text critText;
    public Button closeButton;

    public void Refresh()
    {
        if (stats == null) return;

        brawnText.text = $"Brawn: {stats.brawn}";
        finesseText.text = $"Finesse: {stats.finesse}";
        witText.text = $"Wit: {stats.wit}";
        gritText.text = $"Grit: {stats.grit}";

        hpText.text = $"HP: {stats.currentHP:F0} / {stats.MaxHP}";
        mpText.text = $"Mana: {stats.currentMana:F0} / {stats.MaxMana}";
        damageText.text = $"Damage: {stats.MeleeDamage} (M) / {stats.RangedDamage} (R)";
        defenseText.text = $"Defense: {stats.Defense}";
        if (critText != null) critText.text = $"Crit: {stats.critThreshold}+";
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
