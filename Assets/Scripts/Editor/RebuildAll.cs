using UnityEngine;
using UnityEditor;
using Replica.EditorTools;

public static class RebuildAll {
    [MenuItem("Tools/Reconstruir Todo Limpio")]
    public static void Run() {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots) {
            if (go == null) continue;
            if (go.name == "Player" || go.name == "Canvas" || go.name == "GameManager" || go.name == "FinalSequenceManager" || go.name == "NavMesh") {
                Object.DestroyImmediate(go);
            } else if (go.GetComponentInChildren<Replica.Interaction.InteractableNPC>(true) != null) {
                Object.DestroyImmediate(go);
            }
        }
        
        AutoSceneBuilder.SetupScene();
        Debug.Log("Reconstruccin completa ejecutada.");
    }
}
