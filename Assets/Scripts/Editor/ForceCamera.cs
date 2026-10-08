using UnityEngine;
using UnityEditor;

public static class ForceCamera {
    [MenuItem("Tools/Forzar Cmara en Player")]
    public static void Run() {
        var player = Object.FindAnyObjectByType<Replica.Player.PlayerController>();
        if (player == null) {
            Debug.LogError("No se encontr PlayerController en la escena.");
            return;
        }

        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        Camera mainCam = null;
        foreach(var c in cams) {
            if (c.name == "CinematicCamera") continue;
            if (mainCam == null) mainCam = c;
            else c.gameObject.SetActive(false); // Disable others
        }

        if (mainCam == null) {
            var go = new GameObject("Main Camera");
            mainCam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        mainCam.gameObject.tag = "MainCamera";
        
        var tpc = mainCam.GetComponent<Replica.Player.ThirdPersonCamera>();
        if (tpc == null) tpc = mainCam.gameObject.AddComponent<Replica.Player.ThirdPersonCamera>();
        
        tpc.target = player.transform;
        tpc.enabled = true;

        // Limpiar otros scripts
        foreach (var mb in mainCam.GetComponents<MonoBehaviour>()) {
            if (mb != tpc && mb.GetType().Namespace != null && !mb.GetType().Namespace.StartsWith("UnityEngine")) {
                mb.enabled = false;
            }
        }

        Debug.Log("[Cmara Forzada] Cmara " + mainCam.name + " configurada para seguir a " + player.name);
    }
}
