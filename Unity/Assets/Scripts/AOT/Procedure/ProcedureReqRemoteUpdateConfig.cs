using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.ModuleSetting.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.Variable.Runtime;
using Launcher.UI;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--获取远端资源更新配置流程。
    /// 功能：
    ///     1. 异步获取远端资源更新配置，包括是否需要更新，是否强更、更新内容、App下载地址、资源下载地址。
    ///     2. 获取成功：
    ///             a. 判断是否需要更新，如果需要更新，则弹出更新提示框。点击更新后再判断是否是强更，如果是强更则打开下载APP的Url，否则保留资源下载地址后进入初始化资源包流程。
    ///             b. 如果不需要更新，则直接进入获取资源版本流程。
    ///     3. 获取失败，则提示网络异常，并延迟3秒后重试。
    /// </summary>
    public class ProcedureReqRemoteUpdateConfig : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 2;
#endif

        /// <summary>
        /// 热更进度界面
        /// </summary>
        private WinLauncher m_WinLauncher;

        /// <summary>
        /// 远端资源更新配置
        /// </summary>
        private RemoteUpdateConfig m_UpdateConfig;

        /// <summary>
        /// 远端资源更新配置文件名
        /// </summary>
        private const string RemoteUpdateConfigName = "RemoteUpdateConfig.json";

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------获取远端资源更新配置流程------</color>");

            // 获取热更界面
            m_WinLauncher = GlobalModule.UIModule.GetUI<WinLauncher>();
            if (m_WinLauncher == null)
            {
                FuLogger.LogError("热更界面获取失败！");
                return;
            }

            // 异步获取并处理更新配置
            ProcessUpdateConfigAsync().Forget();
        }

        /// <summary>
        /// 异步获取并处理更新配置
        /// </summary>
        private async UniTaskVoid ProcessUpdateConfigAsync()
        {
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            var configUrl    = $"{assetSetting.ResCdnRootRootURL}{Utility.Application.PlatformName}/{RemoteUpdateConfigName}";

            try
            {
                // 请求远端资源更新配置
                m_UpdateConfig = await ReqRemoteUpdateConfig(configUrl);
                if (m_UpdateConfig is null)
                {
                    // 获取失败，延迟3秒后重试
                    m_WinLauncher.SetTipText("Asset Server error, retrying...");
                    FuLogger.LogError($"获取获取远端资源更新配置异常，3秒后重试：Req=>{configUrl}");
                    await UniTask.WaitForSeconds(3);
                    ProcessUpdateConfigAsync().Forget();
                    return;
                }

                // 设置资源下载地址
                SetResDownloadUrl();

                // 显示更新提示框
                if (m_UpdateConfig.ShowUpgrade)
                {
                    ShowUpdateDialog();
                    return;
                }

                // 不显示更新提示框，进入初始化资源包流程
                ChangeState<ProcedureInitPackage>();
            }
            catch (Exception e)
            {
                // 网络异常，延迟3秒后重试
                FuLogger.LogError($"获取远端资源更新配置异常，3秒后重试：Req=>{configUrl}，{e.Message}");
                m_WinLauncher.SetTipText("Network error, retrying...");
                await UniTask.WaitForSeconds(3);
                ProcessUpdateConfigAsync().Forget();
            }
        }

        /// <summary>
        /// 请求远端资源更新配置
        /// </summary>
        /// <param name="configUrl">请求地址</param>
        /// <returns>远端资源更新配置，获取失败返回null</returns>
        private async UniTask<RemoteUpdateConfig> ReqRemoteUpdateConfig(string configUrl)
        {
            var json = await GlobalModule.WebModule.GetToString(configUrl);
            return Utility.Json.ToObject<RemoteUpdateConfig>(json.Result);
        }

        /// <summary>
        /// 设置资源下载地址
        /// </summary>
        private void SetResDownloadUrl()
        {
            // App主次版本号：如V1.0
            var version = $"v{Utility.Version.MajorMinorVersion}";

            // 保存资源下载路径到流程管理模块的Data变量("ResDownloadUrl")中，如：http://127.0.0.1:8080/CDN/Android/{v1.0}/
            var resDownloadUrl = ReferencePool.Acquire<VarString>();
            resDownloadUrl.SetValue(string.Format(m_UpdateConfig.ResDownloadUrl, version));
            Fsm.SetData("ResDownloadUrl", resDownloadUrl);

            // 保存备用资源下载路径到流程管理模块的Data变量("ResDownloadBackupUrl")中，如：http://127.0.0.1:8080/CDN/Android/{v1.0}/
            var resDownloadBackupUrl = ReferencePool.Acquire<VarString>();
            resDownloadBackupUrl.SetValue(string.Format(m_UpdateConfig.ResDownloadBackupUrl, version));
            Fsm.SetData("ResDownloadBackupUrl", resDownloadBackupUrl);
        }

        /// <summary>
        /// 显示更新提示框
        /// </summary>
        private void ShowUpdateDialog()
        {
            // 打开更新提示框
            m_WinLauncher.SetUpdateSureUIState(true);

            // 设置更新提示框
            SetupUpdateUI();

            // 设置更新提示框回调
            SetupUpdateCallbacks();
        }

        /// <summary>
        /// 设置更新提示框UI
        /// </summary>
        private void SetupUpdateUI()
        {
            var isChinese = GlobalModule.LocalizationModule.Language == ELanguage.ChineseSimplified ||
                            GlobalModule.LocalizationModule.Language == ELanguage.ChineseTraditional;

            m_WinLauncher.SetUpdateBtnTitle(isChinese ? "更新" : "Update");
            m_WinLauncher.SetUpdateTipText(m_UpdateConfig.UpdateAnnouncement);
        }

        /// <summary>
        /// 设置更新提示框回调
        /// </summary>
        private void SetupUpdateCallbacks()
        {
            // 点击更新内容文本，打开对应的内容说明Url
            m_WinLauncher.SetUpdateTipTextOnClick(context =>
            {
                if (context.data != null)
                    Application.OpenURL(context.data.ToString());
            });

            // 点击更新按钮，如果是强更，打开下载安装包地址，否则执行存入下载地址和备用下载地址后进入初始化资源包流程
            m_WinLauncher.SetUpdateBtnOnClick(() =>
            {
                if (m_UpdateConfig.IsForce)
                {
                    Application.OpenURL(m_UpdateConfig.AppDownloadUrl);
                }
                else
                {
                    // 关闭更新提示框
                    m_WinLauncher.SetUpdateSureUIState(false);

                    // 进入初始化资源包流程
                    ChangeState<ProcedureInitPackage>();
                }
            });
        }
    }
}