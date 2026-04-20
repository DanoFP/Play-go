using UnityEditor;
using UnityEngine;
using System.IO;

public static class WebGLBuilder
{
    public static void Build()
    {
        string outputPath = Path.Combine(Application.dataPath, "..", "WebBuild", "Build");

        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/RealmForge.unity" },
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildPipeline.BuildPlayer(opts);
    }
}
