#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupGameManagerInScene
{
    static SetupGameManagerInScene()
    {
        EditorApplication.delayCall += EnsureGameManager;
    }

    [MenuItem("ThunderFighter/Setup GameManager in Scene")]
    public static void EnsureGameManager()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
            Debug.Log("<color=#00FF88>【成功】已自動在場景中建立 GameManager 管理器！</color>");
        }
    }
}
#endif
