using UnityEngine;

/// <summary>
/// 道具類型列舉
/// </summary>
public enum ItemType
{
    PowerUp,      // 火力升級 (最高 Lv.5，滿級 +500 分)
    WeaponSwitch, // 武器切換核心 (循環更換武器，保留等級)
    Heal          // 緊急維修 (HP +1，滿血 +500 分)
}

/// <summary>
/// 掉落道具控制器
/// 負責：直線向下飄落、玩家拾取觸發效果、超出螢幕自動銷毀
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("道具屬性")]
    [Tooltip("道具類型")]
    [SerializeField] private ItemType itemType = ItemType.PowerUp;

    [Tooltip("向下飄落速度")]
    [SerializeField] private float fallSpeed = 2.0f;

    [Tooltip("超出螢幕下界的銷毀高度 (Y)")]
    [SerializeField] private float bottomDestroyY = -8.0f;

    public ItemType Type => itemType;

    private void Awake()
    {
        // 確保碰撞器為 Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        gameObject.tag = "Item";
    }

    private void Update()
    {
        // 直線緩速向下飄落
        transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime), Space.World);

        // 超出螢幕下方邊界時自動銷毀
        if (transform.position.y < bottomDestroyY)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            ApplyPickupEffect(player);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 套用道具效果給玩家
    /// </summary>
    private void ApplyPickupEffect(PlayerController player)
    {
        switch (itemType)
        {
            case ItemType.PowerUp:
                // 火力升級：若已滿級 (Lv.5) 則改為加分 +500
                bool upgraded = player.UpgradeWeapon();
                if (!upgraded)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddScore(500);
                        Debug.Log("<color=#FFD700>【道具拾取】火力已達滿級！獲得額外獎勵分數: +500</color>");
                    }
                }
                break;

            case ItemType.WeaponSwitch:
                // 武器切換核心：循環切換下一種武器類型並保留等級
                player.SwitchWeapon();
                break;

            case ItemType.Heal:
                // 緊急維修：若滿血則改為加分 +500
                if (player.CurrentHealth >= player.MaxHealth)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddScore(500);
                        Debug.Log("<color=#FFD700>【道具拾取】生命已滿！獲得額外獎勵分數: +500</color>");
                    }
                }
                else
                {
                    player.Heal(1);
                }
                break;
        }
    }
}
