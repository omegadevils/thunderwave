#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupProjectTagsAndLayers
{
    static SetupProjectTagsAndLayers()
    {
        EditorApplication.delayCall += EnsureTagsAndLayers;
    }

    [MenuItem("ThunderFighter/Setup Tags and Layers")]
    public static void EnsureTagsAndLayers()
    {
        // 依照 GDD 規範建立 5 個核心 Tags
        string[] requiredTags = { "Player", "Enemy", "PlayerBullet", "EnemyBullet", "Item" };

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool tagsChanged = false;

        foreach (string tag in requiredTags)
        {
            bool exists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = tag;
                tagsChanged = true;
                Debug.Log($"<color=#00FF88>【Tag 建立成功】已自動在專案中建立 Tag: {tag}</color>");
            }
        }

        if (tagsChanged)
        {
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        // 確保剛建立的 Prefabs 套用正確 Tag
        AssignPrefabTag("Assets/_Project/Prefabs/Player.prefab", "Player");
        AssignPrefabTag("Assets/_Project/Prefabs/PlayerBullet.prefab", "PlayerBullet");
        AssignPrefabTag("Assets/_Project/Prefabs/ChargeLaser.prefab", "PlayerBullet");
        AssignPrefabTag("Assets/_Project/Prefabs/EnemyBullet.prefab", "EnemyBullet");

        string[] minionSources = { "TinyShip1", "TinyShip2", "TinyShip5", "TinyShip6", "TinyShip17" };
        foreach (string name in minionSources)
        {
            AssignPrefabTag($"Assets/_Project/Prefabs/Enemies/Minion_{name}.prefab", "Enemy");
        }

        string[] eliteSources = { "TinyShip7", "TinyShip9", "TinyShip10", "TinyShip14", "TinyShip16" };
        foreach (string name in eliteSources)
        {
            AssignPrefabTag($"Assets/_Project/Prefabs/Enemies/Elite_{name}.prefab", "Enemy");
        }

        AssignPrefabTag("Assets/_Project/Prefabs/Enemies/Boss_TinyShip15.prefab", "Enemy");

        AssetDatabase.SaveAssets();
    }

    private static void AssignPrefabTag(string path, string tag)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            if (prefab.tag != tag)
            {
                prefab.tag = tag;
                EditorUtility.SetDirty(prefab);
            }
        }
    }
}
#endif
