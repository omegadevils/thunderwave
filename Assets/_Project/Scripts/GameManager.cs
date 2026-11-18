using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 遊戲狀態列舉
/// </summary>
public enum GameState
{
    StartMenu,
    Playing,
    Paused,
    GameOver,
    Victory
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
    [SerializeField] private int bossSpawnThreshold = 6000;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    /// 開始遊戲 (由 Start Panel 按鈕觸發)
    /// </summary>
    public void StartGame()
    {
        currentScore = 0;
        bossSpawned = false;
        bossDefeated = false;
        SetState(GameState.Playing);
        OnScoreChanged?.Invoke(currentScore);
        OnStageChanged?.Invoke(currentStage);
        Debug.Log("<color=#00FF88>【遊戲開始】戰鬥開始，敵機開始出動！</color>");
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
        bossSpawnThreshold += 6000; // 下一關分數門檻遞增

        OnStageChanged?.Invoke(currentStage);
        SetState(GameState.Playing);
        Debug.Log($"<color=#FFD700>【邁向下一關】進入 STAGE {currentStage}！</color>");
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
        Debug.Log("<color=#FFD700>【關卡大捷】關卡首領已被消滅！獲得勝利 VICTORY！</color>");
        SetState(GameState.Victory);
    }

    /// <summary>
    /// 玩家陣亡
    /// </summary>
    public void TriggerGameOver()
    {
        if (currentState == GameState.GameOver) return;
        Debug.Log("<color=#FF0000>【任務失敗】戰機陣亡，GAME OVER！</color>");
        SetState(GameState.GameOver);
    }

    public void TriggerBomb()
    {
        Debug.Log("<color=#FF3366>【全屏炸彈】震撼爆發！廣播清除場上所有敵機與敵方彈幕！</color>");
        OnBombFired?.Invoke(bombDamageToBoss);
    }
}
