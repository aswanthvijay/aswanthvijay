using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runeheir.Tests
{
    /// <summary>Cleanup shared by the play-mode tests.</summary>
    internal static class TestScene
    {
        /// <summary>The Test Framework's runner object, which lives in the same scene as the tests.</summary>
        private const string RunnerName = "Code-based tests runner";

        /// <summary>
        /// Destroys everything a test left in the active scene, except the Test Framework's runner: destroying that stops the
        /// test coroutine mid-run, and the whole run then waits forever for a test that will never finish.
        /// </summary>
        public static void Clear()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == RunnerName || root.GetComponent("PlaymodeTestsController") != null)
                {
                    continue;
                }

                Object.Destroy(root);
            }
        }
    }
}
