using UnityEngine;

/// <summary>
/// 遊戲直式畫面活動邊界顯示器 (固定 9:16 直式比例)
/// 無論 Game View 當前是 16:9 還是寬螢幕，強制在中央劃定 9:16 的直式遊玩區域與邊界線框
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BoundaryVisualizer : MonoBehaviour
{
    [Header("邊界外觀設定")]
    [Tooltip("線條寬度")]
    [SerializeField] private float lineWidth = 0.016f;

    [Tooltip("線條顏色 (科技亮青藍色)")]
    [SerializeField] private Color boundaryColor = new Color(0.2f, 0.8f, 1f, 0.7f);

    [Header("直式比例設定")]
    [Tooltip("目標寬高比 (9:16 = 0.5625)")]
    [SerializeField] private float targetAspectRatio = 9f / 16f;

    private LineRenderer lineRenderer;
    private Camera mainCamera;

    public float TargetAspectRatio => targetAspectRatio;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 5;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = lineMat;
        lineRenderer.startColor = boundaryColor;
        lineRenderer.endColor = boundaryColor;
        lineRenderer.sortingOrder = 30;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }

    private void Start()
    {
        UpdateBoundaryLine();
    }

    private void LateUpdate()
    {
        UpdateBoundaryLine();
    }

    /// <summary>
    /// 計算並繪製中央 9:16 的直式邊界線
    /// </summary>
    public void UpdateBoundaryLine()
    {
        if (mainCamera == null || lineRenderer == null) return;

        float verticalHeight = mainCamera.orthographic ? mainCamera.orthographicSize : 6.5f;
        // 固定以直式 9:16 計算寬度，而非依賴寬螢幕的 aspect
        float horizontalWidth = verticalHeight * targetAspectRatio;
        Vector3 camPos = mainCamera.transform.position;

        float left = camPos.x - horizontalWidth;
        float right = camPos.x + horizontalWidth;
        float bottom = camPos.y - verticalHeight;
        float top = camPos.y + verticalHeight;
        float z = 0f;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.SetPosition(0, new Vector3(left, bottom, z));
        lineRenderer.SetPosition(1, new Vector3(left, top, z));
        lineRenderer.SetPosition(2, new Vector3(right, top, z));
        lineRenderer.SetPosition(3, new Vector3(right, bottom, z));
        lineRenderer.SetPosition(4, new Vector3(left, bottom, z));
    }

    /// <summary>
    /// 提供給 PlayerController 取得直式 9:16 邊界範圍
    /// </summary>
    public Vector4 GetPortraitBounds()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        float verticalHeight = (mainCamera != null && mainCamera.orthographic) ? mainCamera.orthographicSize : 6.5f;
        float horizontalWidth = verticalHeight * targetAspectRatio;
        Vector3 camPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;

        return new Vector4(camPos.x - horizontalWidth, camPos.x + horizontalWidth, camPos.y - verticalHeight, camPos.y + verticalHeight);
    }
}
