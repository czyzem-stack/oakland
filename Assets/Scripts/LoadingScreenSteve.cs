using UnityEngine;
using System.Collections.Generic;

public class LoadingScreenSteve : MonoBehaviour
{
    private Animator animator;
    public List<string> randomAnimations = new List<string> 
    { 
        "Idle_Normal_SwordAndShield", 
        "Dance_SwordAndShield", 
        "Victory_Battle_SwordAndShield", 
        "LevelUp_Battle_SwordAndShield",
        "Challenging_Battle_SwordAndShield",
        "JumpFull_Spin_RM_SwordAndShield"
    };

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        PlayRandomAnimation();
    }

    public void SetProgress(float progress)
    {
        // Progress no longer moves Steve
    }

    private void PlayRandomAnimation()
    {
        if (animator != null && randomAnimations.Count > 0)
        {
            string anim = randomAnimations[Random.Range(0, randomAnimations.Count)];
            animator.CrossFadeInFixedTime(anim, 0.25f);
        }
    }

    void Update()
    {
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 0.95f && !animator.IsInTransition(0))
            {
                PlayRandomAnimation();
            }
        }
    }
}
