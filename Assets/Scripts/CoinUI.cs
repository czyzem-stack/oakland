using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    public Text coinText;
    private CharacterStats playerStats;

    void Start()
    {
        HeroNavigation hero = Object.FindAnyObjectByType<HeroNavigation>();
        if (hero != null)
        {
            playerStats = hero.GetComponent<CharacterStats>();
        }
    }

    void Update()
    {
        if (playerStats != null && coinText != null)
        {
            coinText.text = $"Gold: {playerStats.coins}";
        }
    }
}
