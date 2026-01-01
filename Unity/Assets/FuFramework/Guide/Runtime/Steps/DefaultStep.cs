using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// 默认引导步骤(用于未定义类型的步骤)
    /// </summary>
    public class DefaultStep : BaseStep
    {
        protected override void OnExecute()
        {
            base.OnExecute();
            
            // 默认步骤立即完成
            Complete(); 
        }

        public override bool CanComplete() => true;

        public override bool CanExecute() => true;

        /// <summary>
        /// 创建默认步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static DefaultStep Create(StepInfo stepInfo)
        {
            var step = ReferencePool.Runtime.ReferencePool.Acquire<DefaultStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}