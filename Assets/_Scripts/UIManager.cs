using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý HUD và màn hình Game Over.
/// Singleton — gọi UIManager.Instance từ bất kỳ script nào.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD - Lives")]
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private Sprite  lifeFullSprite;
    [SerializeField] private Sprite  lifeEmptySprite;

    [Header("HUD - Score")]
    [SerializeField] private TMP_Text scoreText;       // Text hiển thị điểm
    [SerializeField] private TMP_Text multiplierText;  // Text hiển thị multiplier (2x, 4x, 5x)

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel; // Panel Game Over (ẩn ban đầu)
    [SerializeField] private Button     restartButton;
    [SerializeField] private Button     quitButton;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ẩn Game Over panel lúc đầu
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Gán sự kiện nút Restart
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    // ── Score HUD ──────────────────────────────────────────────────────────────

    /// <summary>Cập nhật điểm và multiplier trên HUD.</summary>
    public void UpdateScore(int score, float multiplier)
    {
        if (scoreText != null)
            scoreText.text = score.ToString("N0"); // có dấu phẩy ngăn cách nhóm số

        if (multiplierText != null)
        {
            if (multiplier <= 1f)
            {
                multiplierText.gameObject.SetActive(false); // ẩn khi 1x
            }
            else
            {
                multiplierText.gameObject.SetActive(true);
                multiplierText.text = $"{multiplier:0}x";

                // Đổi màu theo mức multiplier
                multiplierText.color = multiplier >= 5f ? Color.red
                                     : multiplier >= 4f ? new Color(1f, 0.5f, 0f) // cam
                                     :                    Color.yellow;            // 2x
            }
        }
    }

    // ── Lives HUD ─────────────────────────────────────────────────────────────

    /// <summary>Cập nhật icon lives trên HUD.</summary>
    public void UpdateLives(int currentLives, int maxLives)
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;

            bool isAlive = i < currentLives;

            // Đổi sprite nếu có gán, hoặc chỉ bật/tắt color
            if (lifeFullSprite != null && lifeEmptySprite != null)
                lifeIcons[i].sprite = isAlive ? lifeFullSprite : lifeEmptySprite;
            else
                lifeIcons[i].color = isAlive ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }
    }

    // ── Game Over ─────────────────────────────────────────────────────────────

    /// <summary>Hiển thị màn hình Game Over.</summary>
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }
    private void OnQuitClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.QuitGame();
    }
}
