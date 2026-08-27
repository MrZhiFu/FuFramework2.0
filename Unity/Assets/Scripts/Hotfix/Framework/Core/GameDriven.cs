using System;
using Cysharp.Threading.Tasks;
using AOT.Framework.ModuleSetting.Runtime;
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
        /// 框架模块帧更新委托。启动完成后由 Hotfix 侧挂接，指向 ModuleManager.Update。
        /// </summary>
        public Action<float, float> OnUpdate;

        /// <summary>
        /// 框架模块延迟帧更新委托。启动完成后由 Hotfix 侧挂接，指向 ModuleManager.LateUpdate。
        /// </summary>
        public Action<float, float> OnLateUpdate;

        /// <summary>
        /// 框架模块固定帧更新委托。启动完成后由 Hotfix 侧挂接，指向 ModuleManager.FixedUpdate。
        /// </summary>
        public Action OnFixedUpdate;

        /// <summary>
        /// 框架模块每秒更新委托。启动完成后由 Hotfix 侧挂接，指向 ModuleManager.PerSecondUpdate。
        /// </summary>
        public Action OnPerSecondUpdate;

        /// <summary>
        /// 释放全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.Dispose。
        /// </summary>
        public Action DisposeModules;

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

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                QuitGame();
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
            OnUpdate          = null;
            OnLateUpdate      = null;
            OnFixedUpdate     = null;
            OnPerSecondUpdate = null;

            base.OnDispose();
        }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame() => GameSetting.Instance.PauseGame();

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame() => GameSetting.Instance.ResumeGame();

        /// <summary>
        /// 重启游戏（如设置界面重启）。兼容旧入口：转异步流程 fire-and-forget。
        /// 依次释放所有模块、等待 ICancelAsync 模块取消清理完毕、重新初始化模块、重新运行 AOT 启动流程。
        /// </summary>
        public void RestartGame() => RestartGameAsync().Forget();

        /// <summary>
        /// 重启游戏异步流程：Dispose（同步清理 + 各自 Cancel）→ 等待所有 ICancelAsync 模块取消清理完毕 → 完整重跑启动流程。
        /// 完整启动（LaunchProcess）负责资源热更（版本/清单/下载），并由 HotfixLauncher.MainAsync 重启路径
        /// 在重新加载配置后分阶段 重新初始化 模块（基础模块先、依赖配置的功能模块后）再进入游戏。
        /// 取消清理保证旧生命周期在途任务已全部完成，杜绝旧任务写回新生命周期。
        /// </summary>
        private async UniTask RestartGameAsync()
        {
            DisposeModules?.Invoke();
            await ModuleManager.CancelAllAsync();
            await LaunchProcess.RunAsync();
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
