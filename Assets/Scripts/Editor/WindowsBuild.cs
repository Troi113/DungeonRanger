using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menu command that builds the Windows (64-bit) player of every enabled scene in Build Settings
/// to Builds/Windows/DungeonRanger.exe.
/// The result is written to Builds/build-report.txt. Editor-only.
/// </summary>
public static class WindowsBuild
{
    private const string Folder = "Builds/Windows";
    private const string Exe = Folder + "/DungeonRanger.exe";
    private const string Report = "Builds/build-report.txt";

    [MenuItem("DungeonRanger/Build Windows Player")]
    public static void Build()
    {
        Directory.CreateDirectory("Builds");
        if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Exe,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        var lines = $"result: {summary.result}\nerrors: {summary.totalErrors}\nwarnings: {summary.totalWarnings}\n" +
                    $"size: {summary.totalSize / (1024 * 1024)} MB\ntime: {summary.totalTime}\nscenes: {string.Join(", ", scenes)}\n";
        foreach (var step in report.steps)
            foreach (var message in step.messages)
                if (message.type == LogType.Error || message.type == LogType.Exception)
                    lines += $"ERROR: {message.content}\n";
        if (summary.result == BuildResult.Succeeded)
        {
            // The burst debug folder is not needed to run the game.
            foreach (var dir in Directory.GetDirectories(Folder, "*_BurstDebugInformation_DoNotShip")) Directory.Delete(dir, true);
        }
        File.WriteAllText(Report, lines);
        Debug.Log($"Windows build {summary.result}: {Exe}");
    }
}
