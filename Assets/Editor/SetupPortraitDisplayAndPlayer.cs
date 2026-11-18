#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupPortraitDisplayAndPlayer
{
    static SetupPortraitDisplayAndPlayer()
    {
        EditorApplication.delayCall += ExecuteSetup;
    }

    [MenuItem("ThunderFighter/Setup Portrait 9:16 & Position Player")]
    public static void ExecuteSetup()
    {
        // 1. 自動設定 Game View 為直式 9:16 (或 1080x1920)
        SetGameViewToPortrait();

        // 2. 調整主攝影機參數 (Orthographic 5，定位於 (0, 0, -10))
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        }

        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.5f; // 直式最佳視野大小
            mainCam.transform.position = new Vector3(0, 0, -10f);
            EditorUtility.SetDirty(mainCam);
        }

        // 3. 設定玩家起始位置在畫面下方中央 (例如 (0, -4.5, 0))
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Set Player Start Position");
            player.transform.position = new Vector3(0f, -4.5f, 0f);
            EditorUtility.SetDirty(player);
            Debug.Log("<color=#00FF88>【設定成功】已將場景中的 Player 定位在畫面下方：(0, -4.5, 0)</color>");
        }

        // 同步更新 Prefab 起始位置
        string playerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab != null)
        {
            playerPrefab.transform.position = new Vector3(0f, -4.5f, 0f);
            EditorUtility.SetDirty(playerPrefab);
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 透過反射在 Unity 編輯器將 Game 視窗切換為 9:16 直式解析度
    /// </summary>
    private static void SetGameViewToPortrait()
    {
        try
        {
            Type gameViewSizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            Type sizeTypeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");

            if (gameViewSizesType == null || gameViewType == null) return;

            PropertyInfo instanceProp = gameViewSizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            object instance = instanceProp.GetValue(null, null);

            MethodInfo getGroupMethod = gameViewSizesType.GetMethod("GetGroup");
            object group = getGroupMethod.Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });

            // 尋找是否已有 9:16 或新增自訂直式比例
            MethodInfo getCustomCount = group.GetType().GetMethod("get_CustomCount");
            MethodInfo getTotalCount = group.GetType().GetMethod("get_TotalCount");
            int total = (int)getTotalCount.Invoke(group, null);

            int targetIndex = -1;
            MethodInfo getGameViewSize = group.GetType().GetMethod("GetGameViewSize");

            for (int i = 0; i < total; i++)
            {
                object size = getGameViewSize.Invoke(group, new object[] { i });
                PropertyInfo baseTextProp = size.GetType().GetProperty("baseText");
                string name = (string)baseTextProp.GetValue(size, null);

                if (name != null && (name.Contains("9:16") || name.Contains("Portrait")))
                {
                    targetIndex = i;
                    break;
                }
            }

            // 若尚無 9:16，動態新增一個 9:16 Aspect Ratio
            if (targetIndex == -1)
            {
                ConstructorInfo constructor = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize")
                    .GetConstructor(new Type[] { sizeTypeType, typeof(int), typeof(int), typeof(string) });
                object newSize = constructor.Invoke(new object[] { 0 /* AspectRatio */, 9, 16, "9:16 Portrait" });

                MethodInfo addCustomSize = group.GetType().GetMethod("AddCustomSize");
                addCustomSize.Invoke(group, new object[] { newSize });
                targetIndex = (int)getTotalCount.Invoke(group, null) - 1;
            }

            // 將當前 Game 視窗套用該直式尺寸
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView != null && targetIndex >= 0)
            {
                PropertyInfo selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (selectedSizeIndexProp != null)
                {
                    selectedSizeIndexProp.SetValue(gameView, targetIndex, null);
                    gameView.Repaint();
                    Debug.Log("<color=#00FF88>【設定成功】Game 視窗已自動切換為 9:16 直式規格！</color>");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("自動切換直式 Game View 提示 (可手動於 Game 視窗選單選擇 9:16 或 1080x1920)：" + ex.Message);
        }
    }
}
#endif
