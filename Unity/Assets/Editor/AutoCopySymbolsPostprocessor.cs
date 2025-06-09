using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.Editor
{
    /// <summary>
    /// 自动复制Android符号表
    /// </summary>
    public class AutoCopySymbolsPostprocessor
    {
        /// <summary>
        /// 自动复制Android符号表
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pathToBuiltProject"></param>
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.Android)
                PostProcessAndroidBuild(pathToBuiltProject);
        }

        /// <summary>
        /// 自动复制Android符号表
        /// </summary>
        /// <param name="pathToBuiltProject"></param>
        public static void PostProcessAndroidBuild(string pathToBuiltProject)
        {
            CopyAndroidSymbols(pathToBuiltProject);
        }

        /// <summary>
        /// 复制符号表到发布目录
        /// </summary>
        /// <param name="pathToBuiltProject"></param>
        public static void CopyAndroidSymbols(string pathToBuiltProject)
        {
            var startTime = DateTime.Now;
            Debug.Log(nameof(CopyAndroidSymbols) + " Start ");

            var buildName  = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            var symbolsDir = buildName + "_Symbols/";
            var abi_v7a    = "armeabi-v7a/";
            var abi_v8a    = "arm64-v8a/";

            CreateDir(symbolsDir);

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) > 0)
            {
                var dir = CopySymbols(symbolsDir, abi_v8a);
                GenerateAllSymbols(dir);
            }

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) > 0)
            {
                var dir = CopySymbols(symbolsDir, abi_v7a);
                GenerateAllSymbols(dir);
            }

            Debug.Log(nameof(CopyAndroidSymbols) + " End ==>" + (DateTime.Now - startTime).TotalSeconds);
        }

        /// <summary>
        /// 符号表目录
        /// </summary>
        private const string LibPath = "/../Temp/StagingArea/libs/";

        /// <summary>
        /// 复制符号表到指定目录
        /// </summary>
        /// <param name="symbolsDir"></param>
        /// <param name="abi"></param>
        /// <returns></returns>
        private static string CopySymbols(string symbolsDir, string abi)
        {
            var sourceDir = Application.dataPath + LibPath + abi;
            var abiDir    = symbolsDir           + abi;
            CreateDir(abiDir);
            MoveAllFiles(sourceDir, abiDir);
            return abiDir;
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="path"></param>
        public static void CreateDir(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// 移动所有文件到指定目录
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static void MoveAllFiles(string src, string dst)
        {
            var srcInfo = new DirectoryInfo(src);
            if (!srcInfo.Exists) return;
            dst = dst.Replace("\\", "/");
            var files = srcInfo.GetFiles("*.*");
            foreach (var fileInfo in files)
            {
                if (File.Exists(dst + "/" + fileInfo.Name))
                {
                    File.Delete(dst + "/" + fileInfo.Name);
                }

                File.Copy(fileInfo.FullName, dst + "/" + fileInfo.Name);
            }
        }

        /// <summary>
        /// 生成所有符号表
        /// </summary>
        /// <param name="symbolsDir"></param>
        public static void GenerateAllSymbols(string symbolsDir)
        {
            var srcInfo = new DirectoryInfo(symbolsDir);
            if (!srcInfo.Exists) return;

            var cmd    = Application.dataPath;
            var soPath = Application.dataPath;
            var jdk    = EditorPrefs.GetString("JdkPath");
            
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // cmd += "/../BuglySymbol/buglySymbolAndroid.jar";
                cmd    += "/../BuglySymbol/buglyqq-upload-symbol.jar";
                jdk    += "/bin/java";
                soPath += "/../" + symbolsDir;
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // cmd += "/../BuglySymbol/buglySymbolAndroid.jar";
                cmd    += "/../BuglySymbol/buglyqq-upload-symbol.jar";
                cmd    =  cmd.Replace("/", "\\");
                jdk    =  jdk.Replace("/", "\\") + "\\bin\\java.exe";
                soPath += "/../"                 + symbolsDir;
                soPath =  soPath.Replace("/", "\\");
            }

            // var appID = "ef9301f8fb";
            // var appKey = "ea81cbea-41b4-49d0-a40a-0dcad0cf4c6b";
            // var channelTag = GameConfig.Instance().TryGetString("CHANNEL_TAG", "com.bc.avenger");
            // var buglyChannelAsset = Resources.Load<com.bugly.sdk.BuglyChannelAsset>("bugly_config");
            // var buglyCfg = buglyChannelAsset.GetBuglyChannelCfg(channelTag);
            // if (buglyCfg == null)
            // {
            //     Debug.LogError("can not find bugly channel data:" + channelTag);
            // }
            //
            // bool q1DebugValue = GameConfig.Instance().TryGetBool("ENABLE_Q1_DEBUG_MODE", false);
            // if (buglyCfg != null)
            // {
            //     if (q1DebugValue)
            //     {
            //         appID = buglyCfg.debugAppID;
            //         appKey = buglyCfg.debugAppKey;
            //     }
            //     else
            //     {
            //         appID = buglyCfg.appID;
            //         appKey = buglyCfg.appKey;
            //     }
            // }
            //
            // Debug.Log("bugly symbolsdir:" + symbolsdir);
//
// #if UPLOAD_SYMBOLS || true
//             // ProcessCommand(jdk, "-jar " + cmd + " -i " + symbolsdir + " -u -id " + appID + " - key " + appKey + " - package " + PlayerSettings.applicationIdentifier + " -version " + PlayerSettings.bundleVersion);
//             string commandStr = "-jar " + cmd + " -appid " + appID + " -appkey " + appKey + " -bundleid  " + PlayerSettings.applicationIdentifier + " -version " + PlayerSettings.bundleVersion + " -platform Android" + " -inputSymbol " + soPath;
//
//             Debug.Log("bugly ProcessCommand str:" + commandStr);
//             ProcessCommand(jdk, commandStr);
// #else
//             ProcessCommand(jdk, "-jar " + cmd + " -i " + symbolsdir);
// #endif
            //ProcessCommand(cmd,"-i " + symbolsdir + " -u -id 844a29e21e -key b85577b4-1347-40bb-a880-f8a91446007f -package " + PlayerSettings.applicationIdentifier + " -version " + PlayerSettings.bundleVersion);
        }

        /// <summary>
        /// 执行命令行
        /// </summary>
        /// <param name="command"></param>
        /// <param name="argument"></param>
        public static void ProcessCommand(string command, string argument)
        {
            var pInfo = new System.Diagnostics.ProcessStartInfo(command)
            {
                Arguments       = argument,
                CreateNoWindow  = false,
                ErrorDialog     = true,
                UseShellExecute = true
            };

            if (pInfo.UseShellExecute)
            {
                pInfo.RedirectStandardOutput = false;
                pInfo.RedirectStandardError  = false;
                pInfo.RedirectStandardInput  = false;
            }
            else
            {
                pInfo.RedirectStandardOutput = true;
                pInfo.RedirectStandardError  = true;
                pInfo.RedirectStandardInput  = true;
                pInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                pInfo.StandardErrorEncoding  = System.Text.Encoding.UTF8;
            }

            var process = System.Diagnostics.Process.Start(pInfo);

            if (!pInfo.UseShellExecute)
            {
                if (process != null)
                {
                    Debug.Log(process.StandardOutput);
                    Debug.Log(process.StandardError);
                }
            }

            if (process != null)
            {
                process.WaitForExit();
                process.Close();
            }
        }
    }
}