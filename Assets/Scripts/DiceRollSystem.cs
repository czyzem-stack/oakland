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
    private bool isRolling = false;

    private void Start()
    {
        CleanupHierarchy();
        RefreshReferences();
    }

    private void CleanupHierarchy()
    {
        // Remove any leftover dice children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        activeDice.Clear();
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

    public void Roll()
    {
        RefreshReferences(); 

        if (isRolling) 
        {
            Debug.Log("[DiceRollSystem] Roll ignored - already rolling.");
            return;
        }
        
        if (steveAnimator != null)
        {
            steveAnimator.SetTrigger("Roll");
        }

        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        Debug.Log("[DiceRollSystem] RollRoutine started.");
        isRolling = true;

        if (resultText != null) 
        {
            resultText.text = ""; 
            resultText.gameObject.SetActive(false);
        }

        // 1. Cleanup
        CleanupHierarchy();

        GameObject prefab = GetPrefabForType(diceType);
        if (prefab == null)
        {
            Debug.LogError("[DiceRollSystem] No prefab found for " + diceType);
            isRolling = false;
            yield break;
        }

        // 2. Spawn
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = transform.position + spawnOffset + Random.insideUnitSphere * 0.1f;
            GameObject die = Instantiate(prefab, spawnPos, Random.rotation, transform); // Added 'transform' as parent
            die.transform.localScale = Vector3.one * scale;
            activeDice.Add(die);
            Debug.Log($"[DiceRollSystem] Spawning die {i} at {spawnPos} (Scale: {scale})");

            if (diceMaterial != null)
            {
                var renderers = die.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers) r.sharedMaterial = diceMaterial;
            }

            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb == null) rb = die.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.linearDamping = 1.0f;
            rb.angularDamping = 1.0f;

            rb.AddForce((Vector3.up + Random.insideUnitSphere * 0.1f) * popForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        }

        // 3. Wait
        yield return new WaitForSeconds(0.5f);
        
        float timer = 0;
        while (timer < 1.5f)
        {
            bool stillMoving = false;
            foreach (var die in activeDice)
            {
                if (die != null && die.GetComponent<Rigidbody>().linearVelocity.magnitude > 0.2f)
                {
                    stillMoving = true;
                    break;
                }
            }
            if (!stillMoving) break;
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. Result
        int total = 0;
        foreach (var die in activeDice)
        {
            if (die != null)
            {
                DiceStats stats = die.GetComponent<DiceStats>() ?? die.GetComponentInChildren<DiceStats>();
                if (stats != null)
                {
                    int val = stats.side;
                    if (diceType == DiceType.D2) val = (val > 3) ? 2 : 1;
                    total += val;
                }
            }
        }

        if (resultText != null) 
        {
            resultText.gameObject.SetActive(true);
            resultText.text = total.ToString();
            StartCoroutine(AnimateFloatingText(resultText));
        }

        if (heroNav != null) heroNav.OnDiceRolled(total);

        isRolling = false;
        Debug.Log("[DiceRollSystem] RollRoutine finished.");
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

