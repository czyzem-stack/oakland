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

    private List<GameObject> activeDice = new List<GameObject>();
    private bool isRolling = false;

    public void Roll()
    {
        if (isRolling) return;
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        if (resultText != null) 
        {
            resultText.text = ""; 
            resultText.gameObject.SetActive(false);
        }

        foreach (var die in activeDice)
        {
            if (die != null) Destroy(die);
        }
        activeDice.Clear();

        GameObject prefab = GetPrefabForType(diceType);
        if (prefab == null)
        {
            Debug.LogError("No prefab assigned for " + diceType);
            isRolling = false;
            yield break;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject die = Instantiate(prefab, transform.position + spawnOffset + Random.insideUnitSphere * 0.3f, Random.rotation);
            die.transform.localScale = Vector3.one * scale;
            activeDice.Add(die);

            if (diceMaterial != null)
            {
                var renderers = die.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers) r.material = diceMaterial;
            }

            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb == null) rb = die.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;

            rb.AddForce((Vector3.up + Random.insideUnitSphere * 0.2f) * popForce, ForceMode.Impulse);
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * torqueForce, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(0.5f);
        
        float timer = 0;
        bool allStopped = false;
        while (!allStopped && timer < 2.0f)
        {
            allStopped = true;
            foreach (var die in activeDice)
            {
                if (die != null && die.GetComponent<Rigidbody>().linearVelocity.magnitude > 0.1f)
                {
                    allStopped = false;
                    break;
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        int total = 0;
        foreach (var die in activeDice)
        {
            if (die != null)
            {
                DiceStats stats = die.GetComponent<DiceStats>();
                if (stats == null) stats = die.GetComponentInChildren<DiceStats>();
                
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
        
        isRolling = false;
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

