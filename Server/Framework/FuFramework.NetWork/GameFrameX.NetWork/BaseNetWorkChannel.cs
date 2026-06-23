using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.SuperSocket.Server;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.WebSocket.Server;
using FuFramework.Utility.Extensions;
using FuFramework.Utility.Setting;

namespace FuFramework.NetWork;

/// <summary>
/// 基础网络通道
/// </summary>
public class BaseNetWorkChannel : INetWorkChannel
{
	/// <summary>
	/// WebSocket会话
	/// </summary>
	private readonly WebSocketSession _webSocketSession;

	/// <summary>
	/// 关闭源
	/// </summary>
	protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

	/// <summary>
	/// 网络发送超时时间,单位秒
	/// </summary>
	protected readonly TimeSpan NetWorkSendTimeOutSecondsTimeSpan;

	private readonly ConcurrentDictionary<string, object> _userDataKv = new ConcurrentDictionary<string, object>();

	private long _lastReceiveMessageTime;

	/// <summary>
	/// 是否是WebSocket
	/// </summary>
	public bool IsWebSocket { get; }

	/// <summary>
	/// 设置
	/// </summary>
	public AppSetting Setting { get; }

	/// <summary>
	/// 会话
	/// </summary>
	public IGameAppSession GameAppSession { get; }

	/// <summary>
	/// Rpc会话
	/// </summary>
	public IRpcSession RpcSession { get; }

	/// <summary>
	/// 初始化
	/// </summary>
	/// <param name="session"></param>
	/// <param name="setting"></param>
	/// <param name="rpcSession"></param>
	/// <param name="isWebSocket"></param>
	public BaseNetWorkChannel(IGameAppSession session, AppSetting setting, IRpcSession rpcSession, bool isWebSocket)
	{
		setting.CheckNotNull("setting");
		GameAppSession = session;
		IsWebSocket = isWebSocket;
		Setting = setting;
		RpcSession = rpcSession;
		NetWorkSendTimeOutSecondsTimeSpan = TimeSpan.FromSeconds(Setting.NetWorkSendTimeOutSeconds);
		if (isWebSocket)
		{
			_webSocketSession = (WebSocketSession)session;
		}
	}

	/// <summary>
	/// 异步写入消息
	/// </summary>
	/// <param name="messageObject">消息对象</param>
	/// <param name="errorCode">错误码</param>
	/// <returns></returns>
	public virtual async Task WriteAsync(INetworkMessage messageObject, int errorCode = 0)
	{
		messageObject.CheckNotNull("messageObject");
		if (messageObject is IResponseMessage { ErrorCode: 0 } responseMessage && errorCode != 0)
		{
			responseMessage.ErrorCode = errorCode;
		}
		byte[] data = MessageHelper.EncoderHandler.Handler(messageObject);
		if (Setting.IsDebug && Setting.IsDebugSend)
		{
			if (messageObject is IHeartBeatMessage)
			{
				if (Setting.IsDebugSendHeartBeat)
				{
					LogHelper.Debug("---发送" + messageObject.ToFormatMessageString());
				}
			}
			else
			{
				LogHelper.Debug("---发送" + messageObject.ToFormatMessageString());
			}
		}
		if (!GameAppSession.IsConnected)
		{
			return;
		}
		using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(NetWorkSendTimeOutSecondsTimeSpan);
		_ = 1;
		try
		{
			if (!IsWebSocket)
			{
				await GameAppSession.SendAsync(data, cancellationTokenSource.Token);
			}
			else
			{
				await ((AppSession)_webSocketSession).SendAsync(data, cancellationTokenSource.Token);
			}
		}
		catch (OperationCanceledException ex)
		{
			LogHelper.Error("消息发送超时被取消:" + ex.Message);
		}
		catch (Exception exception)
		{
			LogHelper.Error(exception);
		}
	}

	/// <summary>
	/// 关闭
	/// </summary>
	public virtual void Close()
	{
		ClearData();
		CancellationTokenSource.Cancel();
	}

	/// <summary>
	/// 是否关闭
	/// </summary>
	/// <returns></returns>
	public virtual bool IsClosed()
	{
		return CancellationTokenSource.IsCancellationRequested;
	}

	/// <summary>
	/// 获取用户数据对象.
	/// 可能会发生转换失败的异常。
	/// 如果数据不存在则返回null
	/// </summary>
	/// <param name="key">数据Key</param>
	/// <typeparam name="T">将要获取的数据类型。</typeparam>
	/// <returns>用户数据对象</returns>
	public T GetData<T>(string key)
	{
		if (_userDataKv.TryGetValue(key, out var value))
		{
			return (T)value;
		}
		return default(T);
	}

	/// <summary>
	/// 清除自定义数据
	/// </summary>
	public void ClearData()
	{
		_userDataKv.Clear();
	}

	/// <summary>
	/// 删除自定义数据
	/// </summary>
	/// <param name="key"></param>
	public void RemoveData(string key)
	{
		_userDataKv.Remove(key, out var _);
	}

	/// <summary>
	/// 设置自定义数据
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public void SetData(string key, object value)
	{
		_userDataKv[key] = value;
	}

	/// <summary>
	/// 更新接收消息的时间
	/// </summary>
	/// <param name="offsetTicks"></param>
	public void UpdateReceiveMessageTime(long offsetTicks = 0L)
	{
		_lastReceiveMessageTime = DateTime.UtcNow.Ticks + offsetTicks;
	}

	/// <summary>
	/// 获取最后接收消息到现在的时间。单位秒
	/// </summary>
	/// <param name="utcTime"></param>
	/// <returns></returns>
	public long GetLastMessageTimeSecond(in DateTime utcTime)
	{
		return (utcTime.Ticks - _lastReceiveMessageTime) / 10000000;
	}

	long INetWorkChannel.GetLastMessageTimeSecond(in DateTime utcTime)
	{
		return GetLastMessageTimeSecond(in utcTime);
	}
}
