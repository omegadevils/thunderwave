using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵機波次生成器
/// 負責：在畫面頂部定時隨機生成小怪與精英怪，分數達標時召喚關卡首領 (Boss)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成頻率設定")]
    [Tooltip("小怪生成間隔 (秒)")]
    [SerializeField] private float minionSpawnInterval = 0.65f;

    [Tooltip("精英怪生成間隔 (秒)")]
    [SerializeField] private float eliteSpawnInterval = 2.2f;

    [Tooltip("隨機敵人事件間隔 (秒)")]
    [SerializeField] private float randomEventInterval = 4.0f;

    [Tooltip("生成水平範圍 (X 座標最小/最大值，配合 9:16 直式邊界)")]
    [SerializeField] private Vector2 spawnXRange = new Vector2(-2.8f, 2.8f);

    [Tooltip("生成垂直高度 (Y 座標)")]
    [SerializeField] private float spawnY = 7.0f;

    [Header("敵機 Prefabs")]
    [Tooltip("小型偵察機 Prefabs (隨機選取)")]
    [SerializeField] private List<GameObject> minionPrefabs = new List<GameObject>();

    [Tooltip("重型砲艇精英怪 Prefabs (隨機選取)")]
    [SerializeField] private List<GameObject> elitePrefabs = new List<GameObject>();

    [Tooltip("關卡首領 (Boss) Prefab")]
    [SerializeField] private GameObject bossPrefab;

    private float nextMinionTime;
    private float nextEliteTime;
    private float nextRandomEventTime;
    private float nextBossSummonTime;
    private bool bossAlive = false;

    private void Start()
    {
        nextMinionTime = Time.time + 1.0f;
        nextEliteTime = Time.time + 2.5f;
        nextRandomEventTime = Time.time + 2.5f;
    }

    private void OnEnable()
    {
        GameManager.OnBossSpawnRequested += SpawnBoss;
        GameManager.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnBossSpawnRequested -= SpawnBoss;
        GameManager.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(int stage)
    {
        bossAlive = false;
        nextMinionTime = Time.time + 1.0f;
        nextEliteTime = Time.time + 3.0f;
    }

    private void Update()
    {
        // 若 Boss 登場，每 12 秒召喚 10 隻小怪或 5 隻菁英怪
        if (bossAlive)
        {
            if (Time.time >= nextBossSummonTime)
            {
                nextBossSummonTime = Time.time + 7.0f;
                if (Random.value < 0.5f)
                {
                    Debug.Log("<color=#FF3366>【Boss 召喚】Boss 召喚了 10 隻小怪支援！</color>");
                    for (int i = 0; i < 10; i++) SpawnRandomMinion();
                }
                else
                {
                    Debug.Log("<color=#FF3366>【Boss 召喚】Boss 召喚了 5 隻菁英怪支援！</color>");
                    for (int i = 0; i < 5; i++) SpawnRandomElite();
                }
            }
            return;
        }

        // 生成小怪
        if (Time.time >= nextMinionTime)
        {
            nextMinionTime = Time.time + minionSpawnInterval;
            SpawnRandomMinion();
        }

        // 生成精英怪
        if (Time.time >= nextEliteTime)
        {
            nextEliteTime = Time.time + eliteSpawnInterval;
            SpawnRandomElite();
        }

        // 觸發隨機敵人事件
        if (Time.time >= nextRandomEventTime)
        {
            nextRandomEventTime = Time.time + randomEventInterval;
            TriggerRandomEnemyEvent();
        }
    }

    private void TriggerRandomEnemyEvent()
    {
        bool isStage2 = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        int eventType = isStage2 ? Random.Range(0, 9) : Random.Range(0, 7);
        if (eventType >= 7) eventType = 7; // 第二關提高陷阱出現機率
        switch (eventType)
        {
            case 0:
                Debug.Log("<color=#FFAA00>【隨機事件】偵察機編隊突襲！</color>");
                for (int i = 0; i < 5; i++) SpawnRandomMinion();
                break;
            case 1:
                Debug.Log("<color=#FF6600>【隨機事件】重型砲艇雙機護衛！</color>");
                SpawnRandomElite();
                SpawnRandomElite();
                break;
            case 2:
                Debug.Log("<color=#FF3366>【隨機事件】敵軍高速奇襲隊！</color>");
                for (int i = 0; i < 3; i++) SpawnRandomMinion();
                SpawnRandomElite();
                break;
            case 3:
                Debug.Log("<color=#CC9966>【隨機事件】小行星群來襲！（僅能被炸彈摧毀）</color>");
                SpawnAsteroidEvent(Random.Range(2, 5));
                break;
            case 4:
                Debug.Log("<color=#FF0044>【隨機事件】敵方雷射主砲鎖定中！5 秒後發射！</color>");
                SpawnLaserCannonEvent();
                break;
            case 5:
                Debug.Log("<color=#FF0044>【隨機事件】雙重交叉雷射網鎖定中！5 秒後發射！</color>");
                SpawnLaserCannonEvent();
                SpawnLaserCannonEvent();
                break;
            case 6:
                Debug.Log("<color=#CC9966>【隨機事件】大規模隕石雨來襲！</color>");
                SpawnAsteroidEvent(Random.Range(6, 9));
                break;
            case 7:
                Debug.Log("<color=#CC44FF>【隨機事件】電磁干擾陷阱出現！</color>");
                SpawnTrapEvent();
                break;
        }
    }

    private void SpawnTrapEvent()
    {
        StartCoroutine(SpawnTrapEventRoutine());
    }

    private System.Collections.IEnumerator SpawnTrapEventRoutine()
    {
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        float halfW = isHorizontal && BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 2.8f;

        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = isHorizontal
                ? new Vector3(Random.Range(-halfW + 1.5f, halfW - 0.5f), Random.Range(-3.5f, 3.5f), 0f)
                : new Vector3(Random.Range(-halfW, halfW), spawnY, 0f);

            GameObject trap = new GameObject("TrapHazard");
            trap.transform.position = pos;
            TrapHazard hazard = trap.AddComponent<TrapHazard>();
            hazard.Initialize(Random.value < 0.5f ? TrapHazard.TrapType.WeaponDowngrade : TrapHazard.TrapType.Silence2Sec);

            yield return new WaitForSeconds(0.45f);
        }
    }

    private void SpawnAsteroidEvent(int count)
    {
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = isHorizontal
                ? new Vector3(halfW - 0.5f, Random.Range(-3.5f, 3.5f), 0f)
                : new Vector3(Random.Range(spawnXRange.x, spawnXRange.y), spawnY, 0f);

            GameObject ast = new GameObject("AsteroidHazard");
            ast.transform.position = pos;
            ast.AddComponent<AsteroidHazard>();
        }
    }

    private void SpawnLaserCannonEvent()
    {
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        float halfW = isHorizontal && BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 2.8f;

        Vector3 p0, p1;
        if (isHorizontal)
        {
            p0 = new Vector3(-halfW, Random.Range(-4.5f, 4.5f), 0f);
            p1 = new Vector3(halfW, Random.Range(-4.5f, 4.5f), 0f);
        }
        else
        {
            p0 = new Vector3(Random.Range(-halfW, halfW), -6.5f, 0f);
            p1 = new Vector3(Random.Range(-halfW, halfW), 6.5f, 0f);
        }

        GameObject laser = new GameObject("LaserCannonHazard");
        LaserCannonHazard hazard = laser.AddComponent<LaserCannonHazard>();
        hazard.Initialize(p0, p1);
    }

    private void SpawnRandomMinion()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0) return;
        GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Count)];
        if (prefab == null) return;

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        if (isHorizontal)
        {
            float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;
            float y = Random.Range(-3.5f, 3.5f);
            Instantiate(prefab, new Vector3(halfW - 0.5f, y, 0f), Quaternion.Euler(0, 0, 90f));
        }
        else
        {
            float spawnX = Random.Range(spawnXRange.x, spawnXRange.y);
            Instantiate(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.Euler(0, 0, 180f)); // 敵機機頭朝下
        }
    }

    private void SpawnRandomElite()
    {
        if (elitePrefabs == null || elitePrefabs.Count == 0) return;
        GameObject prefab = elitePrefabs[Random.Range(0, elitePrefabs.Count)];
        if (prefab == null) return;

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        if (isHorizontal)
        {
            float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;
            float y = Random.Range(-2.8f, 2.8f);
            Instantiate(prefab, new Vector3(halfW - 0.5f, y, 0f), Quaternion.Euler(0, 0, 90f));
        }
        else
        {
            float spawnX = Random.Range(spawnXRange.x * 0.8f, spawnXRange.y * 0.8f);
            Instantiate(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.Euler(0, 0, 180f)); // 敵機機頭朝下
        }
    }

    /// <summary>
    /// 召喚 Boss 登場 (先播放 2.5 秒警示特效後再降臨)
    /// </summary>
    public void SpawnBoss()
    {
        if (bossPrefab == null || bossAlive) return;

        bossAlive = true;
        nextBossSummonTime = Time.time + 9.5f; // 2.5s 警示 + 7s 後首次召喚
        StartCoroutine(SpawnBossWithWarningRoutine());
    }

    private System.Collections.IEnumerator SpawnBossWithWarningRoutine()
    {
        yield return new WaitForSeconds(2.5f);

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;
        Vector3 spawnPos = isHorizontal ? new Vector3(halfW - 0.8f, 0f, 0f) : new Vector3(0f, spawnY + 1.0f, 0f);
        Quaternion rot = isHorizontal ? Quaternion.Euler(0, 0, 90f) : Quaternion.Euler(0, 0, 180f);
        Instantiate(bossPrefab, spawnPos, rot);
        Debug.Log("<color=#FF0000>【Boss 降臨】關卡首領 TinyShip15 已降臨戰場！</color>");
    }
}

/// <summary>
/// 隨機事件：小行星（玩家撞上扣血，僅能被炸彈摧毀）
/// </summary>
public class AsteroidHazard : MonoBehaviour
{
    private float speed = 3.2f;
    private float rotSpeed = 90f;
    private Vector3 moveDir;
    private PlayerController cachedPlayer;
    private float nextHitTime = 0f;

    private void Start()
    {
        cachedPlayer = FindAnyObjectByType<PlayerController>();
        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        Vector2 baseDir = isHorizontal ? Vector2.left : Vector2.down;
        moveDir = Quaternion.Euler(0, 0, Random.Range(-35f, 35f)) * baseDir;

        // 使用 LineRenderer 繪製不規則小行星外觀
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 8;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        Color c = new Color(0.75f, 0.58f, 0.42f, 1f);
        lr.startColor = c;
        lr.endColor = c;
        lr.sortingOrder = 15;

        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8f) * Mathf.PI * 2f;
            float r = Random.Range(0.45f, 0.72f);
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
        }
    }

    private void OnEnable()
    {
        GameManager.OnBombFired += HandleBombFired;
    }

    private void OnDisable()
    {
        GameManager.OnBombFired -= HandleBombFired;
    }

    private void HandleBombFired(int damage)
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        transform.Translate(moveDir * (speed * Time.deltaTime), Space.World);
        transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerController>();

        if (cachedPlayer != null && Time.time >= nextHitTime && Vector2.Distance(transform.position, cachedPlayer.transform.position) <= 0.9f)
        {
            nextHitTime = Time.time + 0.5f;
            cachedPlayer.TakeDamage(1, EnemyType.Minion);
        }

        if (isHorizontal)
        {
            float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;
            if (transform.position.x <= -halfW || transform.position.y > 6.5f || transform.position.y < -6.5f) Destroy(gameObject);
        }
        else if (transform.position.y < -7.5f || transform.position.x < -3.5f || transform.position.x > 3.5f)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// 隨機事件：雷射主砲（警示 5 秒後發射致命雷射，碰到直接死亡）
/// </summary>
public class LaserCannonHazard : MonoBehaviour
{
    public void Initialize(Vector3 p0, Vector3 p1)
    {
        StartCoroutine(LaserRoutine(p0, p1));
    }

    private System.Collections.IEnumerator LaserRoutine(Vector3 p0, Vector3 p1)
    {
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 18;

        lr.SetPosition(0, p0);
        lr.SetPosition(1, p1);

        // 1. 警示階段 (5 秒)
        float warnDuration = 5.0f;
        float elapsed = 0f;
        while (elapsed < warnDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 6f, 1f);
            Color warnColor = new Color(1f, 0.1f, 0.1f, 0.25f + pulse * 0.65f);
            lr.startColor = warnColor;
            lr.endColor = warnColor;
            lr.startWidth = 0.12f;
            lr.endWidth = 0.12f;
            yield return null;
        }

        // 2. 雷射發射階段 (1.2 秒，碰到直接死亡)
        PlayerController player = FindAnyObjectByType<PlayerController>();
        float fireDuration = 1.2f;
        elapsed = 0f;
        while (elapsed < fireDuration)
        {
            elapsed += Time.deltaTime;
            Color beamColor = new Color(1f, 0.45f, 0.05f, 1f); // 醒目亮橘色雷射主砲
            lr.startColor = beamColor;
            lr.endColor = beamColor;
            float beamWidth = 0.9f + Mathf.PingPong(elapsed * 12f, 0.25f);
            lr.startWidth = beamWidth;
            lr.endWidth = beamWidth;

            if (player != null && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                float dist = DistancePointToSegment(player.transform.position, p0, p1);
                if (dist <= 0.45f)
                {
                    player.TakeDamage(999, EnemyType.Boss);
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float sqrLen = ab.sqrMagnitude;
        if (sqrLen < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqrLen);
        Vector2 proj = a + t * ab;
        return Vector2.Distance(p, proj);
    }
}

/// <summary>
/// 地圖陷阱：武器降等 (紫色) / 無法射擊 2 秒 (黃色)
/// </summary>
public class TrapHazard : MonoBehaviour
{
    public enum TrapType { WeaponDowngrade, Silence2Sec }
    private TrapType trapType;
    private float speed = 2.8f;
    private float lifeTimer = 5.0f;
    private PlayerController cachedPlayer;

    public void Initialize(TrapType type)
    {
        trapType = type;
        cachedPlayer = FindAnyObjectByType<PlayerController>();

        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 4;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        Color c = trapType == TrapType.WeaponDowngrade ? new Color(0.8f, 0.2f, 1f, 1f) : new Color(1f, 0.85f, 0.1f, 1f);
        lr.startColor = c;
        lr.endColor = c;
        lr.sortingOrder = 16;

        float r = 0.45f;
        lr.SetPosition(0, new Vector3(0, r, 0));
        lr.SetPosition(1, new Vector3(r, 0, 0));
        lr.SetPosition(2, new Vector3(0, -r, 0));
        lr.SetPosition(3, new Vector3(-r, 0, 0));
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        bool isHorizontal = GameManager.Instance != null && GameManager.Instance.IsHorizontalStage;
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.Translate((isHorizontal ? Vector3.left : Vector3.down) * (speed * Time.deltaTime), Space.World);
        transform.Rotate(0f, 0f, 120f * Time.deltaTime);

        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerController>();
        if (cachedPlayer != null && Vector2.Distance(transform.position, cachedPlayer.transform.position) <= 0.75f)
        {
            if (trapType == TrapType.WeaponDowngrade)
                cachedPlayer.DowngradeCurrentWeapon();
            else
                cachedPlayer.SilenceShooting(2.0f);

            Destroy(gameObject);
            return;
        }

        if (isHorizontal)
        {
            float halfW = BackgroundScroller.Instance != null ? BackgroundScroller.Instance.GetBackgroundHalfWidth() : 8.0f;
            if (transform.position.x <= -halfW) Destroy(gameObject);
        }
        else if (transform.position.y < -7.5f)
        {
            Destroy(gameObject);
        }
    }
}
