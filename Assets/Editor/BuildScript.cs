using UnityEditor;
using UnityEngine;
using System.Linq;

public static class BuildScript
{
    public static void BuildWindows()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] No scenes enabled in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes            = scenes,
            locationPathName  = "Build/Windows/AI-NPC-Game.exe",
            target            = BuildTarget.StandaloneWindows64,
            options           = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Build] Done.");
    }
}
