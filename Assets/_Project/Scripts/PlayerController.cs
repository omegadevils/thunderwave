using UnityEngine;

/// <summary>
/// 武器類型列舉
/// </summary>
public enum WeaponType
{
    Vulcan, // 火神機砲 (紅色直線)
    Spread, // 散射雷射 (藍色扇形)
    Plasma  // 重型電漿 (綠色高傷)
}

/// <summary>
/// 玩家戰鬥機控制器
/// 負責處理：移動、邊界限制、普通連發射擊、蓄力雷射攻擊 (按住 > 1.0 秒)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("戰機移動速度 (單位/秒)")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("戰機邊界安全距離 (padding，防止機身貼緊螢幕邊緣)")]
    [SerializeField] private Vector2 padding = new Vector2(0.8f, 0.8f);

    [Tooltip("玩家初始出生位置 (若有勾選重置)")]
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0f, -4.5f, 0f);

    [Header("武器與射擊設定")]
    [Tooltip("當前武器類型")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Vulcan;

    [Tooltip("火神機砲等級 (Lv.1 ~ Lv.5)")]
    [Range(1, 5)]
    [SerializeField] private int vulcanLevel = 1;

    [Tooltip("散射雷射等級 (Lv.1 ~ Lv.5)")]
    [Range(1, 5)]
    [SerializeField] private int spreadLevel = 1;

    [Tooltip("重型電漿等級 (Lv.1 ~ Lv.5)")]
    [Range(1, 5)]
    [SerializeField] private int plasmaLevel = 1;

    [Tooltip("普通射擊冷卻時間 (秒)")]
    [SerializeField] private float fireRate = 0.15f;

    [Tooltip("普通子彈 Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("子彈發射點 (未指定則使用玩家本體位置)")]
    [SerializeField] private Transform firePoint;

    [Header("蓄力攻擊設定 (Charge Shot)")]
    [Tooltip("蓄力所需時間 (秒)")]
    [SerializeField] private float chargeTimeRequired = 1.0f;

    [Tooltip("蓄力貫穿雷射 Prefab")]
    [SerializeField] private GameObject chargeLaserPrefab;

    [Tooltip("蓄力完成時的縮放/閃爍比例")]
    [SerializeField] private float chargeFlashFrequency = 15f;

    [Header("全屏炸彈設定 (Bomb)")]
    [Tooltip("開局自帶炸彈使用次數 (整場最多 3 次)")]
    [SerializeField] private int bombCount = 3;

    [Tooltip("全屏炸彈視覺震撼光波 Prefab (選填)")]
    [SerializeField] private GameObject bombEffectPrefab;

    [Header("生命值與無敵機制 (Health & Invulnerability)")]
    [Tooltip("最大/初始生命值")]
    [SerializeField] private int maxHealth = 3;

    [Tooltip("受傷後無敵保護時間 (秒)")]
    [SerializeField] private float invulnerableDuration = 1.5f;

    [Tooltip("無敵期間戰機閃爍頻率")]
    [SerializeField] private float invulnerableFlashFrequency = 12f;

    // 狀態變數
    private float nextFireTime = 0f;
    private float chargeTimer = 0f;
    private bool isCharging = false;
    private Vector3 initialScale;
    private SpriteRenderer[] spriteRenderers;

    // 生命與無敵狀態
    private int currentHealth;
    private bool isInvulnerable = false;
    private float invulnerableTimer = 0f;

    // 組件引用
    private Rigidbody2D rb;
    private Camera mainCamera;

    // 移動輸入與邊界限制
    private Vector2 moveInput;
    private Vector2 minBounds;
    private Vector2 maxBounds;

    // 公開屬性供 UI 與遊戲系統讀取
    public WeaponType CurrentWeapon => currentWeapon;
    public int CurrentWeaponLevel
    {
        get
        {
            switch (currentWeapon)
            {
                case WeaponType.Vulcan: return vulcanLevel;
                case WeaponType.Spread: return spreadLevel;
                case WeaponType.Plasma: return plasmaLevel;
                default: return 1;
            }
        }
    }
    public int WeaponLevel => CurrentWeaponLevel; // 相容現有介面
    public int VulcanLevel => vulcanLevel;
    public int SpreadLevel => spreadLevel;
    public int PlasmaLevel => plasmaLevel;
    public float ChargeProgress => Mathf.Clamp01(chargeTimer / chargeTimeRequired);
    public float BombCooldownRemaining => Mathf.Max(0f, nextBombTime - Time.time);
    public bool IsChargeReady => chargeTimer >= chargeTimeRequired;
    public int BombCount => bombCount;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable => isInvulnerable;

    public static System.Action<int, int> OnHealthChanged; // (currentHp, maxHp)
    public static System.Action OnPlayerDied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        initialScale = transform.localScale;
        currentHealth = maxHealth;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }
    }

    private void Start()
    {
        transform.position = defaultSpawnPosition;
        UpdateCameraBounds();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            moveInput = Vector2.zero;
            return;
        }

        HandleInput();
        HandleAutoChargeAndWingmen();
        HandleShootingAndCharging();
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        transform.rotation = isHorizontal ? Quaternion.Euler(0, 0, -90f) : Quaternion.identity;
        HandleBombInput();
        HandleInvulnerability();
        HandleDebugWeaponSwitch();
    }

    /// <summary>
    /// 鍵盤除錯快速測試：按 1~3 切換武器、按 4/5 升降當前裝備武器的等級
    /// </summary>
    private void HandleDebugWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { currentWeapon = WeaponType.Vulcan; Debug.Log($"<color=#FF4444>【武器】切換為火神機砲 (Vulcan) Lv.{vulcanLevel}</color>"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { currentWeapon = WeaponType.Spread; Debug.Log($"<color=#44CCFF>【武器】切換為散射雷射 (Spread) Lv.{spreadLevel}</color>"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { currentWeapon = WeaponType.Plasma; Debug.Log($"<color=#44FF44>【武器】切換為重型電漿 (Plasma) Lv.{plasmaLevel}</color>"); }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            DowngradeCurrentWeapon();
            Debug.Log($"【火力降級】當前武器降為 Lv.{CurrentWeaponLevel}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            UpgradeWeapon();
        }
    }

    public void DowngradeCurrentWeapon()
    {
        switch (currentWeapon)
        {
            case WeaponType.Vulcan: if (vulcanLevel > 1) vulcanLevel--; break;
            case WeaponType.Spread: if (spreadLevel > 1) spreadLevel--; break;
            case WeaponType.Plasma: if (plasmaLevel > 1) plasmaLevel--; break;
        }
    }

    private void OnEnable()
    {
        GameManager.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(int stage)
    {
        UpdateCameraBounds();
        if (stage >= 2)
        {
            ResetForNewStage();
        }
    }

    /// <summary>
    /// 進入新關卡時重置所有武器等級與玩家狀態 (HP、炸彈、蓄力、無敵)
    /// </summary>
    public void ResetForNewStage()
    {
        currentWeapon = WeaponType.Vulcan;
        vulcanLevel = 1;
        spreadLevel = 1;
        plasmaLevel = 1;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        bombCount = 3;
        nextBombTime = 0f;
        chargeTimer = 0f;
        isCharging = false;
        isInvulnerable = false;
        invulnerableTimer = 0f;
        SetSpriteAlpha(1f);

        Debug.Log("<color=#00CCFF>【狀態重置】已重置所有武器等級、HP 與炸彈次數，迎接新關卡！</color>");
    }

    /// <summary>
    /// 套用第二關選擇的強化技能
    /// </summary>
    public void ApplySkill(SkillType skill)
    {
        activeSkill = skill;
        damageMultiplier = 1;
        autoChargeEnabled = false;
        ClearWingmen();

        switch (skill)
        {
            case SkillType.Wingmen:
                SpawnWingmen();
                break;
            case SkillType.AutoCharge:
                autoChargeEnabled = true;
                break;
            case SkillType.GlassCannon:
                damageMultiplier = 3;
                currentHealth = 1;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                break;
        }
    }

    private void ClearWingmen()
    {
        foreach (var w in wingmen)
        {
            if (w != null) Destroy(w);
        }
        wingmen.Clear();
    }

    private void SpawnWingmen()
    {
        Sprite playerSprite = null;
        if (spriteRenderers != null && spriteRenderers.Length > 0 && spriteRenderers[0] != null)
        {
            playerSprite = spriteRenderers[0].sprite;
        }

        Vector3[] offsets = new Vector3[] { new Vector3(-0.65f, -0.15f, 0f), new Vector3(0.65f, -0.15f, 0f) };
        for (int i = 0; i < offsets.Length; i++)
        {
            SpawnSingleWingman(offsets[i], i + 1);
        }
    }

    private void SpawnSingleWingman(Vector3 localOffset, int index)
    {
        Sprite playerSprite = null;
        if (spriteRenderers != null && spriteRenderers.Length > 0 && spriteRenderers[0] != null)
        {
            playerSprite = spriteRenderers[0].sprite;
        }

        GameObject w = new GameObject($"Wingman_{index}");
        w.transform.SetParent(transform, false);
        w.transform.localPosition = localOffset;
        w.transform.localScale = Vector3.one * 0.45f;

        SpriteRenderer sr = w.AddComponent<SpriteRenderer>();
        sr.sprite = playerSprite;
        sr.color = new Color(0.4f, 0.9f, 1f, 0.95f);
        sr.sortingOrder = 19;

        wingmen.Add(w);
    }

    private System.Collections.IEnumerator RestoreWingmanAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeSkill == SkillType.Wingmen && wingmen.Count < 2 && currentHealth > 0)
        {
            Vector3 offset = wingmen.Count == 0 ? new Vector3(-0.65f, -0.15f, 0f) : new Vector3(0.65f, -0.15f, 0f);
            SpawnSingleWingman(offset, wingmen.Count + 1);
            Debug.Log($"<color=#00FFCC>【輔助戰機恢復】已重新部署 1 台輔助戰機！目前：{wingmen.Count} 台</color>");
        }
    }

    private void HandleAutoChargeAndWingmen()
    {
        if (autoChargeEnabled)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= chargeTimeRequired)
            {
                FireChargeLaser();
                chargeTimer = 0f;
            }
        }

        if (wingmen.Count > 0)
        {
            Vector3[] offsets = new Vector3[] { new Vector3(-0.65f, -0.15f, 0f), new Vector3(0.65f, -0.15f, 0f) };
            for (int i = 0; i < wingmen.Count; i++)
            {
                GameObject w = wingmen[i];
                if (w == null) continue;
                w.transform.localPosition = offsets[Mathf.Min(i, offsets.Length - 1)];
                Vector3 worldPos = w.transform.position;
                worldPos.x = Mathf.Clamp(worldPos.x, minBounds.x, maxBounds.x);
                worldPos.y = Mathf.Clamp(worldPos.y, minBounds.y, maxBounds.y);
                w.transform.position = worldPos;
            }
        }

        if (wingmen.Count > 0 && bulletPrefab != null && Time.time >= nextWingmanFireTime)
        {
            nextWingmanFireTime = Time.time + 0.6f;
            foreach (var w in wingmen)
            {
                if (w != null)
                {
                    SpawnBullet(w.transform.position + transform.up * 0.4f, (Vector2)transform.up, 1, 12f, new Vector3(0.22f, 0.45f, 1f), Color.cyan);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        MovePlayer();
    }

    /// <summary>
    /// 讀取移動輸入 (WASD / 方向鍵)
    /// </summary>
    private void HandleInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;
    }

    /// <summary>
    /// 處理受傷後的無敵時間計時與視覺閃爍提示
    /// </summary>
    private void HandleInvulnerability()
    {
        if (!isInvulnerable) return;

        invulnerableTimer -= Time.deltaTime;

        // 無敵期間透明度高頻閃爍 (半透明 <-> 不透明)
        float alpha = Mathf.PingPong(Time.time * invulnerableFlashFrequency, 0.7f) + 0.3f;
        SetSpriteAlpha(alpha);

        if (invulnerableTimer <= 0f)
        {
            isInvulnerable = false;
            SetSpriteAlpha(1f); // 恢復完全不透明
            Debug.Log("<color=#FFFFFF>【無敵結束】戰機保護光罩消退，恢復正常判定。</color>");
        }
    }

    private void SetSpriteAlpha(float alpha)
    {
        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                sr.sortingOrder = 20;
            }
        }
        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    private EnemyType lastAttackerType = EnemyType.Minion;
    private SkillType activeSkill = SkillType.None;
    private int damageMultiplier = 1;
    private bool autoChargeEnabled = false;
    private readonly System.Collections.Generic.List<GameObject> wingmen = new System.Collections.Generic.List<GameObject>();
    private float nextWingmanFireTime = 0f;
    private float bombCooldown = 5.0f;
    private float nextBombTime = 0f;
    private float silenceEndTime = 0f;

    public void SilenceShooting(float duration)
    {
        silenceEndTime = Mathf.Max(silenceEndTime, Time.time + duration);
        Debug.Log($"<color=#FFCC00>【干擾陷阱】武器系統遭電磁干擾，無法射擊 {duration} 秒！</color>");
    }

    /// <summary>
    /// 受到傷害 (扣除 HP 並觸發 1.5 秒無敵保護)
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, EnemyType.Minion);
    }

    public void TakeDamage(int damage, EnemyType attackerType)
    {
        // 若非遊玩中（例如已通關 Victory 或暫停/劇情），不受任何傷害
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        // 若處於無敵狀態，無視傷害
        if (isInvulnerable || currentHealth <= 0) return;

        // 若有輔助戰機，優先由一台輔助戰機抵擋本次攻擊
        if (wingmen.Count > 0)
        {
            GameObject lostWingman = wingmen[wingmen.Count - 1];
            wingmen.RemoveAt(wingmen.Count - 1);
            if (lostWingman != null) Destroy(lostWingman);

            isInvulnerable = true;
            invulnerableTimer = invulnerableDuration;
            StartCoroutine(RestoreWingmanAfterDelay(10f));
            Debug.Log($"<color=#00CCFF>【輔助戰機擋傷】失去 1 台輔助戰機，10 秒後將恢復！剩餘：{wingmen.Count} 台</color>");
            return;
        }

        // 背水一戰：35% 機率極限閃避免疫攻擊
        if (activeSkill == SkillType.GlassCannon && Random.value < 0.35f)
        {
            isInvulnerable = true;
            invulnerableTimer = 0.8f;
            Debug.Log("<color=#FFCC00>【背水一戰】觸發極限閃避，免疫本次攻擊！</color>");
            return;
        }

        lastAttackerType = attackerType;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"<color=#FF3333>【玩家受傷】受到 {damage} 點傷害！剩餘 HP: {currentHealth}/{maxHealth}，進入 1.5 秒無敵時間！</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 進入 1.5 秒無敵時間
            isInvulnerable = true;
            invulnerableTimer = invulnerableDuration;
        }
    }

    /// <summary>
    /// 治療/回復生命
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"<color=#33FF33>【維修成功】回復 {amount} 點 HP！目前生命: {currentHealth}/{maxHealth}</color>");
    }

    private void Die()
    {
        Debug.Log("<color=#FF0000>【戰機墜毀】玩家生命耗盡！</color>");
        OnPlayerDied?.Invoke();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver(lastAttackerType);
        }
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 檢查是否撞到敵機
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            TakeDamage(1);
            return;
        }

        // 檢查是否被敵方子彈擊中
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null && proj.BulletFaction == Projectile.Faction.Enemy)
        {
            TakeDamage(proj.Damage);
            return;
        }
    }

    /// <summary>
    /// 處理射擊與蓄力邏輯：
    /// - 短按 / 連按 空白鍵或滑鼠左鍵：一般射擊
    /// - 按住超過 1.0 秒：蓄力完成並閃爍提示
    /// - 放開按鍵時：若蓄力完成則施放貫穿雷射，未完成則進行單發射擊
    /// </summary>
    private void HandleShootingAndCharging()
    {
        bool shootKeyHeld = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        bool shootKeyDown = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        bool shootKeyUp = Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0);

        if (shootKeyDown)
        {
            isCharging = true;
            chargeTimer = 0f;
            // 剛按下時若冷卻完畢先發射一發
            if (Time.time >= nextFireTime)
            {
                FireNormalBullet();
                nextFireTime = Time.time + fireRate;
            }
        }

        if (shootKeyHeld && isCharging)
        {
            chargeTimer += Time.deltaTime;

            // 蓄力中：若蓄力完成，戰機進行縮放與高頻呼吸閃爍提示
            if (chargeTimer >= chargeTimeRequired)
            {
                float pulse = 1f + 0.15f * Mathf.Sin(Time.time * chargeFlashFrequency);
                transform.localScale = initialScale * pulse;

                if (!isInvulnerable && spriteRenderers != null)
                {
                    float flash = Mathf.PingPong(Time.time * 8f, 1f);
                    Color flashColor = Color.Lerp(Color.white, new Color(1f, 1f, 0.4f), flash);
                    foreach (var sr in spriteRenderers) if (sr != null) sr.color = flashColor;
                }
            }

            // 無論蓄力是否完成，按住期間均維持普通子彈連射輸出
            if (Time.time >= nextFireTime)
            {
                FireNormalBullet();
                nextFireTime = Time.time + fireRate;
            }
        }

        if (shootKeyHeld && !isCharging)
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        if (shootKeyUp && isCharging)
        {
            // 放開按鍵時還原戰機外觀
            transform.localScale = initialScale;
            if (!isInvulnerable && spriteRenderers != null)
            {
                foreach (var sr in spriteRenderers) if (sr != null) sr.color = Color.white;
            }

            if (chargeTimer >= chargeTimeRequired)
            {
                // 蓄力大於 1 秒 -> 施放貫穿雷射
                FireChargeLaser();
            }

            isCharging = false;
            chargeTimer = 0f;
        }
    }

    /// <summary>
    /// 處理 C 鍵全屏炸彈輸入
    /// </summary>
    private void HandleBombInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            UseBomb();
        }
    }

    /// <summary>
    /// 切換武器類型 (循環切換，各武器各自保留獨立等級)
    /// </summary>
    public void SwitchWeapon()
    {
        currentWeapon = (WeaponType)(((int)currentWeapon + 1) % 3);
        Debug.Log($"<color=#33CCFF>【武器切換】當前武器更換為: {currentWeapon} (獨立等級: Lv.{CurrentWeaponLevel})</color>");
    }

    /// <summary>
    /// 升級當前裝備的武器等級 (各武器獨立計算，最高 Lv.5)
    /// </summary>
    public bool UpgradeWeapon()
    {
        switch (currentWeapon)
        {
            case WeaponType.Vulcan:
                if (vulcanLevel < 5)
                {
                    vulcanLevel++;
                    Debug.Log($"<color=#FF3333>【火神機砲升級】等級提升至: Lv.{vulcanLevel}！</color>");
                    return true;
                }
                break;

            case WeaponType.Spread:
                if (spreadLevel < 5)
                {
                    spreadLevel++;
                    Debug.Log($"<color=#33CCFF>【散射雷射升級】等級提升至: Lv.{spreadLevel}！</color>");
                    return true;
                }
                break;

            case WeaponType.Plasma:
                if (plasmaLevel < 5)
                {
                    plasmaLevel++;
                    Debug.Log($"<color=#33FF33>【重型電漿升級】等級提升至: Lv.{plasmaLevel}！</color>");
                    return true;
                }
                break;
        }
        return false;
    }

    /// <summary>
    /// 設定武器與指定等級
    /// </summary>
    public void SetWeapon(WeaponType type, int level)
    {
        currentWeapon = type;
        level = Mathf.Clamp(level, 1, 5);
        switch (type)
        {
            case WeaponType.Vulcan: vulcanLevel = level; break;
            case WeaponType.Spread: spreadLevel = level; break;
            case WeaponType.Plasma: plasmaLevel = level; break;
        }
    }
    /// 開局 3 次，整場不可補充
    /// </summary>
    public bool UseBomb()
    {
        if (bombCount <= 0)
        {
            Debug.Log("<color=#FFAA00>【全屏炸彈】炸彈已耗盡！無法施放。</color>");
            return false;
        }

        if (Time.time < nextBombTime)
        {
            Debug.Log($"<color=#FFAA00>【全屏炸彈】冷卻中，剩餘 {nextBombTime - Time.time:F1} 秒！</color>");
            return false;
        }

        bombCount--;
        nextBombTime = Time.time + bombCooldown;
        Debug.Log($"<color=#FF0055>【全屏炸彈】按下 C 鍵！施放全畫面衝擊波！剩餘次數: {bombCount}/3</color>");

        // 產生衝擊波視覺效果 (若有)
        if (bombEffectPrefab != null)
        {
            Instantiate(bombEffectPrefab, transform.position, Quaternion.identity);
        }

        // 呼叫 GameManager 進行事件廣播 (解耦全屏銷毀機制)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerBomb();
        }

        return true;
    }

    /// <summary>
    /// 發射普通子彈 (依照當前武器類型與等級)
    /// </summary>
    private void FireNormalBullet()
    {
        if (bulletPrefab == null || Time.time < silenceEndTime) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.up * 0.8f;

        switch (currentWeapon)
        {
            case WeaponType.Vulcan:
                FireVulcan(spawnPos);
                break;
            case WeaponType.Spread:
                FireSpread(spawnPos);
                break;
            case WeaponType.Plasma:
                FirePlasma(spawnPos);
                break;
        }
    }

    private void FireVulcan(Vector3 origin)
    {
        // 火神機砲（紅色）：直線彈幕 (Lv.1 ~ Lv.5 獨立計算)
        float spacing = 0.22f;
        int count = Mathf.Clamp(vulcanLevel, 1, 5);

        float startOffset = -(count - 1) * spacing * 0.5f;
        for (int i = 0; i < count; i++)
        {
            Vector3 offsetPos = origin + transform.right * (startOffset + i * spacing);
            SpawnBullet(offsetPos, (Vector2)transform.up, 1, 15f, new Vector3(0.3f, 0.6f, 1f), Color.red);
        }
    }

    private void FireSpread(Vector3 origin)
    {
        // 散射雷射（藍色）：扇形廣角散射 (Lv.1 ~ Lv.5 獨立計算)
        int count = Mathf.Clamp(spreadLevel, 1, 5);

        float[] angles;
        switch (count)
        {
            case 1:
                angles = new float[] { 0f };
                break;
            case 2:
                angles = new float[] { -10f, 10f };
                break;
            case 3:
                angles = new float[] { -15f, 0f, 15f };
                break;
            case 4:
                angles = new float[] { -22f, -7f, 7f, 22f };
                break;
            case 5:
            default:
                angles = new float[] { -28f, -14f, 0f, 14f, 28f };
                break;
        }

        foreach (float angle in angles)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * (Vector2)transform.up;
            SpawnBullet(origin, dir, 1, 13f, new Vector3(0.3f, 0.6f, 1f), Color.cyan);
        }
    }

    private void FirePlasma(Vector3 origin)
    {
        // 重型電漿（綠色）：高傷害大體積彈 (Lv.1 ~ Lv.5 獨立計算)
        int dmg;
        float scaleMultiplier;

        switch (plasmaLevel)
        {
            case 1:
                dmg = 2;
                scaleMultiplier = 1.0f;
                break;
            case 2:
                dmg = 3;
                scaleMultiplier = 1.5f;
                break;
            case 3:
                dmg = 5;
                scaleMultiplier = 2.0f;
                break;
            case 4:
                dmg = 7;
                scaleMultiplier = 2.5f;
                break;
            case 5:
            default:
                dmg = 10;
                scaleMultiplier = 3.0f;
                break;
        }

        SpawnBullet(origin, (Vector2)transform.up, dmg, 10.5f, new Vector3(0.5f, 0.5f, 1f) * scaleMultiplier, new Color(0.2f, 1f, 0.3f));
    }

    /// <summary>
    /// 生成實體子彈
    /// </summary>
    private void SpawnBullet(Vector3 pos, Vector2 dir, int dmg, float speed, Vector3 scale, Color color)
    {
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);
        bullet.transform.localScale = scale * 1.5f; // 放大子彈尺寸 1.5 倍

        SpriteRenderer sr = bullet.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }

        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(Projectile.Faction.Player, dmg * damageMultiplier, speed, dir, false);
        }
    }

    /// <summary>
    /// 施放蓄力貫穿雷射 (高傷害、大範圍、貫穿敵人)
    /// </summary>
    private void FireChargeLaser()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.up * 1.2f;
        GameObject laserObj = chargeLaserPrefab != null ? chargeLaserPrefab : bulletPrefab;

        if (laserObj != null)
        {
            GameObject laser = Instantiate(laserObj, spawnPos, transform.rotation);
            laser.transform.localScale = new Vector3(2.2f, 4.5f, 1f); // 更加巨大震撼的蓄力雷射

            SpriteRenderer sr = laser.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.85f, 0.2f); // 金黃色高能量光芒
            }

            Projectile proj = laser.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(Projectile.Faction.Player, 10 * damageMultiplier, 20f, (Vector2)transform.up, true);
            }

            Debug.Log("<color=#FFEE00>【蓄力攻擊】施放強力貫穿雷射！(傷害: 10, 貫穿)</color>");
        }
    }

    /// <summary>
    /// 移動玩家戰機並將位置限制於攝影機可視範圍內
    /// </summary>
    private void MovePlayer()
    {
        Vector2 currentPosition = rb.position;
        Vector2 targetPosition = currentPosition + moveInput * (moveSpeed * Time.fixedDeltaTime);

        UpdateCameraBounds();

        float clampedX = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);

        rb.MovePosition(new Vector2(clampedX, clampedY));
    }

    /// <summary>
    /// 計算主攝影機邊界 (強制固定 9:16 直式活動區域)
    /// </summary>
    private void UpdateCameraBounds()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 動態計算機身精準邊界大小 (依照 SpriteRenderer 的真實外接矩形 extents)
        Vector2 bodyExtent = padding;
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            float maxExtentX = 0f;
            float maxExtentY = 0f;
            foreach (var sr in spriteRenderers)
            {
                if (sr != null)
                {
                    maxExtentX = Mathf.Max(maxExtentX, sr.bounds.extents.x);
                    maxExtentY = Mathf.Max(maxExtentY, sr.bounds.extents.y);
                }
            }
            if (maxExtentX > 0f && maxExtentY > 0f)
            {
                bodyExtent = new Vector2(maxExtentX, maxExtentY);
            }
        }

        // 直式鎖定 9:16，第二關橫式則符合太空背景圖片寬度
        float verticalHeight = mainCamera != null && mainCamera.orthographic ? mainCamera.orthographicSize : 6.5f;
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        float horizontalWidth = (isHorizontal && BackgroundScroller.Instance != null)
            ? BackgroundScroller.Instance.GetBackgroundHalfWidth()
            : verticalHeight * (9f / 16f);
        Vector3 camPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;

        minBounds = new Vector2(camPos.x - horizontalWidth + bodyExtent.x, camPos.y - verticalHeight + bodyExtent.y);
        maxBounds = new Vector2(camPos.x + horizontalWidth - bodyExtent.x, camPos.y + verticalHeight - bodyExtent.y);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        UpdateCameraBounds();

        Gizmos.color = Color.green;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, transform.position.z);
        Vector3 size = new Vector3((maxBounds.x - minBounds.x) - padding.x * 2f, (maxBounds.y - minBounds.y) - padding.y * 2f, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
