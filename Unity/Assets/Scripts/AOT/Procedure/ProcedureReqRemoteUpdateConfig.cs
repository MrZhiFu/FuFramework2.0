using System;
using UnityEngine;
using Launcher.UI;
using Cysharp.Threading.Tasks;
using FuFramework.Web.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.ModuleSetting.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.Variable.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--获取远端资源更新配置流程。
    /// 功能：
    ///     1. 获取远端资源更新配置，包括是否需要更新，是否强更、更新内容、App下载地址、资源下载地址。
    ///     2. 获取成功，判断是否需要更新，如果需要更新，则使用FUI控制器弹出更新提示框，
    ///     3. 判断是否需要强更，如果需要强更，则打开下载APP的Url。否则，进入获取资源版本流程。如果不需要更新，则进入获取资源版本流程。
    ///     4. 获取失败，则提示网络异常，并延迟3秒后重试。
    /// </summary>
    public class ProcedureReqRemoteUpdateConfig : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 2;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------获取远端资源更新配置流程------</color>");

            GetRemoteUpdateConfig().Forget();
        }

        /// <summary>
        /// 获取远端资源更新配置，并根据返回结果进行处理
        /// </summary>
        private async UniTaskVoid GetRemoteUpdateConfig()
        {
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            var reqUrl       = $"{assetSetting.ResCdnRootRootURL}{Utility.Application.PlatformName}/RemoteUpdateConfig.json";

            try
            {
                // 请求远端资源更新配置
                var json = await GlobalModule.WebModule.GetToString(reqUrl);
                FuLogger.LogInfo(json);

                var updateConfig = Utility.Json.ToObject<RemoteUpdateConfig>(json.Result);
                if (updateConfig is null)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("Asset Server error, retrying...");
                    FuLogger.LogError($"获取获取远端资源更新配置异常=> Req:{reqUrl} Resp:{json}");

                    // 延迟3秒后重试
                    await UniTask.WaitForSeconds(3);
                    GetRemoteUpdateConfig().Forget();
                    return;
                }

                // 需要更新，显示更新提示框
                if (updateConfig.IsUpgrade)
                {
                    var winLauncher = GlobalModule.UIModule.GetUI<WinLauncher>();
                    if (winLauncher == null) return;
                    winLauncher.SetUpdateSureUIState(true);

                    var isChinese = GlobalModule.LocalizationModule.Language == ELanguage.ChineseSimplified ||
                                    GlobalModule.LocalizationModule.Language == ELanguage.ChineseTraditional;

                    winLauncher.SetUpdateBtnTitle(isChinese ? "更新" : "Update");
                    winLauncher.SetUpdateTipText(updateConfig.UpdateAnnouncement);

                    // 点击更新内容文本，打开对应的说明Url
                    winLauncher.SetUpdateTipTextOnClick(context =>
                    {
                        if (context.data != null)
                            Application.OpenURL(context.data.ToString());
                    });

                    // 点击更新按钮，根据是否强更，打开下载安装包地址 或 存入下载地址和备用下载地址后进入初始化资源包流程
                    winLauncher.SetUpdateBtnOnClick(() =>
                    {
                        // 是否强更APP
                        if (updateConfig.IsForce)
                        {
                            // 强更，点击打开下载安装包Url
                            Application.OpenURL(updateConfig.AppDownloadUrl);
                        }
                        else
                        {
                            // 非强更，只更新资源
                            winLauncher.SetUpdateSureUIState(false);

                            // 保存资源下载路径到流程管理模块的Data变量("ResDownloadUrl")中
                            var resDownloadUrl = ReferencePool.Acquire<VarString>();
                            var versionStr     = $"v{Utility.Version.MajorMinorVersion}";
                            resDownloadUrl.SetValue(string.Format(updateConfig.ResDownloadUrl, versionStr));
                            Fsm.SetData("ResDownloadUrl", resDownloadUrl);

                            // 保存备用资源下载路径到流程管理模块的Data变量("ResDownloadBackupUrl")中
                            var resDownloadBackupUrl = ReferencePool.Acquire<VarString>();
                            resDownloadBackupUrl.SetValue(string.Format(updateConfig.ResDownloadBackupUrl, versionStr));
                            Fsm.SetData("ResDownloadBackupUrl", resDownloadBackupUrl);

                            // 进入初始化资源包流程
                            ChangeState<ProcedureInitPackage>();
                        }
                    });

                    return;
                }

                // 不需要更新，进入初始化资源包流程流程
                ChangeState<ProcedureInitPackage>();
            }
            catch (Exception e)
            {
                FuLogger.LogError($"获取远端资源更新配置异常=>Error:{e.Message}，Req:{reqUrl}");
                LauncherUIHelper.SetTipText("Network error, retrying...");

                // 网络异常，延迟3秒后重试
                await UniTask.WaitForSeconds(3);
                GetRemoteUpdateConfig().Forget();
            }
        }
    }

    /// <summary>
    /// 远端资源更新配置
    /// </summary>
    public sealed class RemoteUpdateConfig
    {
        /// <summary>
        /// 是否需要更新
        /// </summary>
        public bool IsUpgrade { get; set; }

        /// <summary>
        /// 是否强更
        /// </summary>
        public bool IsForce { get; set; }

        /// <summary>
        /// 更新公告
        /// </summary>
        public string UpdateAnnouncement { get; set; }

        /// <summary>
        /// App下载地址
        /// </summary>
        public string AppDownloadUrl { get; set; }

        /// <summary>
        /// 资源下载地址
        /// </summary>
        public string ResDownloadUrl { get; set; }

        /// <summary>
        /// 资源下载备用地址
        /// </summary>
        public string ResDownloadBackupUrl { get; set; }
    }
}