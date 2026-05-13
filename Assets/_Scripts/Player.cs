using System.Collections;
using UnityEngine;

/// <summary>
/// Player – Di chuyển, bắn đạn, hệ thống lives & Recovery state (GDD Part 4).
///
/// State Machine:
///   IDLE     : chơi bình thường, nhận damage
///   RECOVERY : bất tử + sprite flash, sau recoveryTime giây → IDLE
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float speed         = 5f;
    [SerializeField] private float borderPadding = 0.5f;

    // ── Shooting ──────────────────────────────────────────────────────────────
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject bulletPowerupPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private float      fireRate = 4f; // GDD: 4 bullets/s

    // GDD damage values
    private const float NormalDamage  = 25f;
    private const float PowerupDamage = 50f;

    // ── Lives & Recovery ──────────────────────────────────────────────────────
    [Header("Lives & Recovery")]
    [SerializeField] private int   maxLives      = 3;    // GDD: 3 lives
    [SerializeField] private float recoveryTime  = 3f;   // GDD: ~3 seconds
    [SerializeField] private float flashInterval = 0.1f; // tốc độ flash khi recovery

    // ── Internal ──────────────────────────────────────────────────────────────
    private enum PlayerState { Idle, Recovery }
    private PlayerState state = PlayerState.Idle;

    private int            currentLives;
    private float          fireCooldown = 0f;
    private bool           hasPowerup   = false;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Collider2D     col;
    private Vector2        moveInput;
    private Vector2        minBounds;
    private Vector2        maxBounds;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        CalculateScreenBounds();
    }

    private void Start()
    {
        currentLives = maxLives;
        UpdateHUD();
    }

    private void CalculateScreenBounds()
    {
        Camera cam     = Camera.main;
        Vector3 botLeft  = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        minBounds = new Vector2(botLeft.x  + borderPadding, botLeft.y  + borderPadding);
        maxBounds = new Vector2(topRight.x - borderPadding, topRight.y - borderPadding);
    }

    private void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (state == PlayerState.Idle)
            HandleShooting();
    }

    private void FixedUpdate()
    {
        Vector2 newPosition = rb.position + moveInput.normalized * speed * Time.fixedDeltaTime;
        Vector2 clampedPos  = new Vector2(
            Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y)
        );
        rb.MovePosition(clampedPos);
    }


    private void HandleShooting()
    {
        if (fireCooldown > 0f) fireCooldown -= Time.deltaTime;

        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl))
        {
            if (fireCooldown <= 0f)
            {
                FireBullet();
                fireCooldown = 1f / fireRate;
            }
        }
    }

    private void FireBullet()
    {
        GameObject prefabToUse = hasPowerup ? bulletPowerupPrefab : bulletPrefab;
        if (prefabToUse == null)
        {
            Debug.LogWarning("Player: bullet prefab chưa gán!");
            return;
        }

        GameObject bullet       = Instantiate(prefabToUse, firePoint.position, firePoint.rotation);
        Bullet     bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.damage = hasPowerup ? PowerupDamage : NormalDamage;
    }
    public void TakeDamage(float amount)
    {
        if (state == PlayerState.Recovery) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Phần F: reset multiplier khi bị đánh
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnPlayerHit();

        currentLives--;
        UpdateHUD();

        if (currentLives <= 0)
        {       
            Die();
        }
        else
        {
            StartCoroutine(RecoveryRoutine());
        }
    }

    private IEnumerator RecoveryRoutine()
    {
        state       = PlayerState.Recovery;
        col.enabled = false; 

        float timer = 0f;
        while (timer < recoveryTime)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(flashInterval);
            sr.color = Color.clear; 
            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2f;
        }

        sr.color    = Color.white;
        col.enabled = true;
        state       = PlayerState.Idle;
    }

    private void Die()
    {
        gameObject.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
    }


    private void UpdateHUD()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateLives(currentLives, maxLives);
    }
    public void ActivatePowerup()   => hasPowerup = true;
    public void DeactivatePowerup() => hasPowerup = false;

    private void OnDrawGizmos()
    {
        if (minBounds == Vector2.zero && maxBounds == Vector2.zero) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector2(minBounds.x, minBounds.y), new Vector2(maxBounds.x, minBounds.y));
        Gizmos.DrawLine(new Vector2(maxBounds.x, minBounds.y), new Vector2(maxBounds.x, maxBounds.y));
        Gizmos.DrawLine(new Vector2(maxBounds.x, maxBounds.y), new Vector2(minBounds.x, maxBounds.y));
        Gizmos.DrawLine(new Vector2(minBounds.x, maxBounds.y), new Vector2(minBounds.x, minBounds.y));
    }
}
