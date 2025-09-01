using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ReferencePool.Runtime
{
    /// <summary>
    /// 引用池管理器。
    /// 功能:主要用于设置是否开启引用池类型的严格检查。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReferencePoolManager : FuComponent
    {
        [Header("是否开启引用类型严格检查(开启后会检查引用类型为非抽象类，且为IReference的接口实现类, 同时在 Release 调用时，会检查传入的引用是否已经可以重复归还-EnQueue)")]
        [SerializeField] private EReferenceStrictCheckType m_EnableStrictCheck = EReferenceStrictCheckType.AlwaysEnable;

        /// <summary>
        /// 获取或设置是否开启引用类型严格检查。
        /// </summary>
        public static bool EnableStrictCheck
        {
            get => ReferencePool.EnableStrictCheck;
            set
            {
                ReferencePool.EnableStrictCheck = value;
                if (value)
                {
                    Log.Info("对 Reference Pool 启用了严格检查。它将会检查引用类型为非抽象类，且为IReference的接口实现类。这可能会影响性能.");
                }
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            EnableStrictCheck = m_EnableStrictCheck switch
            {
                EReferenceStrictCheckType.AlwaysEnable              => true,
                EReferenceStrictCheckType.OnlyEnableWhenDevelopment => Debug.isDebugBuild,
                EReferenceStrictCheckType.OnlyEnableInEditor        => Application.isEditor,
                EReferenceStrictCheckType.AlwaysDisable             => false,
                _                                                   => false
            };
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            ReferencePool.ClearAll();
        }
    }
}