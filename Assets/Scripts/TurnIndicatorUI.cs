using UnityEngine;
using UnityEngine.UI;

public class TurnIndicatorUI : MonoBehaviour
{
    public Text turnText;
    public GameObject playerIndicator;
    public GameObject enemyIndicator;

    private void Update()
    {
        if (CombatSystem.Instance == null) return;

        if (CombatSystem.Instance.isInCombat)
        {
            if (turnText != null)
            {
                turnText.gameObject.SetActive(true);
                turnText.text = CombatSystem.Instance.isPlayerTurn ? "STEVE'S TURN" : "ORC'S TURN";
                turnText.color = CombatSystem.Instance.isPlayerTurn ? Color.green : Color.red;
            }

            if (playerIndicator != null) playerIndicator.SetActive(CombatSystem.Instance.isPlayerTurn);
            if (enemyIndicator != null) enemyIndicator.SetActive(!CombatSystem.Instance.isPlayerTurn);
        }
        else
        {
            if (turnText != null) turnText.gameObject.SetActive(false);
            if (playerIndicator != null) playerIndicator.SetActive(false);
            if (enemyIndicator != null) enemyIndicator.SetActive(false);
        }
    }
}
