using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.ModuleSetting.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--初始化资源包流程。
    /// 主要作用是：
    /// 1. 初始化设置YooAsset的资源包相关信息，包括：包名称、下载地址、备用下载地址。
    /// 2. 进入获取资源版本号流程
    /// </summary>
    public class ProcedureUpdateInitPackage : ProcedureBase
    {
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程--初始化资源包流程-----</color>");
            
            await InitPackage(procedureOwner);
            ChangeState<ProcedureUpdateGetPackageVersion>(procedureOwner);
        }

        /// <summary>
        /// 获取资源模块配置数据，遍历配置，初始化所有资源包
        /// </summary>
        private async UniTask InitPackage(IFsm<IProcedureManager> procedureOwner)
        {
            // 模拟器环境下，直接进入更新完成流程
            if (AssetManager.Instance.PlayMode == EPlayMode.EditorSimulateMode)
            {
                AssetManager.Instance.InitPackageAsync(AssetManager.Instance.DefaultPackageName);
                ChangeState<ProcedureUpdateDone>(procedureOwner);
                return;
            }
            
            
        }
    }
}