using UnityEngine;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ReferencePools
{
    /// <summary>
    /// 引用池管理模块。
    /// 功能：
    ///     1. 主要用于设置是否开启引用池类型的严格检查。
    ///     2. 开启后会检查引用类型为非抽象类，且为IReference的接口实现类。这可能会影响性能。
    /// </summary>
    public sealed class ReferencePoolModule : ModuleBase
    {
        [Header("是否开启引用类型严格检查(开启后会检查引用类型为非抽象类，且为IReference的接口实现类)")]
        [SerializeField] private EReferenceStrictCheckType m_EnableStrictCheck = EReferenceStrictCheckType.OnlyEnableInEditor;

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
                    FuLogger.LogInfo("[ReferencePoolModule]对 Reference Pool 启用了严格检查。它将会检查引用类型为非抽象类，且为IReference的接口实现类。这可能会影响性能.");
                }
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
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
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            ReferencePool.ClearAll();
        }
    }
}
