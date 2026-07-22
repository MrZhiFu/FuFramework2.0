using System;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
using AOT.Launch;
using UnityEngine;

namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 框架帧驱动 + 游戏控制中枢。
    /// 功能：
    ///     1. 持有帧驱动委托，供 Hotfix 侧挂接 ModuleManager 生命周期方法
    ///     2. 自驱动 MonoBehaviour Update/LateUpdate/FixedUpdate，调用挂接的委托
    ///     3. 提供游戏级别控制：暂停、恢复、重启、退出
    /// </summary>
    public class GameDriven : MonoSingleton<GameDriven>
    {
        /// <summary>
        /// 框架模块帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.Update。
        /// </summary>
        public Action<float, float> OnUpdate;

        /// <summary>
        /// 框架模块延迟帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.LateUpdate。
        /// </summary>
        public Action<float, float> OnLateUpdate;

        /// <summary>
        /// 框架模块固定帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.FixedUpdate。
        /// </summary>
        public Action OnFixedUpdate;

        /// <summary>
        /// 释放全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.Dispose。
        /// </summary>
        public Action DisposeModules;

        /// <summary>
        /// 重新初始化全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.ReInit。
        /// </summary>
        public Action ReInitModules;

        /// <summary>
        /// 框架模块每秒更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.PerSecondUpdate。
        /// </summary>
        public Action OnPerSecondUpdate;

        /// <summary>
        /// 每秒更新累计时间
        /// </summary>
        private float m_PerSecondUpdateTimer;

        /// <summary>
        /// 驱动框架模块帧更新
        /// </summary>
        private void Update()
        {
            OnUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);

            m_PerSecondUpdateTimer += Time.deltaTime;
            if (m_PerSecondUpdateTimer >= 1f)
            {
                m_PerSecondUpdateTimer -= 1f;
                OnPerSecondUpdate?.Invoke();
            }
        }

        /// <summary>
        /// 驱动框架模块延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void OnDispose()
        {
            DisposeModules?.Invoke();

            DisposeModules    = null;
            ReInitModules     = null;
            OnUpdate          = null;
            OnLateUpdate      = null;
            OnFixedUpdate     = null;
            OnPerSecondUpdate = null;

            base.OnDispose();
        }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            GameSetting.Instance.PauseGame();
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            GameSetting.Instance.ResumeGame();
        }

        /// <summary>
        /// 重启游戏（如设置界面重启）。
        /// 依次释放所有模块、重新初始化模块、重新运行 AOT 引导流程。
        /// </summary>
        public void RestartGame()
        {
            DisposeModules?.Invoke();
            ReInitModules?.Invoke();

            // 重新运行 AOT 引导流程（重新显示加载界面并重进热更入口）
            LaunchProcess.RunAsync().Forget();
        }

        /// <summary>
        /// 退出游戏。
        /// </summary>
        public void QuitGame()
        {
            DisposeModules?.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
