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
    [SerializeField] private float minionSpawnInterval = 1.2f;

    [Tooltip("精英怪生成間隔 (秒)")]
    [SerializeField] private float eliteSpawnInterval = 4.0f;

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
    private bool bossAlive = false;

    private void Start()
    {
        nextMinionTime = Time.time + 1.0f;
        nextEliteTime = Time.time + 3.0f;
    }

    private void OnEnable()
    {
        GameManager.OnBossSpawnRequested += SpawnBoss;
    }

    private void OnDisable()
    {
        GameManager.OnBossSpawnRequested -= SpawnBoss;
    }

    private void Update()
    {
        // 若 Boss 登場，可暫停普通小怪生成或維持低頻
        if (bossAlive) return;

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
    }

    private void SpawnRandomMinion()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0) return;
        GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Count)];
        if (prefab == null) return;

        float spawnX = Random.Range(spawnXRange.x, spawnXRange.y);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
        Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, 180f)); // 敵機機頭朝下
    }

    private void SpawnRandomElite()
    {
        if (elitePrefabs == null || elitePrefabs.Count == 0) return;
        GameObject prefab = elitePrefabs[Random.Range(0, elitePrefabs.Count)];
        if (prefab == null) return;

        float spawnX = Random.Range(spawnXRange.x * 0.8f, spawnXRange.y * 0.8f);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
        Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, 180f)); // 敵機機頭朝下
    }

    /// <summary>
    /// 召喚 Boss 登場
    /// </summary>
    public void SpawnBoss()
    {
        if (bossPrefab == null || bossAlive) return;

        bossAlive = true;
        Vector3 spawnPos = new Vector3(0f, spawnY + 1.0f, 0f);
        Instantiate(bossPrefab, spawnPos, Quaternion.Euler(0, 0, 180f)); // 機頭朝下
        Debug.Log("<color=#FF0000>【Boss 降臨】關卡首領 TinyShip15 已降臨戰場！</color>");
    }
}
