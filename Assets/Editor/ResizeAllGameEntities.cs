#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ResizeAllGameEntities
{
    static ResizeAllGameEntities()
    {
        EditorApplication.delayCall += ExecuteResize;
    }

    [MenuItem("ThunderFighter/Resize All Game Entities & Lengthen Game")]
    public static void ExecuteResize()
    {
        // 1. 放大玩家 Prefab (從 3.0 -> 3.8)
        string playerPath = "Assets/_Project/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (playerPrefab != null)
        {
            playerPrefab.transform.localScale = new Vector3(3.8f, 3.8f, 1f);
            EditorUtility.SetDirty(playerPrefab);
        }

        GameObject scenePlayer = GameObject.Find("Player");
        if (scenePlayer != null)
        {
            Undo.RecordObject(scenePlayer.transform, "Resize Scene Player");
            scenePlayer.transform.localScale = new Vector3(3.8f, 3.8f, 1f);
            EditorUtility.SetDirty(scenePlayer);
        }

        // 2. 放大所有小怪 Prefabs (從 2.2 -> 3.0)
        string[] minionNames = { "Minion_TinyShip1", "Minion_TinyShip2", "Minion_TinyShip5", "Minion_TinyShip6", "Minion_TinyShip17" };
        foreach (string name in minionNames)
        {
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            GameObject minion = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (minion != null)
            {
                minion.transform.localScale = new Vector3(3.0f, 3.0f, 1f);
                EditorUtility.SetDirty(minion);
            }
        }

        // 3. 放大精英怪與 Boss Prefabs (保持層次感)
        string[] eliteNames = { "Elite_TinyShip7", "Elite_TinyShip9", "Elite_TinyShip10", "Elite_TinyShip14", "Elite_TinyShip16" };
        foreach (string name in eliteNames)
        {
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            GameObject elite = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (elite != null)
            {
                elite.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
                EditorUtility.SetDirty(elite);
            }
        }

        string bossPath = "Assets/_Project/Prefabs/Enemies/Boss_TinyShip15.prefab";
        GameObject boss = AssetDatabase.LoadAssetAtPath<GameObject>(bossPath);
        if (boss != null)
        {
            boss.transform.localScale = new Vector3(7.2f, 7.2f, 1f);
            EditorUtility.SetDirty(boss);
        }

        // 4. 放大子彈 Prefabs (從 0.4~0.5 -> 0.8)
        string bulletPath = "Assets/_Project/Prefabs/PlayerBullet.prefab";
        GameObject bullet = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
        if (bullet != null)
        {
            bullet.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            EditorUtility.SetDirty(bullet);
        }

        string enemyBulletPath = "Assets/_Project/Prefabs/EnemyBullet.prefab";
        GameObject enemyBullet = AssetDatabase.LoadAssetAtPath<GameObject>(enemyBulletPath);
        if (enemyBullet != null)
        {
            enemyBullet.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            EditorUtility.SetDirty(enemyBullet);
        }

        // 5. 放大道具 Prefabs (從 0.9 -> 1.5，視覺極其清晰醒目)
        string[] itemNames = { "Item_PowerUp", "Item_WeaponSwitch", "Item_Heal" };
        foreach (string name in itemNames)
        {
            string path = $"Assets/_Project/Prefabs/Items/{name}.prefab";
            GameObject item = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (item != null)
            {
                item.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                EditorUtility.SetDirty(item);
            }
        }

        // 6. 同步調整 GameManager 門檻
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            SerializedObject so = new SerializedObject(gm);
            so.FindProperty("bossSpawnThreshold").intValue = 6000;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gm);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("<color=#00FF88>【調整完成】玩家、小怪、子彈、道具已全體放大，且關卡 Boss 出現門檻已調升至 6000 分！</color>");
    }
}
#endif
