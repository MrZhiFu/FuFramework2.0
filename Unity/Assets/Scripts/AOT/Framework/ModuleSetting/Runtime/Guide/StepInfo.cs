// ReSharper disable once CheckNamespace

namespace AOT.Framework.ModuleSetting.Runtime.Guide
{
    /// <summary>
    /// 引导步骤类型
    /// </summary>
    public enum EStepType
    {
        /// <summary>
        /// 无类型
        /// </summary>
        None,

        /// <summary>
        /// 点击UI引导
        /// </summary>
        ClickUI,

        /// <summary>
        /// 对话引导
        /// </summary>
        Dialog,

        /// <summary>
        /// 等待步骤
        /// </summary>
        Wait
    }

    /// <summary>
    /// 引导步骤数据
    /// </summary>
    [System.Serializable]
    public class StepInfo
    {
        /// <summary>
        /// 步骤Id
        /// </summary>
        public string m_StepId;

        /// <summary>
        /// 步骤类型
        /// </summary>
        public EStepType m_EStepType;

        /// <summary>
        /// 下一个步骤Id
        /// </summary>
        public string m_NextStepId;

        /// <summary>
        /// 是否可以跳过
        /// </summary>
        public bool m_IsCanJump;


        /// <summary>
        /// 目标窗口(点击UI引导使用)
        /// </summary>
        public string m_TargetWindow;
        
        /// <summary>
        /// 目标UI(点击UI引导使用)
        /// </summary>
        public string m_TargetUI;

        /// <summary>
        /// 对话内容(对话引导使用)
        /// </summary>
        public string m_DialogContent;

        /// <summary>
        /// 等待时间(等待时间引导使用)
        /// </summary>
        public float m_WaitTime;
    }
}