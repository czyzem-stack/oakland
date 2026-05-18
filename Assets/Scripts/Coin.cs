using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotationSpeed = 360f;
    public float bobSpeed = 3f;
    public float bobHeight = 0.3f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        
        // Ensure we have a trigger collider
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.12f; // Increased local radius for the smaller global scale (6.0)

        // Ensure we have a Rigidbody for trigger detection if player doesn't have one (though Hero usually does)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        // Rotate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Bob up and down
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<HeroNavigation>() != null)
        {
            Collect();
        }
    }

    public void Collect()
    {
        CharacterStats stats = Object.FindAnyObjectByType<HeroNavigation>()?.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.AddGold(1);
        }

        // Play a sound if we had one, for now just destroy
        Destroy(gameObject);
    }
}
