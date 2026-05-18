using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TreasureUpgradeUI : MonoBehaviour
{
    public static TreasureUpgradeUI Instance;

    public GameObject panel;
    public Button button1;
    public Text text1;
    public Button button2;
    public Text text2;

    private CharacterStats playerStats;
    private string stat1;
    private string stat2;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void ShowUpgrade(CharacterStats stats)
    {
        playerStats = stats;
        
        // Pick 2 random unique stats from Brawn, Finesse, Wit, Grit
        List<string> options = new List<string> { "Brawn", "Finesse", "Wit", "Grit" };
        
        int i1 = Random.Range(0, options.Count);
        stat1 = options[i1];
        options.RemoveAt(i1);
        
        int i2 = Random.Range(0, options.Count);
        stat2 = options[i2];

        if (text1 != null) text1.text = "UPGRADE\n" + stat1.ToUpper();
        if (text2 != null) text2.text = "UPGRADE\n" + stat2.ToUpper();

        if (panel != null) panel.SetActive(true);
        
        // Ensure buttons are wired
        button1.onClick.RemoveAllListeners();
        button1.onClick.AddListener(SelectOption1);
        
        button2.onClick.RemoveAllListeners();
        button2.onClick.AddListener(SelectOption2);
    }

    public void SelectOption1()
    {
        ApplyUpgrade(stat1);
        Close();
    }

    public void SelectOption2()
    {
        ApplyUpgrade(stat2);
        Close();
    }

    private void ApplyUpgrade(string stat)
    {
        if (playerStats == null) return;
        switch (stat)
        {
            case "Brawn": playerStats.brawn += 2; break;
            case "Finesse": playerStats.finesse += 2; break;
            case "Wit": playerStats.wit += 2; break;
            case "Grit": playerStats.grit += 2; break;
        }
        playerStats.ResetStats();
        Debug.Log("[TreasureUpgradeUI] Upgraded " + stat);
        
        // Refresh Stats UI if it's visible
        var statsUI = Object.FindAnyObjectByType<StatsUI>();
        if (statsUI != null) statsUI.Refresh();

        // Visual feedback
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.SpawnDamageText(playerStats.transform.position + Vector3.up * 2f, "+" + stat + "!", Color.green);
        }
    }

    private void Close()
    {
        if (panel != null) panel.SetActive(false);
        
        // Resume navigation
        if (playerStats != null)
        {
            var nav = playerStats.GetComponent<HeroNavigation>();
            if (nav != null) nav.ResumeAfterCombat();
        }
    }
}
