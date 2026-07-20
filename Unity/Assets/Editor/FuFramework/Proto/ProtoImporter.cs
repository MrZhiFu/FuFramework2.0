using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using FuFramework.Core.Editor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Proto.Editor
{
    /// <summary>
    /// Proto 协议导出器。
    /// 功能：
    ///     1. 在 Unity Editor 中直接执行 Proto 导出脚本，将 .proto 文件编译为 C# 代码。
    /// </summary>
    public static class ProtoImporter
    {
        /// <summary>
        /// 导出目标
        /// </summary>
        public enum EExportTarget
        {
            /// <summary>
            /// 客户端
            /// </summary>
            Client,

            /// <summary>
            /// 服务端
            /// </summary>
            Server,

            /// <summary>
            /// 全部（客户端 + 服务端）
            /// </summary>
            All
        }

        /// <summary>
        /// 导出 Proto — 客户端
        /// </summary>
        [MenuItem("FuFramework/Proto/导出Proto—客户端", false, 1010)]
        public static void ExportProtoClient()
        {
            ExportProto(EExportTarget.Client);
        }

        /// <summary>
        /// 导出 Proto — 服务端
        /// </summary>
        [MenuItem("FuFramework/Proto/导出Proto—服务端", false, 1011)]
        public static void ExportProtoServer()
        {
            ExportProto(EExportTarget.Server);
        }

        /// <summary>
        /// 导出 Proto — 全部（客户端 + 服务端）
        /// </summary>
        [MenuItem("FuFramework/Proto/导出Proto—全部", false, 1012)]
        public static void ExportProtoAll()
        {
            ExportProto(EExportTarget.All);
        }

        /// <summary>
        /// 导出 Proto
        /// </summary>
        /// <param name="target">导出目标</param>
        private static void ExportProto(EExportTarget target)
        {
            var protoDir  = GetProtoPath();
            var stopwatch = Stopwatch.StartNew();

            var isWin = Application.platform == RuntimePlatform.WindowsEditor;

            if (target == EExportTarget.All)
            {
                // 全部导出：依次执行客户端和服务端脚本
                var clientScript = Path.Combine(protoDir, isWin ? "Proto2CsExport_Client.bat" : "Proto2CsExport_Client.sh");
                var serverScript = Path.Combine(protoDir, isWin ? "Proto2CsExport_Server.bat" : "Proto2CsExport_Server.sh");

                var clientSuccess = BatchRunner.RunBatch(clientScript, protoDir);
                var serverSuccess = BatchRunner.RunBatch(serverScript, protoDir);

                if (clientSuccess && serverSuccess)
                {
                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("成功", $"Proto 全部导出成功, 耗时{stopwatch.Elapsed.TotalSeconds:F2}s", "确定");
                }
                else
                {
                    var failedTarget = !clientSuccess && !serverSuccess ? "客户端、服务端"
                        : !clientSuccess                      ? "客户端"
                        :                                       "服务端";
                    EditorUtility.DisplayDialog("失败", $"Proto 导出失败（{failedTarget}），请查看控制台日志", "确定");
                }

                return;
            }

            // 单个导出
            var scriptName = GetScriptName(target, isWin);
            var scriptPath = Path.Combine(protoDir, scriptName);

            var success = BatchRunner.RunBatch(scriptPath, protoDir);

            if (success)
            {
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("成功", $"Proto 导出成功, 耗时{stopwatch.Elapsed.TotalSeconds:F2}s", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("失败", "Proto 导出失败，请查看控制台日志", "确定");
            }
        }

        /// <summary>
        /// 根据导出目标和平台获取脚本文件名
        /// </summary>
        private static string GetScriptName(EExportTarget target, bool isWin)
        {
            return target switch
            {
                EExportTarget.Client => isWin ? "Proto2CsExport_Client.bat" : "Proto2CsExport_Client.sh",
                EExportTarget.Server => isWin ? "Proto2CsExport_Server.bat" : "Proto2CsExport_Server.sh",
                _                    => "Proto2CsExport_Client.bat"
            };
        }

        /// <summary>
        /// 获取与项目根目录同级的 Protobuf 目录路径, 如 D:\_WorkSpace\Unity\FuFramework2.0\Protobuf
        /// </summary>
        private static string GetProtoPath()
        {
            // Assets 的上级即项目根目录（如 FuFramework2.0/Unity），再上一级即为 FuFramework2.0
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var repoRoot    = Path.GetFullPath(Path.Combine(projectRoot,          ".."));

            return Path.Combine(repoRoot, "Protobuf");
        }
    }
}
