using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    public CharacterStats stats;
    public Image fillImage;
    public Text countdownText;

    public void Update()
    {
        if (stats == null || fillImage == null) return;

        float fill = stats.MaxMana > 0 ? stats.currentMana / (float)stats.MaxMana : 0;
        fillImage.transform.localScale = new Vector3(fill, 1, 1);

        if (countdownText != null)
        {
            if (stats.currentMana < stats.MaxMana)
            {
                countdownText.text = $"{stats.RegenTimeRemaining:F0}s";
            }
            else
            {
                countdownText.text = "";
            }
        }
    }
}


