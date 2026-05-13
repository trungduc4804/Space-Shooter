using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy1 – Vòng đời theo GDD:
///   1. ENTERING  : di chuyển từ off-screen vào targetX (Entry Path)
///   2. STOPPED   : dừng tại targetX trong stopDuration giây, bắn đạn
///   3. EXITING   : di chuyển ra khỏi màn hình (Exit Path) → Destroy
///
/// GDD specs:
///   HP = 100 | fire every 5s | bullet speed = 2x move speed
///   Flash white on hit | 7% powerup drop on death
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    // ── Stats ──────────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private float maxHp     = 100f;
    [SerializeField] private float moveSpeed = 3f;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    [Header("Lifecycle")]
    [SerializeField] private float stopDuration = 8f;  // giây đứng trong màn hình (phải > shootInterval)

    // ── Shooting ───────────────────────────────────────────────────────────────
    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private float      shootInterval = 5f;   // GDD: 1 bullet / 5s

    // ── Powerup Drop ───────────────────────────────────────────────────────────
    [Header("Powerup Drop")]
    [SerializeField] private GameObject         powerupPrefab;
    [SerializeField] [Range(0f,1f)] private float dropChance = 0.07f; // GDD: 7%

    // ── Hit Flash ──────────────────────────────────────────────────────────────
    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.08f;

    // ── Internal State ─────────────────────────────────────────────────────────
    private enum Phase { Entering, Stopped, Exiting }
    private Phase phase = Phase.Entering;

    private float          currentHp;
    private float          shootTimer;
    private float          stopTimer;
    private bool           isDead = false;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Color          originalColor;

    // Set by EnemySpawner
    [HideInInspector] public float targetX;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        sr            = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    private void Start()
    {
        currentHp  = maxHp;
        shootTimer = 0f; // bắn ngay khi fully on screen (không cần chờ 5s đầu)

        phase = Phase.Entering;
    }

    private void Update()
    {
        if (isDead) return;

        switch (phase)
        {
            case Phase.Entering: UpdateEntering(); break;
            case Phase.Stopped:  UpdateStopped();  break;
            case Phase.Exiting:  UpdateExiting();  break;
        }
    }

    // ── Phase: ENTERING ───────────────────────────────────────────────────────

    private void UpdateEntering()
    {
        float currentX = transform.position.x;

        if (currentX > targetX)
        {
            // Di chuyển sang trái về targetX
            rb.linearVelocity = Vector2.left * moveSpeed;
        }
        else
        {
            // Đã đến targetX → dừng lại, bắt đầu đếm stopDuration
            rb.linearVelocity = Vector2.zero;
            stopTimer = stopDuration;
            phase     = Phase.Stopped;
        }
    }

    // ── Phase: STOPPED ────────────────────────────────────────────────────────

    private void UpdateStopped()
    {
        rb.linearVelocity = Vector2.zero;

        // Bắn đạn khi fully on screen
        HandleShooting();

        // Đếm ngược thời gian dừng
        stopTimer -= Time.deltaTime;
        if (stopTimer <= 0f)
        {
            phase = Phase.Exiting;
        }
    }

    // ── Phase: EXITING ────────────────────────────────────────────────────────

    private void UpdateExiting()
    {
        // Di chuyển ra ngoài cạnh phải màn hình
        rb.linearVelocity = Vector2.right * moveSpeed;

        // Destroy khi hoàn toàn ra khỏi màn hình
        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
        if (vp.x > 1.2f)
        {
            Destroy(gameObject);
        }
    }

    // ── Shooting ──────────────────────────────────────────────────────────────

    private void HandleShooting()
    {
        // GDD: chỉ bắn khi FULLY visible on screen
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
            Debug.LogWarning($"Enemy [{name}]: enemyBulletPrefab hoặc firePoint chưa gán!");
            return;
        }

        // Tính hướng bắn đến vị trí player hiện tại
        Vector2 shootDirection = Vector2.left; // fallback

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            shootDirection = ((Vector2)playerObj.transform.position
                              - (Vector2)firePoint.position).normalized;
        }

        // Quan trọng: dùng enemyBulletPrefab.transform.rotation để giữ góc Z=90 mà user đã setup trong prefab
        GameObject  bulletObj = Instantiate(enemyBulletPrefab, firePoint.position, enemyBulletPrefab.transform.rotation);
        EnemyBullet bullet    = bulletObj.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.direction = shootDirection;
            bullet.speed     = moveSpeed * 2f; // GDD: bullet speed = 2× move speed
        }
    }

    // ── Damage & Death ────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // GDD: enemy đang Exit không nhận damage
        if (phase == Phase.Exiting) return;

        currentHp -= amount;
        StartCoroutine(HitFlash());

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    /// <summary>Flash trắng nhanh 1 lần (Cuphead-style).</summary>
    private IEnumerator HitFlash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
    }

    private void Die()
    {
        isDead            = true;
        rb.linearVelocity = Vector2.zero;

        // GDD: 7% cơ hội drop powerup
        if (powerupPrefab != null && Random.value <= dropChance)
        {
            Instantiate(powerupPrefab, transform.position, Quaternion.identity);
        }

        // GDD: +25 điểm × multiplier khi tiêu diệt enemy (Phần E)
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddEnemyKillScore();

        // TODO: explosion VFX/SFX (Part 11)

        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>True khi TẤT CẢ góc sprite nằm trong viewport.</summary>
    private bool IsFullyOnScreen()
    {
        Bounds bounds = sr.bounds;
        Vector3 vpMin = Camera.main.WorldToViewportPoint(bounds.min);
        Vector3 vpMax = Camera.main.WorldToViewportPoint(bounds.max);

        return vpMin.x >= 0f && vpMax.x <= 1f &&
               vpMin.y >= 0f && vpMax.y <= 1f;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ damage player khi enemy đang active (không trong giai đoạn đang exit)
        if (phase == Phase.Exiting) return;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(25f);
        }
    }
}
