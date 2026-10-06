using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Command-line / CI builds. Generates the prototype scenes, then builds a player.
    /// GameCI: <c>buildMethod: Runeheir.EditorTools.RuneheirBuild.BuildFromCommandLine</c>
    /// (reads <c>-buildTarget</c> and <c>-customBuildPath</c>).
    /// Local: <c>Unity -batchmode -quit -projectPath . -executeMethod Runeheir.EditorTools.RuneheirBuild.BuildWindows</c>.
    /// </summary>
    public static class RuneheirBuild
    {
        [MenuItem("Runeheir/Build/Windows Player", priority = 100)]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Runeheir.exe");
        }

        [MenuItem("Runeheir/Build/Linux Player", priority = 101)]
        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64, "Builds/Linux/Runeheir.x86_64");
        }

        [MenuItem("Runeheir/Build/Windows Player", true)]
        [MenuItem("Runeheir/Build/Linux Player", true)]
        public static bool CanBuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer;
        }

        public static void BuildFromCommandLine()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            string targetArg = GetArgument("-buildTarget");
            if (!string.IsNullOrEmpty(targetArg) && Enum.TryParse(targetArg, true, out BuildTarget parsed))
            {
                target = parsed;
            }

            string path = GetArgument("-customBuildPath");
            if (string.IsNullOrEmpty(path))
            {
                path = target == BuildTarget.StandaloneWindows64 ? "Builds/Windows/Runeheir.exe" : "Builds/Linux/Runeheir.x86_64";
            }

            Build(target, path);
        }

        private static void Build(BuildTarget target, string path)
        {
            if (Application.isBatchMode)
            {
                BuildPlayer(target, path);
                return;
            }

            // Scene generation replaces the open scenes: ask to save edits first, and put the user's scenes back after.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildPlayer(target, path);
            }
            finally
            {
                if (setup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                }
            }
        }

        private static void BuildPlayer(BuildTarget target, string path)
        {
            (string login, string field) scenes;
            try
            {
                scenes = RuneheirSetupWizard.GenerateScenes();
            }
            catch (IOException exception)
            {
                Debug.LogError(exception.Message);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenes.login, scenes.field },
                locationPathName = path,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Runeheir] Build {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalSize / (1024 * 1024)} MB -> {path}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
