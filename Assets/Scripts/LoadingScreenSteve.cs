using UnityEngine;

public class LoadingScreenSteve : MonoBehaviour
{
    private Animator animator;
    public string animationName = "SprintFWD_Battle_InPlace_SwordAndShield";
    public float leftBound = -8f;
    public float rightBound = 8f;

    private float currentProgress = 0f;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.Play(animationName);
        }
        transform.localRotation = Quaternion.LookRotation(Vector3.right); // Proper way (Right)
        UpdatePosition();
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float x = Mathf.Lerp(leftBound, rightBound, currentProgress);
        transform.localPosition = new Vector3(x, 0, 0);
    }

    void Update()
    {
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 1.0f)
            {
                animator.Play(animationName, 0, 0f);
            }
        }
    }
}
