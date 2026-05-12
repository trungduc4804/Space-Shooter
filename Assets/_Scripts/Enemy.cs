using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy1 – Spawn off-screen, move into game area, fire bullets at player.
/// GDD specs:
///   HP = 100 | fire every 5s | bullet speed = 2x move speed
///   Flash white on hit | 7% powerup drop chance on death
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHp       = 100f;
    [SerializeField] private float moveSpeed   = 3f;

    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private float      shootInterval = 5f;   // GDD: 1 bullet per 5 seconds

    [Header("Powerup Drop")]
    [SerializeField] private GameObject powerupPrefab;        // assign in inspector
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.07f; // GDD: 7%

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.08f;

    // ── State ─────────────────────────────────────────────────────────────────
    private float         currentHp;
    private float         shootTimer = 0f;
    private bool          isDead     = false;
    private Rigidbody2D   rb;
    private SpriteRenderer sr;
    private Color         originalColor;

    // Target X to stop at inside the screen (set by EnemySpawner)
    [HideInInspector] public float targetX;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    private void Start()
    {
        currentHp  = maxHp;
        shootTimer = shootInterval; // wait before first shot
    }

    private void Update()
    {
        if (isDead) return;

        MoveToTarget();
        HandleShooting();
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves toward targetX, stops when reached.
    /// EnemySpawner sets targetX to a random X inside the screen.
    /// </summary>
    private void MoveToTarget()
    {
        float currentX = transform.position.x;

        if (currentX > targetX)
        {
            // Still entering the screen — move left
            rb.linearVelocity = Vector2.left * moveSpeed;
        }
        else
        {
            // Reached target position — stop horizontal movement
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ── Shooting ──────────────────────────────────────────────────────────────

    private void HandleShooting()
    {
        // GDD: enemy only fires when FULLY visible on screen
        if (!IsFullyOnScreen()) return;

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            FireBullet();
            shootTimer = shootInterval;
        }
    }

    private void FireBullet()
    {
        if (enemyBulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning($"Enemy [{name}]: enemyBulletPrefab or firePoint not assigned!");
            return;
        }

        GameObject bulletObj = Instantiate(enemyBulletPrefab, firePoint.position, Quaternion.identity);
        EnemyBullet bullet   = bulletObj.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.direction = Vector2.left;                 // fires toward player
            bullet.speed     = moveSpeed * 2f;               // GDD: bullet speed = 2× move speed
        }
    }

    // ── Damage & Death ────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHp -= amount;
        StartCoroutine(HitFlash());

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Flash white briefly (Cuphead-style hit feedback).
    /// </summary>
    private IEnumerator HitFlash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // GDD: 7% chance to drop a powerup
        if (powerupPrefab != null && Random.value <= dropChance)
        {
            Instantiate(powerupPrefab, transform.position, Quaternion.identity);
        }

        // TODO: play explosion VFX/SFX here
        // TODO: add score points (Part 9)

        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true only when ALL corners of the sprite are inside the viewport.
    /// </summary>
    private bool IsFullyOnScreen()
    {
        Bounds bounds = sr.bounds;

        Vector3 vpMin = Camera.main.WorldToViewportPoint(bounds.min);
        Vector3 vpMax = Camera.main.WorldToViewportPoint(bounds.max);

        return vpMin.x >= 0f && vpMax.x <= 1f &&
               vpMin.y >= 0f && vpMax.y <= 1f;
    }

    // ── Collision with player body ────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(25f); // enemy ram damage
        }
    }
}
