using System;
using Cysharp.Threading.Tasks;
using GameFrameX.Fsm.Runtime;
using GameFrameX.GlobalConfig.Runtime;
using GameFrameX.Localization.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Web.Runtime;
using UnityEngine;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 获取后台服务端App版本信息流程。
    /// 主要作用是：
    /// 1. 获取后台服务端App版本信息，
    /// 2. 获取成功，判断是否需要更新，如果需要更新，则使用FUI控制器弹出更新提示框，
    /// 3. 再判断是否需要强更，如果需要强更，则打开下载安装的Url。否则，进入获取资源版本流程。如果不需要更新，则进入获取资源版本流程。
    /// 4. 获取失败，则提示网络异常，并延迟3秒后重试。
    /// </summary>
    public class ProcedureGetAppVersionInfoFromGmServer : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 编辑器下的模拟模式
            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                Log.Info("当前为编辑器模式，直接进入资源更新的初始化流程");
                ChangeState<ProcedureUpdateInit>(procedureOwner);
                return;
            }

            // 非编辑器模式下，获取版本信息
            GetAppVersionInfo(procedureOwner);
        }

        /// <summary>
        /// 获取服务端App版本信息，并根据服务端返回结果进行处理
        /// </summary>
        /// <param name="procedureOwner"></param>
        private async void GetAppVersionInfo(IFsm<IProcedureManager> procedureOwner)
        {
            var reqBaseParams = HttpHelper.GetBaseParams();
            try
            {
                // 请求后台服务端，获取App版本信息。
                var json = await GameApp.Web.PostToString(GameApp.GlobalConfig.CheckAppVersionUrl, reqBaseParams);
                Log.Info(json);

                var httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(json.Result);
                if (httpJsonResult.Code > 0)
                {
                    // 获取失败
                    LauncherUIHelper.SetTipText("Server error, retrying...");
                    Log.Error($"获取全局信息返回异常=> Req:{reqBaseParams} Resp:{json}");

                    // 网络异常，延迟3秒后重试
                    await UniTask.Delay(3000);
                    GetAppVersionInfo(procedureOwner);
                }
                else
                {
                    // 获取成功
                    var gameAppVersion = Utility.Json.ToObject<ResponseGameAppVersion>(httpJsonResult.Data);

                    if (gameAppVersion.IsUpgrade)
                    {
                        // 需要更新，显示更新提示框
                        if (UIManager.Instance.GetUI("UI/UILauncher") is not UILauncher uiLauncher) return;
                        uiLauncher.m_IsUpgrade.SetSelectedIndex(1);

                        var isChinese = GameApp.Localization.SystemLanguage == Language.ChineseSimplified ||
                                        GameApp.Localization.SystemLanguage == Language.ChineseTraditional;

                        uiLauncher.m_upgrade.m_EnterButton.title = isChinese ? "确认" : "Enter";
                        uiLauncher.m_upgrade.m_TextContent.title = gameAppVersion.UpdateAnnouncement;
                        uiLauncher.m_upgrade.m_TextContent.onClickLink.Set(context => { Application.OpenURL(context.data.ToString()); });
                        uiLauncher.m_upgrade.m_EnterButton.onClick.Set(() =>
                        {
                            if (gameAppVersion.IsForce)
                            {
                                // 强更，点击打开下载安装包Url
                                Application.OpenURL(gameAppVersion.AppDownloadUrl);
                            }
                            else
                            {
                                // 非强更，点击进入获取资源包版本流程
                                uiLauncher.m_IsUpgrade.SetSelectedIndex(0);
                                ChangeState<ProcedureGetAssetPkgVersionInfoFromGmServer>(procedureOwner);
                            }
                        });
                    }
                    else
                    {
                        ChangeState<ProcedureGetAssetPkgVersionInfoFromGmServer>(procedureOwner);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"获取版本信息异常=>Error:{e.Message}   Req:{reqBaseParams}");
                LauncherUIHelper.SetTipText("Network error, retrying...");

                // 网络异常，延迟3秒后重试
                await UniTask.Delay(3000);
                GetAppVersionInfo(procedureOwner);
            }
        }
    }
}