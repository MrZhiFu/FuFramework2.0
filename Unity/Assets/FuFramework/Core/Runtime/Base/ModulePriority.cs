// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 框架模块优先级定义
    /// </summary>
    public static class ModulePriority
    {
        public const int System = 100; // 系统级：输入、网络
        public const int Core = 80; // 核心：资源、事件
        public const int Game = 60; // 游戏逻辑
        public const int UI = 40; // UI相关
        public const int Render = 20; // 渲染
        public const int Default = 50; // 默认
    }
}