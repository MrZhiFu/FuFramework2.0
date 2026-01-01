using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// UI点击引导步骤
    /// </summary>
    public class ClickUIStep : BaseStep
    {
        protected override void OnExecute()
        {
            // 查找目标UI并添加监听
            var target = GameObject.Find(StepInfo.m_TargetUI);
            if (target != null)
            {
                // var clickHandler = target.AddComponent<GuideClickHandler>();
                // clickHandler.OnClicked += OnTargetClicked;
            }
            else
            {
                Debug.LogError($"[WeakGuideStep] 找不到目标UI: {StepInfo.m_TargetUI}");
                OnFail($"找不到目标UI: {StepInfo.m_TargetUI}");
                return;
            }

            // 显示高亮
            // GuideManager.Instance.ShowHighlight(targetUI, Vector2.zero);
        }

        private void OnTargetClicked() => Complete();

        protected override void OnComplete() => Cleanup();

        protected override void OnCancel() => Cleanup();

        /// <summary>
        /// 清理监听器
        /// </summary>
        private void Cleanup()
        {
            // 移除监听器
            var target = GameObject.Find(StepInfo.m_TargetUI);
            if (target != null)
            {
                // var clickHandler = target.GetComponent<GuideClickHandler>();
                // if (clickHandler != null)
                // {
                // clickHandler.OnClicked -= OnTargetClicked;
                // Object.Destroy(clickHandler);
                // }
            }

            // 隐藏高亮
            // GuideManager.Instance.HideHighlight();
        }

        /// <summary>
        /// 创建默认步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static ClickUIStep Create(StepInfo stepInfo)
        {
            var step = ReferencePool.Runtime.ReferencePool.Acquire<ClickUIStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}