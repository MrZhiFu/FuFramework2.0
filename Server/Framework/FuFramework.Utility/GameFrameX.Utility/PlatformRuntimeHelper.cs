using System.Runtime.InteropServices;

namespace FuFramework.Utility;

/// <summary>
/// 平台运行时帮助类
/// </summary>
public static class PlatformRuntimeHelper
{
	/// <summary>
	/// 是否是Linux
	/// </summary>
	public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

	/// <summary>
	/// 是否是Mac
	/// </summary>
	public static bool IsOsx => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

	/// <summary>
	/// 是否是Windows
	/// </summary>
	public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	/// <summary>
	/// 是否是FreeBSD
	/// </summary>
	public static bool IsFreeBsd => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
}
