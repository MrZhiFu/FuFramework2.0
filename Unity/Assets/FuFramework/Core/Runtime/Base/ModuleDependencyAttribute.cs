using System;

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 模块依赖特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ModuleDependencyAttribute : Attribute
    {
        /// <summary>
        /// 依赖的模块类型。
        /// </summary>
        public Type[] DependentTypes { get; }
    
        /// <summary>
        /// 初始化模块依赖特性。
        /// </summary>
        /// <param name="dependentTypes">依赖的模块类型。</param>
        public ModuleDependencyAttribute(params Type[] dependentTypes)
        {
            DependentTypes = dependentTypes;
        }
    }
}