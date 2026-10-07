using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;
using Debug = UnityEngine.Debug;

[assembly: TestRunCallback(typeof(Runeheir.Tests.EditorTestProgress))]

namespace Runeheir.Tests
{
    /// <summary>
    /// CI diagnostics for the EditMode run: logs every test's start and end (to the Unity log and to
    /// <c>artifacts/test-progress-editmode.log</c> next to the project, written as it happens), and in batch mode kills Unity when
    /// one test hasn't finished after 15 minutes. Unity's own 3-minute test timeout can't stop a frozen main thread or a test
    /// whose coroutine was destroyed,
    /// and without this a hang only ends when the CI job times out, with no clue where it stopped.
    /// </summary>
    public sealed class EditorTestProgress : ITestRunCallback
    {
        private const int HangMinutes = 15;

        private static readonly object Gate = new object();
        private static string s_file;
        private static string s_current;
        private static long s_startedAt;
        private static bool s_watching;

        public void RunStarted(ITest testsToRun)
        {
            try
            {
                string artifacts = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts"));
                Directory.CreateDirectory(artifacts);
                s_file = Path.Combine(artifacts, "test-progress-editmode.log");
            }
            catch (Exception)
            {
                s_file = null;
            }

            Write($"run started: {testsToRun.TestCaseCount} tests");
            if (Application.isBatchMode && !s_watching)
            {
                s_watching = true;
                new Thread(Watch) { IsBackground = true, Name = "Runeheir test watchdog" }.Start();
            }
        }

        public void RunFinished(ITestResult testResults)
        {
            lock (Gate)
            {
                s_current = null;
            }

            Write($"run finished: {testResults.PassCount} passed, {testResults.FailCount} failed, {testResults.SkipCount} skipped");
        }

        public void TestStarted(ITest test)
        {
            if (test.IsSuite)
            {
                return;
            }

            lock (Gate)
            {
                s_current = test.FullName;
                s_startedAt = Stopwatch.GetTimestamp();
            }

            Write("start " + test.FullName);
        }

        public void TestFinished(ITestResult result)
        {
            if (result.Test.IsSuite)
            {
                return;
            }

            lock (Gate)
            {
                s_current = null;
            }

            Write($"end   {result.Test.FullName}: {result.ResultState} in {result.Duration:0.0} s");
            if (result.ResultState.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                Write($"FAILED {result.Test.Name}: {result.Message}\n{result.StackTrace}");
            }
        }

        private static void Watch()
        {
            while (true)
            {
                Thread.Sleep(10000);
                string current;
                double minutes;
                lock (Gate)
                {
                    current = s_current;
                    minutes = (Stopwatch.GetTimestamp() - s_startedAt) / (double)Stopwatch.Frequency / 60.0;
                }

                if (current != null && minutes >= HangMinutes)
                {
                    Write($"HANG: {current} hasn't finished after {minutes:0} minutes. Stopping Unity so CI can report it.");
                    Thread.Sleep(2000);
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        private static void Write(string line)
        {
            string stamped = $"[{DateTime.UtcNow:HH:mm:ss}] [Runeheir tests] {line}";
            Debug.Log(stamped);
            try
            {
                if (s_file != null)
                {
                    lock (Gate)
                    {
                        File.AppendAllText(s_file, stamped + Environment.NewLine);
                    }
                }
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }
    }
}
