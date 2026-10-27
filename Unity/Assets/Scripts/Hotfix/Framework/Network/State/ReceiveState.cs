using System;
using System.IO;

// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 接收状态：攒包缓冲流与当前包的解析进度（目标长度、压缩标记、空包体标志），支撑粘包/半包切分。
	/// </summary>
	internal sealed class ReceiveState : IDisposable
	{
		/// <summary>
		/// 默认缓冲区长度, 64KB
		/// </summary>
		public const int DEFAULT_BUFFER_LENGTH = 1024 * 64;

		/// <summary>
		/// 默认包头长度, 14字节
		/// </summary>
		public const int PACKET_HEADER_LENGTH = 14;

		/// <summary>
		/// 是否已释放
		/// </summary>
		private bool m_disposed = false;

		/// <summary>
		/// 是否为空消息体
		/// </summary>
		public bool IsEmptyBody { get; private set; }

		/// <summary>
		/// 是否处于"已解析包头、等待/收攒包体"阶段（false = 等待包头）
		/// </summary>
		public bool IsAwaitingBody { get; set; }

		/// <summary>
		/// 当前包的压缩标记（包头期从 helper 解析结果写入，包体期跨批次消费）
		/// </summary>
		public byte ZipFlag { get; set; }

		/// <summary>
		/// 接收数据流
		/// </summary>
		public MemoryStream Stream { get; private set; } = new(DEFAULT_BUFFER_LENGTH);

		/// <summary>
		/// 准备接收包头
		/// </summary>
		/// <param name="packetHeaderLength">包头长度</param>
		public void PrepareForPacketHeader(int packetHeaderLength = PACKET_HEADER_LENGTH)
		{
			Reset(packetHeaderLength);
			IsAwaitingBody = false;
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		public void Dispose()
		{
			if (m_disposed) return;

			if (Stream != null)
			{
				Stream.Dispose();
				Stream = null;
			}

			m_disposed = true;
		}

		/// <summary>
		/// 重置接收状态（进入"等待/收攒包体"阶段）
		/// </summary>
		/// <param name="targetLength">目标长度</param>
		public void Reset(int targetLength)
		{
			if (targetLength < 0) throw new InvalidOperationException("目标长度无效。");

			Stream.Position = 0L;
			Stream.SetLength(targetLength);

			// 发现内容长度为空.说明是个空消息或者内容是默认值.
			IsEmptyBody = targetLength == 0;
		}
	}
}
