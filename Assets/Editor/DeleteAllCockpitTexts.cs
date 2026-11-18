#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DeleteAllCockpitTexts
{
    static DeleteAllCockpitTexts()
    {
        EditorApplication.delayCall += ExecuteDelete;
    }

    [MenuItem("ThunderFighter/Delete All Cockpit Texts")]
    public static void ExecuteDelete()
    {
        // 尋找 CockpitFrame
        GameObject cockpitObj = GameObject.Find("CockpitFrame");
        if (cockpitObj != null)
        {
            // 找出所有掛在 CockpitFrame 底下的 Text 元件並徹底刪除
            UnityEngine.UI.Text[] texts = cockpitObj.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var t in texts)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
            }
            Debug.Log($"<color=#00FF88>【清除成功】已徹底刪除駕駛艙介面內的所有文字 ({texts.Length} 個文字物件)！</color>");
        }

        // 獨立檢查常見名稱
        string[] targetNames = { "LeftTitle", "LeftSub", "RightTitle", "RightSub", "ConsoleMark" };
        foreach (string name in targetNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
