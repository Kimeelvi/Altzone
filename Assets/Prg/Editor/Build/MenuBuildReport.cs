using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Prg.Editor.Build
{
    internal static class WindowMenuItems
    {
        private const string MenuRoot = "Window/ALT-Zone/";
        private const string Build = MenuRoot + "Build/";
#if UNITY_STANDALONE
        private const string Target = "Windows";
#elif UNITY_ANDROID
        private const string Target = "Anrdoid";
#elif UNITY_IOS
        private const string Target = "iPhone";
#elif UNITY_WEBGL
        private const string Target = "WebGL";
#else
        private const string Target = "Unsupported";
#endif

        #region Build

        [MenuItem(Build + "Create Build Report", false, 10)]
        private static void CheckBuildReport() => MenuBuildReport.CheckBuildReport();

        [MenuItem(Build + "Create Build Script for " + Target, false, 11)]
        private static void CreateBuildScript() => MenuBuildReport.CreateBuildScript();

        [MenuItem(Build + "Test Android Build Config", false, 12)]
        private static void CheckAndroidBuild() => MenuBuildReport.CheckAndroidBuild();

        [MenuItem(Build + "Set Android Build for Local APK Test", false, 13)]
        private static void SetAndroidBuildTestApk() => MenuBuildReport.SetAndroidBuildTestApk();

        #endregion
    }

    /// <summary>
    /// Analyze text file from bach build to find out unused or suspicious files.
    /// </summary>
    /// <remarks>
    /// Paths for ExcludedFolders and TestFolders uses <c>RegexOptions.IgnoreCase</c>
    /// </remarks>
    internal static class MenuBuildReport
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false, false);

        private static readonly string[] ExcludedFolders =
        {
            "Assets/BuildReports",
            "Assets/Photon",
            "Assets/Plugins",
            "Assets/Presets",
            "Assets/Photon Unity Networking",
            ".*/Editor/.*",
            "Assets/Tests",
            "Assets/TextMesh Pro",
        };

        private static readonly string[] TestFolders =
        {
            ".*/test/.*",
            ".*/zPlayGround/.*",
            ".*/zzDeleteMe/.*",
        };

        private static readonly string[] ExcludedExtensions =
        {
            ".asmdef",
            ".asmref",
        };
        public static void CreateBuildScript()
        {
            Debug.Log("*");
            TeamCity.CreateBuildScript();
        }

        public static void CheckAndroidBuild()
        {
            Debug.Log("*");
            TeamCity.CheckAndroidBuild();
        }

        public static void SetAndroidBuildTestApk()
        {
            Debug.Log("*");
            Debug.Log($"* {RichText.Yellow("DO NOT COMMIT <b>THESE CHANGES</b> TO VERSION CONTROL")}");
            Debug.Log("*");
            // Fix settings for APK.
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            // Reset settings.
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keyaliasName = string.Empty;
            PlayerSettings.keystorePass = string.Empty;
            PlayerSettings.keyaliasPass = string.Empty;
            SettingsService.OpenProjectSettings("Project/Player");
        }

        public static void CheckBuildReport()
        {
            Debug.Log("*");
            var logWriter = new LogWriter();

            var buildTargetName = TeamCity.CommandLine.BuildTargetNameFrom(EditorUserBuildSettings.activeBuildTarget);
            logWriter.Log($"Build target is {buildTargetName}");
            var buildReport = $"m_Build_{buildTargetName}.log";
            if (!File.Exists(buildReport))
            {
                Debug.LogWarning($"Build report {buildReport} not found");
                return;
            }

            var allAssets = ParseBuildReport(buildReport, out var totalSize);
            
            // Calculate base stats.
            var usedAssets = new HashSet<AssetLine>();
            int[] fileCount = { 0, 0, 0, 0 };
            double[] fileSize = { 0, 0, 0, 0 };
            double[] filePercent = { 0, 0, 0, 0 };
            foreach (var assetLine in allAssets)
            {
                if (assetLine.IsAsset)
                {
                    usedAssets.Add(assetLine);
                    fileCount[0] += 1;
                    fileSize[0] += assetLine.FileSizeKb;
                    filePercent[0] += assetLine.Percentage;
                }
                else if (assetLine.IsPackage)
                {
                    fileCount[1] += 1;
                    fileSize[1] += assetLine.FileSizeKb;
                    filePercent[1] += assetLine.Percentage;
                }
                else if (assetLine.IsResource)
                {
                    fileCount[2] += 1;
                    fileSize[2] += assetLine.FileSizeKb;
                    filePercent[2] += assetLine.Percentage;
                }
                else if (assetLine.IsBuiltIn)
                {
                    fileCount[3] += 1;
                    fileSize[3] += assetLine.FileSizeKb;
                    filePercent[3] += assetLine.Percentage;
                }
                else
                {
                    Debug.LogError("Unknown asset line: " + assetLine.FilePath);
                    return;
                }
            }
            logWriter.Log($"Build contains {allAssets.Count} total files, their size is {totalSize:### ### ##0.0} kb");
            logWriter.Log($"Build contains {fileCount[0]} ASSET files, their size is {fileSize[0]:### ### ##0.0} kb ({filePercent[0]:0.0}%)");
            logWriter.Log($"Build contains {fileCount[1]} PACKAGE files, their size is {fileSize[1]:### ### ##0.0} kb ({filePercent[1]:0.0}%)");
            logWriter.Log($"Build contains {fileCount[2]} RESOURCE files, their size is {fileSize[2]:### ### ##0.0} kb ({filePercent[2]:0.0}%)");
            if (fileCount[3] > 0)
            {
                logWriter.Log($"Build contains {fileCount[3]} Built-in files, their size is {fileSize[3]:### ### ##0.0} kb ({filePercent[3]:0.0}%)");
            }
            
            // Calculate un-used asset stats.
            var testAssets = new List<AssetLine>();
            var unusedAssets = CheckUnusedAssets(usedAssets, testAssets);
            logWriter.Log($"Project contains {unusedAssets.Count} unused assets for {buildTargetName} build");
            if (_excludedFolderCount > 0 || _excludedFileCount > 0)
            {
                logWriter.Log($"Excluded {_excludedFolderCount} folders and {_excludedFileCount} files");
            }
            if (testAssets.Count > 0)
            {
                logWriter.Log($"Build uses {testAssets.Count} TEST assets");
                foreach (var testAsset in testAssets)
                {
                    logWriter.Log($"USED\tTEST\t{testAsset.FilePath}\t{testAsset.FileSizeKb:### ### ##0.0} kb");
                }
            }
            var unusedAssetSizeTotal = unusedAssets.Select(x => x.FileSizeKb).Sum();
            logWriter.Log($"Unused assets total size is {unusedAssetSizeTotal:### ### ##0.0} kb");
            
            var unusedCount = 0;
            var unusedSize = 0D;
            foreach (var unusedAsset in unusedAssets.Where(x => !x.IsTestAsset).OrderBy(x => x.FileSizeKb).Reverse())
            {
                unusedCount += 1;
                unusedSize += unusedAsset.FileSizeKb;
                logWriter.Log($"UNUSED\tPROD\t{unusedAsset.FilePath}\t{unusedAsset.FileSizeKb:### ### ##0.0} kb");
            }
            logWriter.Log($"Unused assets PROD size is {unusedSize:### ### ##0.0} kb in {unusedCount} files");
            unusedCount = 0;
            unusedSize = 0D;
            foreach (var unusedAsset in unusedAssets.Where(x => x.IsTestAsset).OrderBy(x => x.FileSizeKb).Reverse())
            {
                unusedCount += 1;
                unusedSize += unusedAsset.FileSizeKb;
                logWriter.Log($"UNUSED\tTEST\t{unusedAsset.FilePath}\t{unusedAsset.FileSizeKb:### ### ##0.0} kb");
            }
            logWriter.Log($"Unused assets TEST size is {unusedSize:### ### ##0.0} kb in {unusedCount} files");

            var reportName = $"{Path.GetFileNameWithoutExtension(buildReport)}_report.log";
            logWriter.Save(reportName);
            Debug.Log($"Report save in {reportName}");
        }

        private static int _excludedFolderCount;
        private static int _excludedFileCount;

        private static List<AssetLine> CheckUnusedAssets(HashSet<AssetLine> usedAssets, List<AssetLine> testAssets)
        {
            bool IsTestAsset(string assetPath, List<Regex> testFiles)
            {
                foreach (var regex in testFiles)
                {
                    if (regex.IsMatch(assetPath))
                    {
                        return true;
                    }
                }
                // Test assets outside Test folder should end with "Test"!
                return assetPath.Contains("Test.");
            }

            void HandleSubFolder(string parent, List<Regex> excluded, List<Regex> testFiles, ref List<AssetLine> result)
            {
                if (ExcludedFolders.Contains(parent))
                {
                    _excludedFolderCount += 1;
                    return;
                }
                string[] guids = AssetDatabase.FindAssets(null, new[] { parent });
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var isExclude = false;
                    foreach (var regex in excluded)
                    {
                        if (regex.IsMatch(assetPath))
                        {
                            isExclude = true;
                            break;
                        }
                    }
                    if (isExclude)
                    {
                        _excludedFileCount += 1;
                        continue;
                    }
                    var extension = Path.GetExtension(assetPath);
                    if (ExcludedExtensions.Contains(extension))
                    {
                        _excludedFileCount += 1;
                        continue;
                    }
                    var isUsed = usedAssets.Any(x => x.FilePath.Equals(assetPath));
                    if (!isUsed)
                    {
                        if (Directory.Exists(assetPath))
                        {
                            continue; // Ignore folders
                        }
                        var assetLine = new AssetLine(assetPath, isFile: true);
                        if (!result.Contains(assetLine))
                        {
                            if (IsTestAsset(assetPath, testFiles))
                            {
                                assetLine.SetIsTestAsset();
                            }
                            result.Add(assetLine);
                        }
                        continue;
                    }
                    if (IsTestAsset(assetPath, testFiles))
                    {
                        var assetLine = new AssetLine(assetPath, isFile: true);
                        assetLine.SetIsTestAsset();
                        testAssets.Add(assetLine);
                        break;
                    }
                }
            }

            _excludedFolderCount = 0;
            _excludedFileCount = 0;
            var excludedList = new List<Regex>();
            foreach (var excludedFolder in ExcludedFolders)
            {
                excludedList.Add(new Regex(excludedFolder, RegexOptions.IgnoreCase));
            }
            var testRegExpList = new List<Regex>();
            foreach (var testPath in TestFolders)
            {
                testRegExpList.Add(new Regex(testPath, RegexOptions.IgnoreCase));
            }
            var resultList = new List<AssetLine>();
            var folders = AssetDatabase.GetSubFolders("Assets");
            foreach (var folder in folders)
            {
                HandleSubFolder(folder, excludedList, testRegExpList, ref resultList);
            }
            return resultList;
        }

        private static List<AssetLine> ParseBuildReport(string buildReport, out double totalSize)
        {
            const string startMarkerLine = "Used Assets and files from the Resources folder, sorted by uncompressed size:";
            const string endMarkerLine = "-------------------------------------------------------------------------------";
            // Example lines:
            //  1.4 mb	 0.2% Assets/Altzone/Graphics/Logo/ALT ZONE logo.png
            //  341.5 kb	 0.1% Assets/TextMesh Pro/Sprites/EmojiOne.png
            //  0.1 kb	 0.0% Assets/MenuUi/Scripts/Shop.cs

            var result = new List<AssetLine>();
            var processing = false;
            totalSize = 0;
            foreach (var line in File.ReadAllLines(buildReport))
            {
                if (processing)
                {
                    if (line == endMarkerLine)
                    {
                        break;
                    }
                    var assetLine = new AssetLine(line);
                    totalSize += assetLine.FileSizeKb;
                    result.Add(assetLine);
                }
                if (line == startMarkerLine)
                {
                    processing = true;
                }
            }
            return result;
        }

        private class AssetLine
        {
            private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");
            private static readonly char[] Separators1 = { '%' };
            private static readonly char[] Separators2 = { ' ' };

            private readonly string _line;
            public readonly double FileSizeKb;
            public readonly double Percentage;
            public readonly string FilePath;
            public bool IsTestAsset { get; private set; }

            public bool IsAsset => FilePath.StartsWith("Assets/");
            public bool IsPackage => FilePath.StartsWith("Packages/");
            public bool IsResource => FilePath.StartsWith("Resources/");
            public bool IsBuiltIn => FilePath.StartsWith("Built-in ");

            public AssetLine(string line, bool isFile = false)
            {
                _line = line ?? string.Empty;
                if (isFile)
                {
                    FilePath = _line;
                    FileSizeKb = (int)(new FileInfo(FilePath).Length / 1024);
                    return;
                }
                var tokens = _line.Split(Separators1);
                if (tokens.Length != 2)
                {
                    FilePath = _line;
                    return;
                }
                FilePath = tokens[1].Trim();
                tokens = tokens[0].Split(Separators2, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 3)
                {
                    return;
                }
                FileSizeKb = double.Parse(tokens[0], Culture);
                Percentage = double.Parse(tokens[2], Culture);
            }

            private bool Equals(AssetLine other)
            {
                return _line == other._line;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((AssetLine)obj);
            }

            public override int GetHashCode()
            {
                return _line.GetHashCode();
            }

            public void SetIsTestAsset()
            {
                IsTestAsset = true;
            }
        }

        private class LogWriter
        {
            private readonly StringBuilder _builder = new StringBuilder();

            public void Log(string line)
            {
                Debug.Log(line);
                _builder.Append(line).AppendLine();
            }

            public void Save(string fileName)
            {
                File.WriteAllText(fileName, _builder.ToString(), Encoding);
            }
        }
    }
}
