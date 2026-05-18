using UnityEngine;
using UnityEngine.UI;

public class TurnIndicatorUI : MonoBehaviour
{
    public GameObject playerIndicator;
    public GameObject enemyIndicator;
    public GameObject visualsContainer;

    [Header("Roll Button Feedback")]
    public CanvasGroup rollButtonCanvasGroup;
    public float dimAlpha = 0.4f;
    public float pulseSpeed = 4f;

    private void Start()
    {
        if (rollButtonCanvasGroup == null)
        {
            GameObject rb = GameObject.Find("RollButton");
            if (rb != null) rollButtonCanvasGroup = rb.GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        var combat = CombatSystem.Instance;
        if (combat == null) return;

        bool inCombat = combat.isInCombat;
        bool isPlayerTurn = combat.isPlayerTurn;

        // Handle Visual Indicators (if any are left)
        if (visualsContainer != null) visualsContainer.SetActive(inCombat);

        if (inCombat)
        {
            if (playerIndicator != null) playerIndicator.SetActive(isPlayerTurn);
            if (enemyIndicator != null) enemyIndicator.SetActive(!isPlayerTurn);
        }

        // Handle Roll Button Feedback
        if (rollButtonCanvasGroup != null)
        {
            if (!inCombat)
            {
                rollButtonCanvasGroup.alpha = 1f;
                rollButtonCanvasGroup.interactable = true;
            }
            else
            {
                if (isPlayerTurn)
                {
                    // Pulse "Glow"
                    float pulse = 0.8f + Mathf.Sin(Time.time * pulseSpeed) * 0.2f;
                    rollButtonCanvasGroup.alpha = pulse;
                    rollButtonCanvasGroup.interactable = true;
                }
                else
                {
                    // Dim
                    rollButtonCanvasGroup.alpha = dimAlpha;
                    rollButtonCanvasGroup.interactable = false;
                }
            }
        }
    }
}
