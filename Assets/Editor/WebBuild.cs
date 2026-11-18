using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Tools > Build > Web Build
/// Builds every scene in the build list (StartScreen first) as a WebGL game into Builds/Web,
/// with settings that work on itch.io and most hosts (gzip + decompression fallback).
/// Needs the "Web Build Support" module installed for this Unity version in Unity Hub.
public static class WebBuild
{
    const string OutDir = "Builds/Web";

    [MenuItem("Tools/Build/Web Build")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog("Web Build",
                "Web Build Support isn't installed for this Unity version.\n\n" +
                "Unity Hub > Installs > (this version) > gear > Add modules > Web Build Support, then restart Unity.", "OK");
            return;
        }

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("[WebBuild] No scenes in the build list."); return; }

        // settings that load anywhere: gzip-compressed, but the page can unpack it itself
        // if the host doesn't send the right headers (itch.io, GitHub Pages, school servers...)
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.runInBackground = true;

        Directory.CreateDirectory(OutDir);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        });

        var s = report.summary;
        if (s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[WebBuild] Done: {s.totalSize / (1024 * 1024)} MB in {Path.GetFullPath(OutDir)}");
            EditorUtility.RevealInFinder(Path.Combine(OutDir, "index.html"));
        }
        else Debug.LogError($"[WebBuild] Build {s.result} with {s.totalErrors} errors - see the Console.");
    }
}
