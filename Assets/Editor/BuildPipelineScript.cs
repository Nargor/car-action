using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildPipelineScript
{
    private static readonly string[] Scenes = new string[]
    {
        "Assets/Scenes/RaceTrackScene.unity"
    };

    [MenuItem("Build/Build Windows (PC)")]
    public static string BuildWindows()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outDir = Path.Combine(projectRoot, "Builds", "PC");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        string exePath = Path.Combine(outDir, "car-action.exe");

        PlayerSettings.productName = "car-action";
        PlayerSettings.companyName = "Nargor";
        PlayerSettings.bundleVersion = "1.0.0";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log($"[Build] Starting Windows 64-bit Build to {exePath}...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        string msg = $"Windows Build {summary.result}! Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}, Time: {summary.totalTime.TotalSeconds:F1}s, Size: {(summary.totalSize / (1024 * 1024)):F1} MB";
        Debug.Log($"[Build] {msg}");
        return msg;
    }

    [MenuItem("Build/Build WebGL")]
    public static string BuildWebGL()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outDir = Path.Combine(projectRoot, "Builds", "WebGL");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        PlayerSettings.productName = "car-action";
        PlayerSettings.companyName = "Nargor";
        PlayerSettings.bundleVersion = "1.0.0";

        // WebGL compatibility settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[Build] Starting WebGL Build to {outDir}...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        string msg = $"WebGL Build {summary.result}! Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}, Time: {summary.totalTime.TotalSeconds:F1}s, Size: {(summary.totalSize / (1024 * 1024)):F1} MB";
        Debug.Log($"[Build] {msg}");
        return msg;
    }
}
