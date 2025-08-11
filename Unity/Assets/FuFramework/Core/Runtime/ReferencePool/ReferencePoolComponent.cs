using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 引用池组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/ReferencePool")]
    public sealed class ReferencePoolComponent : FuComponent
    {
        [Header("是否开启强制检查(开启后会检查引用类型为非抽象类，且为IReference的接口实现类, 同时在 Release 调用时，会检查传入的引用是否已经可以重复归还-EnQueue)")]
        [SerializeField] private ReferenceStrictCheckType m_EnableStrictCheck = ReferenceStrictCheckType.AlwaysEnable;

        /// <summary>
        /// 获取或设置是否开启强制检查。
        /// </summary>

        public bool EnableStrictCheck
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
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            IsAutoRegister = false;
            base.Awake();
        }


        private void Start()
        {
            EnableStrictCheck = m_EnableStrictCheck switch
            {
                ReferenceStrictCheckType.AlwaysEnable              => true,
                ReferenceStrictCheckType.OnlyEnableWhenDevelopment => Debug.isDebugBuild,
                ReferenceStrictCheckType.OnlyEnableInEditor        => Application.isEditor,
                _                                                  => false
            };
        }
    }
}