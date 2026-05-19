using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public CharacterStats stats;
    public Image fillImage;
    private Canvas cachedCanvas;

    private float lastFill = -1f;
    private bool lastVisible = true;

    private void Start()
    {
        cachedCanvas = GetComponentInParent<Canvas>();
        if (cachedCanvas != null) lastVisible = cachedCanvas.enabled;
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
            // Visible if in combat with THIS enemy
            if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat && CombatSystem.Instance.currentEnemyStats == stats)
            {
                shouldBeVisible = true;
            }
            // Visible if damaged
            else if (stats.currentHP < stats.MaxHP - 0.1f && stats.currentHP > 0)
            {
                shouldBeVisible = true;
            }
        }

        if (cachedCanvas == null) cachedCanvas = GetComponent<Canvas>();
        if (cachedCanvas == null) cachedCanvas = GetComponentInParent<Canvas>();

        if (cachedCanvas != null)
        {
            if (cachedCanvas.enabled != shouldBeVisible)
            {
                cachedCanvas.enabled = shouldBeVisible;
            }
        }

        if (!shouldBeVisible) return;

        float fill = stats.MaxHP > 0 ? stats.currentHP / (float)stats.MaxHP : 0;
        if (Mathf.Abs(fill - lastFill) > 0.001f)
        {
            lastFill = fill;
            fillImage.transform.localScale = new Vector3(Mathf.Clamp01(fill), 1, 1);
        }
    }
}
