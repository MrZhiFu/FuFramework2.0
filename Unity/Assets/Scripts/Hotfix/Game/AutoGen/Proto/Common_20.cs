using System;
using ProtoBuf;
using System.Collections.Generic;
using Hotfix.Framework.Network;

namespace Hotfix.Game.Proto
{
	/// <summary>
	/// 返回码
	/// </summary>
	public enum ResultCode
	{
		/// <summary>
		/// 成功
		/// </summary>
		Success = 0, 
		/// <summary>
		/// 失败
		/// </summary>
		Failed = 1, 
	}

	/// <summary>
	/// 操作错误代码
	/// </summary>
	public enum OperationStatusCode
	{
		/// <summary>
		/// 成功
		/// </summary>
		Ok = 0, 
		/// <summary>
		/// 配置表错误
		/// </summary>
		ConfigErr = 1, 
		/// <summary>
		/// 客户端传递参数错误
		/// </summary>
		ParamErr = 2, 
		/// <summary>
		/// 消耗不足
		/// </summary>
		CostNotEnough = 3, 
		/// <summary>
		/// 未开通服务
		/// </summary>
		Forbidden = 4, 
		/// <summary>
		/// 不存在
		/// </summary>
		NotFound = 5, 
		/// <summary>
		/// 已经存在
		/// </summary>
		HasExist = 6, 
		/// <summary>
		/// 账号不存在或为空
		/// </summary>
		AccountCannotBeNull = 7, 
		/// <summary>
		/// 无法执行数据库修改
		/// </summary>
		Unprocessable = 8, 
		/// <summary>
		/// 未知平台
		/// </summary>
		UnknownPlatform = 9, 
		/// <summary>
		/// 正常通知
		/// </summary>
		Notice = 10, 
		/// <summary>
		/// 功能未开启，主消息屏蔽
		/// </summary>
		FuncNotOpen = 11, 
		/// <summary>
		/// 其他
		/// </summary>
		Other = 12, 
		/// <summary>
		/// 内部服务错误
		/// </summary>
		InternalServerError = 13, 
		/// <summary>
		/// 通知客户端服务器人数已达上限
		/// </summary>
		ServerFullyLoaded = 14, 
	}

}
