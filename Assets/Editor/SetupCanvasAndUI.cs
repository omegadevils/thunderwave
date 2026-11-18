#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SetupCanvasAndUI
{
    static SetupCanvasAndUI()
    {
        EditorApplication.delayCall += EnsureUI;
    }

    [MenuItem("ThunderFighter/Setup Full UI & Canvas")]
    public static void EnsureUI()
    {
        // 確保場景中有 EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 確保場景中有 AudioManager
        if (Object.FindFirstObjectByType<AudioManager>() == null)
        {
            GameObject am = new GameObject("AudioManager");
            am.AddComponent<AudioManager>();
        }

        UIManager existingUI = Object.FindFirstObjectByType<UIManager>();
        if (existingUI != null) return;

        // 建立 Canvas
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // 9:16 直式解析度
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        UIManager uiManager = canvasObj.AddComponent<UIManager>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. HUD Panel
        GameObject hudPanel = CreatePanel("HUDPanel", canvasObj.transform, Color.clear);
        Text scoreText = CreateText("ScoreText", hudPanel.transform, "SCORE: 0", 42, defaultFont, TextAnchor.UpperLeft, new Vector2(50, -50), new Vector2(400, 80));
        Text stageText = CreateText("StageText", hudPanel.transform, "STAGE 1", 38, defaultFont, TextAnchor.UpperCenter, new Vector2(0, -50), new Vector2(300, 80));
        Text hpText = CreateText("HpText", hudPanel.transform, "HP: 3/3", 42, defaultFont, TextAnchor.UpperRight, new Vector2(-50, -50), new Vector2(300, 80));
        Text weaponText = CreateText("WeaponText", hudPanel.transform, "WEAPON: Vulcan Lv.1", 34, defaultFont, TextAnchor.LowerLeft, new Vector2(50, 90), new Vector2(500, 60));
        Text bombText = CreateText("BombText", hudPanel.transform, "BOMB: 3/3 (C)", 34, defaultFont, TextAnchor.LowerLeft, new Vector2(50, 40), new Vector2(400, 60));

        // 蓄力 Slider
        GameObject sliderObj = new GameObject("ChargeSlider");
        sliderObj.transform.SetParent(hudPanel.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0f);
        sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0, 120);
        sliderRect.sizeDelta = new Vector2(400, 24);

        GameObject bgObj = CreateImage("Background", sliderObj.transform, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        SetFullStretch(bgObj.GetComponent<RectTransform>());
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        SetFullStretch(fillArea.AddComponent<RectTransform>());
        GameObject fill = CreateImage("Fill", fillArea.transform, new Color(1f, 0.85f, 0.1f));
        SetFullStretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();

        // 2. Start Panel
        GameObject startPanel = CreatePanel("StartPanel", canvasObj.transform, new Color(0, 0, 0, 0.85f));
        CreateText("Title", startPanel.transform, "THUNDER FIGHTER", 76, defaultFont, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(800, 150));
        Button startBtn = CreateButton("StartButton", startPanel.transform, "START GAME", defaultFont, new Vector2(0, -100), new Vector2(400, 100));

        // 3. Pause Panel
        GameObject pausePanel = CreatePanel("PausePanel", canvasObj.transform, new Color(0, 0, 0, 0.85f));
        CreateText("PauseTitle", pausePanel.transform, "GAME PAUSED", 68, defaultFont, TextAnchor.MiddleCenter, new Vector2(0, 350), new Vector2(600, 120));
        Button resumeBtn = CreateButton("ResumeButton", pausePanel.transform, "RESUME (P)", defaultFont, new Vector2(0, 120), new Vector2(380, 90));
        Button pauseRestartBtn = CreateButton("RestartButton", pausePanel.transform, "RESTART", defaultFont, new Vector2(0, 0), new Vector2(380, 90));
        Button soundToggleBtn = CreateButton("SoundToggleButton", pausePanel.transform, "SOUND: ON", defaultFont, new Vector2(0, -120), new Vector2(380, 90));
        Text soundText = soundToggleBtn.GetComponentInChildren<Text>();

        // 4. End Panel
        GameObject endPanel = CreatePanel("EndPanel", canvasObj.transform, new Color(0, 0, 0, 0.9f));
        Text endTitle = CreateText("EndTitle", endPanel.transform, "VICTORY!", 80, defaultFont, TextAnchor.MiddleCenter, new Vector2(0, 350), new Vector2(700, 150));
        Text finalScore = CreateText("FinalScore", endPanel.transform, "FINAL SCORE: 0", 48, defaultFont, TextAnchor.MiddleCenter, new Vector2(0, 180), new Vector2(600, 100));
        Button nextStageBtn = CreateButton("NextStageButton", endPanel.transform, "NEXT STAGE", defaultFont, new Vector2(0, 20), new Vector2(420, 100));
        Button endRestartBtn = CreateButton("EndRestartButton", endPanel.transform, "PLAY AGAIN", defaultFont, new Vector2(0, -120), new Vector2(420, 100));

        // 綁定到 UIManager
        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("startPanel").objectReferenceValue = startPanel;
        so.FindProperty("hudPanel").objectReferenceValue = hudPanel;
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("endPanel").objectReferenceValue = endPanel;

        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("stageText").objectReferenceValue = stageText;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("weaponText").objectReferenceValue = weaponText;
        so.FindProperty("bombText").objectReferenceValue = bombText;
        so.FindProperty("chargeSlider").objectReferenceValue = slider;

        so.FindProperty("startButton").objectReferenceValue = startBtn;
        so.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
        so.FindProperty("pauseRestartButton").objectReferenceValue = pauseRestartBtn;
        so.FindProperty("soundToggleButton").objectReferenceValue = soundToggleBtn;
        so.FindProperty("soundToggleText").objectReferenceValue = soundText;

        so.FindProperty("endTitleText").objectReferenceValue = endTitle;
        so.FindProperty("finalScoreText").objectReferenceValue = finalScore;
        so.FindProperty("endRestartButton").objectReferenceValue = endRestartBtn;
        so.FindProperty("nextStageButton").objectReferenceValue = nextStageBtn;

        so.ApplyModifiedProperties();

        // 初始顯隱 (僅顯示 StartPanel)
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);
        endPanel.SetActive(false);

        Debug.Log("<color=#00FF88>【成功】已建立完整 Canvas 與 4 大面板 (Start, HUD, Pause, End) 及預留下一關！</color>");
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        SetFullStretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, Font font, TextAnchor anchor, Vector2 pos, Vector2 size)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.font = font;
        text.alignment = anchor;
        text.color = Color.white;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        if (anchor == TextAnchor.UpperLeft) { rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); }
        else if (anchor == TextAnchor.UpperRight) { rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); }
        else if (anchor == TextAnchor.UpperCenter) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1); }
        else if (anchor == TextAnchor.LowerLeft) { rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0); }
        else { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); }

        rt.anchoredPosition = pos;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Font font, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.55f, 0.9f, 0.9f);
        Button btn = btnObj.AddComponent<Button>();

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = 32;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        SetFullStretch(textObj.GetComponent<RectTransform>());

        return btn;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imgObj = new GameObject(name);
        imgObj.transform.SetParent(parent, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = color;
        return imgObj;
    }
}
#endif
