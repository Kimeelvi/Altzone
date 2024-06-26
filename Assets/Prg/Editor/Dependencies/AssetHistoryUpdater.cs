using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Prg.Editor.Dependencies
{
    /// <summary>
    /// Constants for <c>AssetHistory</c>.
    /// </summary>
    public static class AssetHistory
    {
        public const string AssetHistoryFilename = "m_Build_AssetHistory.txt";
        public const string AssetHistoryStateFilename = "m_Build_AssetHistoryState.txt";
        public const string AssetPath = "Assets";
        public const string DayNumberKey = "AssetHistory.DayNumber";
        public static readonly int MetaExtensionLength = ".meta".Length;
        public static readonly Encoding Encoding = new UTF8Encoding(false, false);

        public static string[] Load()
        {
            var lines = File.Exists(AssetHistoryFilename)
                ? File.ReadAllLines(AssetHistoryFilename, Encoding)
                : Array.Empty<string>();
            return lines;
        }
    }

    /// <summary>
    /// Local state for <c>AssetHistory</c>.
    /// </summary>
    /// <remarks>
    /// Note that all file extensions should be in lower case. 
    /// </remarks>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AssetHistoryState
    {
        public int DayNumber;
        public List<string> YamlExtensions = new();
        public List<string> OtherExtensions = new();

        /// <summary>
        /// Checks if assets is binary file format and should be skipped.
        /// </summary>
        public bool IsExcludedAsset(string filename) => filename.EndsWith("lightingdata.asset") || filename.EndsWith("navmesh.asset");

        public static AssetHistoryState Load()
        {
            if (!File.Exists(AssetHistory.AssetHistoryStateFilename))
            {
                return new AssetHistoryState()
                {
                    YamlExtensions = new List<string>()
                    {
                        ".anim", ".asset", ".controller", ".cubemap", ".flare", ".guiskin", ".lighting", ".mat", ".overridecontroller",
                        ".physicmaterial", ".physicmaterial", ".physicsmaterial2d", ".prefab", ".preset", ".unity",
                    },
                    OtherExtensions = new List<string>()
                    {
                        ".aar", ".asmdef", ".blend", ".bmp", ".cginc", ".chm", ".cs", ".csv", ".dll", ".exr", ".fbx", ".gif",
                        ".html",".inputactions", ".jpg", ".jslib", ".json", ".lib", ".mp3", ".otf", ".pdb", ".pdf", ".png", ".psd",
                        ".readme", ".shader", ".tga", ".tif", ".ttf", ".txt", ".wav", ".xcf", ".xlsx", ".xml",
                    },
                };
            }
            var jsonData = File.ReadAllText(AssetHistory.AssetHistoryStateFilename, AssetHistory.Encoding);
            return JsonUtility.FromJson<AssetHistoryState>(jsonData);
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(AssetHistory.AssetHistoryStateFilename, json, AssetHistory.Encoding);
        }
    }

    /// <summary>
    /// Keeps a list of files (assets) we have ever seen for a later case when files has been deleted or renamed and
    /// we need to find out what was the original name or location.<br />
    /// Initially files are in the order OS reports them and later additions are appended as they are found.<br />
    /// This facilitates tracking renamed files unambiguously.
    /// </summary>
    /// <remarks>
    /// File format (for lines) is: &lt;asset_name&gt; \t &lt;asset_guid&gt; \t &lt;asset_extension&gt;<br />
    /// We try to run this once a day when UNITY Editor is started first time.
    /// </remarks>
    public static class AssetHistoryUpdater
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;

            var state = AssetHistoryState.Load();
            var now = DateTime.Now;
            var dayOfYear = now.Year * 1000 + now.DayOfYear;
            if (dayOfYear == state.DayNumber && File.Exists(AssetHistory.AssetHistoryFilename))
            {
                return;
            }
            UpdateAssetHistory();
            state.DayNumber = dayOfYear;
            state.Save();
        }

        public static void UpdateAssetHistory()
        {
            var lines = AssetHistory.Load();
            var hasLines = lines.Length > 0;
            var fileHistory = new HashSet<string>(lines);
            var files = Directory.GetFiles(AssetHistory.AssetPath, "*.meta", SearchOption.AllDirectories);
            var currentStatus =
                $"{RichText.Magenta("UpdateAssetHistory")} {AssetHistory.AssetHistoryFilename} with {fileHistory.Count} entries and {files.Length} meta files";
            var newFileCount = 0;
            var isShowNewFiles = Math.Abs(fileHistory.Count - files.Length) < 100;
            var newLines = new StringBuilder();
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }
                var assetPath = file.Substring(0, file.Length - AssetHistory.MetaExtensionLength);
                var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
                var line = $"{assetPath}\t{guid}";
                if (fileHistory.Add(line))
                {
                    newFileCount += 1;
                    newLines.Append(line).AppendLine();
                    if (isShowNewFiles)
                    {
                        Debug.Log(line);
                    }
                }
            }
            if (newFileCount == 0)
            {
                Debug.Log($"{currentStatus} {RichText.White("ok")}");
                return;
            }
            // Remove last CR-LF
            newLines.Length -= 2;
            if (hasLines)
            {
                using var streamWriter = File.AppendText(AssetHistory.AssetHistoryFilename);
                // Add CR-LF
                streamWriter.WriteLine();
                streamWriter.Write(newLines.ToString());
            }
            else
            {
                File.WriteAllText(AssetHistory.AssetHistoryFilename, newLines.ToString(), AssetHistory.Encoding);
            }
            Debug.Log($"{currentStatus} {RichText.Yellow($"updated with {newFileCount} entries")}");
        }
    }
}
