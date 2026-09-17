using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Abstractions.Events;
using FuFramework.Core.BaseHandler;
using FuFramework.Core.Components;
using FuFramework.Foundation.Logger;
using FuFramework.NetWork.Abstractions;
using FuFramework.NetWork.HTTP;
using FuFramework.Utility.Extensions;
using FuFramework.Utility.Setting;

namespace FuFramework.Core.Hotfix;

/// <summary>
/// 热更模块，负责热更新相关的初始化、卸载、解析DLL等工作。
/// </summary>
internal sealed class HotfixModule
{
	/// <summary>
	/// 角色类型到事件ID到监听者的映射。
	/// </summary>
	private readonly Dictionary<ushort, Dictionary<int, List<IEventListener>>> _actorEvtListeners = new Dictionary<ushort, Dictionary<int, List<IEventListener>>>(512);

	/// <summary>
	/// 代理类型到代理包装类型的映射。
	/// </summary>
	private readonly Dictionary<Type, Type> _agentAgentWrapperMap = new Dictionary<Type, Type>(512);

	/// <summary>
	/// 组件类型到代理类型的映射。
	/// </summary>
	private readonly Dictionary<Type, Type> _agentCompMap = new Dictionary<Type, Type>(512);

	/// <summary>
	/// 组件类型到代理类型的映射。
	/// </summary>
	private readonly Dictionary<Type, Type> _compAgentMap = new Dictionary<Type, Type>(512);

	/// <summary>
	/// DLL路径。
	/// </summary>
	private readonly string _dllPath;

	/// <summary>
	/// HTTP命令到处理器的映射。
	/// </summary>
	private readonly ConcurrentDictionary<string, BaseHttpHandler> _httpHandlerMap = new ConcurrentDictionary<string, BaseHttpHandler>();

	/// <summary>
	/// RPC请求类型到响应类型的映射。
	/// </summary>
	private readonly ConcurrentDictionary<Type, Type> _rpcHandlerMap = new ConcurrentDictionary<Type, Type>();

	/// <summary>
	/// 消息ID到处理器类型的映射。
	/// </summary>
	private readonly ConcurrentDictionary<int, Type> _tcpHandlerMap = new ConcurrentDictionary<int, Type>();

	/// <summary>
	/// 消息处理类型列表
	/// </summary>
	private readonly List<Type> _tcpHandlerTypes = new List<Type>(512);

	/// <summary>
	/// 类型缓存。
	/// </summary>
	private readonly ConcurrentDictionary<string, object> _typeCacheMap = new ConcurrentDictionary<string, object>();

	/// <summary>
	/// 是否使用代理包装。
	/// </summary>
	private readonly bool _useAgentWrapper = true;

	/// <summary>
	/// DLL加载器。
	/// </summary>
	private DllLoader _dllLoader;

	/// <summary>
	/// 热更程序集。
	/// </summary>
	internal Assembly HotfixAssembly;

	/// <summary>
	/// 热更桥接接口。
	/// </summary>
	internal IHotfixBridge HotfixBridge { get; private set; }

	/// <summary>
	/// 构造函数，接受DLL路径。
	/// </summary>
	/// <param name="dllPath">DLL路径。</param>
	internal HotfixModule(string dllPath)
	{
		_dllPath = dllPath;
	}

	/// <summary>
	/// 默认构造函数，初始化热更程序集并解析DLL。
	/// </summary>
	internal HotfixModule()
	{
		HotfixAssembly = Assembly.GetEntryAssembly();
		ParseDll();
	}

	/// <summary>
	/// 初始化热更模块。
	/// </summary>
	/// <param name="reload">是否重新加载。</param>
	/// <returns>初始化是否成功。</returns>
	internal bool Init(bool reload)
	{
		bool result = false;
		try
		{
			_dllLoader = new DllLoader(_dllPath);
			HotfixAssembly = _dllLoader.HotfixDll;
			if (!reload)
			{
				LoadRefAssemblies();
			}
			ParseDll();
			LogHelper.Info("热更DLL初始化成功: " + _dllPath);
			result = true;
		}
		catch (Exception value)
		{
			LogHelper.Error($"热更DLL初始化失败...\n{value}");
			if (!reload)
			{
				throw;
			}
		}
		return result;
	}

	/// <summary>
	/// 卸载热更模块。
	/// </summary>
	public void Unload()
	{
		if (_dllLoader == null)
		{
			return;
		}
		WeakReference weak = _dllLoader.Unload();
		if (!GlobalSettings.CurrentSetting.IsDebug)
		{
			return;
		}
		Task.Run(async delegate
		{
			int tryCount = 0;
			while (weak.IsAlive && tryCount++ < 10)
			{
				await Task.Delay(100);
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
			LogHelper.Warn("热更DLL卸载" + (weak.IsAlive ? "失败" : "成功"));
		});
	}

	/// <summary>
	/// 加载引用的程序集。
	/// </summary>
	private void LoadRefAssemblies()
	{
		HashSet<string> hashSet = new HashSet<string>(from t in AppDomain.CurrentDomain.GetAssemblies()
			select t.GetName().Name);
		AssemblyName[] referencedAssemblies = HotfixAssembly.GetReferencedAssemblies();
		foreach (AssemblyName assemblyName in referencedAssemblies)
		{
			if (!hashSet.Contains(assemblyName.Name))
			{
				string text = Environment.CurrentDirectory + "/" + assemblyName.Name + ".dll";
				if (File.Exists(text))
				{
					Assembly.LoadFrom(text);
				}
			}
		}
	}

	/// <summary>
	/// 解析DLL中的类型并进行注册。
	/// </summary>
	private void ParseDll()
	{
		string fullName = typeof(IHotfixBridge).FullName;
		Type[] types = HotfixAssembly.GetTypes();
		foreach (Type type in types)
		{
			if (!AddAgent(type) && !AddEvent(type) && !AddTcpHandler(type) && !AddHttpHandler(type) && HotfixBridge.IsNull() && type.GetInterface(fullName) != null)
			{
				IHotfixBridge hotfixBridge = (IHotfixBridge)Activator.CreateInstance(type);
				HotfixBridge = hotfixBridge;
			}
			AddRpcHandler(type);
		}
	}

	/// <summary>
	/// 添加HTTP处理器。
	/// </summary>
	/// <param name="type">处理器类型。</param>
	/// <returns>是否添加成功。</returns>
	private bool AddHttpHandler(Type type)
	{
		if (!type.IsSubclassOf(typeof(BaseHttpHandler)))
		{
			return false;
		}
		HttpMessageMappingAttribute httpMessageMappingAttribute = (HttpMessageMappingAttribute)type.GetCustomAttribute(typeof(HttpMessageMappingAttribute));
		if (httpMessageMappingAttribute.IsNull())
		{
			return true;
		}
		BaseHttpHandler value = (BaseHttpHandler)Activator.CreateInstance(type);
		if (!_httpHandlerMap.TryAdd(httpMessageMappingAttribute.OriginalCmd, value))
		{
			throw new Exception("HTTP处理器命令重复注册，命令:" + httpMessageMappingAttribute.OriginalCmd);
		}
		if (!_httpHandlerMap.TryAdd(httpMessageMappingAttribute.StandardCmd, value))
		{
			throw new Exception("HTTP处理器命令重复注册，命令:" + httpMessageMappingAttribute.OriginalCmd);
		}
		return true;
	}

	/// <summary>
	/// 添加RPC处理器。
	/// </summary>
	/// <param name="type">处理器类型。</param>
	/// <returns>是否添加成功。</returns>
	private bool AddRpcHandler(Type type)
	{
		MessageRpcMappingAttribute messageRpcMappingAttribute = (MessageRpcMappingAttribute)type.GetCustomAttribute(typeof(MessageRpcMappingAttribute), inherit: true);
		if (messageRpcMappingAttribute.IsNull())
		{
			return false;
		}
		if (_rpcHandlerMap.TryGetValue(messageRpcMappingAttribute.RequestMessage.GetType(), out var value) && value?.GetType() == messageRpcMappingAttribute.ResponseMessage.GetType())
		{
			LogHelper.Error($"重复注册消息RPC处理器:[{messageRpcMappingAttribute.RequestMessage}] 消息:[{messageRpcMappingAttribute.ResponseMessage}]");
			return false;
		}
		_rpcHandlerMap.TryAdd(messageRpcMappingAttribute.RequestMessage.GetType(), messageRpcMappingAttribute.ResponseMessage.GetType());
		return true;
	}

	/// <summary>
	/// 添加TCP处理器。
	/// </summary>
	/// <param name="type">处理器类型。</param>
	/// <returns>是否添加成功。</returns>
	private bool AddTcpHandler(Type type)
	{
		MessageMappingAttribute messageMappingAttribute = (MessageMappingAttribute)type.GetCustomAttribute(typeof(MessageMappingAttribute), inherit: true);
		if (messageMappingAttribute == null)
		{
			return false;
		}
		string fullName = type.FullName;
		if (fullName == null)
		{
			return false;
		}
		if (!type.IsSealed)
		{
			throw new InvalidOperationException(fullName + " 必须是标记为sealed的类");
		}
		if (!fullName.EndsWith("Handler"))
		{
			throw new Exception("消息处理器 必须以[Handler]结尾，" + fullName);
		}
		if (_tcpHandlerTypes.Contains(messageMappingAttribute.MessageType))
		{
			LogHelper.Error("重复注册消息TCP处理器 类型:[" + type.FullName + "]");
			return false;
		}
		MessageTypeHandlerAttribute messageTypeHandlerAttribute = (MessageTypeHandlerAttribute)messageMappingAttribute.MessageType.GetCustomAttribute(typeof(MessageTypeHandlerAttribute), inherit: true);
		if (messageTypeHandlerAttribute == null)
		{
			return false;
		}
		int messageId = messageTypeHandlerAttribute.MessageId;
		if (!_tcpHandlerMap.TryAdd(messageId, type))
		{
			LogHelper.Error($"重复注册消息TCP处理器:[{messageId}] 消息:[{type}]");
		}
		_tcpHandlerTypes.Add(messageMappingAttribute.MessageType);
		return true;
	}

	/// <summary>
	/// 添加事件监听者。
	/// </summary>
	/// <param name="type">监听者类型。</param>
	/// <returns>是否添加成功。</returns>
	private bool AddEvent(Type type)
	{
		if (!type.IsImplWithInterface(typeof(IEventListener)))
		{
			return false;
		}
		string fullName = type.FullName;
		if (fullName == null)
		{
			return false;
		}
		if (!type.IsSealed)
		{
			throw new InvalidOperationException(fullName + " 必须是标记为sealed的类");
		}
		if (!fullName.EndsWith("EventListener"))
		{
			throw new Exception("事件处理器 必须以[EventListener]结尾，" + fullName);
		}
		Type key = type.BaseType.GetGenericArguments()[0].BaseType.GetGenericArguments()[0];
		ushort key2 = ComponentRegister.ComponentActorDic[key];
		Dictionary<int, List<IEventListener>> orAdd = _actorEvtListeners.GetOrAdd(key2);
		List<EventInfoAttribute> list = type.GetCustomAttributes<EventInfoAttribute>().ToList();
		if (list.Count == 0)
		{
			throw new Exception("IEventListener:" + type.FullName + "没有指定监听的事件");
		}
		int eventId = (list.FirstOrDefault() ?? throw new Exception("IEventListener:" + type.FullName + "没有指定监听的事件")).EventId;
		orAdd.GetOrAdd(eventId).Add((IEventListener)Activator.CreateInstance(type));
		return true;
	}

	/// <summary>
	/// 添加组件代理。
	/// </summary>
	/// <param name="type">代理类型。</param>
	/// <returns>是否添加成功。</returns>
	private bool AddAgent(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		if (!type.IsImplWithInterface(typeof(IComponentAgent)))
		{
			return false;
		}
		string fullName = type.FullName;
		if (fullName == null)
		{
			return false;
		}
		if (fullName == "FuFramework.Launcher.Logic.Server.ServerComp")
		{
			return false;
		}
		if (fullName.StartsWith("FuFramework.Hotfix.") && fullName.EndsWith("ComponentAgentWrapper"))
		{
			_agentAgentWrapperMap[type.BaseType] = type;
			return true;
		}
		if (!fullName.EndsWith("ComponentAgent"))
		{
			throw new Exception("组件代理必须以ComponentAgent结尾，" + fullName);
		}
		Type type2 = type.BaseType.GetGenericArguments()[0];
		if (!_compAgentMap.TryAdd(type2, type))
		{
			throw new Exception("组件:" + type2.FullName + "有多个代理");
		}
		_agentCompMap[type] = type2;
		return true;
	}

	/// <summary>
	/// 获取TCP处理器。
	/// </summary>
	/// <param name="msgId">消息ID。</param>
	/// <returns>TCP处理器实例。</returns>
	internal BaseMessageHandler GetTcpHandler(int msgId)
	{
		if (!_tcpHandlerMap.TryGetValue(msgId, out var value))
		{
			return null;
		}
		object obj = Activator.CreateInstance(value);
		if (obj is BaseMessageHandler result)
		{
			return result;
		}
		throw new Exception("错误的TCP处理器类型，" + obj.GetType().FullName);
	}

	/// <summary>
	/// 获取HTTP处理器。
	/// </summary>
	/// <param name="cmd">命令。</param>
	/// <returns>HTTP处理器实例。</returns>
	internal BaseHttpHandler GetHttpHandler(string cmd)
	{
		if (_httpHandlerMap.TryGetValue(cmd, out var value))
		{
			return value;
		}
		return null;
	}

	/// <summary>
	/// 获取HTTP处理器列表。
	/// </summary>
	/// <returns>HTTP处理器列表。</returns>
	internal List<BaseHttpHandler> GetListHttpHandler()
	{
		ICollection<BaseHttpHandler> values = _httpHandlerMap.Values;
		List<BaseHttpHandler> list = new List<BaseHttpHandler>(values.Count / 2);
		foreach (BaseHttpHandler item in values)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	/// <summary>
	/// 获取组件代理。
	/// </summary>
	/// <param name="component">组件实例。</param>
	/// <typeparam name="T">代理类型。</typeparam>
	/// <returns>代理实例。</returns>
	internal T GetAgent<T>(BaseComponent component) where T : IComponentAgent
	{
		Type type = component.GetType();
		if (_compAgentMap.TryGetValue(type, out var value))
		{
			T val = default(T);
			if (_useAgentWrapper && _agentAgentWrapperMap.TryGetValue(value, out var value2))
			{
				val = (T)Activator.CreateInstance(value2);
			}
			if (val.IsNull())
			{
				val = (T)Activator.CreateInstance(value);
			}
			if (val.IsNull())
			{
				throw new ArgumentNullException("agent");
			}
			val.SetOwner(component);
			return val;
		}
		throw new KeyNotFoundException("_compAgentMap ===>type");
	}

	/// <summary>
	/// 查找事件监听者。
	/// </summary>
	/// <param name="actorType">角色类型。</param>
	/// <param name="eventId">事件ID。</param>
	/// <returns>事件监听者列表。</returns>
	internal List<IEventListener> FindListeners(ushort actorType, int eventId)
	{
		if (_actorEvtListeners.TryGetValue(actorType, out var value) && value.TryGetValue(eventId, out var value2))
		{
			return value2;
		}
		return null;
	}

	/// <summary>
	/// 查找事件监听者。
	/// </summary>
	/// <param name="eventId">事件ID。</param>
	/// <returns>事件监听者列表。</returns>
	internal List<IEventListener> FindListeners(int eventId)
	{
		List<IEventListener> list = new List<IEventListener>(32);
		foreach (KeyValuePair<ushort, Dictionary<int, List<IEventListener>>> actorEvtListener in _actorEvtListeners)
		{
			if (actorEvtListener.Value.TryGetValue(eventId, out var value))
			{
				list.AddRange(value);
			}
		}
		return list;
	}

	/// <summary>
	/// 获取实例（主要用于获取Event, Timer, Schedule的处理器实例）。
	/// </summary>
	/// <param name="typeName">类型名称。</param>
	/// <typeparam name="T">实例类型。</typeparam>
	/// <returns>实例对象。</returns>
	internal T GetInstance<T>(string typeName)
	{
		return (T)_typeCacheMap.GetOrAdd(typeName, (string k) => HotfixAssembly.CreateInstance(k));
	}

	/// <summary>
	/// 获取代理类型。
	/// </summary>
	/// <param name="compType">组件类型。</param>
	/// <returns>代理类型。</returns>
	internal Type GetAgentType(Type compType)
	{
		_compAgentMap.TryGetValue(compType, out var value);
		return value;
	}

	/// <summary>
	/// 获取组件类型。
	/// </summary>
	/// <param name="agentType">代理类型。</param>
	/// <returns>组件类型。</returns>
	internal Type GetComponentType(Type agentType)
	{
		_agentCompMap.TryGetValue(agentType, out var value);
		return value;
	}
}
