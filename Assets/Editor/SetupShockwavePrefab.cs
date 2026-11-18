#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupShockwavePrefab
{
    static SetupShockwavePrefab()
    {
        EditorApplication.delayCall += EnsureShockwavePrefab;
    }

    [MenuItem("ThunderFighter/Setup Shockwave Effect Prefab")]
    public static void EnsureShockwavePrefab()
    {
        string prefabsDir = "Assets/_Project/Prefabs";
        string texturesDir = "Assets/_Project/Textures";

        if (!AssetDatabase.IsValidFolder("Assets/_Project"))
        {
            AssetDatabase.CreateFolder("Assets", "_Project");
        }
        if (!AssetDatabase.IsValidFolder(prefabsDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(texturesDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Textures");
        }

        // 1. 生成圓環光圈 Texture (若不存在)
        string ringTexturePath = texturesDir + "/ShockwaveRing.png";
        if (!File.Exists(ringTexturePath))
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float innerRadius = size * 0.38f;
            float outerRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist >= innerRadius && dist <= outerRadius)
                    {
                        // 計算圓環邊緣平滑羽化 (feather)
                        float mid = (innerRadius + outerRadius) * 0.5f;
                        float halfWidth = (outerRadius - innerRadius) * 0.5f;
                        float alpha = 1f - Mathf.Abs(dist - mid) / halfWidth;
                        alpha = Mathf.SmoothStep(0f, 1f, alpha);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(ringTexturePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(ringTexturePath, ImportAssetOptions.ForceUpdate);

            // 調整貼圖為 Sprite (2D and UI)
            TextureImporter importer = AssetImporter.GetAtPath(ringTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        // 2. 建立 ShockwaveEffect Prefab
        string shockwavePrefabPath = prefabsDir + "/ShockwaveEffect.prefab";
        GameObject shockwavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shockwavePrefabPath);
        if (shockwavePrefab == null)
        {
            GameObject shockObj = new GameObject("ShockwaveEffect");
            SpriteRenderer sr = shockObj.AddComponent<SpriteRenderer>();

            Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ringTexturePath);
            sr.sprite = ringSprite;
            sr.sortingOrder = 50; // 確保覆蓋在飛機與子彈上方

            shockObj.AddComponent<ShockwaveEffect>();

            shockwavePrefab = PrefabUtility.SaveAsPrefabAsset(shockObj, shockwavePrefabPath);
            Object.DestroyImmediate(shockObj);
            Debug.Log($"<color=#00FF88>【成功】已建立全屏衝擊波 Prefab：{shockwavePrefabPath}</color>");
        }

        // 3. 自動綁定到 Player.prefab 與場景中的 Player 物件
        string playerPath = prefabsDir + "/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (playerPrefab != null)
        {
            PlayerController pc = playerPrefab.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject so = new SerializedObject(pc);
                so.FindProperty("bombEffectPrefab").objectReferenceValue = shockwavePrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerPrefab);
            }
        }

        GameObject scenePlayer = GameObject.Find("Player");
        if (scenePlayer != null)
        {
            PlayerController pc = scenePlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject so = new SerializedObject(pc);
                so.FindProperty("bombEffectPrefab").objectReferenceValue = shockwavePrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(scenePlayer);
            }
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
