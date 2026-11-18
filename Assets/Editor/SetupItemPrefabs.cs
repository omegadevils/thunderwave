#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupItemPrefabs
{
    static SetupItemPrefabs()
    {
        EditorApplication.delayCall += ExecuteSetup;
    }

    [MenuItem("ThunderFighter/Setup Item Prefabs")]
    public static void ExecuteSetup()
    {
        string itemsDir = "Assets/_Project/Prefabs/Items";
        string texturesDir = "Assets/_Project/Textures";

        if (!AssetDatabase.IsValidFolder(itemsDir)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Items");
        if (!AssetDatabase.IsValidFolder(texturesDir)) AssetDatabase.CreateFolder("Assets/_Project", "Textures");

        // 1. 建立 3 種道具 Prefabs
        GameObject pUpPrefab = CreateItemPrefab("Item_PowerUp", itemsDir, ItemType.PowerUp, new Color(1f, 0.85f, 0.1f), "P"); // 金黃色 P
        GameObject switchPrefab = CreateItemPrefab("Item_WeaponSwitch", itemsDir, ItemType.WeaponSwitch, new Color(0.2f, 0.7f, 1f), "W"); // 科技藍 W
        GameObject healPrefab = CreateItemPrefab("Item_Heal", itemsDir, ItemType.Heal, new Color(0.2f, 1f, 0.4f), "+"); // 翡翠綠 +

        GameObject[] items = new GameObject[] { pUpPrefab, switchPrefab, healPrefab };

        // 2. 自動綁定到所有敵機 Prefabs
        string enemiesDir = "Assets/_Project/Prefabs/Enemies";
        string[] enemyGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { enemiesDir });

        foreach (string guid in enemyGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (enemyPrefab != null)
            {
                EnemyController ec = enemyPrefab.GetComponent<EnemyController>();
                if (ec != null)
                {
                    SerializedObject so = new SerializedObject(ec);
                    SerializedProperty dropProp = so.FindProperty("dropItemPrefabs");
                    dropProp.ClearArray();
                    for (int i = 0; i < items.Length; i++)
                    {
                        dropProp.InsertArrayElementAtIndex(i);
                        dropProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                    }
                    so.FindProperty("dropRate").floatValue = 0.4f; // 40% 掉落率
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(enemyPrefab);
                }
            }
        }

        Debug.Log("<color=#00FF88>【成功】已建立 3 種掉落道具 Prefab 並綁定至所有敵機！</color>");
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateItemPrefab(string name, string targetDir, ItemType type, Color color, string symbol)
    {
        string path = $"{targetDir}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject itemObj = new GameObject(name);
        itemObj.tag = "Item";
        itemObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        SpriteRenderer sr = itemObj.AddComponent<SpriteRenderer>();
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (defaultSprite == null) defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.sprite = defaultSprite;
        sr.color = color;
        sr.sortingOrder = 25;

        CircleCollider2D col = itemObj.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        ItemPickup pickup = itemObj.AddComponent<ItemPickup>();
        SerializedObject so = new SerializedObject(pickup);
        so.FindProperty("itemType").enumValueIndex = (int)type;
        so.FindProperty("fallSpeed").floatValue = 2.2f;
        so.ApplyModifiedProperties();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(itemObj, path);
        Object.DestroyImmediate(itemObj);
        return saved;
    }
}
#endif
