using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace HitTheKit.Unity.EditorTools
{
    public static class WindowsPlaytestBuild
    {
        private const string BuildPathArgument = "-customBuildPath";

        public static void BuildX64()
        {
            string buildPath = GetRequiredArgument(BuildPathArgument);
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes were found in EditorBuildSettings.");
            }

            if (!buildPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException($"{BuildPathArgument} must point to a Windows .exe file.");
            }

            string fullBuildPath = Path.GetFullPath(buildPath);
            string outputDirectory = Path.GetDirectoryName(fullBuildPath);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new BuildFailedException("The build output directory could not be determined.");
            }

            if (Directory.Exists(outputDirectory) || File.Exists(fullBuildPath))
            {
                throw new BuildFailedException($"Build output already exists: {outputDirectory}");
            }

            Directory.CreateDirectory(outputDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Windows x64 playtest build failed: {summary.result}, {summary.totalErrors} errors.");
            }

            Console.WriteLine(
                $"HITTHEKIT_WINDOWS_BUILD_SUCCEEDED path={fullBuildPath} bytes={summary.totalSize}");
        }

        private static string GetRequiredArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], name, StringComparison.Ordinal)) continue;
                string value = arguments[index + 1];
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }

            throw new BuildFailedException($"Missing required command-line argument: {name}");
        }
    }
}
