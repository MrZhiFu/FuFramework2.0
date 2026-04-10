using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// Launcher 游戏控制部分
    /// </summary>
    public partial class Launcher
    {
        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            ModuleSetting.Runtime.ModuleSetting.Instance.PauseGame();
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            ModuleSetting.Runtime.ModuleSetting.Instance.ResumeGame();
        }

        /// <summary>
        /// 重启游戏(如设置界面重启)
        /// </summary>
        public void RestartGame()
        {
            // 释放所有模块
            ModuleManager.Dispose();

            // 重新初始化所有模块
            ModuleManager.ReInit();

            // 开始游戏流程
            StartProcedure();
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            ModuleManager.Dispose();

            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}