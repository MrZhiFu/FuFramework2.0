// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Event
{
    /// <summary>
    /// 游戏逻辑事件基类（零成员标记类，刻意保留）。
    /// 用途：
    ///     1. 划定事件总线边界：订阅签名 EventHandler&lt;GameEventArgs&gt; 只放行游戏事件，
    ///        将来增设第二种事件池可用其它 T，互不污染签名。
    ///     2. 热更侧扩展缝：给游戏事件加公共成员时加在此处，全部事件类自动获得，
    ///        不触碰事件池约束类型 BaseEventArgs。
    /// 勿因零成员而删除或并入 BaseEventArgs：波及 40+ 文件且订阅边界退化。
    /// </summary>
    public abstract class GameEventArgs : BaseEventArgs { }
}