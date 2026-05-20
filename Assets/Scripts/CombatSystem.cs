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

    [Header("Juice Prefabs")]
    public GameObject hitEffectPrefab;
    private TMP_FontAsset damageFont;

    private void Awake()
    {
        Instance = this;
        isInCombat = false;
        isPlayerTurn = false;
        isAttackSequenceRunning = false;
        currentEnemyStats = null;

        // Try to find a default TMP font
damageFont = Resources.Load<TMP_FontAsset>("Fonts/Alata-Regular SDF");
        if (damageFont == null) damageFont = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        
        if (hitEffectPrefab == null)
        {
            hitEffectPrefab = UnityEngine.Resources.Load<GameObject>("FX_Blood_Splatter_01");
        }
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
            if (isInCombat) Debug.LogWarning($"[CombatSystem] Cannot start combat. Already in combat.");
            return;
        }

        currentEnemyStats = enemy;
        isInCombat = true;
        isPlayerTurn = true;
        isAttackSequenceRunning = false; 
        LastCombatAction = $"Combat Started against {enemy.name}";
        Debug.Log($"[CombatSystem] Combat Started against {enemy.name} (Entity ID: {enemy.GetEntityId()})");

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
        if (eAgent != null && eAgent.isOnNavMesh) eAgent.isStopped = true;

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
            
            // Start camera moving toward midpoint immediately
            Vector3 camPos = (pStart + eStart) * 0.5f;
            camPos.y = pStart.y;
            combatCameraAnchor.position = camPos;
            
            camFollow.target = combatCameraAnchor;
            camFollow.isCombatOrbiting = true;
        }

        float duration = isChest ? 1.0f : 0.45f; // slightly longer for chests to feel smoother
float elapsed = 0;

        // If we are already close, don't force a 'jump'
        if (Vector3.Distance(pStart, playerTarget) < 0.5f) duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Buttery smooth cubic curve
            float curve = t * t * (3 - 2 * t); 

            playerStats.transform.position = Vector3.Lerp(pStart, playerTarget, curve);
            
            Vector3 lookTarget = new Vector3(enemyTarget.x, playerStats.transform.position.y, enemyTarget.z);
            if ((lookTarget - playerStats.transform.position).sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookTarget - playerStats.transform.position);
                playerStats.transform.rotation = Quaternion.Slerp(pStartRot, targetRot, curve);
            }

            // Let the enemy lerp to their spot too
            if (!isWorm)
            {
                enemy.transform.position = Vector3.Lerp(eStart, enemyTarget, curve);
            }
            
            Vector3 eLookTarget = new Vector3(playerTarget.x, enemy.transform.position.y, playerTarget.z);
            if ((eLookTarget - enemy.transform.position).sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(eLookTarget - enemy.transform.position);
                enemy.transform.rotation = Quaternion.Slerp(eStartRot, targetRot, curve);
            }

            // Move camera anchor as they move
            if (combatCameraAnchor != null)
            {
                Vector3 currentMid = (playerStats.transform.position + enemy.transform.position) * 0.5f;
                currentMid.y = playerStats.transform.position.y;
                combatCameraAnchor.position = currentMid;
            }

            yield return null;
        }

        playerStats.transform.position = playerTarget;
        if (!isWorm) enemy.transform.position = enemyTarget; 
        
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
            else eAnim.SafeSetTrigger("GetHit");
        }
        if (pAnim != null) pAnim.SafeSetTrigger("GetHit");

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
            
            bool isCritical = rollValue >= playerStats.critThreshold; 
            Animator playerAnim = playerStats.GetComponent<Animator>();
            
            yield return new WaitForSeconds(0.1f);

            if (currentEnemyStats == null || currentEnemyStats.gameObject == null) yield break;

            if (playerAnim != null && playerAnim.gameObject.activeInHierarchy)
            {
                playerAnim.SafeSetTrigger("Attack");
            }

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

            yield return new WaitForSeconds(0.35f); 

            if (currentEnemyStats != null && currentEnemyStats.currentHP <= 0)
            {
                if (playerStats != null)
                {
                    float xpGain = rollValue * playerStats.amountPerKill;
                    playerStats.AddXP(xpGain);
                }

                if (enemyAnim != null && enemyAnim.gameObject.activeInHierarchy)
                {
                    bool isDragon = currentEnemyStats.name.Contains("DragonBob");
                    bool isWorm = currentEnemyStats.name.Contains("Worm");
                    if (isDragon || isWorm) enemyAnim.CrossFade("Die", 0.1f);
                    else enemyAnim.SafeSetTrigger("Die");
                }
                yield return new WaitForSeconds(0.4f);
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
        if (currentEnemyStats == null || currentEnemyStats.gameObject == null || !currentEnemyStats.gameObject.activeInHierarchy)
        {
            EndCombat(true);
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

        yield return new WaitForSeconds(0.2f);

        if (playerStats == null || playerStats.isDead) yield break;

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

        private static Canvas worldCombatTextCanvas;

        public static void SpawnText(Vector3 position, string text, Color color)
        {
            if (worldCombatTextCanvas == null)
            {
                GameObject canvasGo = new GameObject("WorldCombatTextCanvas");
                worldCombatTextCanvas = canvasGo.AddComponent<Canvas>();
                worldCombatTextCanvas.renderMode = RenderMode.WorldSpace;
                canvasGo.transform.localScale = Vector3.one * 0.0075f;
            }

            GameObject textGo = new GameObject("CombatText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(worldCombatTextCanvas.transform, false);
            textGo.transform.position = position + Vector3.up * 0.5f;

            TextMeshProUGUI t = textGo.GetComponent<TextMeshProUGUI>();
            t.font = Resources.Load<TMP_FontAsset>("Alata-Regular SDF");
            if (t.font == null) t.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            t.fontSize = 60; 
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.outlineWidth = 0.2f;
            t.outlineColor = Color.black;
            t.text = text;
            t.color = color;

            textGo.AddComponent<FaceCamera>();
            FloatingCombatText fct = textGo.AddComponent<FloatingCombatText>();
            fct.text = t;
            fct.driftSpeed = 2.0f;
            fct.Setup(text, color);
        }

        public void SpawnDamageText(Vector3 position, string text, Color color)
        {
            SpawnText(position, text, color);
        }

        public void EndCombat(bool playerWon)
        {
            if (!isInCombat) return;
            isInCombat = false;
            isPlayerTurn = false;
            isAttackSequenceRunning = false;

            Animator playerAnim = playerStats.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.ResetTrigger("Attack");
                playerAnim.ResetTrigger("GetHit");
                playerAnim.SafeSetFloat("Speed", 0f);
            }

            if (playerWon)
            {
                LastCombatAction = "Victory! Steve won the battle.";
                // Victory cleanup
                if (camFollow != null)
                {
                    camFollow.isCombatOrbiting = false;
                    camFollow.target = playerStats.transform;
                    if (combatCameraAnchor != null) { Destroy(combatCameraAnchor.gameObject); combatCameraAnchor = null; }
                }

                var agent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = true;
                    var nav = playerStats.GetComponent<HeroNavigation>();
                    if (nav != null)
                        nav.EnsureOnNavMesh();
                    else if (UnityEngine.AI.NavMesh.SamplePosition(playerStats.transform.position, out UnityEngine.AI.NavMeshHit hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }

                if (currentEnemyStats != null)
                {
                    if (playerAnim != null) playerAnim.SafeSetTrigger("Victory");
                    bool isChest = currentEnemyStats.name.Contains("TreasureChest") || currentEnemyStats.name.Contains("Chest");
                    bool isDragon = currentEnemyStats.name.Contains("DragonBob");
                    bool isOrc = currentEnemyStats.name.Contains("Orc");

                    if (isOrc) GameSettings.Instance?.RegisterOrcKill();

                    if (isChest) playerStats.AddGold(Random.Range(20, 51));
                    else if (isDragon) playerStats.AddGold(Random.Range(100, 201));
                    else playerStats.AddGold(Random.Range(5, 11));

                    GameObject.Destroy(currentEnemyStats.gameObject, isDragon ? 3.0f : 1.5f);
                    currentEnemyStats = null;

                    if (isChest)
                        ShowChestUpgradePopup();
                    else
                    {
                        var nav = playerStats.GetComponent<HeroNavigation>();
                        if (nav != null) nav.ResumeAfterCombat();
                    }
                }
            }
            else
            {
                LastCombatAction = "Defeat... Steve has fallen.";
                // Failure cleanup (Steve died)
                StartCoroutine(PlayerDeathSequenceRoutine());
            }
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

        public void ShowChestUpgradePopup()
        {
            EquipmentManager em = EquipmentManager.Instance;
            if (em == null)
            {
                Debug.LogError("EquipmentManager Instance is null");
                return;
            }

            chestsOpenedThisRun++;
            
            // Check FTUE for specific rewards
            var ftue = FTUEManager.Instance;
            if (ftue == null) ftue = Object.FindAnyObjectByType<FTUEManager>();
            
            bool isFTUE = ftue != null && ftue.isFTUEActive;
            FTUEStage currentStage = isFTUE ? ftue.currentStage : FTUEStage.Completed;

            Debug.Log($"[CombatSystem] Opening Chest. isFTUE: {isFTUE}, currentStage: {currentStage}");

            EquipmentItem item1 = null;
            Sprite icon1 = null;
            EquipmentItem item2 = null;
            Sprite icon2 = null;

            if (isFTUE && currentStage == FTUEStage.Chest1)
            {
                Debug.Log("[CombatSystem] FTUE Reward: Chest 1 (Stick or Random)");
                // Chest 1: Stick or Random Weapon
                // Option 1: Basic Stick
                item1 = new EquipmentItem { name = "Wooden Stick", slot = EquipmentSlot.Weapon, attackBonus = 1 };
                icon1 = (em.weaponIcons != null && em.weaponIcons.Length > 7) ? em.weaponIcons[7] : null;

                // Option 2: Random Weapon (excluding Stick)
                int weaponRoll = UnityEngine.Random.Range(0, 7); // 0-6
                string[] names = { "Hunting Bow", "Iron Spear", "Iron Sword 03", "War Axe 10", "War Hammer 11", "Iron Greatsword 01", "Magic Wand 01" };
                int[] bonuses = { 2, 3, 3, 4, 4, 5, 3 };

                item2 = new EquipmentItem { name = names[weaponRoll], slot = EquipmentSlot.Weapon, attackBonus = bonuses[weaponRoll] };
                if (names[weaponRoll] == "Magic Wand 01") item2.witBonus = 2;
                icon2 = (em.weaponIcons != null && em.weaponIcons.Length > weaponRoll) ? em.weaponIcons[weaponRoll] : null;
            }
else if (isFTUE && currentStage == FTUEStage.Chest2)
            {
                Debug.Log("[CombatSystem] FTUE Reward: Chest 2 (Armor Choice)");
                // Chest 2: Armor choices
                string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
                int[] gritBonuses = { 1, 2, 2, 3, 3 };
                int[] brawnBonuses = { 0, 0, 1, 0, 2 };

                int idx1 = UnityEngine.Random.Range(0, 3); // Light armor
                int idx2 = UnityEngine.Random.Range(3, 5); // Heavy armor

                item1 = new EquipmentItem { name = armorNames[idx1], slot = EquipmentSlot.Chest, gritBonus = gritBonuses[idx1], brawnBonus = brawnBonuses[idx1] };
                icon1 = (em.chestIcons != null && em.chestIcons.Length > idx1) ? em.chestIcons[idx1] : null;

                item2 = new EquipmentItem { name = armorNames[idx2], slot = EquipmentSlot.Chest, gritBonus = gritBonuses[idx2], brawnBonus = brawnBonuses[idx2] };
                icon2 = (em.chestIcons != null && em.chestIcons.Length > idx2) ? em.chestIcons[idx2] : null;
            }
            else if (isFTUE && currentStage == FTUEStage.Chest3)
            {
                Debug.Log("[CombatSystem] FTUE Reward: Chest 3 (Shield or Helm/Cape)");
                // Chest 3: Shield or Helmet/Cape
                // Option 1: Shield
                int shieldIdx = UnityEngine.Random.Range(0, 4);
                string[] shieldNames = { "Log", "Iron Shield", "Steel Shield", "Magic Shield" };
                item1 = new EquipmentItem { name = shieldNames[shieldIdx], slot = EquipmentSlot.Shield, gritBonus = shieldIdx + 1, brawnBonus = shieldIdx / 2 };
                icon1 = (em.shieldIcons != null && em.shieldIcons.Length > shieldIdx) ? em.shieldIcons[shieldIdx] : null;

                // Option 2: Helmet or Cape
                if (UnityEngine.Random.value < 0.5f)
                {
                    int idx = UnityEngine.Random.Range(0, 5);
                    string[] names = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Helmet, witBonus = 1 + (idx / 2), gritBonus = idx / 2 };
                    icon2 = (em.helmetIcons != null && em.helmetIcons.Length > idx) ? em.helmetIcons[idx] : null;
                }
                else
                {
                    int capeIdx = UnityEngine.Random.Range(0, 3);
                    string[] capeNames = { "Traveler's Cloak", "Ranger Cape", "Royal Mantle" };
                    item2 = new EquipmentItem { name = capeNames[capeIdx], slot = EquipmentSlot.Cloak, witBonus = capeIdx + 1, gritBonus = capeIdx + 1 };
                    icon2 = (em.cloakIcons != null && em.cloakIcons.Length > capeIdx) ? em.cloakIcons[capeIdx] : null;
                }
            }
else
            {
                // Default random behavior
                if (UnityEngine.Random.value < 0.5f)
                {
                    int weaponRoll = UnityEngine.Random.Range(0, 8);
                    string[] names = { "Hunting Bow", "Iron Spear", "Iron Sword 03", "War Axe 10", "War Hammer 11", "Iron Greatsword 01", "Magic Wand 01", "Stronger Stick" };
                    int[] bonuses = { 2, 3, 3, 4, 4, 5, 3, 2 };
                    item1 = new EquipmentItem { name = names[weaponRoll], slot = EquipmentSlot.Weapon, attackBonus = bonuses[weaponRoll] };
                    if (names[weaponRoll] == "Magic Wand 01") item1.witBonus = 2;
                    icon1 = em.weaponIcons.Length > weaponRoll ? em.weaponIcons[weaponRoll] : null;
                }
                else
                {
                    int armorIndex = UnityEngine.Random.Range(0, 5);
                    string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
                    int[] gritBonuses = { 1, 2, 2, 3, 3 };
                    int[] brawnBonuses = { 0, 0, 1, 0, 2 };
                    item1 = new EquipmentItem { name = armorNames[armorIndex], slot = EquipmentSlot.Chest, gritBonus = gritBonuses[armorIndex], brawnBonus = brawnBonuses[armorIndex] };
                    icon1 = em.chestIcons.Length > armorIndex ? em.chestIcons[armorIndex] : null;
                }

                float roll = UnityEngine.Random.value;
                if (roll < 0.2f)
                {
                    int idx = UnityEngine.Random.Range(0, 5);
                    string[] names = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Helmet, witBonus = 1 + (idx / 2), gritBonus = idx / 2 };
                    icon2 = em.helmetIcons.Length > idx ? em.helmetIcons[idx] : null;
                }
                else if (roll < 0.4f)
                {
                    int idx = UnityEngine.Random.Range(0, 3);
                    string[] names = { "Traveler's Cloak", "Ranger Cape", "Royal Mantle" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Cloak, witBonus = idx + 1, gritBonus = idx + 1 };
                    icon2 = em.cloakIcons.Length > idx ? em.cloakIcons[idx] : null;
                }
                else if (roll < 0.6f)
                {
                    int idx = UnityEngine.Random.Range(0, 3);
                    string[] names = { "Leather Gloves", "Plate Gauntlets", "Silk Mitts" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Gloves, brawnBonus = (idx == 1 ? 2 : 1), witBonus = (idx == 2 ? 2 : 0) };
                    icon2 = em.gloveIcons.Length > idx ? em.gloveIcons[idx] : null;
                }
                else if (roll < 0.8f)
                {
                    int idx = UnityEngine.Random.Range(0, 3);
                    string[] names = { "Leather Boots", "Iron Greaves", "Swift Shoes" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Boots, finesseBonus = (idx == 2 ? 2 : 1), gritBonus = (idx == 1 ? 2 : 0) };
                    icon2 = em.bootIcons.Length > idx ? em.bootIcons[idx] : null;
                }
                else
                {
                    int idx = UnityEngine.Random.Range(0, 4);
                    string[] names = { "Log", "Iron Shield", "Steel Shield", "Magic Shield" };
                    item2 = new EquipmentItem { name = names[idx], slot = EquipmentSlot.Shield, gritBonus = idx + 1, brawnBonus = idx / 2 };
                    icon2 = (em.shieldIcons != null && em.shieldIcons.Length > idx) ? em.shieldIcons[idx] : null;
                }
            }

            EquipmentLootPopup.Show(
                item1, 
                item2, 
                icon1, 
                icon2, 
                () => { em.Equip(item1); ResumeAfterChest(); }, 
                () => { em.Equip(item2); ResumeAfterChest(); }
            );
        }

        private void ResumeAfterChest()
        {
            var nav = playerStats.GetComponent<HeroNavigation>();
            if (nav != null) nav.ResumeAfterCombat();
        }

        private void ApplyChestUpgrade(string statName)
        {
            if (playerStats == null) return;
            playerStats.ApplyStatUpgrade(statName, 2, true);

            ResumeAfterChest();
        }
        }