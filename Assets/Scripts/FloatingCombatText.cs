using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingCombatText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float duration = 1.5f;
    public Vector3 driftDirection = Vector3.up;
    public float driftSpeed = 1f;

    public System.Action<FloatingCombatText> OnComplete;

    public void Setup(string content, Color color)
    {
        if (text != null)
        {
            text.text = content;
            text.color = color;
        }
        
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float elapsed = 0;
        Vector3 startPos = transform.position;
        
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;

        while (elapsed < duration)
        {
            if (group == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = startPos + driftDirection * driftSpeed * t;
            group.alpha = 1 - t;
            
            yield return null;
        }
        
        if (OnComplete != null)
        {
            OnComplete.Invoke(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
