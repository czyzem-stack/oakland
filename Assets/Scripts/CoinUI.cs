using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    public TMP_Text coinText;
    private CharacterStats playerStats;

    void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        playerStats = PlayerReference.GetStats();
    }

    private int lastCoins = -1;

    void Update()
    {
        if (playerStats == null)
        {
            FindPlayer();
            return;
        }

        if (coinText != null && playerStats.coins != lastCoins)
        {
            lastCoins = playerStats.coins;
            coinText.text = $"Gold: {lastCoins}";
        }
    }
}
