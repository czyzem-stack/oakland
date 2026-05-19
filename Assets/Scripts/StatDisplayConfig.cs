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
        if (player == null) return;

        GenericPopup popup = GenericPopup.Show(title, "", "CLOSE");
        if (popup == null) return;

        popup.ClearStats();

        if (showBaseStats)
        {
            popup.AddStat("<b>Brawn</b>", player.brawn.ToString());
            popup.AddStat("<b>Finesse</b>", player.finesse.ToString());
            popup.AddStat("<b>Wit</b>", player.wit.ToString());
            popup.AddStat("<b>Grit</b>", player.grit.ToString());
            popup.AddStat(" ", " ");
        }

        if (showDerivedStats)
        {
            popup.AddStat("Melee Damage", player.MeleeDamage.ToString());
            popup.AddStat("Ranged Damage", player.RangedDamage.ToString());
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
}
