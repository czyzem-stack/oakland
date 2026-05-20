using UnityEngine;
using System.Collections.Generic;

public class CameraOcclusionFade : MonoBehaviour
{
    public Transform target;
    public LayerMask mask;
    public float radius = 0.5f;

    private List<Renderer> hiddenRenderers = new List<Renderer>();
    private List<Renderer> previouslyHiddenRenderers = new List<Renderer>();

    void Update()
    {
        if (target == null) return;

        previouslyHiddenRenderers.Clear();
        previouslyHiddenRenderers.AddRange(hiddenRenderers);
        hiddenRenderers.Clear();

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;
        
        // SphereCast to find objects in front of the target
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction.normalized, distance, mask);

        foreach (var hit in hits)
        {
            if (hit.transform == target) continue;

            // Ignore ground and bridges
            string lowerName = hit.transform.name.ToLower();
            if (lowerName.Contains("bridge") || lowerName.Contains("floor") || lowerName.Contains("ground") || lowerName.Contains("terrain"))
                continue;

            Renderer r = hit.transform.GetComponentInChildren<Renderer>();
            if (r != null && !hiddenRenderers.Contains(r))
            {
                hiddenRenderers.Add(r);
                r.enabled = false;
            }
        }

        // Restore renderers that are no longer blocking
        foreach (var r in previouslyHiddenRenderers)
        {
            if (r != null && !hiddenRenderers.Contains(r))
            {
                r.enabled = true;
            }
        }
    }
}
