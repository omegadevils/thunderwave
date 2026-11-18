#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupBulletPrefabs
{
    static SetupBulletPrefabs()
    {
        EditorApplication.delayCall += EnsurePrefabs;
    }

    [MenuItem("ThunderFighter/Setup Bullet & Player Prefabs")]
    public static void EnsurePrefabs()
    {
        string prefabsDir = "Assets/_Project/Prefabs";
        if (!AssetDatabase.IsValidFolder("Assets/_Project"))
        {
            AssetDatabase.CreateFolder("Assets", "_Project");
        }
        if (!AssetDatabase.IsValidFolder(prefabsDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }

        // 1. 建立普通子彈 Prefab (如果不存在)
        string bulletPath = prefabsDir + "/PlayerBullet.prefab";
        GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
        if (bulletPrefab == null)
        {
            GameObject bulletObj = new GameObject("PlayerBullet");
            SpriteRenderer sr = bulletObj.AddComponent<SpriteRenderer>();

            // 尋找 Unity 內建 Sprite 或是生成方塊 Sprite
            Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (defaultSprite == null)
            {
                defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }
            sr.sprite = defaultSprite;
            sr.color = Color.yellow;

            BoxCollider2D col = bulletObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 1f);

            bulletObj.AddComponent<Projectile>();

            bulletPrefab = PrefabUtility.SaveAsPrefabAsset(bulletObj, bulletPath);
            Object.DestroyImmediate(bulletObj);
            Debug.Log($"<color=#00FF88>【成功】已自動產生子彈 Prefab：{bulletPath}</color>");
        }

        // 2. 建立蓄力貫穿雷射 Prefab (如果不存在)
        string laserPath = prefabsDir + "/ChargeLaser.prefab";
        GameObject laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(laserPath);
        if (laserPrefab == null)
        {
            GameObject laserObj = new GameObject("ChargeLaser");
            SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
            Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.sprite = defaultSprite;
            sr.color = new Color(1f, 0.9f, 0.2f); // 金黃色

            BoxCollider2D col = laserObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);

            Projectile proj = laserObj.AddComponent<Projectile>();

            laserPrefab = PrefabUtility.SaveAsPrefabAsset(laserObj, laserPath);
            Object.DestroyImmediate(laserObj);
            Debug.Log($"<color=#00FF88>【成功】已自動產生蓄力雷射 Prefab：{laserPath}</color>");
        }

        // 3. 綁定到 Player.prefab 與場景中的 Player
        string playerPath = prefabsDir + "/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (playerPrefab != null)
        {
            PlayerController pc = playerPrefab.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject so = new SerializedObject(pc);
                so.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
                so.FindProperty("chargeLaserPrefab").objectReferenceValue = laserPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerPrefab);
            }
        }

        // 若場景中已有 Player 物件，同步綁定
        GameObject scenePlayer = GameObject.Find("Player");
        if (scenePlayer != null)
        {
            PlayerController pc = scenePlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject so = new SerializedObject(pc);
                so.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
                so.FindProperty("chargeLaserPrefab").objectReferenceValue = laserPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(scenePlayer);
            }
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
