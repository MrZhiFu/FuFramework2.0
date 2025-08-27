using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.ModuleSetting.Runtime;

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
            
            await InitPackage();
            ChangeState<ProcedureUpdateGetPackageVersion>(procedureOwner);
        }

        /// <summary>
        /// 获取资源模块配置数据，遍历配置，初始化所有资源包
        /// </summary>
        private async UniTask InitPackage()
        {
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            if (!assetSetting) throw new FuException("资源模块配置数据为空!");

            var initTasks = new List<UniTask>();
            foreach (var packageInfo in assetSetting.AllPackages)
            {
                var initTask = AssetManager.Instance.InitPackageAsync(packageInfo);
                initTasks.Add(initTask);
            }

            await UniTask.WhenAll(initTasks);
        }
    }
}