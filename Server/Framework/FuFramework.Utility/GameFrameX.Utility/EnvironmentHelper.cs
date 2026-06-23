using System;
using Microsoft.Extensions.Hosting;

namespace FuFramework.Utility;

/// <summary>
/// 环境帮助器
/// </summary>
public static class EnvironmentHelper
{
	/// <summary>
	/// 判断是否为开发环境
	/// 通过检查环境变量 ASPNETCORE_ENVIRONMENT 或 DOTNET_ENVIRONMENT 的值是否为 Development
	/// </summary>
	/// <returns>如果是开发环境返回true，否则返回false</returns>
	public static bool IsDevelopment()
	{
		return string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), Environments.Development, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 判断是否为生产环境
	/// 通过检查环境变量 ASPNETCORE_ENVIRONMENT 或 DOTNET_ENVIRONMENT 的值是否为 Production
	/// </summary>
	/// <returns>如果是生产环境返回true，否则返回false</returns>
	public static bool IsProduction()
	{
		return string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), Environments.Production, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 判断是否为测试/预发布环境
	/// 通过检查环境变量 ASPNETCORE_ENVIRONMENT 或 DOTNET_ENVIRONMENT 的值是否为 Staging
	/// </summary>
	/// <returns>如果是测试/预发布环境返回true，否则返回false</returns>
	public static bool IsStaging()
	{
		return string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), Environments.Staging, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 判断是否为任意自定义环境
	/// 通过检查环境变量 ASPNETCORE_ENVIRONMENT 或 DOTNET_ENVIRONMENT 的值是否与指定环境名称匹配
	/// </summary>
	/// <param name="environmentName">要检查的环境名称</param>
	/// <returns>如果当前环境与指定环境名称匹配返回true，否则返回false</returns>
	public static bool IsEnvironment(string environmentName)
	{
		return string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), environmentName, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 判断当前应用是否运行在Docker容器中
	/// 通过检查环境变量 DOTNET_RUNNING_IN_CONTAINER 是否存在来判断
	/// </summary>
	/// <returns>如果在Docker容器中运行返回true，否则返回false</returns>
	public static bool IsDocker()
	{
		return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
	}

	/// <summary>
	/// 判断当前应用是否运行在Kubernetes集群中
	/// 通过检查环境变量 KUBERNETES_SERVICE_HOST 是否存在来判断
	/// </summary>
	/// <returns>如果在Kubernetes集群中运行返回true，否则返回false</returns>
	public static bool IsKubernetes()
	{
		return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));
	}

	/// <summary>
	/// 获取当前运行环境名称
	/// 优先获取 ASPNETCORE_ENVIRONMENT 环境变量，如果不存在则获取 DOTNET_ENVIRONMENT 环境变量
	/// </summary>
	/// <returns>返回当前环境名称，如果未设置环境变量则返回null</returns>
	public static string GetEnvironmentName()
	{
		return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
	}
}
