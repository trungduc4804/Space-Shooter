using UnityEngine;

/// <summary>
/// BackgroundScroller – Phần G theo GDD:
/// "Infinite side-scrolling shooter"
///
/// Dùng 2 background sprite đặt cạnh nhau.
/// Khi sprite nào trôi ra ngoài bên trái → tức thì dịch nó ra sau sprite còn lại.
/// → Tạo hiệu ứng cuộn nền vô tận.
///
/// Setup trong Unity:
///   1. Tạo 2 GameObject Sprite (cùng hình nền), đặt tên bg0 và bg1
///   2. Gán cả 2 vào mảng "backgrounds" trong Inspector
///   3. Căn chỉnh sao cho bg0 phủ toàn màn hình, bg1 nằm ngay bên phải bg0
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 2f; // tốc độ cuộn sang trái

    [Header("Background Sprites")]
    [SerializeField] private Transform[] backgrounds; // gán 2 sprite background vào đây

    // ── Internal ──────────────────────────────────────────────────────────────

    private float bgWidth;         // chiều rộng 1 background tính theo world unit
    private float leftEdgeX;       // X của cạnh trái màn hình (world space)

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (backgrounds == null || backgrounds.Length < 2)
        {
            Debug.LogError("BackgroundScroller: cần gán ít nhất 2 background vào mảng 'backgrounds'!");
            enabled = false;
            return;
        }

        // Tính chiều rộng sprite từ SpriteRenderer
        SpriteRenderer sr = backgrounds[0].GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            bgWidth = sr.bounds.size.x;
        }
        else
        {
            // Fallback: lấy chiều rộng màn hình
            Camera cam = Camera.main;
            Vector3 leftWorld  = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector3 rightWorld = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f));
            bgWidth = rightWorld.x - leftWorld.x;
        }

        // X của cạnh trái màn hình
        leftEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;

        // Đặt vị trí ban đầu: bg0 phủ màn hình, bg1 ngay sau bg0
        AlignBackgrounds();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Di chuyển tất cả sang trái
        Vector3 delta = Vector3.left * scrollSpeed * Time.deltaTime;
        foreach (Transform bg in backgrounds)
        {
            bg.position += delta;
        }

        // Kiểm tra xem sprite nào đã ra khỏi màn hình bên trái
        RecycleBackgrounds();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Đặt bg0 tại trung tâm màn hình, bg1 ngay sau bg0.
    /// Gọi 1 lần lúc Start để căn vị trí đúng.
    /// </summary>
    private void AlignBackgrounds()
    {
        Camera cam        = Camera.main;
        Vector3 center    = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));

        // bg0 căn giữa màn hình
        backgrounds[0].position = new Vector3(center.x, center.y, backgrounds[0].position.z);

        // bg1 đặt ngay bên phải bg0
        backgrounds[1].position = new Vector3(
            backgrounds[0].position.x + bgWidth,
            center.y,
            backgrounds[1].position.z
        );
    }

    /// <summary>
    /// Nếu cạnh phải của một sprite đã ra khỏi cạnh trái màn hình
    /// → dịch nó sang phải của sprite kia.
    /// </summary>
    private void RecycleBackgrounds()
    {
        foreach (Transform bg in backgrounds)
        {
            // Cạnh phải của sprite này (tính theo thế giới)
            float rightEdge = bg.position.x + bgWidth * 0.5f;

            if (rightEdge < leftEdgeX)
            {
                // Tìm sprite còn lại có X lớn nhất
                float maxX = float.MinValue;
                foreach (Transform other in backgrounds)
                {
                    if (other != bg && other.position.x > maxX)
                        maxX = other.position.x;
                }

                // Đặt sprite này ngay sau sprite có X lớn nhất
                bg.position = new Vector3(maxX + bgWidth, bg.position.y, bg.position.z);
            }
        }
    }

    // ── Editor Gizmo ─────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (backgrounds == null) return;
        Gizmos.color = Color.green;
        foreach (Transform bg in backgrounds)
        {
            if (bg == null) continue;
            Gizmos.DrawWireCube(bg.position, new Vector3(bgWidth > 0 ? bgWidth : 10f, 6f, 0f));
        }
    }
}
