using System;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
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
        /// 仅负责模块自身的同步清理，不包含 ReferencePool.ClearAll（后者在排水之后调用，仅释放闲置引用）。
        /// </summary>
        public Action DisposeModules;

        /// <summary>
        /// 每秒更新累计时间
        /// </summary>
        private float m_PerSecondUpdateTimer;

        /// <summary>
        /// 重启流程的生命周期取消源：GameDriven 销毁（OnDispose）时取消，重启链据此中止（生命周期所有者）。
        /// </summary>
        private readonly LifecycleCancellationSource m_RestartCancellation = new();

        /// <summary>
        /// 是否正在重启中（重入守卫：重启流程可跨帧，期间重复请求直接忽略，避免并发跑多轮 Dispose/启动流程）。
        /// </summary>
        private bool m_IsRestarting;

        /// <summary>
        /// 是否已释放（幂等哨兵）：QuitGame 与 OnDispose 都可能触发模块释放，保证只释放一次。
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        /// 驱动框架模块帧更新
        /// </summary>
        private void Update()
        {
            OnUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);

            // 每秒驱动用无缩放时间：暂停（timeScale = 0）时仍需触发（心跳/超时类逻辑），
            // 且用 while 补齐卡顿跨过的整数秒，避免漏触发。
            m_PerSecondUpdateTimer += Time.unscaledDeltaTime;
            while (m_PerSecondUpdateTimer >= 1f)
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
        /// 释放模块（幂等）：QuitGame 与 OnDispose 都会调用，重复进入只释放一次。
        /// 先取出委托并置 null 再调用：即使委托内部抛异常，后续再进入也不会二次释放（模块二次 OnDispose 会 NRE）。
        /// </summary>
        private void DisposeModulesOnce()
        {
            if (m_IsDisposed) return;

            m_IsDisposed = true;

            var disposeModules = DisposeModules;
            DisposeModules = null;
            disposeModules?.Invoke();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void OnDispose()
        {
            // 先取消重启链：生命周期已结束，在途重启流程（可能停在排水等待中）不得继续重跑启动流程
            m_RestartCancellation.Cancel();
            m_RestartCancellation.Dispose();

            DisposeModulesOnce();

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
        /// 重启游戏（如设置界面重启）。
        /// 依次释放所有模块、等待 ICancelAsync 模块取消清理完毕、重新初始化模块、重新运行 AOT 启动流程。
        /// 重入守卫：重启流程可跨帧，期间重复请求直接忽略。
        /// </summary>
        public void RestartGame()
        {
            if (m_IsDisposed || m_IsRestarting) return;
            RestartGameAsync().Forget();
        }

        /// <summary>
        /// 退出游戏。
        /// </summary>
        public void QuitGame()
        {
            // 幂等释放：随后的 OnDestroy → OnDispose 会再次调用释放，此处保证模块只被释放一次
            DisposeModulesOnce();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 重启游戏异步流程：Dispose（同步清理 + 各自 Cancel）→ 等待所有 ICancelAsync 模块取消清理完毕 → 释放引用池闲置对象 → 完整重跑启动流程。
        /// 完整启动（LaunchProcess）负责资源热更（版本/清单/下载），并由 HotfixLauncher.MainAsync 重启路径
        /// 在重新加载配置后分阶段 重新初始化 模块（基础模块先、依赖配置的功能模块后）再进入游戏。
        /// 取消清理保证旧生命周期在途任务已全部完成，杜绝旧任务写回新生命周期；
        /// 随后 ReferencePool.ClearAll 释放各引用池的闲置对象（保留类型条目与计数，迟到 Recycle 仍自洽），
        /// 为新一轮生命周期回收内存。
        /// 生命周期所有者：m_RestartCancellation 持有本轮 Token，OnDispose 取消它使本链中止。
        /// </summary>
        private async UniTask RestartGameAsync()
        {
            var cancellationToken = m_RestartCancellation.Token;

            m_IsRestarting = true;
            try
            {
                DisposeModulesOnce();
                await ModuleManager.CancelAllAsync();

                // 排水等待可跨帧：期间 GameDriven 可能已销毁，此时重启链已失去生命周期，不再继续重跑启动流程
                cancellationToken.ThrowIfCancellationRequested();

                // 排水完成后释放引用池闲置对象（保留类型条目与计数，故不依赖排水“彻底完成”这一强假设）
                ReferencePool.ClearAll();

                await LaunchProcess.RunAsync();
            }
            catch (OperationCanceledException)
            {
                // 生命周期结束（OnDispose 取消）导致的取消属预期路径，不视为错误上报
                FuLogger.LogInfo("[GameDriven] 重启流程随 GameDriven 销毁被取消。");
            }
            finally
            {
                m_IsRestarting = false;
            }
        }
    }
}