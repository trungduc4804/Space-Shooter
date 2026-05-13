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
    [Header("Data")]
    public EnemyData data;

    // ── Powerup Drop ───────────────────────────────────────────────────────────
    [Header("Powerup Drop")]
    [SerializeField] private GameObject powerupPrefab;

    // ── Hit Flash ──────────────────────────────────────────────────────────────
    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Transform firePoint;

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
        if (data == null)
        {
            Debug.LogError($"Enemy [{name}] missing EnemyData!");
            return;
        }
        currentHp  = data.maxHp;
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
            rb.linearVelocity = Vector2.left * data.moveSpeed;
        }
        else
        {
            // Đã đến targetX → dừng lại, bắt đầu đếm stopDuration
            rb.linearVelocity = Vector2.zero;
            stopTimer = data.stopDuration;
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
        rb.linearVelocity = Vector2.right * data.moveSpeed;

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
            shootTimer = data.fireInterval;
        }
    }

    private void FireBullet()
    {
        if (data.bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning($"Enemy [{name}]: bulletPrefab trong EnemyData hoặc firePoint chưa gán!");
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

        // Quan trọng: dùng bulletPrefab.transform.rotation để giữ góc Z=90 mà user đã setup trong prefab
        GameObject  bulletObj = Instantiate(data.bulletPrefab, firePoint.position, data.bulletPrefab.transform.rotation);
        EnemyBullet bullet    = bulletObj.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.direction = shootDirection;
            bullet.speed     = data.moveSpeed * data.bulletSpeedMultiplier; // GDD: bullet speed = 2× move speed
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

        // GDD: drop powerup rate từ EnemyData
        if (powerupPrefab != null && Random.value <= data.powerupDropRate)
        {
            Instantiate(powerupPrefab, transform.position, Quaternion.identity);
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(data.scoreValue);

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
