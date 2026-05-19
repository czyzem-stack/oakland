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

    public void StartCombat(CharacterStats enemy)
    {
        if (isInCombat || enemy == null || enemy.isDead) 
        {
            Debug.LogWarning($"[CombatSystem] Cannot start combat. InCombat={isInCombat}, EnemyValid={enemy != null}");
            return;
        }

        currentEnemyStats = enemy;
        isInCombat = true;
        isPlayerTurn = true;
        isAttackSequenceRunning = false; 
        Debug.Log("[CombatSystem] Combat Started against " + enemy.name);

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

        Vector3 directionToPlayer = (playerStats.transform.position - enemy.transform.position).normalized;
        if (directionToPlayer.sqrMagnitude < 0.01f) directionToPlayer = Vector3.back;

        Vector3 center = (playerStats.transform.position + enemy.transform.position) * 0.5f;
        Vector3 playerTarget = center + directionToPlayer * 1.25f;
        Vector3 enemyTarget = center - directionToPlayer * 1.25f;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(playerTarget, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas)) playerTarget = hit.position;
        if (UnityEngine.AI.NavMesh.SamplePosition(enemyTarget, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas)) enemyTarget = hit.position;

        Animator pAnim = playerStats.GetComponent<Animator>();
        Animator eAnim = enemy.GetComponent<Animator>();
        
        bool isStatic = enemy.name.Contains("Chest");
        bool isDragon = enemy.name.Contains("DragonBob");
        bool isWorm = enemy.name.Contains("Worm");

        if (pAnim != null) pAnim.SafeSetFloat("Speed", 1.5f);
        if (eAnim != null && !isStatic && !isDragon && !isWorm) eAnim.SafeSetFloat("Speed", 1.5f);

        float duration = 0.8f; 
        float elapsed = 0;
        Vector3 pStart = playerStats.transform.position;
        Vector3 eStart = enemy.transform.position;

        // If we are already close (e.g. hitting a chest/POI directly), don't force a 'jump'
        if (Vector3.Distance(pStart, playerTarget) < 0.5f) duration = 0.2f;

        while (elapsed < duration)
{
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth step curve
            float curve = t * t * (3 - 2 * t); 

            playerStats.transform.position = Vector3.Lerp(pStart, playerTarget, curve);
            
            Vector3 lookTarget = new Vector3(enemyTarget.x, playerStats.transform.position.y, enemyTarget.z);
            if ((lookTarget - playerStats.transform.position).sqrMagnitude > 0.001f)
                playerStats.transform.rotation = Quaternion.Slerp(playerStats.transform.rotation, Quaternion.LookRotation(lookTarget - playerStats.transform.position), t);

            if (!isStatic && !isDragon && !isWorm)
            {
                enemy.transform.position = Vector3.Lerp(eStart, enemyTarget, curve);
            }
            
            Vector3 eLookTarget = new Vector3(playerTarget.x, enemy.transform.position.y, playerTarget.z);
            if ((eLookTarget - enemy.transform.position).sqrMagnitude > 0.001f)
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, Quaternion.LookRotation(eLookTarget - enemy.transform.position), t);

            yield return null;
        }

        playerStats.transform.position = playerTarget;
        if (!isDragon && !isWorm) enemy.transform.position = enemyTarget;
        
        if (pAnim != null) pAnim.SafeSetFloat("Speed", 0f);
        if (eAnim != null && !isDragon && !isWorm) eAnim.SafeSetFloat("Speed", 0f);

        if (eAnim != null) 
        {
            if (isDragon) eAnim.CrossFade("Scream", 0.2f);
            else if (isWorm) eAnim.CrossFade("GroundBreakThrough", 0.1f);
            else eAnim.SafeSetTrigger("GetHit");
        }
        if (pAnim != null) pAnim.SafeSetTrigger("GetHit");

        yield return new WaitForSeconds(isWorm ? 1.0f : 0.2f);

        if (camFollow != null)
        {
            if (combatCameraAnchor == null) combatCameraAnchor = new GameObject("CombatCameraAnchor").transform;
            combatCameraAnchor.position = (playerStats.transform.position + enemy.transform.position) * 0.5f;
            camFollow.target = combatCameraAnchor;
            camFollow.isCombatOrbiting = true;
        }

        StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        while (isInCombat)
        {
            if (isPlayerTurn)
            {
                DragonBob bob = currentEnemyStats?.GetComponent<DragonBob>();
                if (bob != null && bob.isFTUECombat)
                {
                    yield return new WaitForSeconds(1.0f);
                    Animator eAnim = currentEnemyStats.GetComponent<Animator>();
                    if (eAnim != null) eAnim.CrossFade("Scream", 0.2f);
                    yield return new WaitForSeconds(2.0f);
                    bob.FTUEFlyAway();
                    EndCombat(false); 
                    yield break;
                }
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
        bool isCritical = rollValue >= playerStats.critThreshold; 
        Animator playerAnim = playerStats.GetComponent<Animator>();
        
        Debug.Log($"[CombatSystem] Instant impact for roll: {rollValue}");
        
        // Very short delay to line up with the weapon-dice-burst
        yield return new WaitForSeconds(0.1f);

        if (currentEnemyStats == null) 
        {
            isAttackSequenceRunning = false;
            yield break;
        }

        int baseDamage = rollValue + playerStats.MeleeDamage;
        int finalDamage = isCritical ? baseDamage * 2 : baseDamage;
        currentEnemyStats.TakeDamage(finalDamage);
        
        string damagePrefix = isCritical ? "CRITICAL! -" : "-";
        SpawnDamageText(currentEnemyStats.transform.position + Vector3.up * 2f, $"{damagePrefix}{finalDamage}", isCritical ? Color.yellow : Color.red);
        
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, currentEnemyStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // Punchy feedback
        if (camFollow != null) camFollow.Shake(0.12f, isCritical ? 0.4f : 0.2f);

        Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
        if (enemyAnim != null)
        {
            bool isWorm = currentEnemyStats.name.Contains("Worm");
            if (isWorm) enemyAnim.CrossFade("GetHit", 0.05f);
            else enemyAnim.SafeSetTrigger("GetHit");
        }

        // Ultra fast turnaround
        yield return new WaitForSeconds(0.4f); 

        if (currentEnemyStats != null && currentEnemyStats.currentHP <= 0)
        {
            // XP on kill: roll value * amount per kill
            if (playerStats != null)
            {
                float xpGain = rollValue * playerStats.amountPerKill;
                playerStats.AddXP(xpGain);
            }

            if (enemyAnim != null)
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
        isAttackSequenceRunning = false;
    }

    private IEnumerator EnemyAttackSequence()
    {
        if (currentEnemyStats == null) yield break;

        Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
        bool isDragon = currentEnemyStats.name.Contains("DragonBob");
        bool isWorm = currentEnemyStats.name.Contains("Worm");

        if (enemyAnim != null)
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
            else enemyAnim.SafeSetTrigger("Attack");
        }

        // Faster enemy impact
        yield return new WaitForSeconds(0.25f);

        int enemyRoll = Random.Range(1, 13);
        bool isCritical = enemyRoll >= currentEnemyStats.critThreshold;
        int damage = enemyRoll + currentEnemyStats.MeleeDamage;
        if (isCritical) damage *= 2;

        playerStats.TakeDamage(damage);
        SpawnDamageText(playerStats.transform.position + Vector3.up * 2f, $"{(isCritical ? "CRITICAL! -" : "-")}{damage}", Color.red);
        
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, playerStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (camFollow != null) camFollow.Shake(0.12f, isCritical ? 0.3f : 0.15f);

        Animator playerAnim = playerStats.GetComponent<Animator>();
        if (playerAnim != null) playerAnim.SafeSetTrigger("GetHit");

        // Faster turnaround for next turn
        yield return new WaitForSeconds(0.5f);
        if (isDragon && enemyAnim != null) enemyAnim.CrossFade("Fly Float", 0.5f);
    }

    public static void SpawnText(Vector3 position, string text, Color color)
    {
        GameObject canvasGo = new GameObject("FloatingTextCanvas");
        canvasGo.transform.position = position + Vector3.up * 0.5f;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<FaceCamera>();
        canvasGo.AddComponent<CanvasGroup>(); 
        
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 100); 
        rect.localScale = Vector3.one * 0.0075f; 

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI t = textGo.GetComponent<TextMeshProUGUI>();
        
        t.font = Resources.Load<TMP_FontAsset>("Alata-Regular SDF");
        if (t.font == null) t.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF"); // Fallback
t.fontSize = 60; 
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        
        t.outlineWidth = 0.2f;
        t.outlineColor = Color.black;

        FloatingCombatText fct = canvasGo.AddComponent<FloatingCombatText>();
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

        if (playerWon && currentEnemyStats != null)
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

    public void ShowChestUpgradePopup()
    {
        EquipmentManager em = EquipmentManager.Instance;
        if (em == null)
        {
            Debug.LogError("EquipmentManager Instance is null");
            return;
        }

        // Option 1: Armor (Helmet, Chest, Cloak, Gloves, or Boots)
        EquipmentItem armor;
        Sprite armorIcon;
        float armorRoll = UnityEngine.Random.value;
        
        if (armorRoll < 0.2f)
        {
            // Randomly pick one of the 5 armors
            int armorIndex = UnityEngine.Random.Range(0, 5);
            string[] armorNames = { "Padded Cloth", "Leather Armor", "Brigandine", "Chainmail", "Plate Armor" };
            int[] gritBonuses = { 1, 2, 2, 3, 3 };
            int[] brawnBonuses = { 0, 0, 1, 0, 2 };

            armor = new EquipmentItem {
                name = armorNames[armorIndex],
                slot = EquipmentSlot.Chest,
                gritBonus = gritBonuses[armorIndex],
                brawnBonus = brawnBonuses[armorIndex]
            };
            armorIcon = em.chestIcons.Length > armorIndex ? em.chestIcons[armorIndex] : null;
        }
        else if (armorRoll < 0.4f)
        {
            // Randomly pick one of the 5 helmets
            int helmetIndex = UnityEngine.Random.Range(0, 5);
            string[] helmetNames = { "Iron Helmet", "Chainmail Hood", "Viking Helmet", "Crusader Helmet", "Great Helmet" };
            int[] witBonuses = { 1, 1, 2, 2, 2 };
            int[] gritBonuses = { 0, 1, 0, 1, 1 };

            armor = new EquipmentItem {
                name = helmetNames[helmetIndex],
                slot = EquipmentSlot.Helmet,
                witBonus = witBonuses[helmetIndex],
                gritBonus = gritBonuses[helmetIndex]
            };
            armorIcon = em.helmetIcons.Length > helmetIndex ? em.helmetIcons[helmetIndex] : null;
        }
        else if (armorRoll < 0.6f)
        {
            // Randomly pick one of the 3 cloaks
            int cloakIndex = UnityEngine.Random.Range(0, 3);
            string[] cloakNames = { "Traveler's Cloak", "Ranger Cape", "Royal Mantle" };
            int[] witBonuses = { 1, 2, 2 };
            int[] gritBonuses = { 1, 1, 3 };

            armor = new EquipmentItem {
                name = cloakNames[cloakIndex],
                slot = EquipmentSlot.Cloak,
                witBonus = witBonuses[cloakIndex],
                gritBonus = gritBonuses[cloakIndex]
            };
            armorIcon = em.cloakIcons.Length > cloakIndex ? em.cloakIcons[cloakIndex] : null;
        }
        else if (armorRoll < 0.8f)
        {
            // Randomly pick one of the 3 gloves
            int gloveIndex = UnityEngine.Random.Range(0, 3);
            string[] gloveNames = { "Leather Gloves", "Plate Gauntlets", "Silk Mitts" };
            int[] brawnBonuses = { 1, 2, 0 };
            int[] witBonuses = { 0, 0, 2 };

            armor = new EquipmentItem {
                name = gloveNames[gloveIndex],
                slot = EquipmentSlot.Gloves,
                brawnBonus = brawnBonuses[gloveIndex],
                witBonus = witBonuses[gloveIndex]
            };
            armorIcon = em.gloveIcons.Length > gloveIndex ? em.gloveIcons[gloveIndex] : null;
        }
        else
        {
            // Randomly pick one of the 3 boots
            int bootIndex = UnityEngine.Random.Range(0, 3);
            string[] bootNames = { "Leather Boots", "Iron Greaves", "Swift Shoes" };
            int[] finesseBonuses = { 1, 0, 2 };
            int[] gritBonuses = { 0, 2, 0 };

            armor = new EquipmentItem {
                name = bootNames[bootIndex],
                slot = EquipmentSlot.Boots,
                finesseBonus = finesseBonuses[bootIndex],
                gritBonus = gritBonuses[bootIndex]
            };
            armorIcon = em.bootIcons.Length > bootIndex ? em.bootIcons[bootIndex] : null;
        }

        // Option 2: Weapon
        EquipmentItem weapon;
        Sprite weaponIcon;
        
        int weaponRoll = UnityEngine.Random.Range(0, 10); // More variety now
        
        if (weaponRoll == 0)
        {
            weapon = new EquipmentItem { name = "Hunting Bow", slot = EquipmentSlot.Weapon, attackBonus = 2 };
            weaponIcon = em.weaponIcons.Length > 0 ? em.weaponIcons[0] : null; // Index 0: Bow
        }
        else if (weaponRoll == 1)
        {
            weapon = new EquipmentItem { name = "Iron Spear", slot = EquipmentSlot.Weapon, attackBonus = 3 };
            weaponIcon = em.weaponIcons.Length > 1 ? em.weaponIcons[1] : null; // Index 1: Spear
        }
        else if (weaponRoll == 2)
        {
            weapon = new EquipmentItem { name = "Iron Sword 03", slot = EquipmentSlot.Weapon, attackBonus = 3 };
            weaponIcon = em.weaponIcons.Length > 2 ? em.weaponIcons[2] : null; // Index 2: Sword
        }
        else if (weaponRoll == 3)
        {
            weapon = new EquipmentItem { name = "War Axe 10", slot = EquipmentSlot.Weapon, attackBonus = 4 };
            weaponIcon = em.weaponIcons.Length > 3 ? em.weaponIcons[3] : null; // Index 3: Axe
        }
        else if (weaponRoll == 4)
        {
            weapon = new EquipmentItem { name = "War Hammer 11", slot = EquipmentSlot.Weapon, attackBonus = 4 };
            weaponIcon = em.weaponIcons.Length > 4 ? em.weaponIcons[4] : null; // Index 4: Hammer
        }
        else if (weaponRoll == 5)
        {
            weapon = new EquipmentItem { name = "Iron Greatsword 01", slot = EquipmentSlot.Weapon, attackBonus = 5 };
            weaponIcon = em.weaponIcons.Length > 5 ? em.weaponIcons[5] : null; // Index 5: THS
        }
        else if (weaponRoll == 6)
        {
            weapon = new EquipmentItem { name = "Magic Wand 01", slot = EquipmentSlot.Weapon, attackBonus = 3, witBonus = 2 };
            weaponIcon = em.weaponIcons.Length > 6 ? em.weaponIcons[6] : null; // Index 6: Wand
        }
        else
        {
            weapon = new EquipmentItem { name = "Stronger Stick", slot = EquipmentSlot.Weapon, attackBonus = 2 };
            weaponIcon = em.weaponIcons.Length > 7 ? em.weaponIcons[7] : null; // Index 7: Stick
        }

        EquipmentLootPopup.Show(
            armor, 
            weapon, 
            armorIcon, 
            weaponIcon, 
            () => { em.Equip(armor); ResumeAfterChest(); }, 
            () => { em.Equip(weapon); ResumeAfterChest(); }
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