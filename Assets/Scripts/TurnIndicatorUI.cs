using UnityEngine;
using UnityEngine.UI;

public class TurnIndicatorUI : MonoBehaviour
{
    public Text turnText;
    public GameObject playerIndicator;
    public GameObject enemyIndicator;
    public GameObject visualsContainer;

    private void Update()
    {
        if (CombatSystem.Instance == null) return;

        if (CombatSystem.Instance.isInCombat)
        {
            if (visualsContainer != null) visualsContainer.SetActive(true);
            
            if (turnText != null)
            {
                turnText.text = CombatSystem.Instance.isPlayerTurn ? "STEVE'S TURN" : "ORC'S TURN";
                turnText.color = CombatSystem.Instance.isPlayerTurn ? Color.green : Color.red;
            }

            if (playerIndicator != null) playerIndicator.SetActive(CombatSystem.Instance.isPlayerTurn);
            if (enemyIndicator != null) enemyIndicator.SetActive(!CombatSystem.Instance.isPlayerTurn);
        }
        else
        {
            if (visualsContainer != null) visualsContainer.SetActive(false);
        }
    }
}
