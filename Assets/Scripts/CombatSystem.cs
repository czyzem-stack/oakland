using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance;

    public CharacterStats playerStats;
    public CharacterStats currentEnemyStats;
    public bool isInCombat = false;
    public bool isPlayerTurn = false;
    public bool isCombatEnding { get; private set; } = false;

    [Header("Juice Prefabs")]
    public GameObject hitEffectPrefab;
    [SerializeField] private TMP_FontAsset damageFont;
    [SerializeField] private int initialPoolSize = 20;

    private List<FloatingCombatText> pool = new List<FloatingCombatText>();
    private static Canvas worldCombatTextCanvas;

    private void Awake()
    {
        Instance = this;
        isInCombat = false;
        isPlayerTurn = false;
        isAttackSequenceRunning = false;
        currentEnemyStats = null;

        // Try to find a default TMP font
        if (damageFont == null)
        {
            damageFont = Resources.Load<TMP_FontAsset>("Fonts/Alata-Regular SDF");
            if (damageFont == null) damageFont = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        }
        
        if (hitEffectPrefab == null)
        {
            hitEffectPrefab = UnityEngine.Resources.Load<GameObject>("FX_Blood_Splatter_01");
        }

        InitializePool();
    }

    private void InitializePool()
    {
        if (worldCombatTextCanvas == null)
        {
            GameObject canvasGo = new GameObject("WorldCombatTextCanvas");
            worldCombatTextCanvas = canvasGo.AddComponent<Canvas>();
            worldCombatTextCanvas.renderMode = RenderMode.WorldSpace;
            canvasGo.transform.localScale = Vector3.one * 0.0075f;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolItem();
        }
    }

    private FloatingCombatText CreateNewPoolItem()
    {
        GameObject textGo = new GameObject("CombatText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(worldCombatTextCanvas.transform, false);
        textGo.SetActive(false);

        TextMeshProUGUI t = textGo.GetComponent<TextMeshProUGUI>();
        t.font = damageFont;
        t.fontSize = 60;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.outlineWidth = 0.2f;
        t.outlineColor = Color.black;

        textGo.AddComponent<FaceCamera>();
        FloatingCombatText fct = textGo.AddComponent<FloatingCombatText>();
        fct.text = t;
        fct.driftSpeed = 2.0f;
        fct.OnComplete = ReturnToPool;

        pool.Add(fct);
        return fct;
    }

    private void ReturnToPool(FloatingCombatText item)
    {
        item.gameObject.SetActive(false);
    }

    private FloatingCombatText GetFromPool()
    {
        foreach (var item in pool)
        {
            if (item != null && !item.gameObject.activeInHierarchy)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }
        
        // Expand pool if needed
        var newItem = CreateNewPoolItem();
        newItem.gameObject.SetActive(true);
        return newItem;
    }

    private Transform combatCameraAnchor;
    private CameraFollow camFollow;

    private void Start()
    {
        if (Camera.main != null) camFollow = Camera.main.GetComponent<CameraFollow>();
    }

    public string LastCombatAction { get; private set; } = "Prepare for Battle!";

    public void StartCombat(CharacterStats enemy)
    {
        if (isInCombat || enemy == null || enemy.isDead || (playerStats != null && playerStats.isDead) || GenericPopup.IsOpen || EquipmentLootPopup.IsOpen) 
        {
            return;
        }

        // Force clear stuck states
        isCombatEnding = false;
        isAttackSequenceRunning = false; 

        currentEnemyStats = enemy;
        isInCombat = true;
        isPlayerTurn = true;
        LastCombatAction = $"Combat Started against {enemy.name}";
        Debug.Log($"[CombatSystem] Combat Started against {enemy.name}");

        var nav = playerStats.GetComponent<HeroNavigation>();
        if (nav != null) nav.StopMoving("Combat Start");

        StopAllCoroutines();
        StartCoroutine(CombatTransitionRoutine(enemy));
    }

    private IEnumerator CombatTransitionRoutine(CharacterStats enemy)
    {
        var pAgent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var eAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (pAgent != null) pAgent.enabled = false;
        // Completely disable enemy agent during the cinematic transition to prevent "fighting" with transform updates
        if (eAgent != null) eAgent.enabled = false;

        // Calculate horizontal direction only to avoid vertical offsets in positioning
        Vector3 dirToPlayer = playerStats.transform.position - enemy.transform.position;
dirToPlayer.y = 0;
        Vector3 directionToPlayer = dirToPlayer.normalized;
        if (directionToPlayer.sqrMagnitude < 0.01f) directionToPlayer = Vector3.back;

        // Pin the combat arena height to the player's current ground level
        Vector3 center = (playerStats.transform.position + enemy.transform.position) * 0.5f;
        center.y = playerStats.transform.position.y; 

        Vector3 playerTarget = center + directionToPlayer * 1.25f;
        Vector3 enemyTarget = center - directionToPlayer * 1.25f;

        UnityEngine.AI.NavMeshHit hit;
        // Sample NavMesh at the calculated ground positions
        if (UnityEngine.AI.NavMesh.SamplePosition(playerTarget, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas)) playerTarget = hit.position;
        if (UnityEngine.AI.NavMesh.SamplePosition(enemyTarget, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas)) enemyTarget = hit.position;

        Animator pAnim = playerStats.GetComponent<Animator>();
        Animator eAnim = enemy.GetComponent<Animator>();
        
        bool isChest = enemy.name.Contains("Chest");
        bool isDragon = enemy.name.Contains("DragonBob");
        bool isWorm = enemy.name.Contains("Worm");

        if (pAnim != null) pAnim.SafeSetFloat("Speed", 1.0f); // Match walk/run speed
        if (eAnim != null)
        {
            if (isChest) eAnim.CrossFade("WalkFWD", 0.15f);
            else if (!isDragon && !isWorm) eAnim.SafeSetFloat("Speed", 1.0f);
        }

        Vector3 pStart = playerStats.transform.position;
        Vector3 eStart = enemy.transform.position;
        Quaternion pStartRot = playerStats.transform.rotation;
        Quaternion eStartRot = enemy.transform.rotation;

        if (camFollow != null)
        {
            if (combatCameraAnchor == null) combatCameraAnchor = new GameObject("CombatCameraAnchor").transform;
            
            // Robust Anchor Placement: Avoid "moving away" by clamping to player/enemy vicinity
            Vector3 camTargetMid = (playerStats.transform.position + enemy.transform.position) * 0.5f;
            camTargetMid.y = playerStats.transform.position.y;
            combatCameraAnchor.position = camTargetMid;
            
            camFollow.target = combatCameraAnchor;
            camFollow.isCombatOrbiting = true;
        }

        float duration = 0.5f; // Faster transition
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = t * t * (3 - 2 * t); // Smoothstep

            if (playerStats == null || enemy == null) yield break;

            playerStats.transform.position = Vector3.Lerp(pStart, playerTarget, curve);
            
            Vector3 lookTarget = new Vector3(enemyTarget.x, playerStats.transform.position.y, enemyTarget.z);
            if ((lookTarget - playerStats.transform.position).sqrMagnitude > 0.01f)
            {
                playerStats.transform.rotation = Quaternion.Slerp(pStartRot, Quaternion.LookRotation(lookTarget - playerStats.transform.position), curve);
            }

            if (!isWorm)
            {
                enemy.transform.position = Vector3.Lerp(eStart, enemyTarget, curve);
                Vector3 eLookTarget = new Vector3(playerTarget.x, enemy.transform.position.y, playerTarget.z);
                if ((eLookTarget - enemy.transform.position).sqrMagnitude > 0.01f)
                {
                    enemy.transform.rotation = Quaternion.Slerp(eStartRot, Quaternion.LookRotation(eLookTarget - enemy.transform.position), curve);
                }
            }

            if (combatCameraAnchor != null)
            {
                combatCameraAnchor.position = (playerStats.transform.position + enemy.transform.position) * 0.5f;
            }

            yield return null;
        }

        if (playerStats != null) playerStats.transform.position = playerTarget;
        if (enemy != null && !isWorm) enemy.transform.position = enemyTarget; 

        if (pAnim != null) pAnim.SafeSetFloat("Speed", 0f);
        if (eAnim != null)
        {
            if (isChest) eAnim.CrossFade("IdleBattle", 0.25f);
            else if (!isDragon && !isWorm) eAnim.SafeSetFloat("Speed", 0f);
        }

        if (eAnim != null) 
        {
            if (isDragon) eAnim.CrossFade("Scream", 0.2f);
            else if (isWorm) eAnim.CrossFade("GroundBreakThrough", 0.1f);
            else if (isChest) { /* IdleBattle set above */ }
            else eAnim.SafeSetFloat("Speed", 0f);
        }
        if (pAnim != null) pAnim.ResetToLocomotion(0.15f);

        yield return new WaitForSeconds(isWorm ? 0.8f : 0.15f);

        StartCoroutine(CombatLoop());
        }

    private IEnumerator CombatLoop()
    {
        while (isInCombat)
        {
            // Ensure enemy is always facing the player at the start of each phase
            if (currentEnemyStats != null && !currentEnemyStats.isDead)
            {
                Vector3 lookPos = playerStats.transform.position;
                lookPos.y = currentEnemyStats.transform.position.y;
                currentEnemyStats.transform.LookAt(lookPos);
            }

            if (isPlayerTurn)
            {
                yield return new WaitUntil(() => !isPlayerTurn || !isInCombat);
            }
else
            {
                yield return new WaitForSeconds(0.5f);
                if (!isInCombat) break;
                yield return StartCoroutine(EnemyAttackSequence());
                if (!isInCombat) break;
                if (playerStats.currentHP <= 0) EndCombat(false);
                else isPlayerTurn = true;
            }
            yield return null;
        }
    }

    public bool IsAttackSequenceRunning => isAttackSequenceRunning;
    private bool isAttackSequenceRunning = false;

    public void OnPlayerRoll(int rollValue)
    {
        Debug.Log($"[CombatSystem] OnPlayerRoll: {rollValue}. InCombat={isInCombat}, PlayerTurn={isPlayerTurn}, AttackRunning={isAttackSequenceRunning}");
        if (!isInCombat || !isPlayerTurn || isAttackSequenceRunning) return;
        StartCoroutine(PlayerAttackSequence(rollValue));
    }

    private IEnumerator PlayerAttackSequence(int rollValue)
    {
        isAttackSequenceRunning = true;
        try
        {
            // Energy Cost check
            int energyCost = 5;
            if (playerStats.currentMana < energyCost)
            {
                SpawnDamageText(playerStats.transform.position + Vector3.up * 2.5f, "LOW ENERGY!", new Color(0.6f, 0f, 1f));
                yield return new WaitForSeconds(0.8f);
                isPlayerTurn = false;
                yield break;
            }

            playerStats.ConsumeMana(energyCost);

            // Add XP on every roll in combat as requested
            if (playerStats != null)
            {
                // Slightly tuned down to ensure L3 target
                float xpGain = rollValue * 3f; 
                playerStats.AddXP(xpGain);
                Debug.Log($"[CombatSystem] Gained {xpGain} XP from combat roll.");
            }

            bool isCritical = rollValue >= playerStats.critThreshold; 
            Animator playerAnim = playerStats.GetComponent<Animator>();
            
            yield return new WaitForSeconds(0.1f);

            if (currentEnemyStats == null || currentEnemyStats.gameObject == null) yield break;

            if (playerAnim != null && playerAnim.gameObject.activeInHierarchy)
            {
                playerAnim.SafeSetTrigger("Attack");
            }

            // Small delay for the animation swing to reach the target before damage hits
            yield return new WaitForSeconds(0.25f);

            if (currentEnemyStats == null || currentEnemyStats.gameObject == null) yield break;

            int baseDamage = rollValue + playerStats.MeleeDamage;

            // Stage FTUE: Steve deals double damage to finish tutorial fights faster
            if (FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
            {
                baseDamage *= 2;
            }

            int finalDamage = isCritical ? baseDamage * 2 : baseDamage;

            Debug.Log($"[CombatSystem] Player attacking {currentEnemyStats.name}. Roll: {rollValue}, BaseAtk: {playerStats.MeleeDamage}, Total: {finalDamage}");
            
            currentEnemyStats.TakeDamage(finalDamage);

            LastCombatAction = $"{currentEnemyStats.name} takes {finalDamage} dmg (Roll {rollValue})";
            
            SpawnDamageText(currentEnemyStats.transform.position + Vector3.up * 2f, $"{(isCritical ? "CRITICAL! -" : "-")}{finalDamage}", isCritical ? Color.yellow : Color.red);
            
            if (hitEffectPrefab != null)
            {
                GameObject fx = Instantiate(hitEffectPrefab, currentEnemyStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                Destroy(fx, 2f);
            }

            if (camFollow != null) camFollow.Shake(0.12f, isCritical ? 0.4f : 0.2f);

            Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
            if (enemyAnim != null && enemyAnim.gameObject.activeInHierarchy)
            {
                if (currentEnemyStats.name.Contains("Worm")) enemyAnim.CrossFade("GetHit", 0.05f);
                else enemyAnim.SafeSetTrigger("GetHit");
            }

            yield return new WaitForSeconds(0.9f); 

            if (currentEnemyStats != null && currentEnemyStats.currentHP <= 0)
            {
                isCombatEnding = true;

                // Immediately disable physical presence and AI to stop "post-death" attacks
                var obstacle = currentEnemyStats.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (obstacle != null) obstacle.enabled = false;
                var collider = currentEnemyStats.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                var orc = currentEnemyStats.GetComponent<OrcPatrol>();
                if (orc != null) orc.enabled = false;

                if (enemyAnim != null && enemyAnim.gameObject.activeInHierarchy)
                {
                    bool isDragon = currentEnemyStats.name.Contains("DragonBob");
                    bool isWorm = currentEnemyStats.name.Contains("Worm");
                    if (isDragon || isWorm) enemyAnim.CrossFade("Die", 0.1f);
                    else enemyAnim.SafeSetTrigger("Die");
                }
                
                // Transition to victory sequence
                EndCombat(true);
            }
            else
            {
                isPlayerTurn = false;
            }
            }
            finally
            {
            isAttackSequenceRunning = false;
            }
            }

    private IEnumerator EnemyAttackSequence()
    {
        if (isCombatEnding || currentEnemyStats == null || currentEnemyStats.isDead || !currentEnemyStats.gameObject.activeInHierarchy)
        {
            yield break;
        }

        bool isPassive = currentEnemyStats.name.Contains("Mushroom");
if (isPassive)
        {
            yield return new WaitForSeconds(0.5f);
            isPlayerTurn = true;
            yield break;
        }

        Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
        bool isDragon = currentEnemyStats.name.Contains("DragonBob");
        bool isWorm = currentEnemyStats.name.Contains("Worm");

        if (enemyAnim != null && enemyAnim.gameObject.activeInHierarchy)
        {
            if (isDragon) 
            {
                string[] attacks = { "Flame Attack", "Claw Attack", "Basic Attack" };
                enemyAnim.CrossFade(attacks[Random.Range(0, attacks.Length)], 0.2f);
            }
            else if (isWorm)
            {
                string[] attacks = { "Attack01", "Attack02", "Attack03", "Attack04" };
                enemyAnim.CrossFade(attacks[Random.Range(0, attacks.Length)], 0.2f);
            }
            else 
            {
                enemyAnim.SafeSetTrigger("Attack");
            }
        }

        yield return new WaitForSeconds(0.45f);

        if (playerStats == null || playerStats.isDead || currentEnemyStats == null) yield break;

        int enemyRoll = Random.Range(1, 13);
bool isCritical = enemyRoll >= currentEnemyStats.critThreshold;
        int damage = enemyRoll + currentEnemyStats.MeleeDamage;
        if (isCritical) damage *= 2;

        playerStats.TakeDamage(damage);
        LastCombatAction = $"{currentEnemyStats.name} does {damage} damg";
        SpawnDamageText(playerStats.transform.position + Vector3.up * 2f, $"{(isCritical ? "CRITICAL! -" : "-")}{damage}", Color.red);
    
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, playerStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (camFollow != null) camFollow.Shake(0.12f, isCritical ? 0.3f : 0.15f);

        Animator playerAnim = playerStats.GetComponent<Animator>();
        if (playerAnim != null && playerAnim.gameObject.activeInHierarchy) playerAnim.SafeSetTrigger("GetHit");

        if (playerStats.isDead) yield break;

        yield return new WaitForSeconds(0.4f);
        if (isDragon && enemyAnim != null && enemyAnim.gameObject.activeInHierarchy) enemyAnim.CrossFade("Fly Float", 0.5f);
        
        isPlayerTurn = true;
    }

        public static void SpawnText(Vector3 position, string text, Color color)
        {
            if (Instance == null) return;
            
            FloatingCombatText fct = Instance.GetFromPool();
            fct.transform.position = position + Vector3.up * 0.5f;
            fct.Setup(text, color);
        }

        public void SpawnDamageText(Vector3 position, string text, Color color)
        {
            SpawnText(position, text, color);
        }

        public void EndCombat(bool playerWon)
        {
            if (!isInCombat && !isCombatEnding) return;
        
            if (playerWon)
            {
                StartCoroutine(VictorySequenceRoutine());
            }
            else
            {
                isCombatEnding = false;
                isInCombat = false;
                isPlayerTurn = false;
                isAttackSequenceRunning = false;
                LastCombatAction = "Defeat... Steve has fallen.";
                StartCoroutine(PlayerDeathSequenceRoutine());
            }
        }

        private IEnumerator VictorySequenceRoutine()
        {
            // Immediate State Lock
            isCombatEnding = true;
            isInCombat = false; // Hide combat UI immediately
            isPlayerTurn = false;
            isAttackSequenceRunning = false;
        
            LastCombatAction = "Victory! Steve won the battle.";
        
            Animator playerAnim = playerStats.GetComponent<Animator>();
            if (playerAnim != null) 
            {
                playerAnim.SafeSetFloat("Speed", 0f);
                playerAnim.SafeSetTrigger("Victory");
            }

            if (currentEnemyStats != null)
            {
                // Immediately disable enemy logic and physical presence
                var chestEnemy = currentEnemyStats.GetComponent<ChestEnemy>();
                if (chestEnemy != null) chestEnemy.enabled = false;
                var orcPatrol = currentEnemyStats.GetComponent<OrcPatrol>();
                if (orcPatrol != null) orcPatrol.enabled = false;
                var wormEnemy = currentEnemyStats.GetComponent<WormEnemy>();
                if (wormEnemy != null) wormEnemy.enabled = false;
            
                var obstacle = currentEnemyStats.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (obstacle != null) obstacle.enabled = false;
                var collider = currentEnemyStats.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;

                bool isChest = currentEnemyStats.name.Contains("TreasureChest") || currentEnemyStats.name.Contains("Chest");
                bool isDragon = currentEnemyStats.name.Contains("DragonBob");
                bool isOrc = currentEnemyStats.name.Contains("Orc");

                if (isOrc) GameSettings.Instance?.RegisterOrcKill();

                string defeatedName = currentEnemyStats.name;

                // Staggered Rewards - Common Sense Flow
                if (!isChest)
                {
                    // 1. XP Pop
                    float xpGain = isDragon ? 50f : 20f;
                    playerStats.AddXP(xpGain);
                
                    yield return new WaitForSeconds(0.5f);
                
                    // 2. Gold Pop
                    int goldGain = isDragon ? Random.Range(100, 201) : Random.Range(5, 11);
                    playerStats.AddGold(goldGain);
                
                    yield return new WaitForSeconds(0.5f);
                }

                // For chests, we want to see them open and then show the popup
                if (isChest)
                {
                    // Play open animation
                    Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
                    if (enemyAnim != null) enemyAnim.CrossFade("Die", 0.1f);

                    // Wait for the chest to "open" fully
                    yield return new WaitForSeconds(1.2f);
                
                    // Show popup BEFORE destroying the object so it stays in the background
                    ShowChestUpgradePopup(defeatedName);
                
                    // The popup callback (ResumeAfterChest) handles the rest of the flow
                    yield break;
                }
                else
                {
                    // For regular enemies, destroy them after showing the death animation
                    GameObject.Destroy(currentEnemyStats.gameObject, isDragon ? 3.0f : 1.5f);
                
                    // Savor the victory pose
                    yield return new WaitForSeconds(1.5f);

                    // Final cleanup
                    FinishCombatCleanup(defeatedName);
                }
            }
            else
            {
                FinishCombatCleanup("");
            }
        
            isCombatEnding = false;
        }

        public void ForceResetCombat()
        {
            isInCombat = false;
            isCombatEnding = false;
            isAttackSequenceRunning = false;
            if (combatCameraAnchor != null) { Destroy(combatCameraAnchor.gameObject); combatCameraAnchor = null; }
            Debug.Log("[CombatSystem] Force reset applied.");
        }

        private void FinishCombatCleanup(string defeatedName)
        {
            isInCombat = false;
            isCombatEnding = false;

            if (camFollow != null)
            {
                camFollow.isCombatOrbiting = false;
                // Transition camera back smoothly
                camFollow.target = playerStats.transform;
                if (combatCameraAnchor != null) { Destroy(combatCameraAnchor.gameObject); combatCameraAnchor = null; }
            }

            var agent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                var nav = playerStats.GetComponent<HeroNavigation>();
                // Ensure Steve is grounded but don't force a 'warp' if he's close enough
                if (nav != null) nav.EnsureOnNavMesh(5.0f);
            }

            var heroNav = playerStats.GetComponent<HeroNavigation>();
            var playerAnim = playerStats.GetComponent<Animator>();
            if (playerAnim != null) playerAnim.ResetToLocomotion(0.25f);
            if (heroNav != null) heroNav.ResumeAfterCombat(defeatedName);
        }

        private IEnumerator PlayerDeathSequenceRoutine()
        {
            // Dramatically wait while the camera still orbits Steve on the ground
            yield return new WaitForSeconds(2.5f);

            // Then show the popup
            playerStats.ShowDeathPopup();

            // Finally reset the camera (though the scene will likely reload anyway)
            if (camFollow != null)
            {
                camFollow.isCombatOrbiting = false;
                camFollow.target = playerStats.transform;
                if (combatCameraAnchor != null) { Destroy(combatCameraAnchor.gameObject); combatCameraAnchor = null; }
            }
        }

        private int chestsOpenedThisRun = 0;
        private string lastChestName = "Chest";

        public void ShowChestUpgradePopup(string chestName)
        {
            EquipmentManager em = EquipmentManager.Instance;
            if (em == null)
            {
                Debug.LogError("EquipmentManager Instance is null");
                return;
            }

            // Capture chest name for FTUE progression
            lastChestName = chestName;
            chestsOpenedThisRun++;

            // Check FTUE for specific rewards
            var ftue = FTUEManager.Instance;
            if (ftue == null) ftue = UnityEngine.Object.FindAnyObjectByType<FTUEManager>();
            
            bool isFTUE = ftue != null && ftue.isFTUEActive;
            FTUERewardType rewardType = FTUERewardType.None;
            if (isFTUE && ftue.CurrentStep != null)
            {
                rewardType = ftue.CurrentStep.rewardType;
                Debug.Log($"[CombatSystem] FTUE Active. Step: {ftue.currentStepIndex} ({ftue.CurrentStep.name}), RewardType: {rewardType}");
            }

            EquipmentItem item1 = null;
            Sprite icon1 = null;
            EquipmentItem item2 = null;
            Sprite icon2 = null;
            EquipmentItem item3 = null;
            Sprite icon3 = null;
            int picks = 1;

            if (isFTUE && rewardType != FTUERewardType.None)
            {
                if (rewardType == FTUERewardType.WoodenStick_RandomWeapon)
                {
                    Debug.Log("[CombatSystem] FTUE Reward: Wooden Stick / Random Weapon");
                    item1 = new EquipmentItem { name = "Wooden Stick", slot = EquipmentSlot.Weapon, attackBonus = 1 };
                    icon1 = (em.weaponIcons != null && em.weaponIcons.Length > 7) ? em.weaponIcons[7] : null;

                    int weaponRoll = UnityEngine.Random.Range(0, 7); 
                    string[] names = { "Hunting Bow", "Iron Spear", "Iron Sword 03", "War Axe 10", "War Hammer 11", "Iron Greatsword 01", "Magic Wand 01" };
                    int[] bonuses = { 2, 3, 3, 4, 4, 5, 3 };

                    item2 = new EquipmentItem { name = names[weaponRoll], slot = EquipmentSlot.Weapon, attackBonus = bonuses[weaponRoll] };
                    if (names[weaponRoll] == "Magic Wand 01") item2.witBonus = 2;
                    icon2 = (em.weaponIcons != null && em.weaponIcons.Length > weaponRoll) ? em.weaponIcons[weaponRoll] : null;
                }
                else if (rewardType == FTUERewardType.Armor_Armor)
                {
                    Debug.Log("[CombatSystem] FTUE Reward: Armor / Armor Choice");
                    string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
                    int[] gritBonuses = { 1, 2, 2, 3, 3 };
                    int[] brawnBonuses = { 0, 0, 1, 0, 2 };

                    int idx1 = UnityEngine.Random.Range(0, 3); 
                    int idx2 = UnityEngine.Random.Range(3, 5); 

                    item1 = new EquipmentItem { name = armorNames[idx1], slot = EquipmentSlot.Chest, gritBonus = gritBonuses[idx1], brawnBonus = brawnBonuses[idx1] };
                    icon1 = (em.chestIcons != null && em.chestIcons.Length > idx1) ? em.chestIcons[idx1] : null;

                    item2 = new EquipmentItem { name = armorNames[idx2], slot = EquipmentSlot.Chest, gritBonus = gritBonuses[idx2], brawnBonus = brawnBonuses[idx2] };
                    icon2 = (em.chestIcons != null && em.chestIcons.Length > idx2) ? em.chestIcons[idx2] : null;
                }
                else if (rewardType == FTUERewardType.Shield_Helm)
                {
                    Debug.Log("[CombatSystem] FTUE Reward: Shield / Helm Choice");
                    
                    int shieldIdx = UnityEngine.Random.Range(0, 4);
                    string[] shieldNames = { "Log", "Iron Shield", "Steel Shield", "Magic Shield" };
                    item1 = new EquipmentItem { name = shieldNames[shieldIdx], slot = EquipmentSlot.Shield, gritBonus = shieldIdx + 1, brawnBonus = shieldIdx / 2 };
                    icon1 = (em.shieldIcons != null && em.shieldIcons.Length > shieldIdx) ? em.shieldIcons[shieldIdx] : null;

                    int helmIdx = UnityEngine.Random.Range(0, 5);
                    string[] helmNames = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
                    item2 = new EquipmentItem { name = helmNames[helmIdx], slot = EquipmentSlot.Helmet, witBonus = 1 + (helmIdx / 2), gritBonus = helmIdx / 2 };
                    icon2 = (em.helmetIcons != null && em.helmetIcons.Length > helmIdx) ? em.helmetIcons[helmIdx] : null;
                    picks = 1;
                }
            }
else
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    int weaponRoll = UnityEngine.Random.Range(0, 8);
                    string[] names = { "Hunting Bow", "Iron Spear", "Iron Sword 03", "War Axe 10", "War Hammer 11", "Iron Greatsword 01", "Magic Wand 01", "Stronger Stick" };
                    int[] bonuses = { 2, 3, 3, 4, 4, 5, 3, 2 };
                    item1 = new EquipmentItem { name = names[weaponRoll], slot = EquipmentSlot.Weapon, attackBonus = bonuses[weaponRoll] };
                    if (names[weaponRoll] == "Magic Wand 01") item1.witBonus = 2;
                    icon1 = (em.weaponIcons != null && em.weaponIcons.Length > weaponRoll) ? em.weaponIcons[weaponRoll] : null;
                }
                else
                {
                    int armorIndex = UnityEngine.Random.Range(0, 5);
                    string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
                    int[] gritBonuses = { 1, 2, 2, 3, 3 };
                    int[] brawnBonuses = { 0, 0, 1, 0, 2 };
                    item1 = new EquipmentItem { name = armorNames[armorIndex], slot = EquipmentSlot.Chest, gritBonus = gritBonuses[armorIndex], brawnBonus = brawnBonuses[armorIndex] };
                    icon1 = (em.chestIcons != null && em.chestIcons.Length > armorIndex) ? em.chestIcons[armorIndex] : null;
                }

                float roll = UnityEngine.Random.value;
                if (roll < 0.2f)
                {
                    int idx = UnityEngine.Random.Range(0, 5);
                    string[] names = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Helmet, witBonus = 1 + (idx / 2), gritBonus = idx / 2 };
                    icon2 = (em.helmetIcons != null && em.helmetIcons.Length > idx) ? em.helmetIcons[idx] : null;
                }
                else if (roll < 0.4f)
                {
                    int idx = UnityEngine.Random.Range(0, 3);
                    string[] names = { "Traveler's Cloak", "Ranger Cape", "Royal Mantle" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Cloak, witBonus = idx + 1, gritBonus = idx + 1 };
                    icon2 = (em.cloakIcons != null && em.cloakIcons.Length > idx) ? em.cloakIcons[idx] : null;
                }
                else if (roll < 0.6f)
                {
                    int idx = UnityEngine.Random.Range(0, 3);
                    string[] names = { "Leather Gloves", "Plate Gauntlets", "Silk Mitts" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Gloves, brawnBonus = (idx == 1 ? 2 : 1), witBonus = (idx == 2 ? 2 : 0) };
                    icon2 = (em.gloveIcons != null && em.gloveIcons.Length > idx) ? em.gloveIcons[idx] : null;
                }
                else if (roll < 0.8f)
                {
                    int idx = UnityEngine.Random.Range(0, 3);
                    string[] names = { "Leather Boots", "Iron Greaves", "Swift Shoes" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Boots, finesseBonus = (idx == 2 ? 2 : 1), gritBonus = (idx == 1 ? 2 : 0) };
                    icon2 = (em.bootIcons != null && em.bootIcons.Length > idx) ? em.bootIcons[idx] : null;
                }
                else
                {
                    int idx = UnityEngine.Random.Range(0, 4);
                    string[] names = { "Log", "Iron Shield", "Steel Shield", "Magic Shield" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Shield, gritBonus = idx + 1, brawnBonus = idx / 2 };
                    icon2 = (em.shieldIcons != null && em.shieldIcons.Length > idx) ? em.shieldIcons[idx] : null;
                }
            }

            EquipmentLootPopup.Show(item1, item2, icon1, icon2, () => { em.Equip(item1); }, () => { em.Equip(item2); },
                item3, icon3, () => { em.Equip(item3); }, picks, ResumeAfterChest);
        }

        private void ResumeAfterChest()
        {
            // Destroy the chest now that we are done with the loot
            if (currentEnemyStats != null)
            {
                // Deactivate first to be doubly sure NavMesh is clear
                currentEnemyStats.gameObject.SetActive(false);
                GameObject.Destroy(currentEnemyStats.gameObject);
                currentEnemyStats = null;
            }

            FinishCombatCleanup(lastChestName);
        }

        private void ApplyChestUpgrade(string statName)
        {
            if (playerStats == null) return;
            playerStats.ApplyStatUpgrade(statName, 2, true);
            ResumeAfterChest();
        }
        }
