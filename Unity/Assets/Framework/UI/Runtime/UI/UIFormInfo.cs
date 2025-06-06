using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组界面信息。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIFormInfo : IReference
    {
        /// <summary>
        /// 获取界面。
        /// </summary>
        public IUIForm UIForm { get; private set; }

        /// <summary>
        /// 界面是否暂停(初始化时默认为false,即界面没有暂停)
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// 界面是否被覆盖(初始化时默认为false,即界面没有被覆盖)
        /// </summary>
        public bool Covered { get; set; }

        /// <summary>
        /// 创建界面组界面信息。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>创建的界面组界面信息。</returns>
        /// <exception cref="GameFrameworkException">界面为空时抛出。</exception>
        public static UIFormInfo Create(IUIForm uiForm)
        {
            if (uiForm == null) throw new GameFrameworkException("ui界面逻辑实例为空.");
            var uiFormInfo = ReferencePool.Acquire<UIFormInfo>();
            uiFormInfo.UIForm = uiForm;
            uiFormInfo.Paused = true;
            uiFormInfo.Covered = true;
            return uiFormInfo;
        }

        /// <summary>
        /// 清理界面组界面信息。
        /// </summary>
        public void Clear()
        {
            UIForm = null;
            Paused = false;
            Covered = false;
        }
    }
}