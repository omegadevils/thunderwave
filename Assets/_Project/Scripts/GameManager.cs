using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 遊戲狀態列舉
/// </summary>
public enum GameState
{
    StartMenu,
    Story,
    SkillSelect,
    Playing,
    Paused,
    GameOver,
    Victory
}

public enum SkillType
{
    None,
    Wingmen,     // 雙翼輔助戰機
    AutoCharge,  // 自動蓄力雷射
    GlassCannon  // 血量剩1、傷害3倍
}

/// <summary>
/// 遊戲核心管理器
/// 負責：遊戲狀態機、分數、關卡波次、Boss觸發、P鍵暫停、全屏炸彈與下一關預留接口
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("遊戲狀態")]
    [SerializeField] private GameState currentState = GameState.StartMenu;

    [Header("關卡設定 (支援多關卡預留)")]
    [Tooltip("當前關卡編號")]
    [SerializeField] private int currentStage = 1;

    [Tooltip("目前累積分數")]
    [SerializeField] private int currentScore = 0;

    [Tooltip("生成 Boss 所需門檻分數")]
    [SerializeField] private int bossSpawnThreshold = 10000;

    [Header("炸彈設定")]
    [Tooltip("全屏炸彈對 Boss 等強大目標造成的固定傷害")]
    [SerializeField] private int bombDamageToBoss = 50;

    // 事件廣播
    public static event Action<int> OnBombFired;
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnStageChanged;
    public static event Action OnBossSpawnRequested;
    public static event Action<GameState> OnGameStateChanged;

    private bool bossSpawned = false;
    private bool bossDefeated = false;

    public GameState CurrentState => currentState;
    public int CurrentScore => currentScore;
    public int CurrentStage => currentStage;
    public int BossSpawnThreshold => bossSpawnThreshold;
    public bool IsHorizontalStage => currentStage >= 2;
    public float EnemyHealthMultiplier => currentStage >= 2 ? 1.5f : 1.0f;
    public EnemyType LastAttackerType { get; private set; } = EnemyType.Minion;
    public bool IsDefeatStory { get; private set; } = false;
    public SkillType SelectedSkill { get; private set; } = SkillType.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }
    }

    private void Start()
    {
        // 初始切換至 StartMenu (遊戲時間暫停，等待按開始)
        SetState(GameState.StartMenu);
    }

    private void Update()
    {
        HandlePauseInput();
    }

    /// <summary>
    /// P 鍵暫停與恢復
    /// </summary>
    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.StartMenu:
            case GameState.Story:
            case GameState.SkillSelect:
            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.GameOver:
            case GameState.Victory:
                Time.timeScale = 0.2f; // 慢動作戲劇性結算
                break;
        }

        OnGameStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// 開始遊戲 (由 Start Panel 按鈕觸發，先進入開頭劇情)
    /// </summary>
    public void StartGame()
    {
        currentScore = 0;
        bossSpawned = false;
        bossDefeated = false;
        IsDefeatStory = false;
        SetState(GameState.Story);
        OnScoreChanged?.Invoke(currentScore);
        OnStageChanged?.Invoke(currentStage);
        Debug.Log("<color=#00CCFF>【劇情演出】播放開頭劇情...</color>");
    }

    /// <summary>
    /// 劇情播放完畢或略過後，進入正式關卡或戰敗結算
    /// </summary>
    public void FinishStory()
    {
        if (currentState != GameState.Story) return;

        if (IsDefeatStory)
        {
            IsDefeatStory = false;
            SetState(GameState.GameOver);
            Debug.Log("<color=#FF3333>【戰敗結算】進入 Game Over 畫面。</color>");
        }
        else if (currentStage >= 2)
        {
            SetState(GameState.SkillSelect);
            Debug.Log("<color=#00CCFF>【技能選擇】請選擇第二關強化技能！</color>");
        }
        else
        {
            SetState(GameState.Playing);
            Debug.Log("<color=#00FF88>【遊戲開始】戰鬥開始，敵機開始出動！</color>");
        }
    }

    public void SelectSkill(SkillType skill)
    {
        SelectedSkill = skill;
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.ApplySkill(skill);
        }
        SetState(GameState.Playing);
        Debug.Log($"<color=#00FF88>【技能啟用】已選擇技能：{skill}，正式進入第二關！</color>");
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetState(GameState.Paused);
            Debug.Log("<color=#FFFF00>【遊戲暫停】時間凍結 (Time.timeScale = 0)</color>");
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetState(GameState.Playing);
            Debug.Log("<color=#00FF88>【繼續遊戲】時間恢復正常！</color>");
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 進入下一關 (預留未來關卡擴展)
    /// </summary>
    public void NextStage()
    {
        currentStage++;
        bossSpawned = false;
        bossDefeated = false;
        bossSpawnThreshold = currentScore + 18000; // 第二關流程加長，需要更多分數才觸發 Boss

        // 清空上一關殘留的子彈、道具與敵機
        foreach (var proj in FindObjectsByType<Projectile>())
        {
            Destroy(proj.gameObject);
        }
        foreach (var item in FindObjectsByType<ItemPickup>())
        {
            Destroy(item.gameObject);
        }
        foreach (var enemy in FindObjectsByType<EnemyController>())
        {
            Destroy(enemy.gameObject);
        }

        OnStageChanged?.Invoke(currentStage);
        SetState(GameState.Story);
        Debug.Log($"<color=#FFD700>【邁向下一關】進入 STAGE {currentStage}，播放戰前劇情！</color>");
    }

    /// <summary>
    /// 增加分數
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);

        if (!bossSpawned && currentScore >= bossSpawnThreshold)
        {
            bossSpawned = true;
            Debug.Log($"<color=#FF0033>【警報】分數達標 {bossSpawnThreshold} 分！Boss 即將降臨！</color>");
            OnBossSpawnRequested?.Invoke();
        }
    }

    /// <summary>
    /// Boss 擊破 (由 Boss 死亡觸發)
    /// </summary>
    public void OnBossKilled()
    {
        if (bossDefeated) return;
        bossDefeated = true;

        // 通關後清空場上所有敵人子彈
        foreach (var proj in FindObjectsByType<Projectile>())
        {
            if (proj != null && proj.BulletFaction == Projectile.Faction.Enemy)
            {
                Destroy(proj.gameObject);
            }
        }

        Debug.Log("<color=#FFD700>【關卡大捷】關卡首領已被消滅！獲得勝利 VICTORY！</color>");
        SetState(GameState.Victory);
    }

    /// <summary>
    /// 玩家陣亡 (先播放對應關卡與擊敗者的戰敗劇情，再進入 Game Over)
    /// </summary>
    public void TriggerGameOver()
    {
        TriggerGameOver(EnemyType.Minion);
    }

    public void TriggerGameOver(EnemyType attackerType)
    {
        if (currentState == GameState.GameOver || (currentState == GameState.Story && IsDefeatStory)) return;
        LastAttackerType = attackerType;
        IsDefeatStory = true;
        Debug.Log($"<color=#FF0000>【任務失敗】玩家戰機被 {attackerType} 擊毀，播放戰敗劇情...</color>");
        SetState(GameState.Story);
    }

    public void TriggerBomb()
    {
        Debug.Log("<color=#FF3366>【全屏炸彈】震撼爆發！廣播清除場上所有敵機與敵方彈幕！</color>");
        OnBombFired?.Invoke(bombDamageToBoss);
    }
}
