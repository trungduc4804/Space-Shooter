using UnityEngine;

/// <summary>
/// Quản lý hệ thống Đợt Lính (Wave) và Đội Hình (Formations).
/// Độ khó tăng dần theo thời gian (0-30s, 30s-60s, >60s).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float waveInterval = 4f;   // Thời gian giữa các đợt
    [SerializeField] private float spawnOffsetX = 1.5f; // Khoảng cách ngoài rìa phải màn hình

    // Điểm dừng của địch trên màn hình (0 = sát mép trái, 1 = sát mép phải)
    [Header("Screen Settings")]
    [SerializeField] [Range(0f, 1f)] private float minTargetXRatio = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float maxTargetXRatio = 0.7f;
    
    // Chỉ spawn trong phần trên cùng của màn hình (70% trên cùng để chừa HUD dưới cùng)
    [SerializeField] [Range(0.1f, 1f)] private float spawnHeightRatio = 0.7f;
    
    // Khoảng cách Y tối thiểu giữa các quái để không bị đè lên nhau
    [SerializeField] private float minSpacingY = 1.5f;

    private float waveTimer = 0f;
    private float elapsedTime = 0f;

    private float screenMinY;
    private float screenMaxY;
    private float screenMinX;
    private float screenMaxX;
    private float spawnX;

    private GameObject currentWavePrefab; // Lưu loại quái đang sinh ra trong đợt này

    private enum WaveType { Single, Column, VShape, Diagonal }

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

        // Bỏ qua phần dưới màn hình (tránh HUD)
        screenMinY = bottomLeft.y + fullHeight * (1f - spawnHeightRatio) + 0.5f;
        screenMaxY = topRight.y - 0.5f;

        spawnX = bottomRight.x + spawnOffsetX;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        waveTimer += Time.deltaTime;

        if (waveTimer >= waveInterval)
        {
            waveTimer = 0f;
            SpawnWave();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WAVE LOGIC
    // ─────────────────────────────────────────────────────────────────────────

    private void SpawnWave()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: Chưa gán enemyPrefabs!");
            return;
        }

        // Chọn ngẫu nhiên 1 loại quái cho cả đợt này
        currentWavePrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        WaveType type = SelectWaveTypeBasedOnDifficulty();

        switch (type)
        {
            case WaveType.Single:
                SpawnSingle();
                break;
            case WaveType.Column:
                SpawnColumn(3); // Ra 3 con xếp hàng dọc
                break;
            case WaveType.VShape:
                SpawnVShape();  // Ra đội hình mũi nhọn 3 con
                break;
            case WaveType.Diagonal:
                SpawnDiagonal(3); // Ra 3 con theo đường chéo
                break;
        }
    }

    private WaveType SelectWaveTypeBasedOnDifficulty()
    {
        if (elapsedTime < 30f)
        {
            // Giai đoạn 1 (0-30s): Rất dễ, chỉ ra 1 con lẻ
            return WaveType.Single;
        }
        else if (elapsedTime < 60f)
        {
            // Giai đoạn 2 (30-60s): Khó vừa, ra lẻ, hàng dọc, hoặc đường chéo
            int rand = Random.Range(0, 3);
            if (rand == 0) return WaveType.Single;
            if (rand == 1) return WaveType.Column;
            return WaveType.Diagonal;
        }
        else
        {
            // Giai đoạn 3 (>60s): Siêu khó, mở khóa đội hình chữ V 
            int rand = Random.Range(0, 4);
            return (WaveType)rand;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FORMATIONS
    // ─────────────────────────────────────────────────────────────────────────

    private void SpawnSingle()
    {
        float y = GetSafeSpawnY();
        if (!float.IsNaN(y))
        {
            SpawnEnemy(spawnX, y, GetRandomStopX());
        }
    }

    private void SpawnColumn(int count)
    {
        float centerY = GetSafeSpawnY();
        if (float.IsNaN(centerY)) return;

        float stopX = GetRandomStopX(); // Cả hàng sẽ dừng chung ở 1 mốc X (tạo thành bức tường)

        for (int i = 0; i < count; i++)
        {
            float offset = (i - count / 2) * minSpacingY;
            float y = centerY + offset;
            
            // Đảm bảo không bị lố ra ngoài màn hình
            if (y >= screenMinY && y <= screenMaxY)
            {
                SpawnEnemy(spawnX, y, stopX);
            }
        }
    }

    private void SpawnVShape()
    {
        float centerY = GetSafeSpawnY();
        if (float.IsNaN(centerY)) return;

        float leadStopX = GetRandomStopX();

        // Mũi nhọn (Con đi đầu, tiến sâu nhất vào trong màn hình)
        SpawnEnemy(spawnX, centerY, leadStopX);

        // 2 cánh (Trên và dưới)
        float wingY1 = centerY + minSpacingY;
        float wingY2 = centerY - minSpacingY;
        
        // Cánh lùi lại phía sau 1 khoảng (tọa độ X lớn hơn)
        float wingStopX = leadStopX + 1.5f; 
        
        // Spawn lùi lại ngoài màn hình 1 chút để tạo cảm giác bay hình chữ V ngay từ đầu
        float wingSpawnX = spawnX + 1.5f; 

        if (wingY1 <= screenMaxY) SpawnEnemy(wingSpawnX, wingY1, wingStopX);
        if (wingY2 >= screenMinY) SpawnEnemy(wingSpawnX, wingY2, wingStopX);
    }

    private void SpawnDiagonal(int count)
    {
        float centerY = GetSafeSpawnY();
        if (float.IsNaN(centerY)) return;

        float baseStopX = GetRandomStopX();

        for (int i = 0; i < count; i++)
        {
            float offset = (i - count / 2) * minSpacingY;
            float y = centerY + offset;
            
            // Xếp thành hình bậc thang chéo
            float stopX = baseStopX + i * 1.5f; 
            float currentSpawnX = spawnX + i * 1.5f;

            if (y >= screenMinY && y <= screenMaxY)
            {
                SpawnEnemy(currentSpawnX, y, stopX);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILS
    // ─────────────────────────────────────────────────────────────────────────

    private float GetRandomStopX()
    {
        float screenWidth = screenMaxX - screenMinX;
        float stopMinX    = screenMinX + screenWidth * minTargetXRatio;
        float stopMaxX    = screenMinX + screenWidth * maxTargetXRatio;
        return Random.Range(stopMinX, stopMaxX);
    }

    private void SpawnEnemy(float x, float y, float targetX)
    {
        Vector3 pos = new Vector3(x, y, 0f);
        GameObject obj = Instantiate(currentWavePrefab, pos, currentWavePrefab.transform.rotation);
        
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.targetX = targetX;
        }
    }

    private float GetSafeSpawnY()
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Hẹp lại 1 chút để đủ không gian cho các đội hình bám theo centerY
            float candidateY = Random.Range(screenMinY + minSpacingY, screenMaxY - minSpacingY);
            if (IsSafeY(candidateY)) return candidateY;
        }
        return float.NaN;
    }

    private bool IsSafeY(float candidateY)
    {
        Enemy[] existing = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in existing)
        {
            if (Mathf.Abs(e.transform.position.y - candidateY) < minSpacingY)
                return false;
        }
        return true;
    }

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

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(gizmoX, spawnMinY, 0f), new Vector3(gizmoX, spawnMaxY, 0f));

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Vector3 zoneCenter = new Vector3((bottomLeft.x + bottomRight.x) / 2f, (spawnMinY + spawnMaxY) / 2f, 0f);
        Vector3 zoneSize = new Vector3(bottomRight.x - bottomLeft.x, spawnMaxY - spawnMinY, 0f);
        Gizmos.DrawCube(zoneCenter, zoneSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(zoneCenter, zoneSize);
    }
}
