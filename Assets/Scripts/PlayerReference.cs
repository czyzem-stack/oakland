using UnityEngine;

public static class PlayerReference
{
    public static CharacterStats GetStats()
    {
        // Try to get from CombatSystem first, but ensure it's not a destroyed object
        if (CombatSystem.Instance != null && CombatSystem.Instance.playerStats != null)
        {
            // Unity's == null check handles destroyed objects
            return CombatSystem.Instance.playerStats;
        }

        // Fallback: Find the hero in the scene
        HeroNavigation hero = Object.FindAnyObjectByType<HeroNavigation>();
        if (hero != null)
        {
            CharacterStats stats = hero.GetComponent<CharacterStats>();
            // If CombatSystem exists, update its reference for next time
            if (CombatSystem.Instance != null && stats != null) CombatSystem.Instance.playerStats = stats;
            return stats;
        }
        
        return null;
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
