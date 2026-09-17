using System;
using System.IO;

namespace FuFramework.Core.Abstractions.Attribute;

/// <summary>
/// 组件类型标记
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ComponentTypeAttribute : System.Attribute
{
	/// <summary>
	/// 组件类型
	/// </summary>
	public ushort Type { get; }

	/// <summary>
	/// 组件类型
	/// </summary>
	/// <param name="type">组件类型,值应大于0且小于ActorType.Max并且不为ActorType.Separator</param>
	public ComponentTypeAttribute(ushort type)
	{
		if (((type == 0 || type == 128) ? true : false) || type >= 999)
		{
			throw new InvalidDataException($"无效的组件类型 {type},值应大于{0}且小于{999}和不为{128}");
		}
		Type = type;
	}
}
