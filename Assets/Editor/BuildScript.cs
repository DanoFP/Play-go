using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;
using System.IO;

/// <summary>
/// Triggered from command line:
///   Unity -batchmode -quit -projectPath . -executeMethod BuildScript.BuildWebGL
/// Outputs to &lt;projectRoot&gt;/WebBuild/Build/
/// </summary>
public static class BuildScript
{
    static readonly string OutputPath = "WebBuild";

    [MenuItem("RealmForge/Build WebGL")]
    public static void BuildWebGL()
    {
        // Scenes to include
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };

        var options = new BuildPlayerOptions
        {
            scenes          = scenes,
            locationPathName = OutputPath,
            target          = BuildTarget.WebGL,
            options         = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("WebGL build succeeded: " + OutputPath);
        else
            Debug.LogError("WebGL build failed: " + report.summary.result);
    }
}
