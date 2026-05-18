using UnityEngine;

public static class AnimatorUtils
{
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
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName) return true;
        }
        return false;
    }
}
