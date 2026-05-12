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
    [SerializeField] private float minTargetX     = 0f;   // leftmost stop X (world space)
    [SerializeField] private float maxTargetX     = 3f;   // rightmost stop X (world space)

    [Header("Overlap Prevention")]
    [SerializeField] private float minSpacingY    = 1.5f; // min vertical gap between enemies

    private float spawnTimer = 0f;
    private float screenMinY;
    private float screenMaxY;
    private float spawnX;    // X position off right side of screen

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        CalculateScreenEdges();
    }

    private void CalculateScreenEdges()
    {
        Camera cam = Camera.main;

        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f));
        Vector3 topRight    = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        screenMinY = bottomRight.y + 0.5f;
        screenMaxY = topRight.y   - 0.5f;
        spawnX     = bottomRight.x + spawnOffsetX;
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
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // Tell the enemy where to stop inside the screen
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.targetX = Random.Range(minTargetX, maxTargetX);
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

    // Editor helper — show spawn line
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        Vector3 bottomRight = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0f, 0f));
        Vector3 topRight    = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        float gizmoX        = bottomRight.x + spawnOffsetX;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(gizmoX, bottomRight.y, 0f),
                        new Vector3(gizmoX, topRight.y,    0f));
    }
}
