using System;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
    /// <summary>
    /// 下载处理器，继承自 DownloadHandlerScript。
    /// 功能：
    ///     1. 接收数据(在从远程服务器接收数据时每帧被调用, 这个方法的返回值是一个布尔值，表示是否继续下载，从而实现断点续传)
    ///     2. 发送更新数据流事件和更新数据大小事件
    /// </summary>
    internal sealed class DownloadHandler : DownloadHandlerScript
    {
        /// <summary>
        /// 下载代理辅助器(使用UnityWebRequest实现)
        /// </summary>
        private readonly UnityWebRequestDownloadAgentHelper m_Owner;

        /// <summary>
        /// 获取本处理器所属的下载代理辅助器。
        /// 供 DownloadAgent 在事件回调中据 sender 判定「事件是否来自自己的辅助器」——
        /// 四个下载辅助器事件的 Id 是共享的（AllowMultiHandler），每次 Broadcast 会分发给全部订阅的下载代理。
        /// </summary>
        internal UnityWebRequestDownloadAgentHelper Owner => m_Owner;

        /// <summary>
        /// 事件管理模块
        /// </summary>
        private readonly EventModule m_EventModule = ModuleManager.GetModule<EventModule>();

        /// <summary>
        /// 构造一个下载处理器
        /// </summary>
        /// <param name="owner">传递一个固定大小的Buffer作为下载的缓冲区</param>
        public DownloadHandler(UnityWebRequestDownloadAgentHelper owner) : base(owner.m_CachedBytes)
        {
            m_Owner = owner;
        }

        /// <summary>
        /// 接收数据(在从远程服务器接收数据时每帧被调用, 这个方法的返回值是一个布尔值，表示是否继续下载，从而实现断点续传)
        /// </summary>
        /// <param name="datas">字节缓冲区，包含从远程服务器接收的未处理数据</param>
        /// <param name="dataLength">缓冲区新接收的字节数</param>
        /// <returns></returns>
        protected override bool ReceiveData(byte[] datas, int dataLength)
        {
            if (m_Owner == null || m_Owner.m_Disposed || m_Owner.m_UnityWebRequest == null || dataLength <= 0)
                return base.ReceiveData(datas, dataLength);

            // 发送更新数据流事件
            // 注意：Unity 的 DownloadHandlerScript(byte[]) 使用预分配缓冲区，该缓冲区跨调用复用且每次从下标 0 开始写入；
            // 而事件经 Broadcast 要到下一帧才被 DownloadAgent 消费并写盘。若事件按引用持有该缓冲区，
            // 同一帧内到达的多块数据(大文件必然)会被最后一块覆盖，导致写盘内容错乱 —— 因此此处必须拷贝出独立副本再交给事件。
            //
            // 关于「改用 ArrayPool<byte>.Shared.Rent/Return 消除本次 ≤4096B/块的 Gen0 分配」的取舍（评估结论：暂不采用）：
            // 归还点只能落在「消费完毕」处，而本事件的消费方 DownloadAgent 无法唯一确定自己就是该数据的产出者 ——
            // 三个 DownloadAgent 在 Initialize 时都订阅了同一个事件 Id（AllowMultiHandler），一次 Broadcast 会分发给
            // 全部订阅者；若在消费方归还，同一数组会被重复归还（ArrayPool 重复归还即把同一数组交给两个租借者，
            // 直接造成数据错乱），而下载场景下这一分发给全部代理是常态而非边角。
            // 唯一能精确配对的归还点是事件参数自身的生命周期终点（DownloadAgentHelperUpdateBytesEventArgs.Clear()），
            // 但该参数类型不在本次改动范围内；故此处保留独立副本的写法，以「确定的正确性」优先于「消除 4KB/块的 Gen0 垃圾」。
            // 若后续要改为池化，须先在事件参数内记录租借来源并在 Clear() 中归还（一次创建对应一次归还），
            // 且事件参数必须保持「仅被批量分发一次」，否则仍会重复归还。
            var bytes = new byte[dataLength];
            Buffer.BlockCopy(datas, 0, bytes, 0, dataLength);

            var downloadAgentHelperUpdateBytesEventArgs = DownloadAgentHelperUpdateBytesEventArgs.Create(bytes, 0, dataLength);
            m_EventModule.Broadcast(this, downloadAgentHelperUpdateBytesEventArgs);

            // 发送更新数据大小事件
            var downloadAgentHelperUpdateLengthEventArgs = DownloadAgentHelperUpdateLengthEventArgs.Create(dataLength);
            m_EventModule.Broadcast(this, downloadAgentHelperUpdateLengthEventArgs);

            return base.ReceiveData(datas, dataLength);
        }
    }
}