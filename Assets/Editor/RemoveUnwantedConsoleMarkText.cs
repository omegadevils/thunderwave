#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RemoveUnwantedConsoleMarkText
{
    static RemoveUnwantedConsoleMarkText()
    {
        EditorApplication.delayCall += ExecuteRemove;
    }

    [MenuItem("ThunderFighter/Clean Unwanted Text")]
    public static void ExecuteRemove()
    {
        GameObject consoleMark = GameObject.Find("ConsoleMark");
        if (consoleMark != null)
        {
            Undo.DestroyObjectImmediate(consoleMark);
            Debug.Log("<color=#00FF88>【成功】已從場景中移除不明底部文字 ConsoleMark！</color>");
        }

        // 檢查是否有其他殘留字串
        GameObject bottomConsole = GameObject.Find("BottomConsole");
        if (bottomConsole != null)
        {
            while (bottomConsole.transform.childCount > 0)
            {
                Undo.DestroyObjectImmediate(bottomConsole.transform.GetChild(0).gameObject);
            }
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
