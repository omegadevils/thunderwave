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

    public static BackgroundScroller Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
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

    [Header("橫式關卡背景設定")]
    [Tooltip("第二關橫式背景半寬 (總寬度 = 半寬 * 2)")]
    [SerializeField] private float horizontalHalfWidth = 8.0f;

    private bool wasHorizontal = false;
    private GameObject horizontalQuad;
    private Renderer horizontalQuadRenderer;

    public float GetBackgroundHalfWidth()
    {
        return horizontalHalfWidth;
    }

    private void Update()
    {
        if (bg1 == null || bg2 == null) return;

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        float delta = scrollSpeed * Time.deltaTime;

        if (isHorizontal)
        {
            if (!wasHorizontal)
            {
                wasHorizontal = true;
                bg1.gameObject.SetActive(false);
                if (bg2 != null) bg2.gameObject.SetActive(false);
                EnsureHorizontalQuad();
            }

            if (horizontalQuadRenderer != null && horizontalQuadRenderer.material != null)
            {
                Vector2 offset = horizontalQuadRenderer.material.mainTextureOffset;
                offset.x += (scrollSpeed * 0.08f) * Time.deltaTime;
                horizontalQuadRenderer.material.mainTextureOffset = offset;
            }
            return;
        }

        if (wasHorizontal)
        {
            wasHorizontal = false;
            bg1.gameObject.SetActive(true);
            if (bg2 != null) bg2.gameObject.SetActive(true);
            if (horizontalQuad != null) horizontalQuad.SetActive(false);
            bg1.position = new Vector3(0f, 0f, bg1.position.z);
            bg2.position = new Vector3(0f, backgroundHeight, bg2.position.z);
        }

        // 向下平移
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

    private void EnsureHorizontalQuad()
    {
        if (horizontalQuad != null)
        {
            horizontalQuad.SetActive(true);
            return;
        }

        horizontalQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        horizontalQuad.name = "HorizontalBgQuad";
        horizontalQuad.transform.SetParent(transform, false);
        Destroy(horizontalQuad.GetComponent<Collider>());

        Camera cam = Camera.main;
        float height = (cam != null && cam.orthographic) ? cam.orthographicSize * 2f : 13f;
        horizontalQuad.transform.position = new Vector3(0f, 0f, bg1.position.z);
        horizontalQuad.transform.localScale = new Vector3(horizontalHalfWidth * 2f, height, 1f);

        horizontalQuadRenderer = horizontalQuad.GetComponent<Renderer>();
        Shader unlitShader = Shader.Find("Unlit/Texture");
        if (unlitShader != null)
        {
            horizontalQuadRenderer.material = new Material(unlitShader);
        }

        SpriteRenderer sr = bg1.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null && sr.sprite.texture != null)
        {
            sr.sprite.texture.wrapMode = TextureWrapMode.Repeat;
            horizontalQuadRenderer.material.mainTexture = sr.sprite.texture;
        }
    }
}
