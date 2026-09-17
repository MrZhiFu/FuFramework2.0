using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FuFramework.NetWork.HTTP;

/// <summary>
/// 保持属性名称大小写的 Schema 过滤器
/// 用于在生成 Swagger/OpenAPI 文档时保持属性名称的原始大小写形式
/// </summary>
public sealed class PreservePropertyCasingSchemaFilter : ISchemaFilter
{
	/// <summary>
	/// 应用 Schema 过滤器，处理属性名称的大小写
	/// </summary>
	/// <param name="schema">要修改的 OpenAPI Schema</param>
	/// <param name="context">Schema 过滤器上下文，包含类型信息</param>
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		if (schema?.Properties == null || schema.Properties.Count == 0)
		{
			return;
		}
		PropertyInfo[] properties = context.Type.GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (schema.Properties.ContainsKey(propertyInfo.Name.ToLowerInvariant()))
			{
				OpenApiSchema value = schema.Properties[propertyInfo.Name.ToLowerInvariant()];
				schema.Properties.Remove(propertyInfo.Name.ToLowerInvariant());
				schema.Properties.Add(propertyInfo.Name, value);
			}
		}
	}
}
