using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 入口类。
    /// 1. 启动游戏
    /// 2. 驱动框架生命周期
    /// </summary>
    public partial class Launcher : MonoSingleton<Launcher>
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            // 注册框架模块
            RegisterModules();

            // 开始游戏流程
            StartProcedure();
        }

        /// <summary>
        /// 驱动框架模块帧更新
        /// </summary>
        private void Update()
        {
            ModuleManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            ModuleManager.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            ModuleManager.FixedUpdate();
        }
    }
}