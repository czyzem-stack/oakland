using UnityEngine;
using System.Collections.Generic;

public enum FTUERewardType 
{ 
    None, 
    WoodenStick_RandomWeapon, 
    Armor_Armor, 
    Shield_Helm
}

[RequireComponent(typeof(PointOfInterest))]
public class FTUEStep : MonoBehaviour
{
    [Header("Sequence Info")]
    [Tooltip("Enemy or Treasure? This is driven by the PointOfInterest component below.")]
    public string nodeType = "Enemy";

    [Header("Rewards & Progression")]
    public FTUERewardType rewardType = FTUERewardType.None;
    public bool levelUpOnComplete = true;

    [Header("Path Settings")]
    public System.Collections.Generic.List<PointOfInterest> pathEnemies;

    public PointOfInterest GetPOI() => GetComponent<PointOfInterest>();

    private void OnValidate()
    {
        var poi = GetPOI();
        if (poi != null)
        {
            nodeType = (poi.enemyType == EnemyType.TreasureChest) ? "TREASURE" : "ENEMY (" + poi.enemyType + ")";
        }
    }

    [ContextMenu("Auto-Assign Name")]
    private void AutoAssignName()
    {
        var poi = GetPOI();
        if (poi != null)
        {
            gameObject.name = $"FTUE_Step_{poi.enemyType}";
        }
    }
}
