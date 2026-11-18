using UnityEngine;

/// <summary>
/// 敵機類型列舉
/// </summary>
public enum EnemyType
{
    Minion, // 小型偵察機 (小怪)：血量低、速度快、直線向下，可被炸彈秒殺
    Elite,  // 重型砲艇 (精英怪)：血量中、速度慢、定時向下開火，可被炸彈秒殺
    Boss    // 關卡首領 (Boss)：血量高、左右巡邏、扇形彈幕、免疫炸彈秒殺 (僅扣固定大量血量)
}

/// <summary>
/// 通用敵機控制器
/// 負責：血量、移動、射擊、受傷、響應全屏炸彈與死亡結算
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    [Header("敵機屬性")]
    [Tooltip("敵機類型")]
    [SerializeField] private EnemyType enemyType = EnemyType.Minion;

    [Tooltip("最大生命值")]
    [SerializeField] private int maxHealth = 2;

    [Tooltip("移動速度")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("擊殺與掉落設定")]
    [Tooltip("擊殺獲得分數")]
    [SerializeField] private int scoreValue = 100;

    [Tooltip("擊毀時道具掉落機率 (0 ~ 1)")]
    [Range(0f, 1f)]
    [SerializeField] private float dropRate = 0.35f;

    [Tooltip("可掉落的 3 種道具 Prefabs (PowerUp, WeaponSwitch, Heal)")]
    [SerializeField] private GameObject[] dropItemPrefabs;

    [Header("射擊設定 (精英怪/Boss)")]
    [Tooltip("是否會開火")]
    [SerializeField] private bool canShoot = false;

    [Tooltip("開火冷卻間隔 (秒)")]
    [SerializeField] private float fireInterval = 2f;

    [Tooltip("敵方子彈 Prefab")]
    [SerializeField] private GameObject enemyBulletPrefab;

    [Tooltip("子彈發射點")]
    [SerializeField] private Transform firePoint;

    // 運行狀態
    private int currentHealth;
    private float nextFireTime;
    private int patrolDirection = 1; // Boss 左右巡邏方向
    private Camera mainCamera;
    private SpriteRenderer[] spriteRenderers;

    public EnemyType Type => enemyType;

    private void Awake()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        // 自動加上 Rigidbody2D 防止碰撞遺漏
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.isKinematic = true;
    }

    private void Start()
    {
        nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
    }

    private void OnEnable()
    {
        GameManager.OnBombFired += HandleBombFired;
    }

    private void OnDisable()
    {
        GameManager.OnBombFired -= HandleBombFired;
    }

    private void Update()
    {
        HandleMovement();
        HandleShooting();
        CheckOutOfBounds();
    }

    /// <summary>
    /// 依據類型處理移動邏輯
    /// </summary>
    private void HandleMovement()
    {
        switch (enemyType)
        {
            case EnemyType.Minion:
                // 小怪：直線向下快速俯衝
                transform.Translate(Vector3.down * (moveSpeed * Time.deltaTime), Space.World);
                break;

            case EnemyType.Elite:
                // 精英怪：緩速向下推進
                transform.Translate(Vector3.down * (moveSpeed * Time.deltaTime), Space.World);
                break;

            case EnemyType.Boss:
                // Boss：進場移動到上方定位後，開始左右平移巡邏
                if (transform.position.y > 3.2f)
                {
                    transform.Translate(Vector3.down * (moveSpeed * Time.deltaTime), Space.World);
                }
                else
                {
                    transform.Translate(Vector3.right * (patrolDirection * moveSpeed * Time.deltaTime), Space.World);
                    if (transform.position.x > 2.8f)
                    {
                        patrolDirection = -1;
                    }
                    else if (transform.position.x < -2.8f)
                    {
                        patrolDirection = 1;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 處理射擊邏輯
    /// </summary>
    private void HandleShooting()
    {
        if (!canShoot || enemyBulletPrefab == null) return;

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireInterval;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.down * 0.8f;

            if (enemyType == EnemyType.Elite)
            {
                // 精英怪：定時向下發射直線單發
                SpawnBullet(spawnPos, Vector2.down, 1, 6f);
            }
            else if (enemyType == EnemyType.Boss)
            {
                // Boss：定時發射 5 向扇形彈幕
                float[] angles = { -30f, -15f, 0f, 15f, 30f };
                foreach (float angle in angles)
                {
                    Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.down;
                    SpawnBullet(spawnPos, dir, 1, 5f);
                }
            }
        }
    }

    private void SpawnBullet(Vector3 pos, Vector2 dir, int damage, float speed)
    {
        GameObject bullet = Instantiate(enemyBulletPrefab, pos, Quaternion.identity);
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(Projectile.Faction.Enemy, damage, speed, dir, false);
        }
    }

    /// <summary>
    /// 承受傷害
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        StartCoroutine(FlashWhiteRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashWhiteRoutine()
    {
        foreach (var sr in spriteRenderers)
        {
            if (sr != null) sr.color = Color.red;
        }
        yield return new WaitForSeconds(0.08f);
        foreach (var sr in spriteRenderers)
        {
            if (sr != null) sr.color = Color.white;
        }
    }

    /// <summary>
    /// 響應 C 鍵全屏炸彈廣播事件
    /// </summary>
    private void HandleBombFired(int bombDamage)
    {
        if (enemyType == EnemyType.Boss)
        {
            // Boss 免疫秒殺，受到固定高額傷害 (如 50 點)
            Debug.Log($"<color=#FF9900>【Boss 受創】受到全屏衝擊波固定重創 {bombDamage} 點！</color>");
            TakeDamage(bombDamage);
        }
        else
        {
            // 小型偵察機與重型砲艇直接一鍵清空秒殺
            Die();
        }
    }

    /// <summary>
    /// 敵機死亡結算
    /// </summary>
    private void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
            if (enemyType == EnemyType.Boss)
            {
                GameManager.Instance.OnBossKilled();
            }
        }

        // 依機率掉落 3 種道具之一
        TryDropItem();

        Debug.Log($"<color=#00FFAA>【敵機擊毀】{gameObject.name} 被摧毀！增加分數: {scoreValue}</color>");
        Destroy(gameObject);
    }

    /// <summary>
    /// 依照機率隨機掉落 3 種道具之一
    /// </summary>
    private void TryDropItem()
    {
        // 若 Inspector 尚未綁定，動態從 Resources / AssetDatabase / Resources 資料庫獲取
        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0)
        {
            // 動態備份加載
            dropItemPrefabs = Resources.LoadAll<GameObject>("Prefabs/Items");
        }

        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0)
        {
            // 嘗試從當前靜態快取中提取
            dropItemPrefabs = CachedDropItems;
        }

        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0) return;

        // 小怪與精英怪掉落機率提高至 45%，Boss 擊毀固定 100% 掉落
        float roll = Random.value;
        if (enemyType == EnemyType.Boss || roll <= dropRate)
        {
            int index = Random.Range(0, dropItemPrefabs.Length);
            GameObject itemPrefab = dropItemPrefabs[index];
            if (itemPrefab != null)
            {
                Instantiate(itemPrefab, transform.position, Quaternion.identity);
                Debug.Log($"<color=#FFD700>【道具掉落】{gameObject.name} 掉落了道具：{itemPrefab.name}</color>");
            }
        }
    }

    public static GameObject[] CachedDropItems;

    /// <summary>
    /// 超出螢幕下方邊界時自動銷毀
    /// </summary>
    private void CheckOutOfBounds()
    {
        if (enemyType != EnemyType.Boss && transform.position.y < -7f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 被玩家子彈擊中：優先由 Projectile 組件判定陣營，輔以安全標籤檢查，防止未定義 Tag 時崩潰
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            if (proj.BulletFaction == Projectile.Faction.Player)
            {
                TakeDamage(proj.Damage);
            }
        }
        else if (SafeCheckTag(other, "PlayerBullet"))
        {
            TakeDamage(1);
        }
    }

    private static bool SafeCheckTag(Component comp, string tag)
    {
        try
        {
            return comp.CompareTag(tag);
        }
        catch
        {
            return false;
        }
    }
}
