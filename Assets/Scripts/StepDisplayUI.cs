using UnityEngine;
using UnityEngine.UI;

public class StepDisplayUI : MonoBehaviour
{
    public UnityEngine.UI.Text stepText;
    private HeroNavigation playerNav;

    void Start()
    {
        playerNav = Object.FindAnyObjectByType<HeroNavigation>();
    }

    void Update()
    {
        if (playerNav != null && stepText != null)
        {
            string targetText = $"Target: {playerNav.TargetName} ({playerNav.DistanceToTarget:F1}m)";
            string movesText = playerNav.remainingMeters > 0.01f ? $" | Moves: {playerNav.remainingMeters:F1}m" : "";
            
            stepText.text = targetText + movesText;
        }
    }
}
