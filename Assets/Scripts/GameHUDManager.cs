using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
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
    public Image energyFill; // If using a fill, otherwise text
    public TMP_Text energyText;
    public TMP_Text gemText;
    public TMP_Text coinText;

    [Header("Bottom Bar")]
    public Button rollButton;
    public TMP_Text rollButtonText;

    private int lastLevel = -1;
    private float lastXP = -1;
    private float lastHP = -1;
    private float lastMana = -1;
    private int lastCoins = -1;

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (playerNameText != null) playerNameText.text = "Ryuen";
        if (rollButtonText != null) rollButtonText.text = "PLAY!"; // Re-matching "PLAY!" from image
        if (gemText != null) gemText.text = "100";

        // Auto-assign if null
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

        // FORCE Colors
        if (hpFill != null) hpFill.color = Color.red;
        if (xpFill != null) xpFill.color = new Color(0.1f, 0.4f, 1f); // Nice Blue

        // Clear stamina trackers to prevent interference
        staminaFill = null;
        staminaText = null;
    }

    private void Update()
    {
        if (playerStats == null) 
        {
            if (!Application.isPlaying) playerStats = Object.FindAnyObjectByType<CharacterStats>();
            if (playerStats == null) return;
        }

        // FORCE Colors every frame to prevent overrides
        if (hpFill != null) hpFill.color = Color.red;
        if (xpFill != null) xpFill.color = new Color(0.1f, 0.4f, 1f);

        // Level
        if (playerStats.level != lastLevel || !Application.isPlaying)
        {
            lastLevel = playerStats.level;
            if (levelText != null) levelText.text = lastLevel.ToString();
        }

        // HP Bar (Bottom)
        float currentHP = playerStats.currentHP;
        int maxHP = playerStats.MaxHP;
        if (Mathf.Abs(currentHP - lastHP) > 0.1f || !Application.isPlaying)
        {
            lastHP = currentHP;
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

        // XP Bar (Top)
        float currentXP = playerStats.currentXP;
        int maxXP = playerStats.MaxXP;
        if (Mathf.Abs(currentXP - lastXP) > 0.1f || !Application.isPlaying)
        {
            lastXP = currentXP;
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

        // Energy (Mana)
        float currentMana = playerStats.currentMana;
        int maxMana = playerStats.MaxMana;
        if (Mathf.Abs(currentMana - lastMana) > 0.1f || !Application.isPlaying)
        {
            lastMana = currentMana;
            if (energyText != null) energyText.text = $"{(int)currentMana} / {maxMana}";
            if (energyFill != null && maxMana > 0) energyFill.fillAmount = currentMana / (float)maxMana;
        }

        // Coins
        if (playerStats.coins != lastCoins || !Application.isPlaying)
        {
            lastCoins = playerStats.coins;
            if (coinText != null) coinText.text = lastCoins.ToString("N0");
        }
    }
}
