using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.Core.Log
{
	/// <summary>
	/// 日志颜色扩展。
	/// 功能：
	///     1. Color 转十六进制色值（#RRGGBB）。
	///     2. 文本富文本染色（&lt;color=#RRGGBB&gt; 包裹）。
	///     3. 编译期自动生成染色的 [模块名] 前缀。
	/// </summary>
	public static class LogColorEx
	{
		/// <summary>
		/// 横幅绿（迁移自历史裸写的 #43F656）。
		/// </summary>
		public static readonly Color Green = new Color32(0x43, 0xF6, 0x56, 0xFF);

		/// <summary>
		/// 模块青（迁移自历史裸写的 #00FBD5）。
		/// </summary>
		public static readonly Color Teal = new Color32(0x00, 0xFB, 0xD5, 0xFF);

		/// <summary>
		/// 亮红，用于关键消息高亮。
		/// </summary>
		public static readonly Color Red = new Color32(0xFF, 0x52, 0x52, 0xFF);

		/// <summary>
		/// 橙色。
		/// </summary>
		public static readonly Color Orange = new Color32(0xFF, 0xA7, 0x26, 0xFF);

		/// <summary>
		/// 黄色。
		/// </summary>
		public static readonly Color Yellow = new Color32(0xFF, 0xD5, 0x4F, 0xFF);

		/// <summary>
		/// 天蓝。
		/// </summary>
		public static readonly Color SkyBlue = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

		/// <summary>
		/// 蓝色。
		/// </summary>
		public static readonly Color Blue = new Color32(0x44, 0x8A, 0xFF, 0xFF);

		/// <summary>
		/// 紫色。
		/// </summary>
		public static readonly Color Purple = new Color32(0xB3, 0x88, 0xFF, 0xFF);

		/// <summary>
		/// 粉色。
		/// </summary>
		public static readonly Color Pink = new Color32(0xFF, 0x80, 0xAB, 0xFF);

		/// <summary>
		/// 青柠色。
		/// </summary>
		public static readonly Color Lime = new Color32(0xC6, 0xFF, 0x00, 0xFF);

		/// <summary>
		/// 模块自动取色调色板（深色控制台底可读；顺序不影响同模块的稳定取色）。
		/// </summary>
		private static readonly Color[] Palette =
		{
			Green, Teal, Red, Orange, Yellow, SkyBlue, Blue, Purple, Pink, Lime
		};

		/// <summary>
		/// Color 转十六进制色值字符串（不含透明度）。
		/// </summary>
		/// <param name="color">目标颜色。</param>
		/// <returns>"#RRGGBB" 形式的色值。</returns>
		public static string ToHex(this Color color)
		{
			return $"#{ToByte(color.r):X2}{ToByte(color.g):X2}{ToByte(color.b):X2}";
		}

		/// <summary>
		/// 文本按富文本染色，包裹 &lt;color=#RRGGBB&gt; 标签。
		/// 注意：应内联进 FuLogger 日志调用使用，随条件编译宏整体裁剪，真机零成本。
		/// </summary>
		/// <param name="text">文本内容。</param>
		/// <param name="color">目标颜色。</param>
		/// <returns>染色后的文本。</returns>
		public static string Color(this string text, Color color)
		{
			return $"<color={color.ToHex()}>{text}</color>";
		}

		/// <summary>
		/// 编译期自动生成染色的 [模块名] 前缀。
		/// 模块名取调用方源文件名（剥去扩展名与部分类后缀，如 WebModule.Debug.cs → WebModule），
		/// 颜色默认按模块名 FNV-1a 哈希从调色板稳定取色，可显式传参覆盖。
		/// 注意：消息本身不应再手写 [前缀]，已有手写前缀的场景请直接使用 Color 扩展；
		/// 应内联进 FuLogger 日志调用使用，随条件编译宏整体裁剪，真机零成本。
		/// </summary>
		/// <param name="message">日志内容（不含前缀）。</param>
		/// <param name="color">前缀颜色；null 时按模块名自动取色。</param>
		/// <param name="callerFile">编译器注入的调用方源文件路径，调用时无需传参。</param>
		/// <returns>带染色 [模块名] 前缀的日志内容。</returns>
		public static string Module(this string message, Color? color = null, [CallerFilePath] string callerFile = "")
		{
			if (string.IsNullOrEmpty(callerFile)) return message;

			var fileName    = Path.GetFileNameWithoutExtension(callerFile);
			var dotIndex    = fileName.IndexOf('.');
			var moduleName  = dotIndex > 0 ? fileName.Substring(0, dotIndex) : fileName;
			var prefixColor = color ?? Palette[PaletteIndexOf(moduleName)];

			return $"[{moduleName}]".Color(prefixColor) + message;
		}

		/// <summary>
		/// 按模块名从调色板确定性取色（FNV-1a 哈希，同模块永远同色）。
		/// </summary>
		/// <param name="moduleName">模块名。</param>
		/// <returns>调色板索引。</returns>
		private static int PaletteIndexOf(string moduleName)
		{
			unchecked
			{
				uint hash = 2166136261;
				foreach (var c in moduleName)
				{
					hash ^= (uint)c;
					hash *= 16777619;
				}

				return (int)(hash % Palette.Length);
			}
		}

		/// <summary>
		/// [0,1] 浮点颜色分量转字节。
		/// </summary>
		/// <param name="value">颜色分量。</param>
		/// <returns>0~255 字节值。</returns>
		private static byte ToByte(float value)
		{
			return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
		}
	}
}