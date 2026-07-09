using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
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
                    m_WinLauncher.SetTipText("资源服务器错误，正在重试...");
                    FuLogger.LogError($"获取远端资源更新配置失败，3秒后重试：Req=>{configUrl}");
                    await UniTask.WaitForSeconds(3);
                    ProcessUpdateConfigAsync().Forget();
                    return;
                }

                // 设置资源更新配置到流程数据中
                var updateConfig = ReferencePool.Acquire<VarObject>();
                updateConfig.SetValue(m_UpdateConfig);
                Fsm.SetData("UpdateConfig", updateConfig);

                // 强更：显示强更提示框，点击更新按钮后打开下载安装包地址
                if (m_UpdateConfig.ForceUpdate)
                {
                    m_WinLauncher.ShowUpdateDialog(m_UpdateConfig.UpdateAnnouncement, () => { Application.OpenURL(m_UpdateConfig.AppDownloadUrl); });
                    return;
                }

                // 非强更：进入初始化资源包流程
                ChangeState<ProcedureInitPackage>();
            }
            catch (Exception e)
            {
                // 网络异常，延迟3秒后重试
                FuLogger.LogError($"获取远端资源更新配置失败，网络异常，3秒后重试：Req=>{configUrl}，{e.Message}");
                m_WinLauncher.SetTipText("网络错误，正在重试...");
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
    }
}