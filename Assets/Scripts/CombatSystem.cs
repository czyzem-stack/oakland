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

    private void Awake()
    {
        Instance = this;
    }

    public void StartCombat(CharacterStats enemy)
    {
        if (isInCombat) return;

        currentEnemyStats = enemy;
        isInCombat = true;
        isPlayerTurn = true;
        Debug.Log("[CombatSystem] Combat Started against " + enemy.name);
        
        // Disable NavMeshAgent to allow manual positioning
        var agent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Position player to face enemy at a proper distance (2.5 units)
        Vector3 directionToPlayer = (playerStats.transform.position - enemy.transform.position).normalized;
        if (directionToPlayer.magnitude < 0.1f) directionToPlayer = Vector3.back; 
        
        Vector3 combatPos = enemy.transform.position + directionToPlayer * 2.5f;
        playerStats.transform.position = combatPos;
        
        // Precise rotation: Look at each other (ignore Y difference)
        Vector3 enemyLookPos = new Vector3(enemy.transform.position.x, playerStats.transform.position.y, enemy.transform.position.z);
        playerStats.transform.LookAt(enemyLookPos);
        
        Vector3 playerLookPos = new Vector3(playerStats.transform.position.x, enemy.transform.position.y, playerStats.transform.position.z);
        enemy.transform.LookAt(playerLookPos);

        StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        while (isInCombat)
        {
            if (isPlayerTurn)
            {
                Debug.Log("[CombatSystem] Waiting for Player Roll...");
                // Wait for DiceRollSystem to trigger OnDiceRolled
                yield return new WaitUntil(() => !isPlayerTurn);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                Debug.Log("[CombatSystem] Enemy Turn...");
                EnemyAttack();
                yield return new WaitForSeconds(2f);
                
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

    private bool isAttackSequenceRunning = false;

    public void OnPlayerRoll(int rollValue)
    {
        if (!isInCombat || !isPlayerTurn || isAttackSequenceRunning) return;

        StartCoroutine(PlayerAttackSequence(rollValue));
    }

    private IEnumerator PlayerAttackSequence(int rollValue)
    {
        isAttackSequenceRunning = true;
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
        int damage = rollValue + playerStats.MeleeDamage;
        currentEnemyStats.TakeDamage(damage);
        SpawnDamageText(currentEnemyStats.transform.position + Vector3.up * 2f, $"-{damage}");
        Debug.Log($"[CombatSystem] Player deals {damage} damage to Enemy.");

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
        int enemyRoll = Random.Range(1, 7);
        int damage = enemyRoll + currentEnemyStats.MeleeDamage;
        playerStats.TakeDamage(damage);
        SpawnDamageText(playerStats.transform.position + Vector3.up * 2f, $"-{damage}");
        Debug.Log($"[CombatSystem] Enemy deals {damage} damage to Player.");

        Animator playerAnim = playerStats.GetComponent<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("GetHit");
    }

    private void SpawnDamageText(Vector3 position, string text)
    {
        GameObject canvasGo = new GameObject("DamageTextCanvas");
        canvasGo.transform.position = position + Vector3.up * 0.5f; // Initial offset
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<FaceCamera>();
        canvasGo.AddComponent<CanvasGroup>(); // Ensure component exists for script
        
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 100); // Larger rect
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
        fct.Setup(text, Color.red);
    }

    private void EndCombat(bool playerWon)
    {
        if (!isInCombat) return;

        isInCombat = false;
        Debug.Log("[CombatSystem] Combat Ended. Player Won: " + playerWon);
        
        // Re-enable NavMeshAgent
        var agent = playerStats.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = true;

        if (playerWon && currentEnemyStats != null)
        {
            // Allow movement to continue
            GameObject.Destroy(currentEnemyStats.gameObject, 2f);
            currentEnemyStats = null;
            var nav = playerStats.GetComponent<HeroNavigation>();
            if (nav != null) nav.ResumeAfterCombat();
        }
    }
}
