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
    private PlayerController cachedPlayer;
    private float nextCollisionTime = 0f;

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
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    private void Start()
    {
        cachedPlayer = FindAnyObjectByType<PlayerController>();
        if (GameManager.Instance != null && GameManager.Instance.EnemyHealthMultiplier > 1f)
        {
            maxHealth = Mathf.CeilToInt(maxHealth * GameManager.Instance.EnemyHealthMultiplier);
            currentHealth = maxHealth;
        }
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
        CheckPlayerOverlap();
        CheckOutOfBounds();
    }

    private void CheckPlayerOverlap()
    {
        if (cachedPlayer == null || Time.time < nextCollisionTime) return;

        float hitRadius = enemyType == EnemyType.Boss ? 1.5f : 0.7f;
        if (Vector2.Distance(transform.position, cachedPlayer.transform.position) <= hitRadius)
        {
            nextCollisionTime = Time.time + 0.5f;
            cachedPlayer.TakeDamage(1, enemyType);
            if (enemyType == EnemyType.Minion)
            {
                Die();
            }
            else
            {
                TakeDamage(5);
            }
        }
    }

    /// <summary>
    /// 依據類型處理移動邏輯
    /// </summary>
    private void HandleMovement()
    {
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        Vector3 forwardMove = isHorizontal ? Vector3.left : Vector3.down;

        switch (enemyType)
        {
            case EnemyType.Minion:
                if (cachedPlayer != null)
                {
                    Vector3 chaseDir = (cachedPlayer.transform.position - transform.position).normalized;
                    transform.Translate(chaseDir * (moveSpeed * Time.deltaTime), Space.World);
                }
                else
                {
                    transform.Translate(forwardMove * (moveSpeed * Time.deltaTime), Space.World);
                }
                break;

            case EnemyType.Elite:
                transform.Translate(forwardMove * (moveSpeed * Time.deltaTime), Space.World);
                break;

            case EnemyType.Boss:
                if (isHorizontal)
                {
                    if (transform.position.x > 3.2f)
                    {
                        transform.Translate(Vector3.left * (moveSpeed * Time.deltaTime), Space.World);
                    }
                    else
                    {
                        transform.Translate(Vector3.up * (patrolDirection * moveSpeed * Time.deltaTime), Space.World);
                        if (transform.position.y > 2.8f) patrolDirection = -1;
                        else if (transform.position.y < -2.8f) patrolDirection = 1;
                    }
                }
                else
                {
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
            nextFireTime = Time.time + (enemyType == EnemyType.Elite ? fireInterval * 0.55f : fireInterval);
            bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
            Vector2 baseDir = isHorizontal ? Vector2.left : Vector2.down;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)baseDir * 0.8f;

            if (enemyType == EnemyType.Elite)
            {
                // 精英怪：高頻率、較快且更密集的 3 向彈幕
                float[] eliteAngles = { -12f, 0f, 12f };
                foreach (float angle in eliteAngles)
                {
                    Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
                    SpawnBullet(spawnPos, dir, 1, 8.5f);
                }
            }
            else if (enemyType == EnemyType.Boss)
            {
                int pattern = Random.Range(0, 3);
                if (pattern == 0)
                {
                    // 模式一：9 向密集扇形彈幕
                    for (int a = -40; a <= 40; a += 10)
                    {
                        Vector2 dir = Quaternion.Euler(0, 0, a) * baseDir;
                        SpawnBullet(spawnPos, dir, 1, 5.5f);
                    }
                }
                else if (pattern == 1 && cachedPlayer != null)
                {
                    // 模式二：鎖定玩家方向的 5 向高速狙擊彈
                    Vector2 aimDir = (cachedPlayer.transform.position - spawnPos).normalized;
                    float[] aimAngles = { -12f, -6f, 0f, 6f, 12f };
                    foreach (float angle in aimAngles)
                    {
                        Vector2 dir = Quaternion.Euler(0, 0, angle) * aimDir;
                        SpawnBullet(spawnPos, dir, 1, 7.5f);
                    }
                }
                else
                {
                    // 模式三：20 向 360 度密集環形彈幕
                    for (int i = 0; i < 20; i++)
                    {
                        float angle = i * 18f;
                        Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
                        SpawnBullet(spawnPos, dir, 1, 4.5f);
                    }
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
            proj.SetSourceEnemyType(enemyType);
        }
    }

    /// <summary>
    /// 承受傷害
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (enemyType == EnemyType.Boss)
        {
            bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
            bool isInsideBounds = isHorizontal ? transform.position.x <= 3.2f : transform.position.y <= 3.2f;
            if (!isInsideBounds) return;
        }

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
                SpawnBossDefeatEffect(transform.position);
                foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                {
                    if (enemy != null && enemy != this && enemy.Type != EnemyType.Boss)
                    {
                        Destroy(enemy.gameObject);
                    }
                }
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
            bool blockHeal = GameManager.Instance != null && GameManager.Instance.SelectedSkill == SkillType.GlassCannon;
            System.Collections.Generic.List<GameObject> candidates = new System.Collections.Generic.List<GameObject>();
            foreach (var prefab in dropItemPrefabs)
            {
                if (prefab == null) continue;
                if (blockHeal)
                {
                    ItemPickup pickup = prefab.GetComponent<ItemPickup>();
                    if (pickup != null && pickup.Type == ItemType.Heal) continue;
                }
                candidates.Add(prefab);
            }

            if (candidates.Count == 0) return;
            GameObject itemPrefab = candidates[Random.Range(0, candidates.Count)];
            if (itemPrefab != null)
            {
                Instantiate(itemPrefab, transform.position, Quaternion.identity);
                Debug.Log($"<color=#FFD700>【道具掉落】{gameObject.name} 掉落了道具：{itemPrefab.name}</color>");
            }
        }
    }

    public static GameObject[] CachedDropItems;

    /// <summary>
    /// 超出邊界時自動銷毀 (第二關橫式不超出左邊界)
    /// </summary>
    private void CheckOutOfBounds()
    {
        if (enemyType == EnemyType.Boss) return;

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        if (isHorizontal)
        {
            float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;
            if (transform.position.x <= -halfW)
            {
                Destroy(gameObject);
            }
        }
        else if (transform.position.y < -7f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 生成 Boss 擊敗爆破衝擊波特效 (兩關通用)
    /// </summary>
    private void SpawnBossDefeatEffect(Vector3 center)
    {
        GameObject fxObj = new GameObject("BossDefeatFX");
        fxObj.transform.position = center;
        BossDefeatFX fx = fxObj.AddComponent<BossDefeatFX>();
        fx.Play();
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
        else
        {
            PlayerController player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(1, enemyType);
                if (enemyType == EnemyType.Minion)
                {
                    Die();
                }
                else
                {
                    TakeDamage(5);
                }
            }
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

/// <summary>
/// Boss 擊敗時的擴散爆破衝擊波特效 (支援慢動作結算)
/// </summary>
public class BossDefeatFX : MonoBehaviour
{
    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    private System.Collections.IEnumerator PlayRoutine()
    {
        int ringCount = 3;
        LineRenderer[] rings = new LineRenderer[ringCount];
        Color[] colors = new Color[]
        {
            new Color(1f, 0.9f, 0.3f, 1f),
            new Color(1f, 0.4f, 0.1f, 1f),
            new Color(1f, 0.15f, 0.2f, 1f)
        };

        Material mat = new Material(Shader.Find("Sprites/Default"));
        int segments = 48;

        for (int r = 0; r < ringCount; r++)
        {
            GameObject ringObj = new GameObject($"Ring_{r}");
            ringObj.transform.SetParent(transform, false);
            LineRenderer lr = ringObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.material = mat;
            lr.sortingOrder = 25;
            rings[r] = lr;
        }

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int r = 0; r < ringCount; r++)
            {
                float delay = r * 0.15f;
                float rt = Mathf.Clamp01((elapsed - delay) / (duration - delay));
                float radius = Mathf.Lerp(0.3f, 5.5f + r * 1.5f, rt);
                float alpha = 1f - rt;

                LineRenderer lr = rings[r];
                Color c = colors[r];
                c.a = alpha;
                lr.startColor = c;
                lr.endColor = c;
                lr.startWidth = 0.25f * (1f - rt * 0.7f);
                lr.endWidth = lr.startWidth;

                for (int i = 0; i < segments; i++)
                {
                    float angle = (i / (float)segments) * Mathf.PI * 2f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
