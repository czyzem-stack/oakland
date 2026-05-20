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

    void Awake()
    {
        Instance = this;
        // Check for force reset
        if (PlayerPrefs.GetInt("ForceResetFTUE", 0) == 1)
        {
            PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 0);
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

    private void Start()
    {
        if (isFTUEActive)
        {
             SuppressAllScenePOIs();
             // Playful start: Instead of hard teleport, we only warp if he is VERY far or it's the first frame
             if (currentStepIndex == 0)
             {
                 GameObject steve = GameObject.Find("Steve") ?? GameObject.Find("Player");
                 if (steve != null)
                 {
                     Vector3 campfirePos = new Vector3(33.1f, 0f, -0.7f);
                     if (Vector3.Distance(steve.transform.position, campfirePos) > 10f)
                     {
                         Debug.Log("[FTUE] Steve is far from campfire, moving him to start.");
                         var agent = steve.GetComponent<UnityEngine.AI.NavMeshAgent>();
                         if (agent != null) agent.Warp(campfirePos);
                         else steve.transform.position = campfirePos;
                         
                         CombatSystem.Instance?.SpawnDamageText(campfirePos + Vector3.up * 2f, "LET'S GO!", Color.yellow);
                     }
                 }
             }
        }
    }

    public void SuppressAllScenePOIs()
    {
        PointOfInterest[] allPOIs = Object.FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include);
        foreach (var poi in allPOIs)
        {
            if (poi.gameObject.activeSelf && !poi.name.Contains("FTUE") && !poi.name.Contains("Forced") && !IsPartOfCurrentStep(poi))
            {
                poi.gameObject.SetActive(false);
            }
        }

        CharacterStats[] allStats = Object.FindObjectsByType<CharacterStats>(FindObjectsInactive.Include);
        foreach (var stats in allStats)
        {
            if (stats != null && stats.name != "Steve" && stats.name != "Player" && 
                !stats.name.Contains("FTUE") && !stats.name.Contains("Forced") && !IsStatsOfCurrentStep(stats))
            {
                if (stats.gameObject.activeSelf) stats.gameObject.SetActive(false);
                
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
        if (current == null) return false;
        if (poi == current.targetPOI) return true;
        if (current.pathEnemies != null && current.pathEnemies.Contains(poi)) return true;
        return false;
    }

    private bool IsStatsOfCurrentStep(CharacterStats stats) => IsPartOfCurrentStep(stats.GetComponentInParent<PointOfInterest>());
    private bool IsParentOfCurrentStep(Transform parent) => IsPartOfCurrentStep(parent.GetComponent<PointOfInterest>());

    public void OnStageCompleted(string targetName)
    {
        if (!isFTUEActive) return;

        FTUEStep current = CurrentStep;
        if (current == null) 
        {
            Debug.LogWarning("[FTUE] OnStageCompleted called but CurrentStep is null.");
            return;
        }

        Debug.Log($"[FTUE] OnStageCompleted called with '{targetName}'. Current Target: '{current.targetPOI?.name}'");

        // Robust matching: allow "Chest" to match any TreasureChest target, 
        // or check if targetName matches the POI name.
        bool isChestCompletion = targetName.ToLower().Contains("chest");
        bool stepIsChest = current.targetPOI != null && current.targetPOI.enemyType == EnemyType.TreasureChest;
        
        bool isMatch = false;
        if (current.targetPOI != null)
        {
            string tName = targetName.ToLower();
            string pName = current.targetPOI.name.ToLower();
            if (tName.Contains(pName) || pName.Contains(tName)) isMatch = true;
        }

        if (!isMatch && isChestCompletion && stepIsChest) isMatch = true;

        if (!isMatch && current.pathEnemies != null)
        {
            foreach (var p in current.pathEnemies)
            {
                if (p != null)
                {
                    string tName = targetName.ToLower();
                    string pName = p.name.ToLower();
                    if (tName.Contains(pName) || pName.Contains(tName)) { isMatch = true; break; }
                }
            }
        }

        if (!isMatch)
        {
            Debug.LogWarning($"[FTUE] Match failed. Target '{targetName}' completed but does not match Step {currentStepIndex} ('{current.targetPOI?.name}').");
            return;
        }

        Debug.Log($"[FTUE] Step {currentStepIndex} ({current.gameObject.name}) COMPLETED by {targetName}. Reward: {current.rewardType}");

        // Playful Reward: Instead of instant level up, give a small XP bonus
        if (current.levelUpOnComplete)
        {
             var stats = CombatSystem.Instance?.playerStats;
             if (stats != null)
             {
                 stats.AddXP(10f); 
                 Debug.Log("[FTUE] Playful XP bonus granted.");
             }
        }

        currentStepIndex++;
        if (currentStepIndex >= steps.Count)
        {
            isFTUEActive = false;
            PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("[FTUE] All steps completed! Steve is leaving the tutorial at Level 3 and FULLY CHARGED.");
            
            // Fully charge Steve
            var stats = CombatSystem.Instance?.playerStats;
            if (stats != null)
            {
                stats.currentHP = stats.MaxHP;
                stats.currentMana = stats.MaxMana;
                CombatSystem.Instance.SpawnDamageText(stats.transform.position + Vector3.up * 3f, "FULLY CHARGED!", Color.green);
            }

            if (POIManager.Instance != null) POIManager.Instance.SetupPOIs();
        }
        else
        {
            Debug.Log($"[FTUE] Advancing to Step {currentStepIndex}: {CurrentStep.gameObject.name}");
            SuppressAllScenePOIs(); 
        }
    }

    public Transform GetNextTarget(Vector3 playerPos)
    {
        if (!isFTUEActive) return null;

        FTUEStep current = CurrentStep;
        if (current != null && current.targetPOI != null)
        {
            current.targetPOI.gameObject.SetActive(true);
            current.targetPOI.EnsureEnemy();
            
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

            return current.targetPOI.transform;
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

