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
        HeroNavigation hero = Object.FindAnyObjectByType<HeroNavigation>();
        if (hero != null)
        {
            playerStats = hero.GetComponent<CharacterStats>();
            if (playerStats != null)
            {
                Debug.Log($"[CoinUI] Found player {hero.name} and CharacterStats.");
            }
        }
    }

    void Update()
    {
        if (playerStats == null)
        {
            FindPlayer();
            return;
        }

        if (coinText != null)
        {
            coinText.text = $"Gold: {playerStats.coins}";
        }
    }
}
