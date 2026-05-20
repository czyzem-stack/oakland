using UnityEngine;
using System.Collections.Generic;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    [Header("Tutorial Nodes")]
    public List<FTUEStep> steps = new List<FTUEStep>();
    public int currentStepIndex = 0;
    
    public bool isFTUEActive = true;

    private const string FTUE_COMPLETED_KEY = "FTUE_Completed_v14"; 

    public FTUEStep CurrentStep => (steps != null && currentStepIndex < steps.Count) ? steps[currentStepIndex] : null;

    public bool resetStateOnPlay = true;

    void Awake()
    {
        Instance = this;

        // Ensure "play should start new" behavior
        if (Application.isEditor && resetStateOnPlay)
        {
            ResetAllData();
        }

        // Check for force reset
        if (PlayerPrefs.GetInt("ForceResetFTUE", 0) == 1)
        {
            ResetAllData();
            PlayerPrefs.SetInt("ForceResetFTUE", 0);
            PlayerPrefs.Save();
            Debug.Log("[FTUE] Force reset detected.");
        }

        if (PlayerPrefs.GetInt(FTUE_COMPLETED_KEY, 0) == 1)
        {
            isFTUEActive = false;
        }

        if (isFTUEActive)
        {
            Debug.Log($"[FTUE] Awake - Current Step: {currentStepIndex}");
        }
    }

    public void ResetAllData()
    {
        Debug.Log("[FTUE] Resetting all game data for a fresh start.");
        PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 0);
        PlayerPrefs.DeleteKey("Perm_Brawn");
        PlayerPrefs.DeleteKey("Perm_Finesse");
        PlayerPrefs.DeleteKey("Perm_Wit");
        PlayerPrefs.DeleteKey("Perm_Grit");
        PlayerPrefs.Save();
        currentStepIndex = 0;
        isFTUEActive = true;
        
        // Re-align steps to requested sequence if they exist
        ValidateStepSequence();
    }

    private void ValidateStepSequence()
    {
        // Re-order based on requirements: 1.orc, 2.orc, 3.chest, 4.orc, 5.mushroom, 6.chest, 7.orc, 8.worm, 9.chest
        EnemyType[] req = { 
            EnemyType.Orc, EnemyType.Orc, EnemyType.TreasureChest, 
            EnemyType.Orc, EnemyType.Mushroom, EnemyType.TreasureChest, 
            EnemyType.Orc, EnemyType.Worm, EnemyType.TreasureChest 
        };

        if (steps.Count != req.Length) return;

        for (int i = 0; i < steps.Count; i++)
        {
            var poi = steps[i].GetPOI();
            if (poi != null) poi.enemyType = req[i];
            steps[i].rewardType = (req[i] == EnemyType.TreasureChest) ? GetRewardForStep(i) : FTUERewardType.None;
            steps[i].levelUpOnComplete = (req[i] == EnemyType.TreasureChest);
        }
    }

    private FTUERewardType GetRewardForStep(int index)
    {
        if (index == 2) return FTUERewardType.WoodenStick_RandomWeapon; // Node 3
        if (index == 5) return FTUERewardType.Armor_Armor;              // Node 6
        if (index == 8) return FTUERewardType.Shield_Helm;               // Node 9
        return FTUERewardType.None;
    }

    private void Start()
    {
        if (isFTUEActive)
        {
             SuppressAllScenePOIs();
             
             // Always warp to PlayerStart position (or Hero fallback) on first start of tutorial
             if (currentStepIndex == 0)
             {
                 GameObject steve = GameObject.Find("Steve") ?? GameObject.Find("Player");
                 if (steve != null)
                 {
                     PlayerStart spawnPoint = Object.FindAnyObjectByType<PlayerStart>();
                     Vector3 spawnPos;
                     
                     if (spawnPoint != null)
                     {
                         spawnPos = spawnPoint.transform.position;
                         Debug.Log($"[FTUE] New Game start: Moving Steve to PlayerStart position: {spawnPos}");
                     }
                     else
                     {
                         GameObject heroGo = GameObject.Find("Hero");
                         spawnPos = heroGo != null ? heroGo.transform.position : new Vector3(31.0f, 0f, 0f);
                         Debug.Log($"[FTUE] New Game start: PlayerStart not found. Using {(heroGo != null ? "Hero GO" : "default")} position: {spawnPos}");
                     }
             
                     var agent = steve.GetComponent<UnityEngine.AI.NavMeshAgent>();
                     if (agent != null) agent.Warp(spawnPos);
                     else steve.transform.position = spawnPos;
             
                     CombatSystem.Instance?.SpawnDamageText(spawnPos + Vector3.up * 2f, "LET'S GO!", Color.yellow);
                 }
             }
        }
    }

    public void SuppressAllScenePOIs()
    {
        PointOfInterest[] allPOIs = Object.FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include);
        foreach (var poi in allPOIs)
        {
            bool isCurrent = IsPartOfCurrentStep(poi);
            if (isCurrent)
            {
                poi.gameObject.SetActive(true);
                poi.EnsureEnemy();
                
                // Ensure all children (enemies, health bars) are also active
                foreach (Transform child in poi.transform) child.gameObject.SetActive(true);
            }
            else if (poi.gameObject.activeSelf && !poi.name.Contains("FTUE") && !poi.name.Contains("Forced"))
            {
                poi.gameObject.SetActive(false);
            }
        }

        CharacterStats[] allStats = Object.FindObjectsByType<CharacterStats>(FindObjectsInactive.Include);
        foreach (var stats in allStats)
        {
            if (stats == null || stats.name == "Steve" || stats.name == "Player") continue;
            
            bool isCurrent = IsStatsOfCurrentStep(stats);
            if (isCurrent)
            {
                stats.gameObject.SetActive(true);
            }
            else if (stats.gameObject.activeSelf && !stats.name.Contains("FTUE") && !stats.name.Contains("Forced"))
            {
                stats.gameObject.SetActive(false);
                
                Transform parent = stats.transform.parent;
                if (parent != null && parent.gameObject.activeSelf && !parent.name.Contains("FTUE") && !parent.name.Contains("Forced") && parent.name != "POI" && !IsParentOfCurrentStep(parent))
                {
                    parent.gameObject.SetActive(false);
                }
            }
        }
    }

    private bool IsPartOfCurrentStep(PointOfInterest poi)
    {
        FTUEStep current = CurrentStep;
        if (current == null || poi == null) return false;
        if (poi == current.GetPOI()) return true;
        if (current.pathEnemies != null && current.pathEnemies.Contains(poi)) return true;
        return false;
    }

    private bool IsStatsOfCurrentStep(CharacterStats stats)
    {
        if (stats == null) return false;
        PointOfInterest poi = stats.GetComponentInParent<PointOfInterest>();
        return IsPartOfCurrentStep(poi);
    }

    private bool IsParentOfCurrentStep(Transform parent)
    {
        if (parent == null) return false;
        PointOfInterest poi = parent.GetComponent<PointOfInterest>();
        return IsPartOfCurrentStep(poi);
    }

    public void OnStageCompleted(string targetName)
    {
        if (!isFTUEActive) return;

        FTUEStep current = CurrentStep;
        if (current == null) return;

        PointOfInterest currentPOI = current.GetPOI();
        string cleanTarget = targetName.ToLower().Replace("(clone)", "").Trim();
        string cleanStep = currentPOI != null ? currentPOI.name.ToLower().Trim() : "null";
        
        bool isMatch = false;
        if (currentPOI != null)
        {
            if (cleanTarget.Contains(cleanStep) || cleanStep.Contains(cleanTarget)) isMatch = true;
            if (!isMatch && cleanTarget.Contains(currentPOI.enemyType.ToString().ToLower())) isMatch = true;
        }

        // Proximity Fallback: if steve is physically at the target
        if (!isMatch)
        {
            GameObject steve = GameObject.Find("Steve") ?? GameObject.Find("Player");
            if (steve != null && currentPOI != null)
            {
                if (Vector3.Distance(steve.transform.position, currentPOI.transform.position) < 5f) isMatch = true;
            }
        }

        if (!isMatch) return;

        currentStepIndex++;
        
        GameObject player = GameObject.Find("Steve") ?? GameObject.Find("Player");
        if (player != null)
        {
            var nav = player.GetComponent<HeroNavigation>();
            if (nav != null) nav.ClearTarget();
        }

        if (currentStepIndex >= steps.Count)
        {
            CompleteFTUE();
        }
        else
        {
            SuppressAllScenePOIs(); 
        }
    }

    private void CompleteFTUE()
    {
        isFTUEActive = false;
        PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[FTUE] All steps completed! Steve is leaving the tutorial.");
        
        // 1. Fully charge Steve
        var stats = CombatSystem.Instance?.playerStats;
        if (stats != null)
        {
            stats.currentHP = stats.MaxHP;
            stats.currentMana = stats.MaxMana;
            CombatSystem.Instance.SpawnDamageText(stats.transform.position + Vector3.up * 3f, "TUTORIAL COMPLETE!", Color.green);
        }

        // 2. Stop Steve from 'panicking' and snapping to new POIs immediately
        var nav = Object.FindAnyObjectByType<HeroNavigation>();
        if (nav != null)
        {
            nav.NotifyTutorialCompleted();
            Debug.Log("[FTUE] Steve navigation locked for transition to free exploration.");
        }

        // 3. Setup world POIs
        if (POIManager.Instance != null) POIManager.Instance.SetupPOIs();
    }

    public Transform GetNextTarget(Vector3 playerPos)
    {
        if (!isFTUEActive) return null;

        FTUEStep current = CurrentStep;
        PointOfInterest currentPOI = current?.GetPOI();

        if (current != null && currentPOI != null)
        {
            currentPOI.gameObject.SetActive(true);
            currentPOI.EnsureEnemy();
            
            if (current.pathEnemies != null)
            {
                foreach (var p in current.pathEnemies)
                {
                    if (p != null)
                    {
                        p.gameObject.SetActive(true);
                        p.EnsureEnemy();
                    }
                }
            }

            return currentPOI.transform;
        }

        return null;
    }
}

public class PlayerStart : MonoBehaviour
{
    void Awake()
    {
        gameObject.name = "PlayerSpawnPoint";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}

