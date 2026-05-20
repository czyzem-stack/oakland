using UnityEngine;

public class MushroomEnemy : MonoBehaviour
{
    private Animator animator;
    private CharacterStats stats;

    void Start()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
        
        if (animator != null)
        {
            // Use correct state name for Mushroom
            animator.CrossFade("Mushroom_IdlePlant", 0.2f);
        }
    }

    // Mushrooms are passive, so they don't do much. 
    // They just stay in one spot and idle.
}
