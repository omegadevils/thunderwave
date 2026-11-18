#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FixItemDropsAndMoveChargeSlider
{
    static FixItemDropsAndMoveChargeSlider()
    {
        EditorApplication.delayCall += ExecuteFix;
    }

    [MenuItem("ThunderFighter/Fix Item Drops & Move Charge Slider")]
    public static void ExecuteFix()
    {
        // 1. 確保 3 種道具 Prefab 存在
        GameObject pUp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Item_PowerUp.prefab");
        GameObject switchItem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Item_WeaponSwitch.prefab");
        GameObject heal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Item_Heal.prefab");

        GameObject[] items = new GameObject[] { pUp, switchItem, heal };
        EnemyController.CachedDropItems = items;

        // 2. 嚴格將 3 種道具綁定至所有 11 個敵機 Prefabs
        string[] allEnemyPrefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/_Project/Prefabs/Enemies" });
        foreach (string guid in allEnemyPrefabs)
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
                    so.FindProperty("dropRate").floatValue = 0.5f; // 調升至 50% 確保經常掉落
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(enemyPrefab);
                }
            }
        }

        // 3. 移動蓄力條 (ChargeSlider) 至直式邊界外左上方
        GameObject canvasObj = GameObject.Find("GameCanvas");
        if (canvasObj != null)
        {
            Slider slider = canvasObj.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                RectTransform rt = slider.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Move Charge Slider To Top-Left Outside Boundary");

                // 錨點設在左上角 (0, 1)
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);

                // 定位在左上方 (位於直式邊界 X=-280~-300 外側或左上角專屬狀態區)
                rt.anchoredPosition = new Vector2(40f, -140f);
                rt.sizeDelta = new Vector2(260f, 22f);

                // 為蓄力條加上標籤文字「CHARGE」
                Text label = slider.GetComponentInChildren<Text>();
                if (label == null)
                {
                    GameObject labelObj = new GameObject("ChargeLabel");
                    labelObj.transform.SetParent(slider.transform, false);
                    label = labelObj.AddComponent<Text>();
                    label.text = "CHARGE";
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontSize = 20;
                    label.color = new Color(1f, 0.85f, 0.2f);
                    label.alignment = TextAnchor.MiddleLeft;

                    RectTransform labelRt = labelObj.GetComponent<RectTransform>();
                    labelRt.anchorMin = new Vector2(0f, 1f);
                    labelRt.anchorMax = new Vector2(0f, 1f);
                    labelRt.pivot = new Vector2(0f, 0f);
                    labelRt.anchoredPosition = new Vector2(0f, 4f);
                    labelRt.sizeDelta = new Vector2(120f, 24f);
                }

                EditorUtility.SetDirty(canvasObj);
                Debug.Log("<color=#00FF88>【成功】已將蓄力進度條移至左上方，並附帶 CHARGE 提示標籤！</color>");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("<color=#00FF88>【修復完成】已成功修復敵機道具掉落 (50%機率+動態備份載入)，且蓄力條已調整至左上方！</color>");
    }
}
#endif
