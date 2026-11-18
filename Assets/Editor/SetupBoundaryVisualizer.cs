#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupBoundaryVisualizer
{
    static SetupBoundaryVisualizer()
    {
        EditorApplication.delayCall += EnsureBoundaryVisualizer;
    }

    [MenuItem("ThunderFighter/Setup Boundary Visualizer")]
    public static void EnsureBoundaryVisualizer()
    {
        BoundaryVisualizer visualizer = Object.FindFirstObjectByType<BoundaryVisualizer>();
        if (visualizer == null)
        {
            GameObject boundaryObj = new GameObject("BoundaryVisualizer");
            visualizer = boundaryObj.AddComponent<BoundaryVisualizer>();
            Undo.RegisterCreatedObjectUndo(boundaryObj, "Create Boundary Visualizer");
            Debug.Log("<color=#00FF88>【成功】已在場景中建立 BoundaryVisualizer 邊界顯示器！</color>");
        }
        else
        {
            visualizer.UpdateBoundaryLine();
        }
    }
}
#endif
