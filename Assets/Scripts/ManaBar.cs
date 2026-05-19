using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    public CharacterStats stats;
    public Image fillImage;
    public Text countdownText;

    private float lastFill = -1f;
    private float lastCountdown = -1f;

    public void Update()
    {
        if (stats == null || fillImage == null) return;

        float fill = stats.MaxMana > 0 ? stats.currentMana / (float)stats.MaxMana : 0;
        if (Mathf.Abs(fill - lastFill) > 0.01f)
        {
            lastFill = fill;
            fillImage.transform.localScale = new Vector3(fill, 1, 1);
        }

        if (countdownText != null)
        {
            float currentRegen = stats.RegenTimeRemaining;
            if (Mathf.Abs(currentRegen - lastCountdown) > 0.5f)
            {
                lastCountdown = currentRegen;
                if (stats.currentMana < stats.MaxMana)
                {
                    countdownText.text = $"{currentRegen:F0}s";
                    countdownText.color = Color.white;
                }
                else
                {
                    countdownText.text = "";
                }
            }
        }
    }
}


