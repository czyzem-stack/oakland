using UnityEngine;
using System.Collections.Generic;

public enum FTUERewardType { None, T1_Weapon, T2_Armor, T3_Mixed }

public class FTUEStep : MonoBehaviour
{
    [Header("Objective")]
    public PointOfInterest targetPOI;
    public List<PointOfInterest> pathEnemies;
    
    [Header("Rewards & Progression")]
    public FTUERewardType rewardType = FTUERewardType.None;
    public bool levelUpOnComplete = true;

    [ContextMenu("Auto-Assign Name")]
    private void AutoAssignName()
    {
        if (targetPOI != null)
        {
            gameObject.name = $"Step_{targetPOI.name}";
        }
    }
}
