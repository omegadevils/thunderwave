using UnityEngine;

/// <summary>
/// 全屏炸彈衝擊波視覺特效
/// 從玩家位置向外迅速擴散光環並淡出，伴隨全螢幕白色閃光效果
/// </summary>
public class ShockwaveEffect : MonoBehaviour
{
    [Header("擴散設定")]
    [Tooltip("衝擊波擴散持續時間 (秒)")]
    [SerializeField] private float duration = 0.75f;

    [Tooltip("衝擊波最大半徑 (Scale)")]
    [SerializeField] private float maxRadius = 35f;

    [Tooltip("光環顏色 (起始色)")]
    [SerializeField] private Color ringColor = new Color(0.3f, 0.8f, 1f, 0.9f); // 科技亮藍青色

    private SpriteRenderer spriteRenderer;
    private float timer = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 起始半徑設為極小
        transform.localScale = Vector3.zero;
        spriteRenderer.color = ringColor;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);

        // 使用 Ease-Out 曲線擴散 (初期快、後期平滑減速)
        float currentScale = Mathf.Lerp(0f, maxRadius, Mathf.Sin(progress * Mathf.PI * 0.5f));
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        // 隨時間線性透明淡出
        Color c = ringColor;
        c.a = Mathf.Lerp(ringColor.a, 0f, progress);
        spriteRenderer.color = c;

        // 時間結束自動銷毀
        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
