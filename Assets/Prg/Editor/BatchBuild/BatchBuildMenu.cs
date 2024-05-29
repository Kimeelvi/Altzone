using System;
using System.Diagnostics;
using Prg.Scripts.Common.Util;
using UnityEditor;

namespace Prg.Editor.BatchBuild
{
    internal static class BatchBuildMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Build/";

        [MenuItem(MenuItem + "Show Build Report in browser", false, 10)]
        private static void HtmlBuildReportBrowser() => Logged(() => BuildReportAnalyzer.HtmlBuildReportFast());

        [MenuItem(MenuItem + "Show Build Report with unused Assets", false, 11)]
        private static void HtmlBuildReportBrowserFull() => Logged(() => BuildReportAnalyzer.HtmlBuildReportFull());

        [MenuItem(MenuItem + "Test Dump Secret Keys Folder", false, 12)]
        private static void TestDumpSecretKeysFolder() => Logged(() =>
        {
            Debug.Log($"* BuildTarget {BuildTarget.Android}");
            var secretKeys = BatchBuild.LoadSecretKeys(@".\etc\secretKeys", BuildTarget.Android);
            foreach (var pair in secretKeys)
            {
                Debug.Log($"{pair.Key}={pair.Value}");
            }
        });

        private static void Logged(Action action)
        {
            LogFileWriter.CreateLogFileWriter();
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            Debug.Log($"Command took {stopwatch.Elapsed.TotalSeconds:0.0} s");
        }
    }
}
