using UnityEngine;

public static class PlayerReference
{
    public static CharacterStats GetStats()
    {
        if (CombatSystem.Instance != null && CombatSystem.Instance.playerStats != null)
            return CombatSystem.Instance.playerStats;

        HeroNavigation hero = Object.FindAnyObjectByType<HeroNavigation>();
        return hero != null ? hero.GetComponent<CharacterStats>() : null;
    }

    public static HeroNavigation GetNavigation()
    {
        if (CombatSystem.Instance != null && CombatSystem.Instance.playerStats != null)
        {
            HeroNavigation nav = CombatSystem.Instance.playerStats.GetComponent<HeroNavigation>();
            if (nav != null) return nav;
        }

        return Object.FindAnyObjectByType<HeroNavigation>();
    }
}
