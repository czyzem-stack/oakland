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
    private CharacterStats cachedStats;
    private Transform weaponPoint;

    private List<GameObject> activeDice = new List<GameObject>();
private List<Rigidbody> activeDiceRBs = new List<Rigidbody>();
    public bool isRolling = false;
    public bool autoRoll = false;
    private float nextAutoRollTime;
    private const float CombatAutoRollCooldown = 0.75f;
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

    private void Update()
    {
        if (autoRoll && CanRoll && Time.time >= nextAutoRollTime)
        {
            Roll();
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

        if (cachedStats == null)
        {
            cachedStats = GetComponentInParent<CharacterStats>();
            if (cachedStats == null) cachedStats = GetComponentInChildren<CharacterStats>();
        }

        if (weaponPoint == null && steveAnimator != null)
        {
            Transform[] allChildren = steveAnimator.GetComponentsInChildren<Transform>();
            foreach (var t in allChildren)
            {
                if (t.name.ToLower().Contains("weapon_r"))
                {
                    weaponPoint = t;
                    break;
                }
            }
        }
    }

    private void Awake()
    {
        isRolling = false;
        // The static autoRoll might be the problem if it was true and stayed true? 
        // No, autoRoll is an instance variable.
    }

    public bool CanRoll
    {
        get
        {
            if (isRolling) { Debug.Log("[DiceRollSystem] Cannot roll: isRolling is true"); return false; }
            if (IsSteveBusy()) { Debug.Log("[DiceRollSystem] Cannot roll: Steve is busy (moving or combat turn)"); return false; }
            if (GenericPopup.IsOpen) { Debug.Log("[DiceRollSystem] Cannot roll: GenericPopup is open"); return false; }
            if (EquipmentLootPopup.IsOpen) { Debug.Log("[DiceRollSystem] Cannot roll: EquipmentLootPopup is open"); return false; }

            if (cachedStats == null) RefreshReferences();
            if (cachedStats == null || cachedStats.isDead) { Debug.Log("[DiceRollSystem] Cannot roll: Steve is dead or stats missing"); return false; }

            // In combat, we can always roll (safety net for soft-locks)
            if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat) return true;

            bool hasMana = cachedStats.currentMana >= 1;
            if (!hasMana) Debug.Log("[DiceRollSystem] Cannot roll: Low energy");
            return hasMana;
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

        bool inCombat = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat;
        if (inCombat)
            nextAutoRollTime = Time.time + CombatAutoRollCooldown;

        if (steveAnimator != null)
        {
            // If in combat, skip the 'Roll' prep and go right to 'Attack'
            if (inCombat) steveAnimator.SetTrigger("Attack");
            else steveAnimator.SetTrigger("Roll");
        }

        // Consume 1 Energy per roll only if NOT in combat
        if (!inCombat)
        {
            if (cachedStats != null) cachedStats.ConsumeMana(1);
        }

        StartCoroutine(RollRoutine(inCombat));
        }

    private bool IsSteveBusy()
    {
        if (heroNav != null && heroNav.isMoving && !heroNav.IsStuck) return true;
        
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            // If we are in combat, we are only "busy" if it's the enemy's turn 
            // or if a player attack animation is already playing.
            // But we allow the Roll if it's player turn and no sequence is running.
            return !CombatSystem.Instance.isPlayerTurn || CombatSystem.Instance.IsAttackSequenceRunning;
        }

        return false;
    }

    private IEnumerator RollRoutine(bool isCombatRoll)
    {
        Debug.Log($"[DiceRollSystem] RollRoutine started. CombatRoll: {isCombatRoll}");
        isRolling = true;

        try
        {
            // Reactive feel: dice spawn much sooner during an attack swing
            float waitTime = isCombatRoll ? 0.1f : 0.45f;
            yield return new WaitForSeconds(waitTime);

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
                worldDiceContainer = GameObject.Find("WorldDiceContainer") ?? new GameObject("WorldDiceContainer");
            }

            Vector3 baseSpawnPos = transform.position + spawnOffset;
if (isCombatRoll && weaponPoint != null) 
            {
                baseSpawnPos = weaponPoint.position;
            }

            int total = 0;
            List<int> rollValues = new List<int>();

            for (int i = 0; i < amount; i++)
            {
                Vector3 spawnPos = baseSpawnPos + Random.insideUnitSphere * 0.1f;
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

                // Playful/Reactive: Dice "burst" out with more energy in combat
                float finalPopForce = isCombatRoll ? popForce * 2.0f : popForce;
                Vector3 forceDir = isCombatRoll ? (transform.forward + Vector3.up).normalized : Vector3.up;
                
                rb.AddForce((forceDir + Random.insideUnitSphere * 0.4f) * finalPopForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * torqueForce * 3.0f, ForceMode.Impulse);

                // Pre-calculate result for "Instant Resolve" in combat
                DiceStats dStats = die.GetComponent<DiceStats>() ?? die.GetComponentInChildren<DiceStats>();
                if (dStats != null)
                {
                    int val = Random.Range(1, GetMaxSides(diceType) + 1);
                    // For D2 simulation using D6
                    if (diceType == DiceType.D2) val = Random.Range(1, 3); 
                    
                    rollValues.Add(val);
                    total += val;
                }
            }

            // Doubles logic
            bool isDoubles = rollValues.Count == 2 && rollValues[0] == rollValues[1];
            if (isDoubles) total *= 2;

            // INSTANT RESOLVE IN COMBAT:
            // Snappier feel: don't wait for physics, just pop the text and move on
            if (isCombatRoll)
            {
                ApplyResult(total, isDoubles);
                isRolling = false; 
                StartCoroutine(FadeAndDestroyDice(new List<GameObject>(activeDice)));
                
                // Small camera shake on roll impact in combat
                var cam = Object.FindAnyObjectByType<CameraFollow>();
                if (cam != null) cam.Shake(0.1f, 0.2f);
                
                yield break; 
            }

            // 3. Wait for dice to settle (Normal Mode only)
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

            // 4. Result calculation (Normal Mode only)
            total = 0;
            rollValues.Clear();
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

            isDoubles = rollValues.Count == 2 && rollValues[0] == rollValues[1];
            if (isDoubles) total *= 2;

            ApplyResult(total, isDoubles);
            StartCoroutine(FadeAndDestroyDice(new List<GameObject>(activeDice)));
        }
        finally
        {
            isRolling = false;
            Debug.Log("[DiceRollSystem] RollRoutine finished.");
        }
    }

    private void ApplyResult(int total, bool isDoubles)
    {
        if (cachedStats == null) RefreshReferences();

        if (resultText != null) 
        {
            resultText.gameObject.SetActive(true);
            resultText.text = isDoubles ? $"DOUBLE! {total}" : total.ToString();
            StartCoroutine(AnimateFloatingText(resultText));
        }

        bool inCombat = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat;
        if (cachedStats != null && !inCombat)
        {
            cachedStats.AddXP(total);
        }

        OnAnyDiceRolled?.Invoke(total);
        if (heroNav != null) heroNav.OnDiceRolled(total);
    }

    private int GetMaxSides(DiceType type)
    {
        switch (type)
        {
            case DiceType.D2: return 2;
            case DiceType.D4: return 4;
            case DiceType.D6: return 6;
            case DiceType.D8: return 8;
            case DiceType.D10: return 10;
            case DiceType.D12: return 12;
            case DiceType.D20: return 20;
            default: return 6;
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

