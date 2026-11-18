#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SetupSpaceBackgroundAndCockpit
{
    static SetupSpaceBackgroundAndCockpit()
    {
        EditorApplication.delayCall += ExecuteSetup;
    }

    [MenuItem("ThunderFighter/Setup Space Background & Cockpit Frame")]
    public static void ExecuteSetup()
    {
        string texturesDir = "Assets/_Project/Textures";
        if (!AssetDatabase.IsValidFolder(texturesDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Textures");
        }

        // 1. 生成深邃太空星雲貼圖 (SpaceNebula.png)
        string spaceTexPath = texturesDir + "/SpaceNebula.png";
        if (!File.Exists(spaceTexPath))
        {
            int width = 512;
            int height = 1024;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 深邃宇宙基底藍黑色
                    float u = (float)x / width;
                    float v = (float)y / height;

                    // 柏林雜訊產生星雲光澤
                    float n1 = Mathf.PerlinNoise(u * 3f, v * 3f);
                    float n2 = Mathf.PerlinNoise(u * 6f + 5.2f, v * 6f + 3.1f);
                    Color nebulaColor = Color.Lerp(new Color(0.02f, 0.03f, 0.08f), new Color(0.08f, 0.05f, 0.2f), n1);
                    nebulaColor += new Color(0.04f, 0.08f, 0.16f) * n2;

                    // 隨機繁星
                    float starChance = Random.value;
                    if (starChance > 0.994f)
                    {
                        float brightness = Random.Range(0.6f, 1f);
                        nebulaColor = new Color(brightness, brightness, brightness * 1.1f);
                    }

                    tex.SetPixel(x, y, nebulaColor);
                }
            }
            tex.Apply();
            File.WriteAllBytes(spaceTexPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(spaceTexPath, ImportAssetOptions.ForceUpdate);

            TextureImporter ti = AssetImporter.GetAtPath(spaceTexPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
        }

        // 2. 建立場景中的 BackgroundScroller 物件
        Sprite spaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spaceTexPath);
        GameObject bgParent = GameObject.Find("SpaceBackground");
        if (bgParent == null)
        {
            bgParent = new GameObject("SpaceBackground");
            Undo.RegisterCreatedObjectUndo(bgParent, "Create SpaceBackground");
        }

        // 清理原有子物件避免重複
        while (bgParent.transform.childCount > 0)
        {
            Object.DestroyImmediate(bgParent.transform.GetChild(0).gameObject);
        }

        float bgHeight = 15f;
        float bgWidth = 8.5f; // 覆蓋 9:16 邊界範圍

        GameObject bg1 = new GameObject("Nebula_1");
        bg1.transform.SetParent(bgParent.transform, false);
        bg1.transform.position = new Vector3(0, 0, 10f); // 放置在背景層深處
        SpriteRenderer sr1 = bg1.AddComponent<SpriteRenderer>();
        sr1.sprite = spaceSprite;
        sr1.drawMode = SpriteDrawMode.Sliced;
        sr1.size = new Vector2(bgWidth, bgHeight);
        sr1.sortingOrder = -100;

        GameObject bg2 = new GameObject("Nebula_2");
        bg2.transform.SetParent(bgParent.transform, false);
        bg2.transform.position = new Vector3(0, bgHeight, 10f);
        SpriteRenderer sr2 = bg2.AddComponent<SpriteRenderer>();
        sr2.sprite = spaceSprite;
        sr2.drawMode = SpriteDrawMode.Sliced;
        sr2.size = new Vector2(bgWidth, bgHeight);
        sr2.sortingOrder = -100;

        BackgroundScroller scroller = bgParent.GetComponent<BackgroundScroller>();
        if (scroller == null) scroller = bgParent.AddComponent<BackgroundScroller>();
        scroller.SetupChildren(bg1.transform, bg2.transform, bgHeight);

        // 3. 在 Canvas 上建立邊界外的太空船駕駛艙裝甲介面 (Cockpit Frame)
        GameObject canvasObj = GameObject.Find("GameCanvas");
        if (canvasObj != null)
        {
            Transform cockpit = canvasObj.transform.Find("CockpitFrame");
            if (cockpit == null)
            {
                GameObject cockpitObj = new GameObject("CockpitFrame");
                cockpitObj.transform.SetParent(canvasObj.transform, false);
                cockpit = cockpitObj.transform;
                cockpit.SetAsFirstSibling(); // 置於 UI 最下層但高於世界場景
            }

            // 清除現有面板子物件
            while (cockpit.childCount > 0)
            {
                Object.DestroyImmediate(cockpit.GetChild(0).gameObject);
            }

            Color steelColor = new Color(0.08f, 0.1f, 0.14f, 1f); // 駕駛艙金屬暗鋼色
            Color neonCyan = new Color(0.1f, 0.8f, 1f, 0.4f);

            // 左側駕駛艙操作儀表側板 (乾淨科技鋼板，不生成雜亂文字)
            GameObject leftPanel = CreateCockpitPanel("LeftPanel", cockpit, steelColor, new Vector2(0f, 0f), new Vector2(0.24f, 1f));
            CreateNeonDivider("LeftDivider", leftPanel.transform, neonCyan, new Vector2(0.5f, 0.5f), new Vector2(180, 2));

            // 右側駕駛艙戰術儀表側板 (乾淨科技鋼板，不生成雜亂文字)
            GameObject rightPanel = CreateCockpitPanel("RightPanel", cockpit, steelColor, new Vector2(0.76f, 0f), new Vector2(1f, 1f));
            CreateNeonDivider("RightDivider", rightPanel.transform, neonCyan, new Vector2(0.5f, 0.5f), new Vector2(180, 2));

            // 頂部遮罩橫樑
            CreateCockpitPanel("TopHeader", cockpit, new Color(0.05f, 0.07f, 0.1f, 1f), new Vector2(0.24f, 0.94f), new Vector2(0.76f, 1f));

            // 底部操縱台橫樑
            CreateCockpitPanel("BottomConsole", cockpit, new Color(0.05f, 0.07f, 0.1f, 1f), new Vector2(0.24f, 0f), new Vector2(0.76f, 0.06f));
        }

        AssetDatabase.SaveAssets();
        Debug.Log("<color=#00FF88>【成功】已建立無限垂直捲動太空星雲背景與直式邊界外太空船駕駛艙戰術面板！</color>");
    }

    private static GameObject CreateCockpitPanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static void CreatePanelText(string name, Transform parent, string text, int fontSize, Vector2 anchor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.3f, 0.85f, 1f, 0.85f); // 科幻螢光青藍色

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(240, 120);
    }

    private static void CreateNeonDivider(string name, Transform parent, Color color, Vector2 anchor, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }
}
#endif
