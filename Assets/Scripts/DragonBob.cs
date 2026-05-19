using UnityEngine;

public class DragonBob : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flyHeight = 25f; 
    public float rotateSpeed = 1.5f;
    public float followSpeed = 1.2f;
    public float orbitRadius = 22f;
    public float orbitSpeed = 0.1f;

    private Animator animator;
    private HeroNavigation playerNav;
    private Light cachedDirectionalLight;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        playerNav = PlayerReference.GetNavigation();
        cachedDirectionalLight = FindDirectionalLight();
        
        // Dragon is a titan of the sky
        transform.localScale = Vector3.one * 5.0f; 

        if (animator != null)
        {
            animator.CrossFade("Fly Forward", 0.1f);
        }

        // Clean up any AI/Combat components to make it a pure visual asset
        var stats = GetComponent<CharacterStats>();
        if (stats != null) Object.DestroyImmediate(stats);
        
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) Object.DestroyImmediate(agent);

        var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null) Object.DestroyImmediate(obstacle);

        // No interaction needed
        foreach (var col in GetComponentsInChildren<Collider>()) { col.enabled = false; }
    }

    void Update()
    {
        if (playerNav == null) 
        {
            playerNav = PlayerReference.GetNavigation();
            return;
        }

        Light mainLight = cachedDirectionalLight ?? FindDirectionalLight();
        if (mainLight == null) return;

        Vector3 lightDir = mainLight.transform.forward;
        if (lightDir.y >= -0.01f) return;

        // Orbit logic
        float angle = Time.time * orbitSpeed;
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(angle) * orbitRadius,
            0,
            Mathf.Sin(angle) * orbitRadius
        );

        Vector3 targetShadowPos = playerNav.transform.position + orbitOffset;
        
        // Geometry to find sky position
        float dragonY = playerNav.transform.position.y + flyHeight;
        float distToSky = (dragonY - targetShadowPos.y) / -lightDir.y;
        Vector3 targetSkyPos = targetShadowPos - lightDir * distToSky;

        // Smooth drift
        transform.position = Vector3.Lerp(transform.position, targetSkyPos, Time.deltaTime * followSpeed);

        // Face movement direction
        Vector3 moveDir = (targetSkyPos - transform.position);
        moveDir.y = 0; 
        if (moveDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }
    }

    private Light FindDirectionalLight()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (l.type == LightType.Directional && l.shadows != LightShadows.None)
            {
                cachedDirectionalLight = l;
                return l;
            }
        }
        return null;
    }

    public void FTUEFlyAway() { }
}