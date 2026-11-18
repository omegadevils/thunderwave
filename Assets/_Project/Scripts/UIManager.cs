using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 介面管理器
/// 負責：管理 Start, HUD, Pause, End 4 個面板、分數血量更新、音效切換與下一關預留
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("4 大核心面板")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject endPanel;

    [Header("HUD 元件")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text stageText;
    [SerializeField] private Text hpText;
    [SerializeField] private Text weaponText;
    [SerializeField] private Text bombText;
    [SerializeField] private Slider chargeSlider;

    [Header("Pause 面板元件")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button soundToggleButton;
    [SerializeField] private Text soundToggleText;

    [Header("End 面板元件")]
    [SerializeField] private Text endTitleText;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Button endRestartButton;
    [SerializeField] private Button nextStageButton; // 預留下一關按鈕

    [Header("Start 面板元件")]
    [SerializeField] private Button startButton;

    private PlayerController player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BindButtons();
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();

        UpdateSoundButtonText(AudioManager.Instance != null && AudioManager.Instance.IsSoundEnabled);
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += UpdateScore;
        GameManager.OnStageChanged += UpdateStage;
        PlayerController.OnHealthChanged += UpdateHealth;
        AudioManager.OnSoundToggled += UpdateSoundButtonText;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnStageChanged -= UpdateStage;
        PlayerController.OnHealthChanged -= UpdateHealth;
        AudioManager.OnSoundToggled -= UpdateSoundButtonText;
    }

    private void Update()
    {
        // 即時刷新 HUD 蓄力條、武器與炸彈
        if (player != null && hudPanel.activeSelf)
        {
            if (chargeSlider != null) chargeSlider.value = player.ChargeProgress;
            if (weaponText != null) weaponText.text = $"WEAPON: {player.CurrentWeapon} Lv.{player.WeaponLevel}";
            if (bombText != null) bombText.text = $"BOMB: {player.BombCount}/3 (C)";
        }
    }

    private void BindButtons()
    {
        if (startButton != null) startButton.onClick.AddListener(() => GameManager.Instance.StartGame());
        if (resumeButton != null) resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());
        if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        if (endRestartButton != null) endRestartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        if (soundToggleButton != null) soundToggleButton.onClick.AddListener(() => AudioManager.Instance.ToggleSound());
        if (nextStageButton != null) nextStageButton.onClick.AddListener(() => GameManager.Instance.NextStage());
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (startPanel != null) startPanel.SetActive(state == GameState.StartMenu);
        if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);

        if (endPanel != null)
        {
            bool isEnd = state == GameState.GameOver || state == GameState.Victory;
            endPanel.SetActive(isEnd);

            if (isEnd)
            {
                if (endTitleText != null)
                {
                    endTitleText.text = state == GameState.Victory ? "VICTORY!" : "GAME OVER";
                    endTitleText.color = state == GameState.Victory ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.2f, 0.2f);
                }
                if (finalScoreText != null && GameManager.Instance != null)
                {
                    finalScoreText.text = $"FINAL SCORE: {GameManager.Instance.CurrentScore}";
                }
                // 勝利時顯示「下一關」按鈕，失敗時隱藏
                if (nextStageButton != null)
                {
                    nextStageButton.gameObject.SetActive(state == GameState.Victory);
                }
            }
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
    }

    private void UpdateStage(int stage)
    {
        if (stageText != null) stageText.text = $"STAGE {stage}";
    }

    private void UpdateHealth(int curHp, int maxHp)
    {
        if (hpText != null) hpText.text = $"HP: {curHp}/{maxHp}";
    }

    private void UpdateSoundButtonText(bool enabled)
    {
        if (soundToggleText != null)
        {
            soundToggleText.text = enabled ? "SOUND: ON" : "SOUND: OFF";
        }
    }
}
