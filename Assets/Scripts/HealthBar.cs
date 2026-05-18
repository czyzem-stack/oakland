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

        bool isPlayer = (CombatSystem.Instance != null && CombatSystem.Instance.playerStats == stats);

        // Visibility logic
        bool shouldBeVisible = true;
        if (!isPlayer)
        {
            shouldBeVisible = false;
            
            // 1. Visible if this is the active enemy in combat
            if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat && CombatSystem.Instance.currentEnemyStats == stats)
            {
                shouldBeVisible = true;
            }
            // 2. Visible if damaged (covers Impact damage before combat formally starts)
            else if (stats.currentHP < stats.MaxHP && stats.currentHP > 0)
            {
                shouldBeVisible = true;
            }
        }

        // Ensure we have the canvas reference
        if (cachedCanvas == null) cachedCanvas = GetComponentInParent<Canvas>();

        // Toggle visibility
        if (cachedCanvas != null && cachedCanvas.enabled != shouldBeVisible)
        {
            cachedCanvas.enabled = shouldBeVisible;
        }

        if (!shouldBeVisible) return;

        float fill = stats.MaxHP > 0 ? stats.currentHP / (float)stats.MaxHP : 0;
        fillImage.transform.localScale = new Vector3(fill, 1, 1);
    }
}
