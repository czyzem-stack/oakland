using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DiceRollSystem : MonoBehaviour
{
    public enum DiceType
    {
        D2, // Simulated using D6
        D4,
        D6,
        D8,
        D10,
        D12,
        D20
    }

    [Header("Dice Configuration")]
    public DiceType diceType = DiceType.D2;
    public Material diceMaterial;
    public int amount = 2;
    public float scale = 0.5f;
    public float diceLifetime = 3.0f; // Seconds dice stay before fading
    public float fadeDuration = 1.0f; // Seconds to fade away

    [Header("Physics Settings")]
    public float popForce = 6.0f;
    public float torqueForce = 10.0f;
    public Vector3 spawnOffset = new Vector3(0, 1.5f, 0.5f);

    [Header("References")]
    public List<GameObject> dicePrefabs = new List<GameObject>();
    public List<Material> diceMaterials = new List<Material>();
    public Text resultText;
    public Animator steveAnimator;
    public HeroNavigation heroNav;

    private List<GameObject> activeDice = new List<GameObject>();
    private List<Rigidbody> activeDiceRBs = new List<Rigidbody>();
    public bool isRolling = false;
    private GameObject worldDiceContainer;

    public static bool WarmedUp = false;

    private void Start()
    {
        CleanupHierarchy();
        RefreshReferences();
        if (!WarmedUp)
        {
            StartCoroutine(WarmupRoutine());
        }
    }

    private IEnumerator WarmupRoutine()
    {
        WarmedUp = true;
        Debug.Log("[DiceRollSystem] Starting Warmup...");
List<GameObject> tempObjects = new List<GameObject>();

        // Spawn all prefabs to force mesh/material/shader loading
        for (int i = 0; i < dicePrefabs.Count; i++)
        {
            var prefab = dicePrefabs[i];
            if (prefab == null) continue;
            // Spawn far below the world
            GameObject temp = Instantiate(prefab, new Vector3(0, -500, 0), Quaternion.identity);
            temp.SetActive(true); // Must be active to warm up
            
            if (diceMaterial != null)
            {
                var renderers = temp.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers) r.sharedMaterial = diceMaterial;
            }

            tempObjects.Add(temp);
            
            yield return null; 
        }

        // Wait a few frames for engine to process initial physics/render calls
        yield return new WaitForSeconds(0.5f);

        // Cleanup
        foreach (var obj in tempObjects) Destroy(obj);
        
        // Final settle time to allow GC/Cleanup to finish before screen fades
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[DiceRollSystem] Warmup and Settle Complete.");
        }

    private void CleanupHierarchy()
    {
        // Cleanup local children if any
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        activeDice.Clear();

        // Ensure we have a world container for dice to stay in place
        if (worldDiceContainer == null)
        {
            worldDiceContainer = GameObject.Find("WorldDiceContainer");
            if (worldDiceContainer == null) worldDiceContainer = new GameObject("WorldDiceContainer");
        }
    }

    private void RefreshReferences()
    {
        if (steveAnimator == null)
        {
            steveAnimator = GetComponentInParent<Animator>();
            if (steveAnimator == null) steveAnimator = GetComponentInChildren<Animator>();
        }

        if (heroNav == null)
        {
            heroNav = GetComponentInParent<HeroNavigation>();
            if (heroNav == null) heroNav = GetComponentInChildren<HeroNavigation>();
        }
    }

    public bool CanRoll
    {
        get
        {
            if (isRolling || IsSteveBusy()) return false;
            
            CharacterStats stats = GetComponentInParent<CharacterStats>();
            if (stats == null) stats = GetComponentInChildren<CharacterStats>();
            return stats != null && stats.currentMana >= 1;
        }
    }

    public static System.Action<int> OnAnyDiceRolled;

    public void Roll()
    {
        RefreshReferences(); 

        if (!CanRoll) 
        {
            Debug.Log("[DiceRollSystem] Roll ignored - cannot roll right now.");
            return;
        }
        
        if (steveAnimator != null)
        {
            steveAnimator.SetTrigger("Roll");
        }

        // Consume 1 Energy per roll
        CharacterStats stats = GetComponentInParent<CharacterStats>();
        if (stats == null) stats = GetComponentInChildren<CharacterStats>();
        if (stats != null) stats.ConsumeMana(1);

        StartCoroutine(RollRoutine());
    }

    private bool IsSteveBusy()
    {
        if (heroNav != null && heroNav.isMoving) return true;
        
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            // If we are in combat, we are only "busy" if it's the enemy's turn 
            // or if a player attack animation is already playing.
            return !CombatSystem.Instance.isPlayerTurn || CombatSystem.Instance.IsAttackSequenceRunning;
        }

        return false;
    }

    private IEnumerator RollRoutine()
    {
        Debug.Log("[DiceRollSystem] RollRoutine started.");
        isRolling = true;

        try
        {
            // Give the animation a moment to reach the 'release' point
            yield return new WaitForSeconds(0.45f);

            if (resultText != null) 
            {
                resultText.text = ""; 
                resultText.gameObject.SetActive(false);
            }

            // 1. Mark old dice for immediate destruction or let them fade out naturally
            activeDice.Clear();
            activeDiceRBs.Clear();

            GameObject prefab = GetPrefabForType(diceType);
            if (prefab == null)
            {
                Debug.LogError("[DiceRollSystem] No prefab found for " + diceType);
                yield break;
            }

            // 2. Spawn in World Space
            if (worldDiceContainer == null)
            {
                worldDiceContainer = GameObject.Find("WorldDiceContainer");
                if (worldDiceContainer == null) worldDiceContainer = new GameObject("WorldDiceContainer");
            }

            for (int i = 0; i < amount; i++)
            {
                Vector3 spawnPos = transform.position + spawnOffset + Random.insideUnitSphere * 0.1f;
                GameObject die = Instantiate(prefab, spawnPos, Random.rotation, worldDiceContainer.transform); 
                die.transform.localScale = Vector3.one * scale;
                activeDice.Add(die);
                
                if (diceMaterial != null)
                {
                    var renderers = die.GetComponentsInChildren<MeshRenderer>();
                    foreach (var r in renderers) r.sharedMaterial = diceMaterial;
                }

                Rigidbody rb = die.GetComponent<Rigidbody>();
                if (rb == null) rb = die.AddComponent<Rigidbody>();
                activeDiceRBs.Add(rb);

                rb.mass = 1.0f;
                rb.linearDamping = 1.0f;
                rb.angularDamping = 1.0f;

                rb.AddForce((Vector3.up + Random.insideUnitSphere * 0.1f) * popForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
            }

            // 3. Wait for dice to settle
            yield return new WaitForSeconds(0.5f);
            
            float timer = 0;
            while (timer < 1.5f)
            {
                bool stillMoving = false;
                foreach (var rb in activeDiceRBs)
                {
                    if (rb != null && rb.linearVelocity.magnitude > 0.2f)
                    {
                        stillMoving = true;
                        break;
                    }
                }
                if (!stillMoving) break;
                timer += Time.deltaTime;
                yield return null;
            }

            // 4. Result calculation
            int total = 0;
            List<int> rollValues = new List<int>();
            foreach (var die in activeDice)
            {
                if (die != null)
                {
                    DiceStats stats = die.GetComponent<DiceStats>() ?? die.GetComponentInChildren<DiceStats>();
                    if (stats != null)
                    {
                        int val = stats.side;
                        if (diceType == DiceType.D2) val = (val > 3) ? 2 : 1;
                        rollValues.Add(val);
                        total += val;
                    }
                }
            }

            bool isDoubles = rollValues.Count == 2 && rollValues[0] == rollValues[1];
            if (isDoubles) total *= 2;

            CharacterStats pStats = GetComponentInParent<CharacterStats>();
            if (pStats == null) pStats = GetComponentInChildren<CharacterStats>();

            if (resultText != null) 
            {
                resultText.gameObject.SetActive(true);
                resultText.text = isDoubles ? $"DOUBLE! {total}" : total.ToString();
                StartCoroutine(AnimateFloatingText(resultText));
            }

            if (pStats != null) 
            {
                pStats.AddXP(total); // Gaining XP on every roll (Floating text handled by CharacterStats)
            }

            OnAnyDiceRolled?.Invoke(total);
            if (heroNav != null) heroNav.OnDiceRolled(total);

            StartCoroutine(FadeAndDestroyDice(new List<GameObject>(activeDice)));
        }
        finally
        {
            isRolling = false;
            Debug.Log("[DiceRollSystem] RollRoutine finished.");
        }
    }

    private IEnumerator FadeAndDestroyDice(List<GameObject> diceBatch)
    {
        yield return new WaitForSeconds(diceLifetime);

        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));

            foreach (var die in diceBatch)
            {
                if (die == null) continue;
                
                // Shrink as a "fade" alternative since materials might not be transparent
                die.transform.localScale = Vector3.one * scale * alpha;
                
                // If materials support transparency, we could fade them here, but shrinking is safer/snappier
            }
            yield return null;
        }

        foreach (var die in diceBatch)
        {
            if (die != null) Destroy(die);
        }
    }

    private IEnumerator AnimateFloatingText(Text text)
    {
        Transform canvasTransform = text.transform.parent;
        CanvasGroup group = canvasTransform.GetComponent<CanvasGroup>();
        if (group == null) group = canvasTransform.gameObject.AddComponent<CanvasGroup>();

        Vector3 startPos = transform.position + new Vector3(0, 2.5f, 0);
        Vector3 endPos = startPos + Vector3.up * 1.5f;
        
        float duration = 2.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            float moveCurve = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f); 
            canvasTransform.position = Vector3.Lerp(startPos, endPos, moveCurve);

            float scaleCurve = 1f;
            if (normalizedTime < 0.2f) {
                scaleCurve = Mathf.Lerp(0f, 1.2f, normalizedTime / 0.2f);
            } else if (normalizedTime < 0.4f) {
                scaleCurve = Mathf.Lerp(1.2f, 1f, (normalizedTime - 0.2f) / 0.2f);
            }
            canvasTransform.localScale = Vector3.one * 0.015f * scaleCurve;

            if (normalizedTime > 0.5f)
            {
                group.alpha = Mathf.Lerp(1f, 0f, (normalizedTime - 0.5f) / 0.5f);
            }
            else
            {
                group.alpha = 1f;
            }

            yield return null;
        }

        text.gameObject.SetActive(false);
    }

    private GameObject GetPrefabForType(DiceType type)
    {
        string searchString = "";
        switch (type)
        {
            case DiceType.D2: searchString = "6Sided"; break;
            case DiceType.D4: searchString = "4Sided"; break;
            case DiceType.D6: searchString = "6Sided"; break;
            case DiceType.D8: searchString = "8Sided"; break;
            case DiceType.D10: searchString = "10Sided"; break;
            case DiceType.D12: searchString = "12Sided"; break;
            case DiceType.D20: searchString = "20Sided"; break;
        }
        
        foreach (var p in dicePrefabs)
        {
            if (p != null && p.name.Contains(searchString)) return p;
        }
        return null;
    }
}

