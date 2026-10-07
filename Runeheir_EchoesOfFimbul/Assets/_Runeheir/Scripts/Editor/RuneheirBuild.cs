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
    /// Phase 6 realm servers: the Realm Server menu items (or <c>-realmServer</c> on the command line) build a headless
    /// "Dedicated Server" player that starts the realm on launch. Any normal build also runs one with <c>-server</c>.
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

        [MenuItem("Runeheir/Build/Linux Realm Server (headless)", priority = 110)]
        public static void BuildLinuxServer()
        {
            Build(BuildTarget.StandaloneLinux64, "Builds/LinuxServer/RuneheirRealm.x86_64", server: true);
        }

        [MenuItem("Runeheir/Build/Windows Realm Server (headless)", priority = 111)]
        public static void BuildWindowsServer()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/WindowsServer/RuneheirRealm.exe", server: true);
        }

        [MenuItem("Runeheir/Build/Windows Player", true)]
        [MenuItem("Runeheir/Build/Linux Player", true)]
        [MenuItem("Runeheir/Build/Linux Realm Server (headless)", true)]
        [MenuItem("Runeheir/Build/Windows Realm Server (headless)", true)]
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

            bool server = Array.IndexOf(Environment.GetCommandLineArgs(), "-realmServer") >= 0;
            string path = GetArgument("-customBuildPath");
            if (string.IsNullOrEmpty(path))
            {
                path = target == BuildTarget.StandaloneWindows64
                    ? server ? "Builds/WindowsServer/RuneheirRealm.exe" : "Builds/Windows/Runeheir.exe"
                    : server ? "Builds/LinuxServer/RuneheirRealm.x86_64" : "Builds/Linux/Runeheir.x86_64";
            }

            Build(target, path, server);
        }

        private static void Build(BuildTarget target, string path, bool server = false)
        {
            if (Application.isBatchMode)
            {
                BuildPlayer(target, path, server);
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
                BuildPlayer(target, path, server);
            }
            finally
            {
                if (setup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                }
            }
        }

        private static void BuildPlayer(BuildTarget target, string path, bool server)
        {
            (string login, string world) scenes;
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
                scenes = new[] { scenes.login, scenes.world },
                locationPathName = path,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                // A realm server is Unity's headless "Dedicated Server" player (UNITY_SERVER: the realm starts on launch).
                subtarget = (int)(server ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player),
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Runeheir] {(server ? "Realm server" : "Player")} build {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalSize / (1024 * 1024)} MB -> {path}");
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
