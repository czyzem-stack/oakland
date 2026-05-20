using UnityEngine;
using System.Collections.Generic;

public static class AnimatorUtils
{
    private static Dictionary<EntityId, HashSet<string>> parameterCache = new Dictionary<EntityId, HashSet<string>>();

    public static void SafeSetFloat(this Animator animator, string parameterName, float value)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) return;
        if (HasParameter(animator, parameterName))
        {
            animator.SetFloat(parameterName, value);
        }
    }

    public static void SafeSetTrigger(this Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) return;
        if (HasParameter(animator, parameterName))
        {
            animator.SetTrigger(parameterName);
        }
    }

    public static bool HasParameter(this Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) return false;
        
        EntityId id = animator.GetEntityId();
        if (!parameterCache.TryGetValue(id, out HashSet<string> parameters))
        {
            parameters = new HashSet<string>();
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                parameters.Add(param.name);
            }
            parameterCache[id] = parameters;
        }

        return parameters.Contains(parameterName);
    }
    
    public static void SafeCrossFade(this Animator animator, string stateName, float duration = 0.2f, int layer = 0)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (animator.HasState(layer, Animator.StringToHash(stateName)))
            animator.CrossFade(stateName, duration, layer);
    }

    public static void ResetToLocomotion(this Animator animator, float duration = 0.2f)
    {
        if (animator == null) return;
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("GetHit");
        animator.ResetTrigger("Victory");
        animator.ResetTrigger("Die");
        animator.SafeSetFloat("Speed", 0f);
        animator.SafeCrossFade("Locomotion", duration);
    }

    public static void ClearCache(this Animator animator)
    {
        if (animator != null) parameterCache.Remove(animator.GetEntityId());
    }
}
