#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupPlayerPrefab
{
    static SetupPlayerPrefab()
    {
        EditorApplication.delayCall += ExecuteSetup;
    }

    [MenuItem("ThunderFighter/Setup Player Prefab")]
    public static void ExecuteSetup()
    {
        string sourcePath = "Assets/Tiny Ships/Prefabs/TinyShip4.prefab";
        string targetDir = "Assets/_Project/Prefabs";
        string targetPath = targetDir + "/Player.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/_Project"))
        {
            AssetDatabase.CreateFolder("Assets", "_Project");
        }
        if (!AssetDatabase.IsValidFolder(targetDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }

        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (sourcePrefab == null)
        {
            Debug.LogError($"[SetupPlayerPrefab] 找不到來源 Prefab: {sourcePath}");
            return;
        }

        // 實例化 Prefab 進行配置
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
        instance.name = "Player";

        // 設定放大 3 倍 (Scale = 3, 3, 3)
        instance.transform.localScale = new Vector3(3f, 3f, 3f);

        // 確保有 PlayerController
        PlayerController controller = instance.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = instance.AddComponent<PlayerController>();
        }

        // 確保有 Rigidbody2D 並設定參數
        Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = instance.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 確保有碰撞體 Collider2D
        Collider2D col = instance.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circleCol = instance.AddComponent<CircleCollider2D>();
            circleCol.radius = 0.4f;
            circleCol.isTrigger = true;
        }

        // 儲存為專案專屬的 Player Prefab
        bool success;
        PrefabUtility.SaveAsPrefabAsset(instance, targetPath, out success);
        Object.DestroyImmediate(instance);

        if (success)
        {
            Debug.Log($"<color=#00FF88>【成功】已建立玩家 Prefab（放大 3 倍）並掛載 PlayerController：{targetPath}</color>");
        }
        else
        {
            Debug.LogError($"[SetupPlayerPrefab] 儲存 Prefab 失敗：{targetPath}");
        }

        // 同步更新當前開啟場景中的 Player 物件 (若場景已有放置)
        GameObject scenePlayer = GameObject.Find("Player");
        if (scenePlayer != null)
        {
            Undo.RecordObject(scenePlayer.transform, "Scale Player 3x");
            scenePlayer.transform.localScale = new Vector3(3f, 3f, 3f);
            EditorUtility.SetDirty(scenePlayer);
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
