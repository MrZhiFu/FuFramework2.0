using UnityEngine.Networking;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public partial class UnityWebRequestDownloadAgentHelper
    {
        private sealed class DownloadHandler : DownloadHandlerScript
        {
            /// 使用 UnityWebRequest 实现的下载代理辅助器
            private readonly UnityWebRequestDownloadAgentHelper m_Owner;

            /// <summary>
            /// 事件管理器
            /// </summary>
            private readonly EventManager m_EventManager = ModuleManager.GetModule<EventManager>();

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
                if (!m_Owner || m_Owner.m_UnityWebRequest == null || dataLength <= 0) 
                    return base.ReceiveData(datas, dataLength);
                
                // 发送更新数据流事件
                var downloadAgentHelperUpdateBytesEventArgs = DownloadAgentHelperUpdateBytesEventArgs.Create(datas, 0, dataLength);
                m_EventManager.Fire(this, downloadAgentHelperUpdateBytesEventArgs);

                // 发送更新数据大小事件
                var downloadAgentHelperUpdateLengthEventArgs = DownloadAgentHelperUpdateLengthEventArgs.Create(dataLength);
                m_EventManager.Fire(this, downloadAgentHelperUpdateLengthEventArgs);

                return base.ReceiveData(datas, dataLength);
            }
        }
    }
}
