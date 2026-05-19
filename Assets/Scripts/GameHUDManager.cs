using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUDManager : MonoBehaviour
{
    public CharacterStats playerStats;

    [Header("Top Left")]
    public TMP_Text levelText;
    public TMP_Text playerNameText;
    public Image hpFill;
    public TMP_Text hpText;
    public Image xpFill;
    public TMP_Text xpText;
    public Image staminaFill;
    public TMP_Text staminaText;

    [Header("Top Bar")]
    public Image energyFill;
    public TMP_Text energyText;
    public TMP_Text gemText;
    public TMP_Text coinText;

    [Header("Bottom Bar")]
    public Button rollButton;
    public TMP_Text rollButtonText;
    public Button heroesButton;
    public StatDisplayConfig statsConfig;

    [Header("Navigation")]
    public TMP_Text stepText;

    private HeroNavigation playerNav;
    private string lastStepText = "";

    private int lastLevel = -1;
    private float lastXP = -1;
    private int lastMaxXP = -1;
    private float lastHP = -1;
    private int lastMaxHP = -1;
    private float lastMana = -1;
    private int lastMaxMana = -1;
    private int lastCoins = -1;

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (playerNameText != null) playerNameText.text = "Steve";
        if (rollButtonText != null) rollButtonText.text = "ROLL";
        if (gemText != null) gemText.text = "100";

        if (xpFill == null)
        {
            GameObject s = GameObject.Find("Slider");
            if (s != null) xpFill = s.transform.Find("FillArea/Fill")?.GetComponent<Image>();
        }
        if (hpFill == null)
        {
            GameObject s = GameObject.Find("Slider_Stamina");
            if (s != null) hpFill = s.transform.Find("FillArea/Fill")?.GetComponent<Image>();
        }
        if (xpText == null) xpText = GameObject.Find("Text_Value")?.GetComponent<TMP_Text>();
        if (hpText == null) hpText = GameObject.Find("Text_StaminaValue")?.GetComponent<TMP_Text>();

        if (heroesButton == null)
        {
            Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var b in allButtons)
            {
                if (b.name == "Nav_HEROES")
                {
                    heroesButton = b;
                    break;
                }
            }
        }

        if (heroesButton != null)
        {
            heroesButton.onClick.RemoveAllListeners();
            heroesButton.onClick.AddListener(ShowSteveStats);
        }

        if (hpFill != null) hpFill.color = Color.red;
        if (xpFill != null) xpFill.color = new Color(0.1f, 0.4f, 1f);

        staminaFill = null;
        staminaText = null;

        playerNav = PlayerReference.GetNavigation();
        EnsureStepText();
    }

    private void EnsureStepText()
    {
        if (stepText != null) return;

        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.name == "Text_Step")
            {
                stepText = t;
                return;
            }
        }

        GameObject go = new GameObject("Text_Step", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 150f);
        rect.sizeDelta = new Vector2(920f, 36f);

        stepText = go.AddComponent<TextMeshProUGUI>();
        stepText.fontSize = 20;
        stepText.alignment = TextAlignmentOptions.Center;
        stepText.color = Color.white;
        stepText.outlineWidth = 0.15f;
        stepText.outlineColor = Color.black;
        stepText.text = "Target: - | Roll to move";

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/Alata-Regular SDF");
        if (font == null) font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        if (font != null) stepText.font = font;
    }

    private void Update()
    {
        if (playerStats == null)
        {
            playerStats = PlayerReference.GetStats();
            if (playerStats == null) return;
        }

        if (playerNav == null)
            playerNav = PlayerReference.GetNavigation();

        UpdateStepDisplay();

        if (playerStats.level != lastLevel)
        {
            lastLevel = playerStats.level;
            if (levelText != null) levelText.text = lastLevel.ToString();
        }

        float currentHP = playerStats.currentHP;
        int maxHP = playerStats.MaxHP;
        if (Mathf.Abs(currentHP - lastHP) > 0.1f || maxHP != lastMaxHP)
        {
            lastHP = currentHP;
            lastMaxHP = maxHP;
            if (hpFill != null && maxHP > 0)
            {
                float pct = Mathf.Clamp01(currentHP / (float)maxHP);
                hpFill.rectTransform.anchorMin = Vector2.zero;
                hpFill.rectTransform.anchorMax = new Vector2(pct, 1f);
                hpFill.rectTransform.offsetMin = Vector2.zero;
                hpFill.rectTransform.offsetMax = Vector2.zero;
            }
            if (hpText != null) hpText.text = $"{(int)currentHP}/{maxHP}";
        }

        float currentXP = playerStats.currentXP;
        int maxXP = playerStats.MaxXP;
        if (Mathf.Abs(currentXP - lastXP) > 0.1f || maxXP != lastMaxXP)
        {
            lastXP = currentXP;
            lastMaxXP = maxXP;
            if (xpFill != null && maxXP > 0)
            {
                float pct = Mathf.Clamp01(currentXP / (float)maxXP);
                xpFill.rectTransform.anchorMin = Vector2.zero;
                xpFill.rectTransform.anchorMax = new Vector2(pct, 1f);
                xpFill.rectTransform.offsetMin = Vector2.zero;
                xpFill.rectTransform.offsetMax = Vector2.zero;
            }
            if (xpText != null) xpText.text = $"{(int)currentXP}/{maxXP}";
        }

        float currentMana = playerStats.currentMana;
        int maxMana = playerStats.MaxMana;
        if (Mathf.Abs(currentMana - lastMana) > 0.1f || maxMana != lastMaxMana)
        {
            lastMana = currentMana;
            lastMaxMana = maxMana;
            if (energyText != null)
            {
                energyText.text = $"{(int)currentMana} / {maxMana}";
                energyText.color = Color.white;
            }
            if (energyFill != null && maxMana > 0) energyFill.fillAmount = currentMana / (float)maxMana;
        }

        if (playerStats.coins != lastCoins)
        {
            lastCoins = playerStats.coins;
            if (coinText != null) coinText.text = lastCoins.ToString("N0");
        }
    }

    private void UpdateStepDisplay()
    {
        if (stepText == null) return;
        if (playerNav == null) return;

        string targetPart = playerNav.TargetName != "None"
            ? $"Target: {playerNav.TargetName} ({playerNav.PathDistanceToTarget:F0}m away)"
            : "Target: -";

        string rollPart = playerNav.LastDiceTotal > 0
            ? $" | Roll: {playerNav.LastDiceTotal} ({playerNav.LastRollMeters:F0}m)"
            : "";

        string leftPart = playerNav.remainingMeters > 0.01f
            ? $" | Left: {playerNav.remainingMeters:F1}m"
            : "";

        string currentText = targetPart + rollPart + leftPart;
        if (currentText != lastStepText)
        {
            lastStepText = currentText;
            stepText.text = currentText;
        }
    }

    private void ShowSteveStats()
    {
        if (playerStats == null) return;

        if (statsConfig != null)
        {
            statsConfig.ShowStats();
            return;
        }

        GenericPopup popup = GenericPopup.Show("STEVE'S STATS", "", "CLOSE");
        if (popup != null)
        {
            popup.ClearStats();
            EquipmentManager em = EquipmentManager.Instance;

            popup.AddStat("<b>NAME</b>", "STEVE");
            popup.AddStat("<b>LEVEL</b>", playerStats.level.ToString());
            popup.AddStat(" ", " ");

            popup.AddStat("Brawn", playerStats.brawn.ToString() + GetBonusString(em, "Brawn"));
            popup.AddStat("Finesse", playerStats.finesse.ToString() + GetBonusString(em, "Finesse"));
            popup.AddStat("Wit", playerStats.wit.ToString() + GetBonusString(em, "Wit"));
            popup.AddStat("Grit", playerStats.grit.ToString() + GetBonusString(em, "Grit"));

            popup.AddStat(" ", " ");
            popup.AddStat("<i>Melee Damage</i>", playerStats.MeleeDamage.ToString());
            popup.AddStat("<i>Ranged Damage</i>", playerStats.RangedDamage.ToString());
            popup.AddStat("<i>Defense</i>", playerStats.Defense.ToString());
            popup.AddStat("<i>Crit Chance</i>", $"{playerStats.CritChance:F1}%");

            popup.AddStat(" ", " ");
            popup.AddStat("HP", $"{(int)playerStats.currentHP} / {playerStats.MaxHP}");
            popup.AddStat("Energy", $"{(int)playerStats.currentMana} / {playerStats.MaxMana}");
            popup.AddStat("XP", $"{(int)playerStats.currentXP} / {playerStats.MaxXP}");
        }
    }

    private string GetBonusString(EquipmentManager em, string statName)
    {
        if (em == null) return "";
        int bonus = em.GetBonus(statName);
        return bonus != 0 ? $" <color=green>(+{bonus})</color>" : "";
    }
}
