#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupEnemyPrefabsAndSpawner
{
    static SetupEnemyPrefabsAndSpawner()
    {
        EditorApplication.delayCall += ExecuteSetup;
    }

    [MenuItem("ThunderFighter/Setup Enemy Prefabs & Spawner")]
    public static void ExecuteSetup()
    {
        string prefabsDir = "Assets/_Project/Prefabs/Enemies";
        string bulletDir = "Assets/_Project/Prefabs";

        // 1. 建立敵方專用子彈 Prefab (若不存在)
        string enemyBulletPath = bulletDir + "/EnemyBullet.prefab";
        GameObject enemyBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyBulletPath);
        if (enemyBulletPrefab == null)
        {
            GameObject bulletObj = new GameObject("EnemyBullet");
            SpriteRenderer sr = bulletObj.AddComponent<SpriteRenderer>();
            Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (defaultSprite == null) defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.sprite = defaultSprite;
            sr.color = new Color(1f, 0.2f, 0.2f); // 鮮紅敵彈
            bulletObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

            CircleCollider2D col = bulletObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            bulletObj.AddComponent<Projectile>();
            enemyBulletPrefab = PrefabUtility.SaveAsPrefabAsset(bulletObj, enemyBulletPath);
            Object.DestroyImmediate(bulletObj);
        }

        // 2. 製作小怪 Prefabs (TinyShip1, 2, 5, 6, 17) - 體積 Scale 2.2f，血量 2，速度 4.5f
        string[] minionSources = { "TinyShip1", "TinyShip2", "TinyShip5", "TinyShip6", "TinyShip17" };
        List<GameObject> generatedMinions = new List<GameObject>();
        foreach (string name in minionSources)
        {
            string srcPath = $"Assets/Tiny Ships/Prefabs/{name}.prefab";
            string dstPath = $"{prefabsDir}/Minion_{name}.prefab";
            GameObject minion = SetupEnemyPrefab(srcPath, dstPath, EnemyType.Minion, 2.2f, 2, 4.5f, 100, false, 0f, null);
            if (minion != null) generatedMinions.Add(minion);
        }

        // 3. 製作精英怪 Prefabs (TinyShip7, 9, 10, 14, 16) - 體積 Scale 3.8f (明顯大於小怪)，血量 8，速度 2f，定時射擊
        string[] eliteSources = { "TinyShip7", "TinyShip9", "TinyShip10", "TinyShip14", "TinyShip16" };
        List<GameObject> generatedElites = new List<GameObject>();
        foreach (string name in eliteSources)
        {
            string srcPath = $"Assets/Tiny Ships/Prefabs/{name}.prefab";
            string dstPath = $"{prefabsDir}/Elite_{name}.prefab";
            GameObject elite = SetupEnemyPrefab(srcPath, dstPath, EnemyType.Elite, 3.8f, 8, 2.0f, 300, true, 2.0f, enemyBulletPrefab);
            if (elite != null) generatedElites.Add(elite);
        }

        // 4. 製作 Boss Prefab (TinyShip15) - 體積 Scale 6.0f (巨大首領)，血量 150，扇形彈幕
        string bossSrcPath = "Assets/Tiny Ships/Prefabs/TinyShip15.prefab";
        string bossDstPath = $"{prefabsDir}/Boss_TinyShip15.prefab";
        GameObject bossPrefab = SetupEnemyPrefab(bossSrcPath, bossDstPath, EnemyType.Boss, 6.0f, 150, 1.8f, 2000, true, 2.2f, enemyBulletPrefab);

        // 5. 在場景中建立或更新 EnemySpawner
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("EnemySpawner");
            spawner = spawnerObj.AddComponent<EnemySpawner>();
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create EnemySpawner");
        }

        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty minionsProp = so.FindProperty("minionPrefabs");
        minionsProp.ClearArray();
        for (int i = 0; i < generatedMinions.Count; i++)
        {
            minionsProp.InsertArrayElementAtIndex(i);
            minionsProp.GetArrayElementAtIndex(i).objectReferenceValue = generatedMinions[i];
        }

        SerializedProperty elitesProp = so.FindProperty("elitePrefabs");
        elitesProp.ClearArray();
        for (int i = 0; i < generatedElites.Count; i++)
        {
            elitesProp.InsertArrayElementAtIndex(i);
            elitesProp.GetArrayElementAtIndex(i).objectReferenceValue = generatedElites[i];
        }

        so.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);

        Debug.Log("<color=#00FF88>【成功】已完成所有敵機 (5款小怪、5款精英怪、1款Boss) Prefabs 製作與 EnemySpawner 配置！</color>");
        AssetDatabase.SaveAssets();
    }

    private static GameObject SetupEnemyPrefab(string srcPath, string dstPath, EnemyType type, float scale, int hp, float speed, int score, bool canShoot, float fireInterval, GameObject bulletPrefab)
    {
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (src == null)
        {
            Debug.LogWarning($"找不到來源 Prefab: {srcPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(src);
        instance.name = System.IO.Path.GetFileNameWithoutExtension(dstPath);
        instance.tag = "Enemy";
        instance.transform.localScale = new Vector3(scale, scale, 1f);

        // Collider2D
        Collider2D col = instance.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = instance.AddComponent<CircleCollider2D>();
            circle.radius = 0.45f;
            circle.isTrigger = true;
        }

        // EnemyController
        EnemyController ec = instance.GetComponent<EnemyController>();
        if (ec == null)
        {
            ec = instance.AddComponent<EnemyController>();
        }

        SerializedObject so = new SerializedObject(ec);
        so.FindProperty("enemyType").enumValueIndex = (int)type;
        so.FindProperty("maxHealth").intValue = hp;
        so.FindProperty("moveSpeed").floatValue = speed;
        so.FindProperty("scoreValue").intValue = score;
        so.FindProperty("canShoot").boolValue = canShoot;
        so.FindProperty("fireInterval").floatValue = fireInterval;
        if (bulletPrefab != null)
        {
            so.FindProperty("enemyBulletPrefab").objectReferenceValue = bulletPrefab;
        }
        so.ApplyModifiedProperties();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, dstPath);
        Object.DestroyImmediate(instance);
        return saved;
    }
}
#endif
