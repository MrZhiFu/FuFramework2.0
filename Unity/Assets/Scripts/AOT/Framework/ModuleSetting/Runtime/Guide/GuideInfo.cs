using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 引导数据
    /// </summary>
    [System.Serializable]
    public class GuideInfo
    {
        /// <summary>
        /// 引导Id
        /// </summary>
        public string m_GuideId;

        /// <summary>
        /// 引导名称
        /// </summary>
        public string m_GuideName;

        /// <summary>
        /// 开始步骤Id
        /// </summary>
        public string m_StartStepId;

        /// <summary>
        /// 引导所包含的步骤
        /// </summary>
        public List<StepInfo> m_Steps;
    }
}