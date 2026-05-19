using UnityEngine;

public class StatDisplayConfig : MonoBehaviour
{
    public string title = "HERO STATS";
    public bool showBaseStats = true;
    public bool showDerivedStats = true;
    public bool showStatus = true;
    public bool showXP = true;

    public void ShowStats()
    {
        CharacterStats player = PlayerReference.GetStats();
        if (player == null)
        {
            Debug.LogError("[StatDisplayConfig] Player stats not found!");
            return;
        }

        Debug.Log("[StatDisplayConfig] Showing stats for " + player.name);
        GenericPopup popup = GenericPopup.Show(title, "", "CLOSE");
        if (popup == null) 
        {
            Debug.LogError("[StatDisplayConfig] Failed to show GenericPopup!");
            return;
        }

        popup.ClearStats();
        EquipmentManager em = EquipmentManager.Instance;

        if (showBaseStats)
        {
            popup.AddStat("<b>Brawn</b>", player.brawn.ToString() + GetBonusString(em, "Brawn"));
            popup.AddStat("<b>Finesse</b>", player.finesse.ToString() + GetBonusString(em, "Finesse"));
            popup.AddStat("<b>Wit</b>", player.wit.ToString() + GetBonusString(em, "Wit"));
            popup.AddStat("<b>Grit</b>", player.grit.ToString() + GetBonusString(em, "Grit"));
            popup.AddStat(" ", " ");
        }

        if (showDerivedStats)
        {
            int atkBonus = em != null ? em.GetBonus("Attack") : 0;
            string atkBonusText = atkBonus != 0 ? $" <color=green>(+{atkBonus})</color>" : "";

            popup.AddStat("Melee Damage", player.MeleeDamage.ToString() + atkBonusText);
            popup.AddStat("Ranged Damage", player.RangedDamage.ToString() + atkBonusText);
            popup.AddStat("Defense", player.Defense.ToString());
            popup.AddStat("Crit Chance", $"{player.CritChance:F1}%");
            popup.AddStat(" ", " ");
        }

        if (showStatus)
        {
            popup.AddStat("Health", $"{(int)player.currentHP} / {player.MaxHP}");
            popup.AddStat("Energy", $"{(int)player.currentMana} / {player.MaxMana}");
        }

        if (showXP)
        {
            popup.AddStat("Experience", $"{(int)player.currentXP} / {player.MaxXP}");
        }
    }

    private string GetBonusString(EquipmentManager em, string statName)
    {
        if (em == null) return "";
        int bonus = em.GetBonus(statName);
        return bonus != 0 ? $" <color=green>(+{bonus})</color>" : "";
    }
}
