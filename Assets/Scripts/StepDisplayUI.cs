using UnityEngine;
using TMPro;

/// <summary>
/// Optional standalone step display. Main HUD uses GameHUDManager.stepText instead.
/// </summary>
public class StepDisplayUI : MonoBehaviour
{
    public TMP_Text stepText;
    private HeroNavigation playerNav;
    private string lastText = "";

    void Start()
    {
        playerNav = PlayerReference.GetNavigation();
        if (stepText == null)
        {
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.name == "Text_Step") { stepText = t; break; }
            }
        }

        if (stepText != null)
        {
            stepText.textWrappingMode = TextWrappingModes.NoWrap;
            stepText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    void Update()
    {
        if (stepText == null) return;

        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            UpdateCombatText();
            return;
        }

        if (playerNav == null) playerNav = PlayerReference.GetNavigation();
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
        UpdateDisplay(currentText);
    }

    private void UpdateCombatText()
    {
        UpdateDisplay(CombatSystem.Instance.LastCombatAction);
    }

    private void UpdateDisplay(string currentText)
    {
        if (currentText != lastText)
        {
            lastText = currentText;
            stepText.text = currentText;
        }
    }
}
