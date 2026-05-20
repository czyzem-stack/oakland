using UnityEngine;
using System.Collections.Generic;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    [Header("Tutorial Nodes")]
    public List<FTUEStep> steps = new List<FTUEStep>();
    public int currentStepIndex = 0;
    
    public bool isFTUEActive = true;

    private const string FTUE_COMPLETED_KEY = "FTUE_Completed_v12"; 

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
             // Reset Steve to campfire if we just started
             if (currentStepIndex == 0)
             {
                 GameObject steve = GameObject.Find("Steve") ?? GameObject.Find("Player");
                 if (steve != null) steve.transform.position = new Vector3(33.1f, 0f, -0.7f);
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
        if (current == null) return;

        // Robust matching: allow "Chest" to match any TreasureChest target, 
        // or check if targetName contains the POI name.
        bool isChestCompletion = targetName.Contains("Chest") || targetName == "Chest";
        bool stepIsChest = current.targetPOI != null && current.targetPOI.enemyType == EnemyType.TreasureChest;
        
        bool isMatch = false;
        if (current.targetPOI != null && targetName.Contains(current.targetPOI.name)) isMatch = true;
        else if (isChestCompletion && stepIsChest) isMatch = true;
        else if (current.pathEnemies != null)
        {
            foreach (var p in current.pathEnemies)
            {
                if (p != null && targetName.Contains(p.name)) { isMatch = true; break; }
            }
        }

        if (!isMatch)
        {
            Debug.LogWarning($"[FTUE] Target {targetName} completed but does not match Current Step {currentStepIndex} ({current.gameObject.name}). Ignoring.");
            return;
        }

        Debug.Log($"[FTUE] Step {currentStepIndex} COMPLETED by {targetName}. Reward: {current.rewardType}");

        if (current.levelUpOnComplete)
        {
             var stats = CombatSystem.Instance?.playerStats;
             if (stats != null)
             {
                 stats.AddXP(stats.MaxXP);
                 Debug.Log("[FTUE] Level Up granted for step completion.");
             }
        }

        currentStepIndex++;
        if (currentStepIndex >= steps.Count)
        {
            isFTUEActive = false;
            PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("[FTUE] All steps completed! Tutorial finished.");
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

