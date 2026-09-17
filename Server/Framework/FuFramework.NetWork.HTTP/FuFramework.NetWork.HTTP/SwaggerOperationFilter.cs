using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FuFramework.NetWork.HTTP;

/// <summary>
/// 自定义 Swagger 操作过滤器,用于处理动态路由和请求/响应文档
/// </summary>
public sealed class SwaggerOperationFilter : IOperationFilter
{
	/// <summary>
	/// HTTP 处理器字典,key为命令ID,value为处理器类型
	/// </summary>
	private readonly List<BaseHttpHandler> _handlers;

	/// <summary>
	/// 构造函数
	/// </summary>
	/// <param name="handlers">HTTP处理器字典</param>
	public SwaggerOperationFilter(List<BaseHttpHandler> handlers)
	{
		_handlers = handlers;
	}

	/// <summary>
	/// 应用过滤器配置
	/// </summary>
	/// <param name="operation">OpenAPI操作对象</param>
	/// <param name="context">操作过滤器上下文</param>
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		string routeTemplate = context.ApiDescription.RelativePath;
		if (string.IsNullOrEmpty(routeTemplate))
		{
			return;
		}
		operation.Parameters.Clear();
		BaseHttpHandler baseHttpHandler = _handlers.FirstOrDefault((BaseHttpHandler h) => routeTemplate.EndsWith(h.GetType().GetCustomAttribute<HttpMessageMappingAttribute>()?.StandardCmd ?? "", StringComparison.OrdinalIgnoreCase));
		if (baseHttpHandler == null)
		{
			return;
		}
		Type type = baseHttpHandler.GetType();
		HttpMessageRequestAttribute customAttribute = type.GetCustomAttribute<HttpMessageRequestAttribute>();
		HttpMessageResponseAttribute customAttribute2 = type.GetCustomAttribute<HttpMessageResponseAttribute>();
		if (customAttribute?.MessageType != null)
		{
			OpenApiSchema openApiSchema = context.SchemaGenerator.GenerateSchema(customAttribute.MessageType, context.SchemaRepository);
			if (openApiSchema.Properties != null)
			{
				PropertyInfo[] properties = customAttribute.MessageType.GetProperties();
				Dictionary<string, OpenApiSchema> dictionary = new Dictionary<string, OpenApiSchema>();
				PropertyInfo[] array = properties;
				foreach (PropertyInfo propertyInfo in array)
				{
					string key = propertyInfo.Name.ToLowerInvariant();
					if (openApiSchema.Properties.TryGetValue(key, out var value))
					{
						dictionary[propertyInfo.Name] = value;
					}
				}
				openApiSchema.Properties.Clear();
				foreach (KeyValuePair<string, OpenApiSchema> item in dictionary)
				{
					openApiSchema.Properties[item.Key] = item.Value;
				}
			}
			operation.RequestBody = new OpenApiRequestBody
			{
				Required = true,
				Description = "请求参数",
				Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new OpenApiMediaType
				{
					Schema = openApiSchema
				} }
			};
		}
		else
		{
			operation.RequestBody = new OpenApiRequestBody
			{
				Required = true,
				Description = "请求参数",
				Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new OpenApiMediaType
				{
					Schema = new OpenApiSchema
					{
						Type = "object"
					}
				} }
			};
		}
		OpenApiSchema openApiSchema2 = new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["code"] = new OpenApiSchema
				{
					Type = "integer",
					Description = "响应状态码",
					Example = new OpenApiInteger(0)
				},
				["message"] = new OpenApiSchema
				{
					Type = "string",
					Description = "响应消息",
					Example = new OpenApiString("success")
				}
			}
		};
		if (customAttribute2?.MessageType != null)
		{
			openApiSchema2.Properties["data"] = context.SchemaGenerator.GenerateSchema(customAttribute2.MessageType, context.SchemaRepository);
		}
		else
		{
			openApiSchema2.Properties["data"] = new OpenApiSchema
			{
				Type = "object"
			};
		}
		operation.Responses = new OpenApiResponses { ["200"] = new OpenApiResponse
		{
			Description = "成功响应",
			Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new OpenApiMediaType
			{
				Schema = openApiSchema2
			} }
		} };
		operation.Summary = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
		operation.Description = GetTypeDescription(type);
	}

	private string GetTypeDescription(Type type)
	{
		return type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
	}
}
