using System.Collections;
using UnityEngine;

/// <summary>
/// Powerup – Phần A + B theo GDD:
///   A) Spawn khi enemy chết (7% chance, xử lý trong Enemy.Die())
///      → Di chuyển theo hướng scroll (sang trái)
///      → Tự Destroy khi ra khỏi màn hình
///
///   B) Collect: Player chạm vào → nhặt powerup
///      → Gọi Player.ActivatePowerup()
///      → Sau powerupDuration giây → Gọi Player.DeactivatePowerup()
/// </summary>
public class Powerup : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f; // tốc độ trôi sang trái cùng scroll

    [Header("Powerup Duration")]
    [SerializeField] private float powerupDuration = 10f; // giây hiệu lực

    [Header("Visual - Bob Effect")]
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobFrequency = 2f;

    // ─────────────────────────────────────────────────────────────────────────

    private float startY;
    private bool  isCollected = false;

    private void Start()
    {
        startY = transform.position.y;
    }

    private void Update()
    {
        if (isCollected) return;

        // Di chuyển sang trái (theo hướng side-scroll)
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Hiệu ứng nhấp nhô nhẹ
        float newY = startY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Phần A: Destroy khi ra khỏi màn hình bên trái
        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
        if (vp.x < -0.1f)
        {
            Destroy(gameObject);
        }
    }

    // ── Phần B: Collect ───────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        isCollected = true;

        // Kích hoạt powerup trên player
        player.ActivatePowerup();

        // Dùng GameManager để chạy coroutine đếm thời gian
        // (vì Powerup object sẽ bị Destroy ngay bên dưới)
        if (GameManager.Instance != null)
            GameManager.Instance.StartCoroutine(PowerupTimer(player));

        Destroy(gameObject);
    }

    /// <summary>Đếm ngược powerupDuration giây rồi tắt powerup của player.</summary>
    private System.Collections.IEnumerator PowerupTimer(Player player)
    {
        yield return new WaitForSeconds(powerupDuration);

        if (player != null)
            player.DeactivatePowerup();
    }
}
