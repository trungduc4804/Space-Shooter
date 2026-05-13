using UnityEngine;

/// <summary>
/// Spawns Enemy1 at random Y positions off the right edge of the screen.
/// – Enemies are spaced apart to prevent overlap.
/// – targetX is set to a random X inside the visible area.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval  = 3f;   // seconds between spawns
    [SerializeField] private float spawnOffsetX   = 1.5f; // how far off the right edge

    // Stop position range as fraction of screen width (0 = left edge, 1 = right edge)
    [SerializeField] [Range(0f, 1f)] private float minTargetXRatio = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float maxTargetXRatio = 0.7f;

    // Chỉ spawn trong phần trên cùng của màn hình
    // 0.7 = spawn từ 30% chiều cao trở lên (bỏ 30% phía dưới)
    [SerializeField] [Range(0.1f, 1f)] private float spawnHeightRatio = 0.7f;

    [Header("Overlap Prevention")]
    [SerializeField] private float minSpacingY    = 1.5f; // min vertical gap between enemies

    private float spawnTimer = 0f;
    private float screenMinY;
    private float screenMaxY;
    private float screenMinX;
    private float screenMaxX;
    private float spawnX;    // X position off right side of screen

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        CalculateScreenEdges();
    }

    private void CalculateScreenEdges()
    {
        Camera cam = Camera.main;

        Vector3 bottomLeft  = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f));
        Vector3 topRight    = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        screenMinX = bottomLeft.x;
        screenMaxX = bottomRight.x;

        float fullHeight = topRight.y - bottomLeft.y;

        // Chỉ spawn trong phần trên (spawnHeightRatio)
        // VD: 0.7 → bỏ 30% phía dưới, chỉ dùng 70% phía trên
        screenMinY = bottomLeft.y + fullHeight * (1f - spawnHeightRatio) + 0.5f;
        screenMaxY = topRight.y - 0.5f;

        spawnX = bottomRight.x + spawnOffsetX;
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnEnemy();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void TrySpawnEnemy()
    {
        float spawnY = GetSafeSpawnY();
        if (float.IsNaN(spawnY)) return; // no safe position found

        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
        // Quan trọng: Sử dụng enemyPrefab.transform.rotation thay vì Quaternion.identity
        // để giữ nguyên góc xoay (Z=90) mà user đã setup trong Prefab.
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, enemyPrefab.transform.rotation);

        // Tell the enemy where to stop inside the screen (calculated from viewport ratio)
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            float screenWidth = screenMaxX - screenMinX;
            float stopMinX    = screenMinX + screenWidth * minTargetXRatio;
            float stopMaxX    = screenMinX + screenWidth * maxTargetXRatio;
            enemy.targetX     = Random.Range(stopMinX, stopMaxX);
        }
    }

    /// <summary>
    /// Picks a random Y that is at least minSpacingY away from all existing enemies.
    /// Returns float.NaN if no safe spot found after several attempts.
    /// </summary>
    private float GetSafeSpawnY()
    {
        const int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            float candidateY = Random.Range(screenMinY, screenMaxY);

            if (IsSafeY(candidateY))
                return candidateY;
        }

        return float.NaN; // skip this spawn
    }

    private bool IsSafeY(float candidateY)
    {
        // Find all active enemies and check vertical distance
        Enemy[] existing = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in existing)
        {
            if (Mathf.Abs(e.transform.position.y - candidateY) < minSpacingY)
                return false;
        }
        return true;
    }

    // Editor helper — show spawn line & spawn zone
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        Vector3 bottomLeft  = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 bottomRight = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0f, 0f));
        Vector3 topRight    = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        float fullHeight   = topRight.y - bottomLeft.y;
        float spawnMinY    = bottomLeft.y + fullHeight * (1f - spawnHeightRatio);
        float spawnMaxY    = topRight.y;
        float gizmoX       = bottomRight.x + spawnOffsetX;

        // Đường vàng = vị trí spawn (ngoài màn hình)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(gizmoX, spawnMinY, 0f),
                        new Vector3(gizmoX, spawnMaxY, 0f));

        // Vùng xanh lá = khu vực Y cho phép spawn (trong màn hình)
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Vector3 zoneCenter = new Vector3(
            (bottomLeft.x + bottomRight.x) / 2f,
            (spawnMinY + spawnMaxY) / 2f,
            0f);
        Vector3 zoneSize = new Vector3(bottomRight.x - bottomLeft.x, spawnMaxY - spawnMinY, 0f);
        Gizmos.DrawCube(zoneCenter, zoneSize);

        // Viền xanh lá
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(zoneCenter, zoneSize);
    }
}
