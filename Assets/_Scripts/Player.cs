using UnityEngine;

public class Player : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float borderPadding = 0.5f;

    // ── Shooting ──────────────────────────────────────────────────────────────
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;          // Normal bullet prefab
    [SerializeField] private GameObject bulletPowerupPrefab;   // Powerup bullet prefab
    [SerializeField] private Transform  firePoint;             // Empty child at the ship's nose

    // GDD: 4 bullets per second → 1 bullet every 0.25s
    [SerializeField] private float fireRate = 4f;

    // GDD damage values
    private const float NormalDamage  = 25f;
    private const float PowerupDamage = 50f;

    private float fireCooldown = 0f;    // time remaining until next shot
    private bool  hasPowerup   = false; // powerup active state

    // ── Internal ──────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 minBounds;
    private Vector2 maxBounds;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CalculateScreenBounds();
    }

    private void CalculateScreenBounds()
    {
        Camera cam = Camera.main;
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 topRight   = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        minBounds = new Vector2(bottomLeft.x + borderPadding, bottomLeft.y + borderPadding);
        maxBounds = new Vector2(topRight.x   - borderPadding, topRight.y   - borderPadding);
    }

    private void Update()
    {
        // Movement input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Shooting input – hold or press Space / Left Ctrl
        HandleShooting();
    }

    private void FixedUpdate()
    {
        // Tính vị trí tiếp theo dựa vào input
        Vector2 newPosition = rb.position + moveInput.normalized * speed * Time.fixedDeltaTime;

        // Clamp rồi mới di chuyển — không dùng velocity để tránh conflict
        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y)
        );
        rb.MovePosition(clampedPos);
    }

    // ── Shooting ──────────────────────────────────────────────────────────────

    private void HandleShooting()
    {
        // Count down the cooldown every frame
        if (fireCooldown > 0f)
        {
            fireCooldown -= Time.deltaTime;
        }

        // Fire while the player holds the fire key and cooldown is ready
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl))
        {
            if (fireCooldown <= 0f)
            {
                FireBullet();
                fireCooldown = 1f / fireRate;   // 1/4 = 0.25s between shots
            }
        }
    }

    private void FireBullet()
    {
        // Choose prefab based on powerup state
        GameObject prefabToUse = hasPowerup ? bulletPowerupPrefab : bulletPrefab;

        if (prefabToUse == null)
        {
            Debug.LogWarning("Player: bullet prefab is not assigned!");
            return;
        }

        // Spawn at the fire point (tip of the ship), same rotation
        GameObject bullet = Instantiate(prefabToUse, firePoint.position, firePoint.rotation);

        // Pass the correct damage to the bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = hasPowerup ? PowerupDamage : NormalDamage;
        }
    }

    // ── Powerup API (called by Powerup pickup) ────────────────────────────────

    /// <summary>Call this when the player collects a powerup.</summary>
    public void ActivatePowerup()
    {
        hasPowerup = true;
    }

    /// <summary>Call this when the powerup expires.</summary>
    public void DeactivatePowerup()
    {
        hasPowerup = false;
    }

    // ── Damage API (full implementation in Part 4) ────────────────────────────

    /// <summary>
    /// Called by enemies and enemy bullets on collision.
    /// Full Lives/Recovery logic will be added in Part 4.
    /// </summary>
    public void TakeDamage(float amount)
    {
        // TODO (Part 4): reduce lives, trigger Recovery state, flash sprite
        Debug.Log($"Player took {amount} damage!");
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // Draw screen boundary box in the editor
        if (minBounds == Vector2.zero && maxBounds == Vector2.zero) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector2(minBounds.x, minBounds.y), new Vector2(maxBounds.x, minBounds.y));
        Gizmos.DrawLine(new Vector2(maxBounds.x, minBounds.y), new Vector2(maxBounds.x, maxBounds.y));
        Gizmos.DrawLine(new Vector2(maxBounds.x, maxBounds.y), new Vector2(minBounds.x, maxBounds.y));
        Gizmos.DrawLine(new Vector2(minBounds.x, maxBounds.y), new Vector2(minBounds.x, minBounds.y));
    }
}


