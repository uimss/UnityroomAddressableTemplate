using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System;
using System.Text.RegularExpressions;

public class UnityroomBuilder : IPostprocessBuildWithReport
{
    public int callbackOrder { get; } = 1;

    // Build local Unity Editor
    [MenuItem("Tools/Build WebGL for unityroom")]
    public static void BuildGame()
    {
        var options = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
        BuildGameImpl(options);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        var args = Environment.GetCommandLineArgs();
        if (report.summary.platform == BuildTarget.WebGL
            && args != null
            && Array.IndexOf(args, "-enableUnityroomBuilder") > -1)
        {
            WebGLBuildUtility.ReplaceStreamingAssetUrl(report.summary.outputPath);
        }
    }

    static void BuildGameImpl(BuildPlayerOptions buildPlayerOptions)
    {
        if (buildPlayerOptions.target != BuildTarget.WebGL)
        {
            Debug.LogError("Target platform is not WebGL. Please switch target platform.");
            return;
        }
        if (string.IsNullOrEmpty(UnityroomBuilderSettings.instance.StreamingAssetsUrl))
        {
            Debug.LogError("StreamingAssetsUrl is not set, please set it in Project Settings -> Unityroom Builder.");
            return;
        }

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");

            var replaceResult = WebGLBuildUtility.ReplaceStreamingAssetUrl(buildPlayerOptions.locationPathName);
            if (replaceResult)
            {
                EditorUtility.RevealInFinder(buildPlayerOptions.locationPathName);
            }
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
        else if (summary.result == BuildResult.Cancelled)
        {
            Debug.Log("Build canceled");
        }
        else
        {
            Debug.Log("Build failed (unknown)");
        }
    }
}

public static class WebGLBuildUtility
{
    public static bool ReplaceStreamingAssetUrl(string buildPath)
    {
        var searchPath = Path.Combine(buildPath, "Build");
        var searchResults = Directory.GetFiles(searchPath, "*loader.js");

        if (string.IsNullOrEmpty(UnityroomBuilderSettings.instance.StreamingAssetsUrl))
        {
            Debug.LogError("StreamingAssetsUrl is not set, please set it in Project Settings -> Unityroom Builder.");
            return false;
        }
        if (searchResults.Length != 1)
        {
            Debug.LogError("Missing or multiple loader.js files found. Please check build results.");
            return false;
        }

        var loaderJsPath = searchResults[0];
        var newStreamingAssetUrl = UnityroomBuilderSettings.instance.GetParsedStreamingAssetsUrl();
        try
        {
            // NOTICE: Deeply dependent on loader.js generation logic
            var originalUrlPattern = @"\w+\.streamingAssetsUrl,document\.URL";
            var newUrl = $"\"{newStreamingAssetUrl}\"";
            Debug.Log($"Replace StreamingAssetUrl file: {loaderJsPath} from: {originalUrlPattern} to: {newUrl}");
            ReplaceStringInFile(loaderJsPath, originalUrlPattern, newUrl);
            return true;
        }
        catch (IOException e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    static void ReplaceStringInFile(string filePath, string pattern, string replacement)
    {
        var tmpFilePath = filePath + ".tmp";
        var backupFilePath = filePath + ".bak";
        var streamReader = new StreamReader(filePath);
        var streamWriter = new StreamWriter(tmpFilePath);

        while (!streamReader.EndOfStream)
        {
            var data = streamReader.ReadLine();
            data = Regex.Replace(data, pattern, replacement);
            streamWriter.WriteLine(data);
        }

        streamReader.Close();
        streamWriter.Close();

        File.Replace(tmpFilePath, filePath, backupFilePath);
        File.Delete(backupFilePath);
    }
}
