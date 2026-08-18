using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace HitTheKit.Unity.EditorTools
{
    public static class MacOSPlaytestBuild
    {
        private const string BuildPathArgument = "-customBuildPath";
        public static void BuildArm64()
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

            if (!buildPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException($"{BuildPathArgument} must point to an .app bundle.");
            }

            string fullBuildPath = Path.GetFullPath(buildPath);
            if (Directory.Exists(fullBuildPath) || File.Exists(fullBuildPath))
            {
                throw new BuildFailedException($"Build output already exists: {fullBuildPath}");
            }

            string outputDirectory = Path.GetDirectoryName(fullBuildPath);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new BuildFailedException("The build output directory could not be determined.");
            }

            Directory.CreateDirectory(outputDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullBuildPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"macOS playtest build failed: {summary.result}, {summary.totalErrors} errors.");
            }

            Console.WriteLine(
                $"HITTHEKIT_MACOS_BUILD_SUCCEEDED path={fullBuildPath} bytes={summary.totalSize}");
        }

        private static string GetRequiredArgument(string name)
        {
            return GetOptionalArgument(name)
                ?? throw new BuildFailedException($"Missing required command-line argument: {name}");
        }

        private static string GetOptionalArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    string value = arguments[index + 1];
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }

            return null;
        }
    }
}
