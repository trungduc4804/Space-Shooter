using UnityEngine;

/// <summary>
/// ScoreManager – Phần E + F theo GDD:
///
///   E) Hệ thống điểm:
///      - Bắt đầu với 0 điểm
///      - Mỗi 5 giây sống sót → +10 điểm × multiplier
///      - Mỗi enemy bị tiêu diệt → +25 điểm × multiplier
///
///   F) Hệ thống Multiplier:
///      - Không nhận damage 5s  → 2x
///      - Không nhận damage 10s → 4x
///      - Không nhận damage 30s → 5x
///      - Nhận damage → reset về 1x
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ── GDD Constants ─────────────────────────────────────────────────────────
    private const int   SurvivalPoints      = 10;  // điểm mỗi 5 giây
    private const float SurvivalInterval    = 5f;  // giây
    private const int   EnemyKillPoints     = 25;  // điểm mỗi enemy

    private const float Mult2xThreshold    = 5f;
    private const float Mult4xThreshold    = 10f;
    private const float Mult5xThreshold    = 30f;

    // ── State ─────────────────────────────────────────────────────────────────
    private int   currentScore      = 0;
    private float survivalTimer     = 0f;
    private float noHitTimer        = 0f;   // thời gian không bị đánh
    private float currentMultiplier = 1f;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // ── Đếm thời gian sống sót → cộng điểm mỗi 5 giây
        survivalTimer += Time.deltaTime;
        if (survivalTimer >= SurvivalInterval)
        {
            survivalTimer -= SurvivalInterval;
            AddScore(SurvivalPoints);
        }

        // ── Đếm thời gian không bị đánh → tính multiplier (Phần F)
        noHitTimer += Time.deltaTime;
        UpdateMultiplier();
    }

    // ── Multiplier Logic (Phần F) ─────────────────────────────────────────────

    private void UpdateMultiplier()
    {
        float oldMultiplier = currentMultiplier;

        if      (noHitTimer >= Mult5xThreshold) currentMultiplier = 5f;
        else if (noHitTimer >= Mult4xThreshold) currentMultiplier = 4f;
        else if (noHitTimer >= Mult2xThreshold) currentMultiplier = 2f;
        else                                    currentMultiplier = 1f;

        // Chỉ update UI khi multiplier thay đổi
        if (!Mathf.Approximately(oldMultiplier, currentMultiplier))
            UpdateUI();
    }

    /// <summary>Gọi khi player nhận damage → reset multiplier về 1x.</summary>
    public void OnPlayerHit()
    {
        noHitTimer = 0f;
        currentMultiplier = 1f;
        UpdateUI();
    }

    // ── Score API ─────────────────────────────────────────────────────────────

    /// <summary>Thêm điểm × multiplier hiện tại.</summary>
    public void AddScore(int basePoints)
    {
        int earned = Mathf.RoundToInt(basePoints * currentMultiplier);
        currentScore += earned;
        UpdateUI();
    }

    /// <summary>Gọi bởi Enemy.Die() – cộng 25 điểm × multiplier.</summary>
    public void AddEnemyKillScore()
    {
        AddScore(EnemyKillPoints);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(currentScore, currentMultiplier);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    public int   CurrentScore      => currentScore;
    public float CurrentMultiplier => currentMultiplier;
}
