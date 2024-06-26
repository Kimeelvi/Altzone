﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Prg.Editor.Build
{
    /// <summary>
    /// Utility class to perform command line builds for Android, Desktop (Win64 only) and WebGL.<br />
    /// See <c>CommandLine</c> for supported command line options.
    /// </summary>
    /// <remarks>
    /// Should be compatible with most CI systems.<br />
    /// For example TeamCity, Jenkins and CircleCI are some well known CI/CD systems.
    /// </remarks>
    internal static class TeamCity
    {
        private const string LogPrefix = "LOG_" + nameof(TeamCity);
        private const string LogSeparator = "========================================";

        private const string OutputFolderAndroid = "buildAndroid";
        private const string OutputFolderWebgl = "buildWebGL";
        private const string OutputFolderWin64 = "buildWin64";

        private static readonly Encoding Encoding = new UTF8Encoding(false, false);

        private static readonly List<string> LogMessages = new()
        {
            $"{LogPrefix} {LogSeparator}",
        };

        private static string OutputBaseFilename =>
            SanitizePath($"{Application.productName}_{Application.version}_{PlayerSettings.Android.bundleVersionCode}");

        private static string[] Scenes => EditorBuildSettings.scenes
            .Where(x => x.enabled)
            .Select(x => x.path)
            .ToArray();

        /// <summary>
        /// Entry point to the build called by external agent or script.
        /// </summary>
        internal static void Build()
        {
            _Build();
        }

        internal static void CheckAndroidBuild()
        {
            // We assume that local keystore and password folder is one level up from current working directory (the UNITY project folder)
            var keystore = Path.Combine("..", $"local_{GetCurrentUser()}", "altzone.keystore");
            var args = CommandLine.Parse(new[] { "-buildTarget", "Android", "-keystore", keystore });
            configure_Android(args);
            Log($"output filename: {GetOutputFile(args.BuildTarget)}");
        }

        private static void do_Android_Build_Post_processing()
        {
            const string scriptName = "m_BuildScript_PostProcess.bat";
            var symbolsName = $"{OutputBaseFilename}.symbols";
            var script = CommandLineTemplate.AndroidPostProcessScript.Replace("<<altzone_symbols_name>>", symbolsName);
            File.WriteAllText(scriptName, script, Encoding);
            Debug.Log($"PostProcess script '{scriptName}' written");
        }

        private static void do_WebGL_Build_Post_processing()
        {
            void PatchIndexHtml(string htmlFile, string curTitle, string newTitle)
            {
#if UNITY_2019
                var oldTitleText = $"<div class=\"title\">{curTitle}</div>";
                var newTitleText = $"<div class=\"title\">{newTitle}</div>";
#else
                var oldTitleText = $"<div id=\"unity-build-title\">{curTitle}</div>";
                var newTitleText = $"<div id=\"unity-build-title\">{newTitle}</div>";
#endif
                var htmlContent = File.ReadAllText(htmlFile, Encoding);
                var newHtmlContent = htmlContent.Replace(oldTitleText, newTitleText);
                if (newHtmlContent == htmlContent)
                {
                    Log($"COULD NOT update file {htmlFile}, old title should be '{oldTitleText}'");
                    return;
                }
                Log($"update file {htmlFile}");
                Log($"old html title '{oldTitleText}'");
                Log($"new html title '{newTitleText}'");
                File.WriteAllText(htmlFile, newHtmlContent, Encoding);
            }

            var indexHtml = Path.Combine(OutputFolderWebgl, "index.html");
            var curName = Application.productName;
            var title = $"{Application.productName} built {DateTime.Now:u}";
            var gitTagCompliantLabel =
                title.Substring(0, title.Length - 4) // remove seconds
                    .Replace(" ", "_")
                    .Replace(":", ".");
            var bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            PatchIndexHtml(indexHtml, curName, $"{gitTagCompliantLabel} ({bundleVersionCode})");

            const string scriptName = "m_BuildScript_PostProcess.bat";
            File.WriteAllText(scriptName, CommandLineTemplate.WebGLPostProcessScript, Encoding);
            Debug.Log($"PostProcess script '{scriptName}' written");
        }

        internal static void CreateBuildScript()
        {
            // (1) Create core build script.
            const string scriptName = "m_BuildScript.bat";
            var sep1 = Path.AltDirectorySeparatorChar.ToString();
            var sep2 = Path.DirectorySeparatorChar.ToString();
            var unityName = EditorApplication.applicationPath.Replace(sep1, sep2);
            var methodName = $"{typeof(TeamCity).FullName}.{nameof(Build)}";
            var script = CommandLineTemplate.BuildScript
                .Replace("<<unity_version>>", Application.unityVersion)
                .Replace("<<unity_name>>", unityName)
                .Replace("<<method_name>>", methodName);
            File.WriteAllText(scriptName, script, Encoding);
            Debug.Log($"Build script '{scriptName}' written");

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetName = CommandLine.GetBuildTargetName(buildTarget);
            var driverName = $"{Path.GetFileNameWithoutExtension(scriptName)}_{buildTargetName}.bat";
            var driverScript = CommandLineTemplate.BuildDriverScript
                .Replace("<<build_script_name>>", scriptName)
                .Replace("<<build_target_name>>", buildTargetName);
            File.WriteAllText(driverName, driverScript, Encoding);
            Debug.Log($"Build script driver '{driverName}' written");

            // (2) Build output post processing (like copying to some other place).
            const string copyScriptName = "m_BuildScript_CopyOutput.bat";
            if (buildTarget != BuildTarget.WebGL)
            {
                // Check other build target status.
                if (!File.Exists(copyScriptName))
                {
                    Debug.Log($"Create build copy output script '{copyScriptName}' is SKIPPED for {buildTarget}");
                    Debug.Log($"- you can manually copy them from '{BuildTarget.WebGL} build' if required");
                }
                else
                {
                    Debug.Log($"Existing copy build output script or .env file were not touched");
                }
                return;
            }
            // Currently we create this automatically only for WebGL builds
            if (!File.Exists(copyScriptName))
            {
                File.WriteAllText(copyScriptName, CommandLineTemplate.CopyBuildOutputScript, Encoding);
                Debug.Log($"Copy build output script '{copyScriptName}' written");
            }
            else
            {
                Debug.Log($"Existing copy build output script '{copyScriptName}' was not modified");
            }
            const string copyScriptEnvName = "m_BuildScript_CopyOutput.env";
            if (!File.Exists(copyScriptEnvName))
            {
                var username = Environment.GetEnvironmentVariable("USERNAME");
                var envLines = $@"DROPBOX_PATH=C:\Users\{username}\Dropbox\altgame";
                File.WriteAllText(copyScriptEnvName, envLines, Encoding);
                Debug.Log($"Copy build output .env file '{copyScriptEnvName}' written");
            }
            else
            {
                Debug.Log($"Existing build output .env file '{copyScriptEnvName}' was not modified");
            }
        }

        private static void _Build()
        {
            BuildResult buildResult;
            try
            {
                DumpEnvironment();
                WriteSourceCodeChanges(PlayerSettings.Android.bundleVersionCode);
                var args = CommandLine.Parse(Environment.GetCommandLineArgs());
                Log($"build with args: {args}");
                // By default we use always BuildOptions.DetailedBuildReport for build report analysis tools to use.
                var buildOptions = BuildOptions.DetailedBuildReport;
                if (args.IsDevelopmentBuild)
                {
                    buildOptions |= BuildOptions.Development;
                }
                string outputDir;
                BuildTargetGroup targetGroup;
                switch (args.BuildTarget)
                {
                    case BuildTarget.Android:
                        outputDir = Path.Combine(OutputFolderAndroid, GetOutputFile(args.BuildTarget));
                        targetGroup = BuildTargetGroup.Android;
                        configure_Android(args);
                        break;
                    case BuildTarget.WebGL:
                        outputDir = OutputFolderWebgl;
                        targetGroup = BuildTargetGroup.WebGL;
                        break;
                    case BuildTarget.StandaloneWindows64:
                        outputDir = Path.Combine(OutputFolderWin64, GetOutputFile(args.BuildTarget));
                        targetGroup = BuildTargetGroup.Standalone;
                        break;
                    default:
                        throw new UnityException($"build target '{args.BuildTarget}' not supported");
                }
                // Output (artifacts) should be inside project folder for CI systems to find them
                var buildPlayerOptions = new BuildPlayerOptions
                {
                    locationPathName = Path.Combine(args.ProjectPath, outputDir),
                    options = buildOptions,
                    scenes = Scenes,
                    target = args.BuildTarget,
                    targetGroup = targetGroup,
                };

                Log($"build productName: {Application.productName}");
                Log($"build version: {Application.version}");
                Log($"build bundleVersionCode: {PlayerSettings.Android.bundleVersionCode}");
                Log($"build output: {buildPlayerOptions.locationPathName}");
                if (Directory.Exists(buildPlayerOptions.locationPathName))
                {
                    Directory.Delete(buildPlayerOptions.locationPathName, recursive: true);
                }
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';');
                Log($"build defines:\r\n{string.Join("\r\n", defines)}");

                var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
                var summary = buildReport.summary;
                buildResult = summary.result;
                Log($"build result: {buildResult}");
                if (buildResult == BuildResult.Succeeded)
                {
                    if (summary.platform == BuildTarget.Android)
                    {
                        do_Android_Build_Post_processing();
                    }
                    else if (summary.platform == BuildTarget.WebGL)
                    {
                        do_WebGL_Build_Post_processing();
                    }
                }
            }
            catch (Exception x)
            {
                Log($"Unhandled exception: {x.Message} ({x.GetType().FullName})");
                throw;
            }
            finally
            {
                if (LogMessages.Count > 0)
                {
                    // Show all logged messages together without call stack for convenience!
                    LogMessages.Add($"{LogPrefix} {LogSeparator}");
                    Debug.Log($"{LogPrefix} LOG_MESSAGES:\r\n{string.Join("\r\n", LogMessages)}");
                }
            }
            // We must exit outside try-finally block as it seems that EditorApplication.Exit does not allow C# to unwind call stack properly
            if (buildResult != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void WriteSourceCodeChanges(int bundleVersionCode)
        {
            const string bundleVersionCodeFilename = @"Assets\Prg\BuildProperties.cs";
            const string bundleVersionCodeTextStart = @"private const string BundleVersionCodeValue = """;
            const string bundleVersionCodeTextEnd = @""";";
            const string compiledOnDateTextStart = @"private const string CompiledOnDateValue = """;
            const string compiledOnDateTextEnd = @""";";

            if (!File.Exists(bundleVersionCodeFilename))
            {
                Log($"File not found {bundleVersionCodeFilename}");
                return;
            }
            var orgContent = File.ReadAllText(bundleVersionCodeFilename, Encoding);
            var newContent = ReplaceTextLocal(orgContent, bundleVersionCodeTextStart, bundleVersionCodeTextEnd,
                $"{bundleVersionCode}");
            newContent = ReplaceTextLocal(newContent, compiledOnDateTextStart, compiledOnDateTextEnd,
                DateTime.Now.FormatMinutes());

            File.WriteAllText(bundleVersionCodeFilename, newContent, Encoding);
            AssetDatabase.Refresh(ImportAssetOptions.Default);

            string ReplaceTextLocal(string text, string startMarker, string endMarker, object replacement)
            {
                var builder = new StringBuilder();
                var index1 = text.IndexOf(startMarker, 0, StringComparison.Ordinal);
                if (index1 < 0)
                {
                    throw new UnityException($"Start marker {startMarker} not found {bundleVersionCodeFilename}");
                }
                index1 += startMarker.Length;
                var index2 = text.IndexOf(endMarker, index1, StringComparison.Ordinal);
                if (index2 < 0)
                {
                    throw new UnityException($"Start marker {endMarker} not found {bundleVersionCodeFilename}");
                }
                builder.Append(text[..index1]);
                builder.Append(replacement);
                builder.Append(text[index2..]);
                return builder.ToString();
            }
        }

        private static string GetOutputFile(BuildTarget buildTarget)
        {
            if (buildTarget == BuildTarget.WebGL)
            {
                return "buildWebGL";
            }
            string extension;
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    extension = "aab";
                    break;
                case BuildTarget.StandaloneWindows64:
                    extension = "exe";
                    break;
                default:
                    throw new UnityException($"getOutputFile: build target '{buildTarget}' not supported");
            }
            var filename = $"{OutputBaseFilename}.{extension}";
            return filename;
        }

        private static void configure_Android(CommandLine args)
        {
            string GetLocalPasswordFor(string folder, string filename)
            {
                var file = Path.Combine(folder, filename);
                if (File.Exists(file))
                {
                    return File.ReadAllLines(file)[0];
                }
                throw new UnityException($"getLocalPasswordFor: file '{file}' not found");
            }

            void LogObfuscated(string name, string value)
            {
                var result = (value == null || value.Length < 9)
                    ? "******"
                    : value.Substring(0, 3) + "******" + value.Substring(value.Length - 3);
                Log($"{name}={result}");
            }

            // Enable application signing with a custom keystore!
            // - Android.keystoreName : as command line parameter
            // - keystorePass : read from keystore folder
            // - Android.keyaliasName : product name in lowercase
            // - keyaliasPass : read from keystore folder

            Log("configure_Android");
            PlayerSettings.Android.keystoreName = args.KeystoreName;
            Log($"keystoreName={PlayerSettings.Android.keystoreName}");

            // EditorUserBuildSettings
            EditorUserBuildSettings.buildAppBundle = true; // For Google Play this must be always true!
            Log($"buildAppBundle={EditorUserBuildSettings.buildAppBundle}");
            if (args.IsAndroidFull)
            {
                Log($"Override settings in Editor");
#if UNITY_2019
                EditorUserBuildSettings.androidCreateSymbolsZip = true;
                EditorUserBuildSettings.androidReleaseMinification = AndroidMinification.Proguard;
#else
                EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
                PlayerSettings.Android.minifyRelease = true;
                PlayerSettings.Android.minifyWithR8 = true;
#endif
            }
            else
            {
                // Do not change current settings!
                Log($"Using current settings from Editor");
            }
#if UNITY_2019
            Log($"androidCreateSymbolsZip={EditorUserBuildSettings.androidCreateSymbolsZip}");
            Log($"androidReleaseMinification={EditorUserBuildSettings.androidReleaseMinification}");
#else
            Log($"androidCreateSymbols={EditorUserBuildSettings.androidCreateSymbols}");
            Log($"Android.minifyRelease={PlayerSettings.Android.minifyRelease} R8={PlayerSettings.Android.minifyWithR8}");
#endif

            PlayerSettings.Android.useCustomKeystore = true;
            Log($"useCustomKeystore={PlayerSettings.Android.useCustomKeystore}");
            PlayerSettings.Android.keyaliasName = Application.productName.ToLower();
            Log($"keyaliasName={PlayerSettings.Android.keyaliasName}");

            if (!File.Exists(PlayerSettings.Android.keystoreName))
            {
                throw new UnityException($"Keystore file '{PlayerSettings.Android.keystoreName}' not found, can not sign without it");
            }

            // Password files must be in same folder where keystore is!
            var passwordFolder = Path.GetDirectoryName(args.KeystoreName);
            Log($"passwordFolder={passwordFolder}");
            PlayerSettings.keystorePass = GetLocalPasswordFor(passwordFolder, "keystore_password");
            LogObfuscated("keystorePass", PlayerSettings.keystorePass);
            PlayerSettings.keyaliasPass = GetLocalPasswordFor(passwordFolder, "alias_password");
            LogObfuscated("keyaliasPass", PlayerSettings.keyaliasPass);
        }

        private static string SanitizePath(string path)
        {
            // https://www.mtu.edu/umc/services/websites/writing/characters-avoid/
            var illegalCharacters = new[]
            {
                '#', '<', '$', '+',
                '%', '>', '!', '`',
                '&', '*', '\'', '|',
                '{', '?', '"', '=',
                '}', '/', ':',
                '\\', ' ', '@',
            };
            for (var i = 0; i < path.Length; ++i)
            {
                var c = path[i];
                if (illegalCharacters.Contains(c))
                {
                    path = path.Replace(c, '_');
                }
            }
            return path;
        }

        private static string GetCurrentUser()
        {
            var variables = Environment.GetEnvironmentVariables();
            foreach (var key in variables.Keys)
            {
                if (key.Equals("USERNAME"))
                {
                    return variables[key].ToString();
                }
            }
            throw new ArgumentException("Environment variable 'USERNAME' not found");
        }

        private static void DumpEnvironment()
        {
            var variables = Environment.GetEnvironmentVariables();
            var keys = variables.Keys.Cast<string>().ToList();
            keys.Sort();
            var builder = new StringBuilder($"GetEnvironmentVariables: {variables.Count}");
            foreach (var key in keys)
            {
                var value = variables[key];
                builder.AppendLine().Append($"{key}={value}");
            }
            Log(builder.ToString());
        }

        private static void Log(string message)
        {
            Debug.Log($"{LogPrefix} {message}");
            LogMessages.Add(message);
        }

        /// <summary>
        /// CommandLine class to parse and hold UNITY standard command line parameters and some custom build parameters.
        /// </summary>
        public class CommandLine
        {
            // Standard UNITY command line parameters.
            public readonly string ProjectPath;
            public readonly BuildTarget BuildTarget;
            public readonly string LogFile;

            // Custom build parameters.
            public readonly string KeystoreName;
            public readonly bool IsDevelopmentBuild;
            public readonly bool IsAndroidFull;

            private CommandLine(string projectPath, BuildTarget buildTarget, string logFile,
                string keystoreName, bool isDevelopmentBuild, bool isAndroidFull)
            {
                ProjectPath = projectPath;
                BuildTarget = buildTarget;
                LogFile = logFile;
                KeystoreName = keystoreName;
                IsDevelopmentBuild = isDevelopmentBuild;
                IsAndroidFull = isAndroidFull;
            }

            public override string ToString()
            {
                return
                    $"{nameof(ProjectPath)}: {ProjectPath}, {nameof(BuildTarget)}: {BuildTarget}, {nameof(KeystoreName)}: {KeystoreName}" +
                    $", {nameof(IsDevelopmentBuild)}: {IsDevelopmentBuild}, {nameof(IsAndroidFull)}: {IsAndroidFull}";
            }

            // Build target parameter mapping
            // See: https://docs.unity3d.com/Manual/CommandLineArguments.html
            // See: https://docs.unity3d.com/2019.4/Documentation/ScriptReference/BuildTarget.html
            // See: https://docs.unity3d.com/ScriptReference/BuildPipeline.GetBuildTargetName.html
            private static readonly Dictionary<string, BuildTarget> KnownBuildTargets = new()
            {
                {
                    /*" Win64" */ BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneWindows64), BuildTarget.StandaloneWindows64
                },
                {
                    /*" Android" */ BuildPipeline.GetBuildTargetName(BuildTarget.Android), BuildTarget.Android
                },
                {
                    /*" WebGL" */ BuildPipeline.GetBuildTargetName(BuildTarget.WebGL), BuildTarget.WebGL
                },
            };

            public static string GetBuildTargetName(BuildTarget buildTarget) => BuildPipeline.GetBuildTargetName(buildTarget);

            public static CommandLine Parse(string[] args)
            {
                var projectPath = "./";
                var buildTarget = BuildTarget.StandaloneWindows64;
                var logFile = string.Empty;
                var keystore = string.Empty;
                var isDevelopmentBuild = false;
                var isAndroidFull = false;
                for (var i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];
                    switch (arg)
                    {
                        case "-projectPath":
                            i += 1;
                            projectPath = args[i];
                            break;
                        case "-buildTarget":
                            i += 1;
                            if (!KnownBuildTargets.TryGetValue(args[i], out buildTarget))
                            {
                                throw new ArgumentException($"BuildTarget '{args[i]}' is invalid or unsupported");
                            }
                            break;
                        case "-keystore":
                            i += 1;
                            keystore = args[i];
                            break;
                        case "-DevelopmentBuild":
                            isDevelopmentBuild = true;
                            break;
                        case "-AndroidFull":
                            isAndroidFull = true;
                            break;
                        case "-logFile":
                            i += 1;
                            logFile = args[i];
                            break;
                    }
                }
                return new CommandLine(projectPath, buildTarget, logFile, keystore, isDevelopmentBuild, isAndroidFull);
            }
        }

        /// <summary>
        /// Collection of command line script templates our build system.
        /// </summary>
        private static class CommandLineTemplate
        {
            public static string BuildScript => BuildScriptContent;
            public static string CopyBuildOutputScript => CopyBuildOutputScriptContent;
            public static string BuildDriverScript => BuildDriverScriptContent;
            public static string AndroidPostProcessScript => AndroidPostProcessScriptContent;
            public static string WebGLPostProcessScript => WebGLPostProcessScriptContent;

            #region BuildScriptContent

            // Build target names are hard coded here - but they should be same that BuildPipeline.GetBuildTargetName() returns!
            private const string BuildScriptContent = @"@echo off
set VERSION=<<unity_version>>
set UNITY=<<unity_name>>
if not exist ""%UNITY%"" (
    echo *
    echo * UNITY executable not found: ""%UNITY%""
    echo *
    goto :eof
)

set BUILDTARGET=%1
if ""%BUILDTARGET%"" == ""Win64"" goto :valid_build
if ""%BUILDTARGET%"" == ""Android"" goto :valid_build
if ""%BUILDTARGET%"" == ""WebGL"" goto :valid_build
echo *
echo * Can not build: invalid build target '%BUILDTARGET%'
echo *
echo * Build target must be one of UNITY command line build targets:
echo *
echo *	Win64
echo *	Android
echo *	WebGL
echo *
goto :eof

:valid_build

set PROJECTPATH=./
set METHOD=<<method_name>>
set LOGFILE=m_Build_%BUILDTARGET%.log
if exist %LOGFILE% (
    del /Q %LOGFILE%
)
if ""%BUILDTARGET%"" == ""Android"" (
    set ANDROID_KEYSTORE=-keystore ..\local_%USERNAME%\altzone.keystore
)
rem try to simulate TeamCity invocation
set CUSTOM_OPTIONS=%ANDROID_KEYSTORE%
set UNITY_OPTIONS=-batchmode -projectPath %PROJECTPATH% -buildTarget %BUILDTARGET% -executeMethod %METHOD% %CUSTOM_OPTIONS% -quit -logFile ""%LOGFILE%""

set build_output=build%BUILDTARGET%
if exist %build_output% (
    echo Delete folder %build_output%
    rmdir /S /Q %build_output%
)
echo.
echo Start build with UNITY %VERSION% log file %LOGFILE% build target '%BUILDTARGET%'
echo.
echo ""%UNITY%"" %UNITY_OPTIONS%
""%UNITY%"" %UNITY_OPTIONS%
set RESULT=%ERRORLEVEL%
if not ""%RESULT%"" == ""0"" (
    echo *
    echo * Build FAILED with %RESULT%, check log for errors
    echo *
    goto :eof
)
echo.
if not exist m_BuildScript_PostProcess.bat (
    echo Build done, check log for results
    goto :eof
)
echo Build done, start post processing
echo *
call m_BuildScript_PostProcess.bat
echo *
echo Post processing done
goto :eof
";

            #endregion

            #region CopyBuildOutputScriptContent

            // See 'for /f .. in' usage: https://stackoverflow.com/questions/8797983/can-a-windows-batch-file-determine-its-own-file-name
            private const string CopyBuildOutputScriptContent = @"@echo off
rem .
rem . This scipt is machine generated, do not edit!
rem .
set BUILD_DIR=%1
echo BUILD_DIR=%BUILD_DIR%
if ""%BUILD_DIR%"" == """" (
    echo *
    echo * Config error, BUILD_DIR not given as parameter for this script
    echo *
    goto :eof
)
if not exist %BUILD_DIR% (
    echo *
    echo * Config error, BUILD_DIR not found
    echo *
    goto :eof
)
set ENVFILE=%~n0.env
echo ENVFILE=%ENVFILE%
if not exist %ENVFILE% (
    echo *
    echo * Skip DROPBOX copy, environment file %ENVFILE% not found
    echo *
    goto :eof
)

FOR /F ""eol=# tokens=*"" %%i IN (%ENVFILE%) DO SET %%i
if ""%DROPBOX_PATH%"" == """" (
    echo *
    echo * Config error, environment file missing DROPBOX_PATH setting
    echo *
    goto :eof
)
set DROPBOX_DIR=%DROPBOX_PATH%\%BUILD_DIR%
echo DROPBOX_DIR=%DROPBOX_DIR%
if not exist %DROPBOX_DIR% (
    echo *
    echo * Skip DROPBOX copy, output folder %DROPBOX_DIR% not found
    echo *
    goto :eof
)
if exist %0.log (
	del /Q %0.log
)
if ""%LOGFILE%"" == """" (
    set LOGFILE=%0.log
)
echo LOGFILE=%LOGFILE%
set OPTIONS=/S /E /PURGE /V /NP /R:0 /W:0
echo robocopy ""%BUILD_DIR%"" ""%DROPBOX_DIR%"" %OPTIONS% /LOG+:%LOGFILE%
robocopy ""%BUILD_DIR%"" ""%DROPBOX_DIR%"" %OPTIONS% /LOG+:%LOGFILE%
echo.
echo DROPBOX copy %DROPBOX_DIR% status %errorlevel%
if %errorlevel% leq 8 (
	echo *
	echo * Copy SUCCESS %BUILD_DIR% to %DROPBOX_DIR%
	echo *
	goto :eof
)
echo *
echo * Check DROPBOX log %LOGFILE% for possible errors
echo *
goto :eof
";

            #endregion

            #region BuildDriverScriptContent

            private const string BuildDriverScriptContent = @"@echo off
echo ~~~~~ BUILD start ~~~~~
call <<build_script_name>> <<build_target_name>>
echo ~~~~~ BUILD  done ~~~~~
pause";

            #endregion

            #region AndroidPostProcessScriptContent

            private const string AndroidPostProcessScriptContent = @"@echo off
rem .
rem . This scipt is machine generated, do not edit!
rem .
set BUILD_DIR=BuildAndroid
set ZIP=C:\Program Files\7-Zip\7z.exe

echo BUILD_DIR=%BUILD_DIR%
echo DROPBOX_DIR=%DROPBOX_DIR%
echo ZIP=%ZIP%

if not exist ""%BUILD_DIR%"" (
    goto :eof
)

if not exist ""%ZIP%"" (
    echo ZIP not found
    goto :dropbox
)
:zip_symbols
set SYMBOLS_STORED=%BUILD_DIR%\<<altzone_symbols_name>>.zip
set SYMBOLS_DEFLATED=%BUILD_DIR%\<<altzone_symbols_name>>.deflated.zip
if not exist ""%SYMBOLS_STORED%"" (
    echo No symbols.zip file found
    goto :dropbox
)

set TEMP_SYMBOLS=%BUILD_DIR%\temp_symbols
echo UNZIP symbols to %TEMP_SYMBOLS%
if exist ""%TEMP_SYMBOLS%"" rmdir /S /Q ""%TEMP_SYMBOLS%""
""%ZIP%"" x -y -o""%TEMP_SYMBOLS%"" ""%SYMBOLS_STORED%""
set RESULT=%ERRORLEVEL%
echo UNZIP result %RESULT%
if not ""%RESULT%"" == ""0"" (
    echo UNZIP symbols failed
    exit /B 1
)

echo ZIP deflate symbols
if exist %SYMBOLS_DEFLATED% del /Q %SYMBOLS_DEFLATED%
""%ZIP%"" a -y -bd ""%SYMBOLS_DEFLATED%"" "".\%TEMP_SYMBOLS%\*""
set RESULT=%ERRORLEVEL%
echo ZIP result %RESULT%
if not ""%RESULT%"" == ""0"" (
    echo ZIP deflate symbols failed
    exit /B 1
)
echo clean up temp dir
if exist ""%SYMBOLS_STORED%"" del /Q ""%SYMBOLS_STORED%""
if exist ""%TEMP_SYMBOLS%"" rmdir /S /Q ""%TEMP_SYMBOLS%""
goto :dropbox

:dropbox
if not exist m_BuildScript_CopyOutput.bat (
    echo Copy build output is skipped
    goto :eof
)
echo Build done, copy output
echo *
call m_BuildScript_CopyOutput.bat %BUILD_DIR%
echo *
echo Copy build output done
goto :eof
";

            #endregion

            #region WebGLPostProcessScriptContent

            private const string WebGLPostProcessScriptContent = @"@echo off
rem .
rem . This scipt is machine generated, do not edit!
rem .
set BUILD_DIR=BuildWebGL
:dropbox
if not exist m_BuildScript_CopyOutput.bat (
    echo Copy build output is skipped
    goto :eof
)
echo Build done, copy output
echo *
call m_BuildScript_CopyOutput.bat %BUILD_DIR%
echo *
echo Copy build output done
goto :eof
";

            #endregion
        }
    }
}
