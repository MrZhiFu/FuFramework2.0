using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FuFramework.Core.Abstractions;
using FuFramework.Core.Abstractions.Agent;
using FuFramework.Core.Abstractions.Attribute;
using FuFramework.Core.Actors;
using FuFramework.Core.Hotfix;
using FuFramework.Core.Utility;
using FuFramework.Foundation.Logger;
using FuFramework.Utility.Extensions;

namespace FuFramework.Core.Components;

/// <summary>
/// 组件注册器
/// </summary>
public static class ComponentRegister
{
	/// <summary>
	/// ActorType 到 CompTypeList 的映射
	/// </summary>
	private static readonly Dictionary<ushort, HashSet<Type>> ActorComponentDic = new Dictionary<ushort, HashSet<Type>>();

	/// <summary>
	/// CompType 到 ActorType 的映射
	/// </summary>
	internal static readonly Dictionary<Type, ushort> ComponentActorDic = new Dictionary<Type, ushort>();

	/// <summary>
	/// 功能码到 CompTypes 的映射
	/// </summary>
	private static readonly Dictionary<int, HashSet<Type>> FuncComponentDic = new Dictionary<int, HashSet<Type>>();

	/// <summary>
	/// CompType 到功能码的映射
	/// </summary>
	private static readonly Dictionary<Type, short> ComponentFuncDic = new Dictionary<Type, short>();

	/// <summary>
	/// 根据 CompType 获取对应的 ActorType 类型
	/// </summary>
	/// <param name="componentType">组件类型</param>
	/// <returns>ActorType 类型</returns>
	public static ushort GetActorType(Type componentType)
	{
		ComponentActorDic.TryGetValue(componentType, out var value);
		return value;
	}

	/// <summary>
	/// 根据 ActorType 类型获取对应的 CompTypes 列表
	/// </summary>
	/// <param name="actorType">ActorType 类型</param>
	/// <returns>CompTypes 列表</returns>
	public static IEnumerable<Type> GetComponents(ushort actorType)
	{
		ActorComponentDic.TryGetValue(actorType, out var value);
		return value;
	}

	/// <summary>
	/// 初始化组件注册器
	/// </summary>
	/// <param name="assembly">目标程序集</param>
	/// <returns>初始化任务</returns>
	/// <exception cref="T:System.Exception">当程序集为 null 时抛出</exception>
	public static Task Init(Assembly assembly = null)
	{
		if (assembly == null)
		{
			assembly = Assembly.GetEntryAssembly();
		}
		assembly.CheckNotNull("assembly");
		Type typeFromHandle = typeof(BaseComponent);
		Type[] types = assembly.GetTypes();
		foreach (Type type in types)
		{
			if (!type.IsAbstract && type.IsSubclassOf(typeFromHandle))
			{
				if (!(type.GetCustomAttribute(typeof(ComponentTypeAttribute)) is ComponentTypeAttribute { Type: var type2 }))
				{
					throw new Exception("comp:" + type.FullName + "未绑定actor类型");
				}
				ActorComponentDic.GetOrAdd(type2).Add(type);
				ComponentActorDic[type] = type2;
				if (type2 < 128 && type.GetCustomAttribute(typeof(FuncAttribute)) is FuncAttribute funcAttribute)
				{
					FuncComponentDic.GetOrAdd(funcAttribute.Func).Add(type);
					ComponentFuncDic[type] = funcAttribute.Func;
				}
			}
		}
		LogHelper.Info("初始化组件注册完成");
		return Task.CompletedTask;
	}

	/// <summary>
	/// 激活全局组件
	/// </summary>
	/// <returns>激活任务</returns>
	public static async Task ActiveGlobalComponents()
	{
		try
		{
			foreach (var (num2, hashSet2) in ActorComponentDic)
			{
				foreach (Type item in hashSet2)
				{
					if (HotfixManager.GetAgentType(item) == null)
					{
						LogHelper.Warn($"{item}未实现Agent,请检查业务代码是否正确");
					}
				}
				if (num2 > 128)
				{
					LogHelper.Debug($"激活全局Actor: {num2}");
					await ActorManager.GetOrNew(ActorIdGenerator.GetActorId(num2));
				}
			}
			LogHelper.Debug("激活全局组件并检测组件是否都包含Agent实现完成");
		}
		catch (Exception)
		{
			LogHelper.Error("激活全局组件并检测组件是否都包含Agent实现失败");
			throw;
		}
	}

	/// <summary>
	/// 激活角色组件
	/// </summary>
	/// <param name="componentAgent">组件代理</param>
	/// <param name="openFuncSet">开放的功能集合</param>
	/// <returns>激活任务</returns>
	public static Task ActiveRoleComponents(IComponentAgent componentAgent, HashSet<short> openFuncSet)
	{
		short value;
		return ActiveComponents(componentAgent.Owner.Actor, (Type t) => !ComponentFuncDic.TryGetValue(t, out value) || openFuncSet.Contains(value));
	}

	/// <summary>
	/// 激活指定条件下的组件
	/// </summary>
	/// <param name="actor">演员</param>
	/// <param name="predict">条件判断函数</param>
	/// <returns>激活任务</returns>
	internal static async Task ActiveComponents(IActor actor, Func<Type, bool> predict = null)
	{
		IEnumerable<Type> components = GetComponents(actor.Type);
		if (components != null)
		{
			foreach (Type item in components)
			{
				if (predict == null || predict(item))
				{
					Type agentType = HotfixManager.GetAgentType(item);
					try
					{
						await actor.GetComponentAgent(agentType);
					}
					catch (Exception exception)
					{
						LogHelper.Fatal(exception);
					}
				}
			}
		}
		else
		{
			LogHelper.Fatal($"获取不属于此actor：{actor.Type}的组件");
		}
	}

	/// <summary>
	/// 创建组件实例
	/// </summary>
	/// <param name="actor">演员</param>
	/// <param name="compType">组件类型</param>
	/// <returns>创建的组件实例</returns>
	internal static BaseComponent CreateComponent(Actor actor, Type compType)
	{
		if (!ActorComponentDic.TryGetValue(actor.Type, out var value))
		{
			throw new Exception($"获取不属于此actor：{actor.Type}的Component:{compType.FullName}");
		}
		if (!value.Contains(compType))
		{
			throw new Exception($"获取不属于此actor：{actor.Type}的Component:{compType.FullName}");
		}
		BaseComponent baseComponent = (BaseComponent)Activator.CreateInstance(compType);
		if (baseComponent != null)
		{
			baseComponent.Actor = actor;
			return baseComponent;
		}
		return null;
	}
}
