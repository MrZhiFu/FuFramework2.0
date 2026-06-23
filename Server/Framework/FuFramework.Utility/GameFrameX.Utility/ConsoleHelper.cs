using System;

namespace FuFramework.Utility;

/// <summary>
/// 控制台辅助器
/// </summary>
public static class ConsoleHelper
{
	/// <summary>
	/// 打印控制台logo
	/// </summary>
	public static void ConsoleLogo()
	{
		Console.WriteLine("                                                                           ");
		Console.WriteLine(" _____                         ______                               __   __");
		Console.WriteLine("|  __ \\                        |  ___|                              \\ \\ / /");
		Console.WriteLine("| |  \\/  __ _  _ __ ___    ___ | |_    _ __   __ _  _ __ ___    ___  \\ V / ");
		Console.WriteLine("| | __  / _` || '_ ` _ \\  / _ \\|  _|  | '__| / _` || '_ ` _ \\  / _ \\ /   \\ ");
		Console.WriteLine("| |_\\ \\| (_| || | | | | ||  __/| |    | |   | (_| || | | | | ||  __// /^\\ \\");
		Console.WriteLine(" \\____/ \\__,_||_| |_| |_| \\___|\\_|    |_|    \\__,_||_| |_| |_| \\___|\\/   \\/");
		Console.WriteLine("                                                                           ");
		Console.WriteLine("           项目主页:https://github.com/MrZhiFu/FuFramework2.0                ");
		Console.WriteLine("           在线文档:https://fuframework.doc.alianblank.com");
		Console.WriteLine("                                                                           ");
	}
}
