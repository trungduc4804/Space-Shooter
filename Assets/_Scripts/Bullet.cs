using UnityEngine;

/// <summary>
/// Attach to a Bullet prefab.
/// – Moves in its local "up" direction (so rotate the prefab to face right if needed).
/// – Destroys itself when it leaves the camera viewport.
/// – Carries a damage value that enemies will read on collision.
/// </summary>
public class Bullet : MonoBehaviour
{
    // Set by PlayerShooter when the bullet is spawned
    [HideInInspector] public float damage = 25f;

    [SerializeField] private float speed = 10f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Fire in the direction the bullet is facing (local up = right for a sideways shooter)
        rb.linearVelocity = transform.up * speed;
    }

    private void Update()
    {
        // Destroy when fully off the camera viewport
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPos.x > 1.1f || viewportPos.x < -0.1f ||
            viewportPos.y > 1.1f || viewportPos.y < -0.1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
