using UnityEngine;

/// <summary>
/// 通用子彈與投射物腳本
/// 支援：玩家/敵方子彈陣營區分、速度飛行、貫穿判定、生命週期自動銷毀、安全標籤檢查
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public enum Faction
    {
        Player,
        Enemy
    }

    [Header("基礎屬性")]
    [Tooltip("陣營 (玩家或敵方)")]
    [SerializeField] private Faction faction = Faction.Player;

    [Tooltip("傷害值")]
    [SerializeField] private int damage = 1;

    [Tooltip("飛行速度 (單位/秒)")]
    [SerializeField] private float speed = 12f;

    [Tooltip("飛行方向 (預設朝上)")]
    [SerializeField] private Vector2 direction = Vector2.up;

    [Tooltip("存活時間上限 (秒)，超時自動銷毀")]
    [SerializeField] private float lifetime = 4f;

    [Header("貫穿屬性")]
    [Tooltip("是否為貫穿型子彈 (如蓄力雷射)")]
    [SerializeField] private bool isPiercing = false;

    public Faction BulletFaction => faction;
    public int Damage => damage;
    public bool IsPiercing => isPiercing;

    private void Start()
    {
        // 自動依照生命週期銷毀
        Destroy(gameObject, lifetime);
    }

    private void OnEnable()
    {
        // 註冊全屏炸彈廣播事件
        GameManager.OnBombFired += HandleBombFired;
    }

    private void OnDisable()
    {
        // 取消註冊事件
        GameManager.OnBombFired -= HandleBombFired;
    }

    /// <summary>
    /// 響應全屏炸彈：清除場上所有敵方子彈
    /// </summary>
    private void HandleBombFired(int bombDamage)
    {
        if (faction == Faction.Enemy)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 沿指定方向直線飛行
        transform.Translate(direction * (speed * Time.deltaTime), Space.World);
    }

    /// <summary>
    /// 初始化子彈參數 (供發射時自訂方向、傷害與速度)
    /// </summary>
    public void Initialize(Faction bulletFaction, int bulletDamage, float bulletSpeed, Vector2 bulletDirection, bool piercing = false)
    {
        faction = bulletFaction;
        damage = bulletDamage;
        speed = bulletSpeed;
        direction = bulletDirection.normalized;
        isPiercing = piercing;

        // 計算旋轉角，使子彈朝向飛行方向
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 若命中敵方且不具貫穿屬性，則擊中後銷毀
        // 安全檢查：先以組件判斷，再以安全 Tag 判斷，避免未配置 Tag 時報錯
        bool isTargetEnemy = other.GetComponent<EnemyController>() != null || SafeCheckTag(other, "Enemy");
        bool isTargetPlayer = other.GetComponent<PlayerController>() != null || SafeCheckTag(other, "Player");

        if (faction == Faction.Player && isTargetEnemy)
        {
            if (!isPiercing)
            {
                Destroy(gameObject);
            }
        }
        else if (faction == Faction.Enemy && isTargetPlayer)
        {
            if (!isPiercing)
            {
                Destroy(gameObject);
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
