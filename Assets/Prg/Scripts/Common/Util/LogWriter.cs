using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Prg.Scripts.Common.Util
{
    /// <summary>
    /// Simple file logger that catches all log messages from UNITY and writes them to a file.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LogWriter : MonoBehaviour
    {
        private const string LogFileSuffix = "game.log";

        public static void AddLogLineContentFilter(Func<string, string> filter)
        {
            _logLineContentFilter += filter;
        }

        private static Func<string, string> _logLineContentFilter;

        private static LogWriter _instance;
        private static readonly object Lock = new();
        private static readonly Encoding Encoding = new UTF8Encoding(false, false);

        [Header("Live Data"), SerializeField] private string _fileName;
        private StreamWriter _file;

        // Formatted log messages share this.
        private static readonly StringBuilder Builder = new StringBuilder(500);

        private void Awake()
        {
            if (_instance != null)
            {
                throw new UnityException("LogWriter already created");
            }
            // Register us as the singleton!
            _instance = this;
        }

        private void OnEnable()
        {
            var baseName = GetLogName();
            try
            {
                var baseFileName = Path.Combine(Application.persistentDataPath, baseName);
                _fileName = baseFileName;
                var retry = 1;
                for (;;)
                {
                    try
                    {
                        // Open for overwrite!
                        _file = new StreamWriter(_fileName, false, Encoding) { AutoFlush = true };
                        break;
                    }
                    catch (IOException) // Sharing violation if more than one instance at the same time
                    {
                        if (++retry > 10) throw new UnityException("Unable to allocate log file");
                        var newSuffix = $"{retry:D2}_{LogFileSuffix}";
                        _fileName = baseFileName.Replace(LogFileSuffix, newSuffix);
                    }
                }
                // Show effective log filename.
                if (AppPlatform.IsWindows)
                {
                    _fileName = AppPlatform.ConvertToWindowsPath(_fileName);
                }
                UnityEngine.Debug.Log($"LogWriter Open file {_fileName}");
                Application.logMessageReceivedThreaded += UnityLogCallback;
            }
            catch (Exception x)
            {
                _file = null;
                UnityEngine.Debug.LogWarning($"unable to create log file '{_fileName}'");
                UnityEngine.Debug.LogException(x);
            }
        }

        private void OnDestroy()
        {
            // OnApplicationQuit() comes before OnDestroy() so we are *not* interested to listen it.

            Application.logMessageReceivedThreaded -= UnityLogCallback;
            UnityEngine.Debug.Log($"LogWriter OnDestroy Close file {_fileName}");
            _instance = null;
            _logLineContentFilter = null;
            if (_file != null)
            {
                _file.Close();
            }
        }

        private void WriteLogInternal(string message)
        {
            if (_file != null)
            {
                _file.WriteLine(message);
                _file.Flush();
            }
        }

        private static void WriteLog(string message)
        {
            _instance.WriteLogInternal(message);
        }

        private static string _prevLogString = string.Empty;
        private static int _prevLogLineCount;

        /// <summary>
        /// Thread safe callback to listen UNITY Debug messages and write them to a file.
        /// </summary>
        /// <remarks>
        /// This is thread safe because Debug.Log can be called from background threads as well.
        /// </remarks>
        private static void UnityLogCallback(string logString, string stackTrace, LogType type)
        {
            lock (Lock)
            {
                if (logString.Equals(_prevLogString, StringComparison.Ordinal) && type != LogType.Error)
                {
                    // Filter away messages that comes in every frame like:
                    // There are no audio listeners in the scene. Please ensure there is always one audio listener in the scene
                    // Warning	Mesh has more materials (2) than subsets (1)
                    _prevLogLineCount += 1;
                    return;
                }

                if (_prevLogLineCount > 1)
                {
                    WriteLog($"duplicate_lines {_prevLogLineCount}");
                    _prevLogLineCount = 0;
                }
                _prevLogString = logString;
                if (_logLineContentFilter != null)
                {
                    // As we can modify the input parameter on the fly we must call each delegate separately with correct input.
                    // - avoid DynamicInvoke because it can be order of magnitude slower than "function pointer".
                    var invocationList = _logLineContentFilter.GetInvocationList();
                    if (invocationList.Length == 1)
                    {
                        logString = _logLineContentFilter(logString);
                    }
                    else
                    {
                        foreach (var callback in invocationList)
                        {
                            logString = callback.DynamicInvoke(logString) as string;
                        }
                    }
                }
                // Reset builder
                Builder.Length = 0;

                // File log has timestamp (and optionally category) before message.
                Builder.AppendFormat("{0:HH:mm:ss.fff} ", DateTime.Now);
                if (type != LogType.Log)
                {
                    Builder.Append(type).Append(' ');
                }

                Builder.Append(logString);
                WriteLog(Builder.ToString());
                if (type == LogType.Error || type == LogType.Exception)
                {
                    // Show stack trace only for real errors.
                    if (stackTrace.Length > 5)
                    {
                        Builder.Length = 0;
                        Builder.AppendFormat("{0:HH:mm:ss.fff}\t{1}\t{2}", DateTime.Now, "STACK", stackTrace);
                        WriteLog(Builder.ToString());
                    }
                }
            }
        }

        public static string GetLogName()
        {
            var isEditor = AppPlatform.IsEditor;
            if (!isEditor)
            {
                // Delete old files.
                var oldFiles = Directory.GetFiles(Application.persistentDataPath, $"*_{LogFileSuffix}");
                var today = DateTime.Now.Day;
                foreach (var oldFile in oldFiles)
                {
                    if (File.GetCreationTime(oldFile).Day != today)
                    {
                        try
                        {
                            File.Delete(oldFile);
                        }
                        catch (IOException)
                        {
                            // NOP - we just swallow it
                        }
                    }
                }
            }
            var prefix = isEditor ? "editor" : Application.platform.ToString().ToLower().Replace("player", string.Empty);
            var baseName = Application.productName.ToLower();
            return $"{prefix}_{baseName}_{LogFileSuffix}";
        }
    }
}