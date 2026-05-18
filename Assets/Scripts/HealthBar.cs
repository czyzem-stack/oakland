using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public CharacterStats stats;
    public Image fillImage;
    private Canvas cachedCanvas;

    private void Start()
    {
        cachedCanvas = GetComponentInParent<Canvas>();
    }

    public void Update()
    {
        if (stats == null || fillImage == null) return;

        bool isPlayer = false;
        if (CombatSystem.Instance != null && CombatSystem.Instance.playerStats == stats)
        {
            isPlayer = true;
        }

        // Enemy-specific visibility logic
        bool shouldBeVisible = true;
        if (!isPlayer)
        {
            shouldBeVisible = false;
            if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
            {
                if (CombatSystem.Instance.currentEnemyStats == stats)
                {
                    shouldBeVisible = true;
                }
            }
        }

        // Toggle visibility using cached reference
        if (cachedCanvas != null && cachedCanvas.enabled != shouldBeVisible)
        {
            cachedCanvas.enabled = shouldBeVisible;
        }

        if (!shouldBeVisible) return;

        float fill = stats.MaxHP > 0 ? stats.currentHP / (float)stats.MaxHP : 0;
// Use localScale instead of fillAmount to allow solid color without sprite
        fillImage.transform.localScale = new Vector3(fill, 1, 1);
    }
}
