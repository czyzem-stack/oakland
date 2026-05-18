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

    public void StartCombat(CharacterStats enemy)
    {
        if (isInCombat || enemy == null || enemy.isDead) return;

        currentEnemyStats = enemy;
        isInCombat = true;
        isPlayerTurn = true;
        Debug.Log("[CombatSystem] Combat Started against " + enemy.name);

        // Disable NavMeshAgent to allow manual positioning
        var agent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Position player to face enemy at a proper distance (2.5 units)
        Vector3 directionToPlayer = (playerStats.transform.position - enemy.transform.position).normalized;
        if (directionToPlayer.sqrMagnitude < 0.01f) directionToPlayer = Vector3.back; 
        
        playerStats.transform.position = enemy.transform.position + directionToPlayer * 2.5f;
        playerStats.transform.LookAt(new Vector3(enemy.transform.position.x, playerStats.transform.position.y, enemy.transform.position.z));
        enemy.transform.LookAt(new Vector3(playerStats.transform.position.x, enemy.transform.position.y, playerStats.transform.position.z));

        // Dynamic Camera
        var camFollow = Camera.main.GetComponent<CameraFollow>();
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
                yield return new WaitForSeconds(1f);
                if (!isInCombat) break; // Check again after wait

                Debug.Log("[CombatSystem] Enemy Turn...");
                EnemyAttack();
                yield return new WaitForSeconds(2f);
                
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
        
        // Check for Critical Hit (Natural 11 or 12 if using 2D6)
        bool isCritical = rollValue >= 11; 
        
        // Player attack animation
        Animator playerAnim = playerStats.GetComponent<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.5f);

        // Check if enemy still exists
        if (currentEnemyStats == null) 
        {
            isAttackSequenceRunning = false;
            yield break;
        }

        // Deal damage
        int baseDamage = rollValue + playerStats.MeleeDamage;
        int finalDamage = isCritical ? baseDamage * 2 : baseDamage;
        
        currentEnemyStats.TakeDamage(finalDamage);
        
        string damagePrefix = isCritical ? "CRITICAL! -" : "-";
        Color textColor = isCritical ? Color.yellow : Color.red;
        SpawnDamageText(currentEnemyStats.transform.position + Vector3.up * 2f, $"{damagePrefix}{finalDamage}", textColor);
        
        Debug.Log($"[CombatSystem] Player deals {finalDamage} damage to Enemy (Crit: {isCritical}).");

        // Juice: VFX
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, currentEnemyStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // Juice: Camera Shake
        Camera.main.GetComponent<CameraFollow>()?.Shake(0.2f, isCritical ? 0.3f : 0.15f);

        Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
        if (enemyAnim != null) enemyAnim.SetTrigger("GetHit");

        yield return new WaitForSeconds(1.5f);

        if (currentEnemyStats != null && currentEnemyStats.currentHP <= 0)
        {
            if (enemyAnim != null) enemyAnim.SetTrigger("Die");
            EndCombat(true);
        }
        else
        {
            isPlayerTurn = false;
        }
        isAttackSequenceRunning = false;
    }

    private void EnemyAttack()
    {
        if (currentEnemyStats == null) return;

        Animator enemyAnim = currentEnemyStats.GetComponent<Animator>();
        if (enemyAnim != null) enemyAnim.SetTrigger("Attack");

        // Enemy damage (simulated roll for now)
        int enemyRoll = Random.Range(1, 13);
        bool isCritical = enemyRoll >= 11;
        int damage = enemyRoll + currentEnemyStats.MeleeDamage;
        if (isCritical) damage *= 2;

        playerStats.TakeDamage(damage);
        
        string damagePrefix = isCritical ? "CRITICAL! -" : "-";
        SpawnDamageText(playerStats.transform.position + Vector3.up * 2f, $"{damagePrefix}{damage}", Color.red);
        Debug.Log($"[CombatSystem] Enemy deals {damage} damage to Player (Crit: {isCritical}).");

        // Juice: VFX
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, playerStats.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // Juice: Camera Shake
        Camera.main.GetComponent<CameraFollow>()?.Shake(0.2f, isCritical ? 0.25f : 0.1f);

        Animator playerAnim = playerStats.GetComponent<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("GetHit");
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
        var camFollow = Camera.main.GetComponent<CameraFollow>();
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
            // Allow movement to continue
            GameObject.Destroy(currentEnemyStats.gameObject, 1.5f);
            currentEnemyStats = null;
            var nav = playerStats.GetComponent<HeroNavigation>();
            if (nav != null) nav.ResumeAfterCombat();
        }
    }
}
