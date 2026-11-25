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
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject skillSelectPanel;
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
    private Image lowHpOverlay;

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
        player = FindAnyObjectByType<PlayerController>();

        UpdateSoundButtonText(AudioManager.Instance != null && AudioManager.Instance.IsSoundEnabled);
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += UpdateScore;
        GameManager.OnStageChanged += UpdateStage;
        GameManager.OnBossSpawnRequested += ShowBossWarning;
        PlayerController.OnHealthChanged += UpdateHealth;
        AudioManager.OnSoundToggled += UpdateSoundButtonText;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnStageChanged -= UpdateStage;
        GameManager.OnBossSpawnRequested -= ShowBossWarning;
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
            if (bombText != null)
            {
                if (player.BombCount <= 0)
                    bombText.text = "BOMB: 0/3 (已耗盡)";
                else if (player.BombCooldownRemaining > 0f)
                    bombText.text = $"BOMB: {player.BombCount}/3 (冷卻 {player.BombCooldownRemaining:F1}s)";
                else
                    bombText.text = $"BOMB: {player.BombCount}/3 (C - 就緒!)";
            }
        }

        UpdateLowHpFlash();
    }

    private void UpdateLowHpFlash()
    {
        if (lowHpOverlay == null)
        {
            Transform parentCanvas = hudPanel != null ? hudPanel.transform.parent : transform;
            GameObject obj = new GameObject("LowHpOverlay", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parentCanvas, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            lowHpOverlay = obj.GetComponent<Image>();
            lowHpOverlay.raycastTarget = false;
            lowHpOverlay.color = new Color(1f, 0f, 0f, 0f);
        }

        bool isLowHp = GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameState.Playing
            && player != null
            && player.CurrentHealth == 1;

        if (isLowHp)
        {
            float alpha = Mathf.PingPong(Time.time * 1.2f, 0.12f) + 0.02f;
            lowHpOverlay.color = new Color(1f, 0f, 0f, alpha);
        }
        else
        {
            lowHpOverlay.color = new Color(1f, 0f, 0f, 0f);
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
        if (state == GameState.Story && storyPanel == null)
        {
            CreateStoryPanel();
        }
        if (state == GameState.SkillSelect && skillSelectPanel == null)
        {
            CreateSkillSelectPanel();
        }

        if (startPanel != null) startPanel.SetActive(state == GameState.StartMenu);
        if (storyPanel != null) storyPanel.SetActive(state == GameState.Story);
        if (skillSelectPanel != null) skillSelectPanel.SetActive(state == GameState.SkillSelect);
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

    private void CreateStoryPanel()
    {
        Transform parentCanvas = startPanel != null ? startPanel.transform.parent : transform;
        storyPanel = new GameObject("StoryPanel", typeof(RectTransform), typeof(StoryController));
        storyPanel.transform.SetParent(parentCanvas, false);

        RectTransform rect = storyPanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ShowBossWarning()
    {
        StartCoroutine(BossWarningRoutine());
    }

    private System.Collections.IEnumerator BossWarningRoutine()
    {
        Transform parentCanvas = hudPanel != null ? hudPanel.transform.parent : transform;
        GameObject warningObj = new GameObject("BossWarningBanner", typeof(RectTransform), typeof(Image));
        warningObj.transform.SetParent(parentCanvas, false);

        RectTransform rect = warningObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.42f);
        rect.anchorMax = new Vector2(1f, 0.58f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = warningObj.GetComponent<Image>();
        bg.raycastTarget = false;

        GameObject textObj = new GameObject("WarningText", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(warningObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text warningText = textObj.GetComponent<Text>();
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        warningText.font = uiFont;
        warningText.text = "⚠ WARNING // 偵測到巨大敵方旗艦反應 ⚠";
        warningText.fontSize = 32;
        warningText.fontStyle = FontStyle.Bold;
        warningText.alignment = TextAnchor.MiddleCenter;
        warningText.raycastTarget = false;

        float elapsed = 0f;
        float duration = 2.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 5f, 1f);
            bg.color = new Color(0.6f, 0f, 0f, 0.25f + pulse * 0.45f);
            warningText.color = Color.Lerp(new Color(1f, 0.2f, 0.2f, 1f), new Color(1f, 0.9f, 0.2f, 1f), pulse);
            yield return null;
        }

        Destroy(warningObj);
    }

    private void CreateSkillSelectPanel()
    {
        Transform parentCanvas = startPanel != null ? startPanel.transform.parent : transform;
        skillSelectPanel = new GameObject("SkillSelectPanel", typeof(RectTransform), typeof(Image));
        skillSelectPanel.transform.SetParent(parentCanvas, false);

        RectTransform panelRect = skillSelectPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        skillSelectPanel.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.92f);

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject titleObj = new GameObject("SkillTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(skillSelectPanel.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.76f);
        titleRect.anchorMax = new Vector2(0.9f, 0.88f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        Text titleText = titleObj.GetComponent<Text>();
        titleText.font = uiFont;
        titleText.text = "第二關戰術強化 // 請選擇一項技能";
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.3f, 0.9f, 1f, 1f);

        CreateSkillCard(skillSelectPanel.transform, uiFont, 0, "【輔助戰機】", "旁邊多出 2 台輔助戰機\n協助低傷害射擊\n每台可幫忙抵擋 1 次攻擊", new Color(0.12f, 0.25f, 0.42f, 0.95f), () => GameManager.Instance.SelectSkill(SkillType.Wingmen));
        CreateSkillCard(skillSelectPanel.transform, uiFont, 1, "【自動蓄力】", "蓄力攻擊自動蓄能\n蓄滿後自動施放\n強力貫穿雷射", new Color(0.18f, 0.28f, 0.18f, 0.95f), () => GameManager.Instance.SelectSkill(SkillType.AutoCharge));
        CreateSkillCard(skillSelectPanel.transform, uiFont, 2, "【背水一戰】", "玩家血量變為只剩 1 滴\n但所有武器與雷射\n傷害提升為 3 倍！", new Color(0.38f, 0.14f, 0.18f, 0.95f), () => GameManager.Instance.SelectSkill(SkillType.GlassCannon));
    }

    private void CreateSkillCard(Transform parent, Font font, int index, string title, string desc, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        float minX = 0.08f + index * 0.29f;
        float maxX = minX + 0.26f;

        GameObject cardObj = new GameObject($"SkillCard_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        cardObj.transform.SetParent(parent, false);
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(minX, 0.22f);
        rect.anchorMax = new Vector2(maxX, 0.70f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        cardObj.GetComponent<Image>().color = bgColor;
        cardObj.GetComponent<Button>().onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(cardObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.08f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text label = textObj.GetComponent<Text>();
        label.font = font;
        label.text = $"{title}\n\n{desc}";
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
    }
}
