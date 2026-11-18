using UnityEngine;

/// <summary>
/// 背景無限垂直循環捲動器
/// 透過 UV 貼圖偏移或雙精靈垂直無縫循環位移
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("捲動設定")]
    [Tooltip("背景向下捲動速度 (單位/秒)")]
    [SerializeField] private float scrollSpeed = 1.8f;

    [Tooltip("背景單張高度 (Tile Size)")]
    [SerializeField] private float backgroundHeight = 20f;

    private Transform bg1;
    private Transform bg2;

    private void Awake()
    {
        // 尋找或建立兩塊背景子物件進行交替循環
        if (transform.childCount >= 2)
        {
            bg1 = transform.GetChild(0);
            bg2 = transform.GetChild(1);
        }
    }

    public void SetupChildren(Transform first, Transform second, float height)
    {
        bg1 = first;
        bg2 = second;
        backgroundHeight = height;
    }

    private void Update()
    {
        if (bg1 == null || bg2 == null) return;

        // 向下平移
        float delta = scrollSpeed * Time.deltaTime;
        bg1.position += Vector3.down * delta;
        bg2.position += Vector3.down * delta;

        // 當某一塊完全移出下方時，傳送接回上方
        if (bg1.position.y <= -backgroundHeight)
        {
            bg1.position = new Vector3(bg1.position.x, bg2.position.y + backgroundHeight, bg1.position.z);
        }
        if (bg2.position.y <= -backgroundHeight)
        {
            bg2.position = new Vector3(bg2.position.x, bg1.position.y + backgroundHeight, bg2.position.z);
        }
    }
}
