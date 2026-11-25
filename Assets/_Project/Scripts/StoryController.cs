using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 開頭劇情控制器
/// 負責在按下開始遊戲後播放戰前簡報劇情，支援打字機效果、點擊/空白鍵推進與略過功能
/// </summary>
public class StoryController : MonoBehaviour
{
    [Header("第一關劇情文本")]
    [TextArea(2, 4)]
    [SerializeField] private string[] storyLines = new string[]
    {
        "西元 2147 年，未知外星艦隊『暗影軍團』突破了太陽系外圍防線。",
        "地球聯合防衛軍主力艦隊在木星軌道陷入苦戰，通訊幾乎全面中斷。",
        "作為最後的王牌——『雷霆戰機』駕駛員，你奉命單機突入敵陣核心。",
        "長官：「任務目標只有一個：突破封鎖線，擊潰敵方母艦。祝你好運，王牌！」",
        "引擎點火完畢，武器系統上線……準備迎擊！"
    };

    [Header("第二關劇情文本")]
    [TextArea(2, 4)]
    [SerializeField] private string[] stage2StoryLines = new string[]
    {
        "成功突破第一道封鎖線！雷霆戰機已深入木星軌道的小行星帶。",
        "長官：「幹得好！但偵測到敵方主力艦隊正從側翼包夾，航道已切換為橫向突圍模式。」",
        "警告：前方敵機裝甲強度大幅提升，請善用蓄力雷射與全屏炸彈！",
        "全系統超載運轉，準備迎接第二波決戰！"
    };

    [Header("第一關戰敗劇情 (依擊敗者區分)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] stage1DefeatMinionLines = new string[]
    {
        "警報：機翼遭敵方小型偵察機密集撞擊，推進器嚴重受損！",
        "副官：「居然在第一道防線就被偵察機群包圍了……緊急彈射逃生！」"
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] stage1DefeatEliteLines = new string[]
    {
        "警告：重型砲艇的直擊火力貫穿了雷霆戰機的主裝甲！",
        "長官：「敵方重型砲艇的火力比預估更強，快撤離這片空域！」"
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] stage1DefeatBossLines = new string[]
    {
        "轟隆——！第一關首領的扇形彈幕徹底壓制了雷霆戰機的護盾。",
        "首領通訊：「就憑這點本事，也想突破太陽系封鎖線嗎？」"
    };

    [Header("第二關戰敗劇情 (依擊敗者區分)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] stage2DefeatMinionLines = new string[]
    {
        "在木星小行星帶的橫向突圍中，雷霆戰機不幸遭高速偵察機群封鎖退路。",
        "副官：「裝甲強化後的敵機數量太多了，我們撐不住了！」"
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] stage2DefeatEliteLines = new string[]
    {
        "強化型重型砲艇的側翼齊射擊中了核心反應爐！",
        "長官：「第二波敵軍的火力太猛烈了……王牌，立刻棄機！」"
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] stage2DefeatBossLines = new string[]
    {
        "在最後的決戰中，雷霆戰機終究沒能擋下第二關首領的毀滅性彈幕。",
        "通訊中斷前的最後一刻，木星軌道上只餘下暗影軍團旗艦的冰冷身影……"
    };

    private string[] activeLines;
    private Text titleText;

    [Header("打字機效果設定")]
    [SerializeField] private float charsPerSecond = 35f;

    [Header("UI 元件 (可留空由程式自動生成)")]
    [SerializeField] private Text storyText;
    [SerializeField] private Text promptText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button nextButton;

    private int currentLineIndex = 0;
    private float charTimer = 0f;
    private int visibleCharCount = 0;
    private bool isLineComplete = false;
    private bool isInitialized = false;

    private void Awake()
    {
        EnsureUI();
    }

    private void OnEnable()
    {
        EnsureUI();
        StartStory();
    }

    private void Update()
    {
        if (!isInitialized || activeLines == null || activeLines.Length == 0) return;

        UpdateTypewriter();
        HandleInput();
    }

    /// <summary>
    /// 開始從第一句播放劇情
    /// </summary>
    public void StartStory()
    {
        bool isDefeat = GameManager.Instance != null && GameManager.Instance.IsDefeatStory;
        bool isStage2 = GameManager.Instance != null && GameManager.Instance.CurrentStage >= 2;

        if (isDefeat)
        {
            EnemyType attacker = GameManager.Instance.LastAttackerType;
            if (isStage2)
            {
                activeLines = attacker == EnemyType.Boss ? stage2DefeatBossLines
                            : attacker == EnemyType.Elite ? stage2DefeatEliteLines
                            : stage2DefeatMinionLines;
            }
            else
            {
                activeLines = attacker == EnemyType.Boss ? stage1DefeatBossLines
                            : attacker == EnemyType.Elite ? stage1DefeatEliteLines
                            : stage1DefeatMinionLines;
            }

            if (titleText != null)
            {
                titleText.text = "DEFEAT REPORT // 戰敗紀錄";
                titleText.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
        }
        else
        {
            activeLines = (isStage2 && stage2StoryLines != null && stage2StoryLines.Length > 0) ? stage2StoryLines : storyLines;
            if (titleText != null)
            {
                titleText.text = "MISSION BRIEFING // 戰前簡報";
                titleText.color = new Color(0.2f, 0.85f, 1f, 1f);
            }
        }

        currentLineIndex = 0;
        if (activeLines == null || activeLines.Length == 0)
        {
            FinishStory();
            return;
        }
        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        currentLineIndex = index;
        visibleCharCount = 0;
        charTimer = 0f;
        isLineComplete = false;

        if (storyText != null) storyText.text = "";
        if (promptText != null) promptText.text = "";
    }

    private void UpdateTypewriter()
    {
        if (isLineComplete) return;

        string fullLine = activeLines[currentLineIndex];
        charTimer += Time.unscaledDeltaTime * charsPerSecond;
        int targetCount = Mathf.Clamp(Mathf.FloorToInt(charTimer), 0, fullLine.Length);

        if (targetCount != visibleCharCount)
        {
            visibleCharCount = targetCount;
            if (storyText != null)
            {
                storyText.text = fullLine.Substring(0, visibleCharCount);
            }
        }

        if (visibleCharCount >= fullLine.Length)
        {
            CompleteCurrentLine();
        }
    }

    private void CompleteCurrentLine()
    {
        isLineComplete = true;
        if (storyText != null && currentLineIndex < activeLines.Length)
        {
            storyText.text = activeLines[currentLineIndex];
        }
        if (promptText != null)
        {
            bool isLast = currentLineIndex >= activeLines.Length - 1;
            bool isDefeat = GameManager.Instance != null && GameManager.Instance.IsDefeatStory;
            promptText.text = isLast
                ? (isDefeat ? "點擊畫面或按 [Space] 查看結算 >>" : "點擊畫面或按 [Space] 進入戰鬥 >>")
                : "點擊畫面或按 [Space] 繼續 ▼";
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FinishStory();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Advance();
        }
    }

    /// <summary>
    /// 推進劇情：若當前句子尚未顯示完則立即顯示全文，否則切換下一句或結束劇情
    /// </summary>
    public void Advance()
    {
        if (!isLineComplete)
        {
            CompleteCurrentLine();
            return;
        }

        if (currentLineIndex < activeLines.Length - 1)
        {
            ShowLine(currentLineIndex + 1);
        }
        else
        {
            FinishStory();
        }
    }

    /// <summary>
    /// 結束或略過劇情，通知 GameManager 進入正式關卡
    /// </summary>
    public void FinishStory()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FinishStory();
        }
    }

    /// <summary>
    /// 若未在 Inspector 手動綁定 UI，則自動建立深色科幻簡報面板
    /// </summary>
    private void EnsureUI()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (storyText != null && skipButton != null && nextButton != null)
        {
            skipButton.onClick.AddListener(FinishStory);
            nextButton.onClick.AddListener(Advance);
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 全螢幕點擊推進按鈕 + 深色半透明背景
        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.12f, 0.94f);

        nextButton = GetComponent<Button>();
        if (nextButton == null) nextButton = gameObject.AddComponent<Button>();
        nextButton.transition = Selectable.Transition.None;
        nextButton.onClick.AddListener(Advance);

        // 標題：戰前簡報
        GameObject titleObj = new GameObject("StoryTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.72f);
        titleRect.anchorMax = new Vector2(0.9f, 0.84f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        titleText = titleObj.GetComponent<Text>();
        titleText.font = uiFont;
        titleText.text = "MISSION BRIEFING // 戰前簡報";
        titleText.fontSize = 30;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.2f, 0.85f, 1f, 1f);

        // 對話框背景
        GameObject boxObj = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image));
        boxObj.transform.SetParent(transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.08f, 0.22f);
        boxRect.anchorMax = new Vector2(0.92f, 0.68f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;
        boxObj.GetComponent<Image>().color = new Color(0.06f, 0.1f, 0.2f, 0.85f);

        // 劇情內文
        GameObject textObj = new GameObject("StoryText", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(boxObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.06f, 0.18f);
        textRect.anchorMax = new Vector2(0.94f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        storyText = textObj.GetComponent<Text>();
        storyText.font = uiFont;
        storyText.fontSize = 28;
        storyText.alignment = TextAnchor.MiddleLeft;
        storyText.color = Color.white;
        storyText.supportRichText = true;

        // 繼續提示文字
        GameObject promptObj = new GameObject("PromptText", typeof(RectTransform), typeof(Text));
        promptObj.transform.SetParent(boxObj.transform, false);
        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.06f, 0.04f);
        promptRect.anchorMax = new Vector2(0.94f, 0.18f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;

        promptText = promptObj.GetComponent<Text>();
        promptText.font = uiFont;
        promptText.fontSize = 20;
        promptText.alignment = TextAnchor.MiddleRight;
        promptText.color = new Color(1f, 0.85f, 0.3f, 1f);

        // 略過按鈕 (右上角)
        GameObject skipObj = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skipObj.transform.SetParent(transform, false);
        RectTransform skipRect = skipObj.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.76f, 0.88f);
        skipRect.anchorMax = new Vector2(0.95f, 0.95f);
        skipRect.offsetMin = Vector2.zero;
        skipRect.offsetMax = Vector2.zero;

        skipObj.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.38f, 0.9f);
        skipButton = skipObj.GetComponent<Button>();
        skipButton.onClick.AddListener(FinishStory);

        GameObject skipLabelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        skipLabelObj.transform.SetParent(skipObj.transform, false);
        RectTransform skipLabelRect = skipLabelObj.GetComponent<RectTransform>();
        skipLabelRect.anchorMin = Vector2.zero;
        skipLabelRect.anchorMax = Vector2.one;
        skipLabelRect.offsetMin = Vector2.zero;
        skipLabelRect.offsetMax = Vector2.zero;

        Text skipLabel = skipLabelObj.GetComponent<Text>();
        skipLabel.font = uiFont;
        skipLabel.text = "略過劇情 >>";
        skipLabel.fontSize = 20;
        skipLabel.alignment = TextAnchor.MiddleCenter;
        skipLabel.color = new Color(0.85f, 0.92f, 1f, 1f);
    }
}

