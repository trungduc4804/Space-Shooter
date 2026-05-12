using UnityEngine;

/// <summary>
/// Attach to the EnemyBullet prefab.
/// Direction and speed are set by Enemy when the bullet is spawned.
/// Damages the Player on collision.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [HideInInspector] public Vector2 direction = Vector2.left;
    [HideInInspector] public float   speed     = 10f;
    [HideInInspector] public float   damage    = 25f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        // Destroy when off-screen
        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
        if (vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
