using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public CharacterStats stats;
    public Image fillImage;

    public void Update()
    {
        if (stats == null || fillImage == null) return;

        float fill = stats.MaxHP > 0 ? stats.currentHP / (float)stats.MaxHP : 0;
        // Use localScale instead of fillAmount to allow solid color without sprite
        fillImage.transform.localScale = new Vector3(fill, 1, 1);
    }
}
