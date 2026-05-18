using UnityEngine;
using UnityEngine.UI;
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

    private void Awake()
    {
        Instance = this;
        // Try to auto-load the blood splatter if not set
        if (hitEffectPrefab == null)
        {
            hitEffectPrefab = UnityEngine.Resources.Load<GameObject>("FX_Blood_Splatter_01");
            // If Resources.Load fails (it will if not in Resources), we'll search in Start via AssetDatabase (Editor only) or just keep it null.
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
        if (isInCombat || enemy == null || enemy.isDead) return;

        currentEnemyStats = enemy;
        isInCombat = true;
        isPlayerTurn = true;
        Debug.Log("[CombatSystem] Combat Started against " + enemy.name);

        StartCoroutine(CombatTransitionRoutine(enemy));
    }

    private IEnumerator CombatTransitionRoutine(CharacterStats enemy)
    {
        // 1. Setup - Disable navigation
        var pAgent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var eAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (pAgent != null) pAgent.enabled = false;
        if (eAgent != null && eAgent.isOnNavMesh) eAgent.isStopped = true;

        // 2. Calculate Target Positions (Facing each other at 2.5m distance)
        Vector3 directionToPlayer = (playerStats.transform.position - enemy.transform.position).normalized;
        if (directionToPlayer.sqrMagnitude < 0.01f) directionToPlayer = Vector3.back;

        // Find a center point to anchor the fight
        Vector3 center = (playerStats.transform.position + enemy.transform.position) * 0.5f;
        Vector3 playerTarget = center + directionToPlayer * 1.25f;
        Vector3 enemyTarget = center - directionToPlayer * 1.25f;

        // Sample NavMesh to ensure we don't end up in a wall
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(playerTarget, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) playerTarget = hit.position;
        if (UnityEngine.AI.NavMesh.SamplePosition(enemyTarget, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) enemyTarget = hit.position;

        // 3. Charge!
        Animator pAnim = playerStats.GetComponent<Animator>();
        Animator eAnim = enemy.GetComponent<Animator>();
        
        bool isStatic = enemy.name.Contains("Chest");

        // Set to run speed
        if (pAnim != null) pAnim.SetFloat("Speed", 1.5f);
        if (eAnim != null && !isStatic) eAnim.SetFloat("Speed", 1.5f);

        float duration = 0.5f; // Faster charge
        float elapsed = 0;
        Vector3 pStart = playerStats.transform.position;
        Vector3 eStart = enemy.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Punchy ease-in ease-out
            float curve = t * t * (3 - 2 * t); 

            playerStats.transform.position = Vector3.Lerp(pStart, playerTarget, curve);
            playerStats.transform.LookAt(new Vector3(enemyTarget.x, playerStats.transform.position.y, enemyTarget.z));

            if (!isStatic)
            {
                enemy.transform.position = Vector3.Lerp(eStart, enemyTarget, curve);
            }
            enemy.transform.LookAt(new Vector3(playerTarget.x, enemy.transform.position.y, playerTarget.z));

            yield return null;
        }

        // 4. Finalize position and rotation
        playerStats.transform.position = playerTarget;
        enemy.transform.position = enemyTarget;
        
        if (pAnim != null) pAnim.SetFloat("Speed", 0f);
        if (eAnim != null) eAnim.SetFloat("Speed", 0f);

        // Visual "Ready" flinch
        if (eAnim != null) eAnim.SetTrigger("GetHit");
        if (pAnim != null) pAnim.SetTrigger("GetHit");

        yield return new WaitForSeconds(0.2f);

        // 5. Dynamic Camera Setup
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
                Debug.Log("[CombatSystem] Waiting for Player Roll...");
                // Wait for DiceRollSystem to trigger OnDiceRolled, but also exit if combat ends
                yield return new WaitUntil(() => !isPlayerTurn || !isInCombat);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                if (!isInCombat) break;

                Debug.Log("[CombatSystem] Enemy Turn...");
                yield return StartCoroutine(EnemyAttackSequence());
                
                if (!isInCombat) break;

                if (playerStats.currentHP <= 0)
                {
                    EndCombat(false);
                }
                else
                {
                    isPlayerTurn = true;
                }
            }
yield return null;
        }
    }

    public bool IsAttackSequenceRunning => isAttackSequenceRunning;
    private bool isAttackSequenceRunning = false;

    public void OnPlayerRoll(int rollValue)
    {
        if (!isInCombat || !isPlayerTurn || isAttackSequenceRunning) return;

        StartCoroutine(PlayerAttackSequence(rollValue));
    }

    private IEnumerator PlayerAttackSequence(int rollValue)
    {
        isAttackSequenceRunning = true;
        
        bool isCritical = rollValue >= playerStats.critThreshold; 
        
        Animator playerAnim = playerStats.GetComponent<Animator>();
if (playerAnim != null) playerAnim.SetTrigger("Attack");

        // Faster impact timing (snappier)
        yield return new WaitForSeconds(0.35f);

        if (currentEnemyStats == null) 
        {
            isAttackSequenceRunning = false;
            yield break;
        }

        int baseDamage = rollValue + playerStats.MeleeDamage;
        int finalDamage = isCritical ? baseDamage * 2 : baseDamage;
        
        currentEnemyStats.TakeDamage(finalDamage);
        
        string damagePrefix = isCritical ? "CRITICAL! -" : "-";
        Color textColor = isCritical ? Color.yellow : Color.red;
        SpawnDamageText(currentEnemyStats.transform.position + Vector3.up * 2f, $"{damagePrefix}{finalDamage}", textColor);
        
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, currentEnemyStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (camFollow != null) camFollow.Shake(0.2f, isCritical ? 0.35f : 0.18f);

        Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
if (enemyAnim != null) enemyAnim.SetTrigger("GetHit");

        // Recovery time
        yield return new WaitForSeconds(1.0f);

        if (currentEnemyStats != null && currentEnemyStats.currentHP <= 0)
        {
            if (enemyAnim != null) enemyAnim.SetTrigger("Die");
            yield return new WaitForSeconds(0.5f);
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
        if (enemyAnim != null) enemyAnim.SetTrigger("Attack");

        // Wait for impact
        yield return new WaitForSeconds(0.35f);

        int enemyRoll = Random.Range(1, 13);
        bool isCritical = enemyRoll >= currentEnemyStats.critThreshold;
        int damage = enemyRoll + currentEnemyStats.MeleeDamage;
if (isCritical) damage *= 2;

        playerStats.TakeDamage(damage);
        
        string damagePrefix = isCritical ? "CRITICAL! -" : "-";
        SpawnDamageText(playerStats.transform.position + Vector3.up * 2f, $"{damagePrefix}{damage}", Color.red);
        
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, playerStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (camFollow != null) camFollow.Shake(0.2f, isCritical ? 0.25f : 0.12f);

        Animator playerAnim = playerStats.GetComponent<Animator>();
if (playerAnim != null) playerAnim.SetTrigger("GetHit");

        yield return new WaitForSeconds(1.0f);
    }

    public void SpawnDamageText(Vector3 position, string text, Color color)
    {
        GameObject canvasGo = new GameObject("DamageTextCanvas");
canvasGo.transform.position = position + Vector3.up * 0.5f; // Initial offset
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<FaceCamera>();
        canvasGo.AddComponent<CanvasGroup>(); // Ensure component exists for script
        
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 100); // Larger rect
        rect.localScale = Vector3.one * 0.015f; // Balanced scale

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(canvasGo.transform, false);
        Text t = textGo.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 60; // Bigger font
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        
        Outline outline = textGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        FloatingCombatText fct = canvasGo.AddComponent<FloatingCombatText>();
        fct.text = t;
        fct.driftSpeed = 2.0f; // Faster drift
        fct.Setup(text, color);
    }

    private void EndCombat(bool playerWon)
    {
        if (!isInCombat) return;

        isInCombat = false;
        isPlayerTurn = false;
        isAttackSequenceRunning = false;
        
        Debug.Log("[CombatSystem] Combat Ended. Player Won: " + playerWon);
        
        // Reset Steve's animator triggers/state
        Animator playerAnim = playerStats.GetComponent<Animator>();
        if (playerAnim != null)
        {
            playerAnim.ResetTrigger("Attack");
            playerAnim.ResetTrigger("GetHit");
            playerAnim.SetFloat("Speed", 0f);
        }

        // Reset Camera
        if (camFollow != null) 
        {
            camFollow.isCombatOrbiting = false;
camFollow.target = playerStats.transform;
            if (combatCameraAnchor != null) {
                Destroy(combatCameraAnchor.gameObject);
                combatCameraAnchor = null;
            }
        }

        // Re-enable NavMeshAgent
        var agent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = true;

        if (playerWon && currentEnemyStats != null)
        {
            bool isChest = currentEnemyStats.name.Contains("TreasureChest") || currentEnemyStats.name.Contains("Chest");
            
            // Allow movement to continue
            GameObject.Destroy(currentEnemyStats.gameObject, 1.5f);
            currentEnemyStats = null;

            if (isChest && TreasureUpgradeUI.Instance != null)
            {
                TreasureUpgradeUI.Instance.ShowUpgrade(playerStats);
            }
            else
            {
                var nav = playerStats.GetComponent<HeroNavigation>();
                if (nav != null) nav.ResumeAfterCombat();
            }
        }
        }
        }
