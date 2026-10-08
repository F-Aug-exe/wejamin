using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Replica.Editor
{
    public static class WebGLBuildScript
    {
        [MenuItem("Tools/Réplica/Build WebGL", false, 100)]
        public static void PerformWebGLBuild()
        {
            Debug.Log("[WebGLBuildScript] Iniciando compilación de WebGL...");

            string buildPath = "Builds/WebGL";
            Directory.CreateDirectory(buildPath);

            string[] scenes = { "Assets/Scenes/ScenaBase.unity" };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuildScript] ¡Compilación WebGL exitosa! Total: {summary.totalSize} bytes en {summary.totalTime.TotalSeconds:F1}s en '{buildPath}'.");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[WebGLBuildScript] La compilación WebGL falló con {summary.totalErrors} errores.");
            }
        }
    }
}
