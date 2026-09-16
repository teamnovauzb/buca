#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds the enabled scenes for WebGL with the project's Luxodd template and
/// creates an upload-ready ZIP whose root contains index.html.
///
/// Menu: RealBuca -> Build Luxodd WebGL ZIP
/// Batch: -executeMethod BuildLuxoddWebGL.BuildBatch
/// </summary>
public static class BuildLuxoddWebGL
{
    const string MenuPath = "RealBuca/Build Luxodd WebGL ZIP";

    [MenuItem(MenuPath)]
    static void BuildFromMenu()
    {
        try
        {
            string zipPath = Build();
            EditorUtility.RevealInFinder(zipPath);
            EditorUtility.DisplayDialog("Luxodd WebGL build complete",
                "Upload this ZIP to Luxodd:\n\n" + zipPath +
                "\n\nindex.html is at the ZIP root.", "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Luxodd WebGL build failed",
                exception.Message + "\n\nSee the Console for details.", "OK");
        }
    }

    public static void BuildBatch()
    {
        string zipPath = Build();
        Debug.Log("LUXODD_WEBGL_ZIP=" + zipPath);
    }

    static string Build()
    {
        if (EditorApplication.isCompiling)
            throw new BuildFailedException("Unity is still compiling scripts. Wait for compilation to finish, then build again.");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes were found in Build Settings.");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildsRoot = Path.Combine(projectRoot, "Builds");
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string buildName = "Buca-Luxodd-WebGL-" + stamp;
        string outputDirectory = Path.Combine(buildsRoot, buildName);
        string zipPath = Path.Combine(buildsRoot, buildName + ".zip");

        Directory.CreateDirectory(outputDirectory);
        AssetDatabase.SaveAssets();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputDirectory,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(
                "WebGL build did not succeed: " + report.summary.result +
                " (" + report.summary.totalErrors + " error(s)).");

        string indexPath = Path.Combine(outputDirectory, "index.html");
        if (!File.Exists(indexPath))
            throw new BuildFailedException("Unity finished, but index.html was not created.");

        CreateZipWithRootContents(outputDirectory, zipPath);

        if (!File.Exists(zipPath))
            throw new BuildFailedException("The WebGL files were built, but the ZIP was not created.");

        Debug.Log("Luxodd WebGL build succeeded.\nFolder: " + outputDirectory +
                  "\nUpload ZIP: " + zipPath);
        return zipPath;
    }

    static void CreateZipWithRootContents(string sourceDirectory, string zipPath)
    {
        using FileStream zipStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        foreach (string filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = filePath.Substring(sourceDirectory.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');

            // Unity may place Burst symbols beside the WebGL player. The folder is
            // explicitly marked DoNotShip and must never be included in Luxodd ZIPs.
            if (relativePath.Split('/').Any(segment =>
                    segment.EndsWith("_BurstDebugInformation_DoNotShip", StringComparison.OrdinalIgnoreCase)))
                continue;

            ZipArchiveEntry entry = archive.CreateEntry(relativePath,
                System.IO.Compression.CompressionLevel.Optimal);
            using Stream input = File.OpenRead(filePath);
            using Stream output = entry.Open();
            input.CopyTo(output);
        }
    }
}
#endif
