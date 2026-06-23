using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FuFramework.Foundation.Json;

/// <summary>
/// JSON序列化和反序列化的帮助类
/// 提供了默认和格式化两种序列化配置选项
/// 支持枚举字符串转换、忽略null值、忽略循环引用等功能
/// </summary>
public static class JsonHelper
{
	/// <summary>
	/// 默认序列化配置
	/// 包含以下特性:
	/// <list type="bullet">
	/// <item><description>忽略null值属性 - 序列化时不输出值为null的属性</description></item>
	/// <item><description>忽略循环引用 - 避免因对象间循环引用导致的序列化异常</description></item>
	/// <item><description>属性名称大小写不敏感 - 反序列化时属性名称匹配不区分大小写</description></item>
	/// <item><description>枚举值序列化为字符串 - 枚举值输出为其名称字符串而非数字</description></item>
	/// <item><description>允许从字符串读取数字 - 支持将字符串形式的数字反序列化为数值类型</description></item>
	/// <item><description>支持特殊浮点数值 - 可处理NaN、Infinity等特殊浮点数</description></item>
	/// <item><description>允许JSON注释 - 可以包含并跳过注释内容</description></item>
	/// <item><description>允许尾随逗号 - JSON对象或数组的最后一项后可以有逗号</description></item>
	/// </list>
	/// </summary>
	public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		PropertyNamingPolicy = null,
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
		NumberHandling = (JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals),
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	/// <summary>
	/// 格式化序列化配置
	/// 在默认配置基础上增加了JSON格式化功能
	/// 包含以下特性:
	/// <list type="bullet">
	/// <item><description>忽略null值属性 - 序列化时不输出值为null的属性</description></item>
	/// <item><description>忽略循环引用 - 避免因对象间循环引用导致的序列化异常</description></item>
	/// <item><description>属性名称大小写不敏感 - 反序列化时属性名称匹配不区分大小写</description></item>
	/// <item><description>枚举值序列化为字符串 - 枚举值输出为其名称字符串而非数字</description></item>
	/// <item><description>输出格式化的JSON文本 - 包含适当的缩进和换行，提高可读性</description></item>
	/// <item><description>允许从字符串读取数字 - 支持将字符串形式的数字反序列化为数值类型</description></item>
	/// <item><description>支持特殊浮点数值 - 可处理NaN、Infinity等特殊浮点数</description></item>
	/// <item><description>允许JSON注释 - 可以包含并跳过注释内容</description></item>
	/// <item><description>允许尾随逗号 - JSON对象或数组的最后一项后可以有逗号</description></item>
	/// </list>
	/// </summary>
	public static readonly JsonSerializerOptions FormatOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		PropertyNamingPolicy = null,
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
		NumberHandling = (JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals),
		WriteIndented = true,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	/// <summary>
	/// 将对象序列化为JSON字符串
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <returns>序列化后的JSON字符串</returns>
	/// <exception cref="T:System.ArgumentNullException">当obj为null时抛出</exception>
	public static string Serialize(object obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		return JsonSerializer.Serialize(obj, DefaultOptions);
	}

	/// <summary>
	/// 将对象序列化为JSON字符串
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <param name="options">自定义序列化配置</param>
	/// <returns>序列化后的JSON字符串</returns>
	/// <exception cref="T:System.ArgumentNullException">当obj或options为null时抛出</exception>
	public static string Serialize(object obj, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		ArgumentNullException.ThrowIfNull(options, "options");
		return JsonSerializer.Serialize(obj, options);
	}

	/// <summary>
	/// 将对象序列化为格式化的JSON字符串
	/// 使用格式化序列化配置(FormatOptions)，生成的JSON包含适当的缩进和换行
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <returns>格式化后的JSON字符串</returns>
	/// <exception cref="T:System.ArgumentNullException">当obj为null时抛出</exception>
	public static string SerializeFormat(object obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		return JsonSerializer.Serialize(obj, FormatOptions);
	}

	/// <summary>
	/// 将对象序列化为UTF8编码的字节数组
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <returns>序列化后的UTF8字节数组</returns>
	/// <exception cref="T:System.ArgumentNullException">当obj为null时抛出</exception>
	public static byte[] SerializeToUtf8Bytes(object obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		return JsonSerializer.SerializeToUtf8Bytes(obj, DefaultOptions);
	}

	/// <summary>
	/// 将对象序列化为UTF8编码的字节数组
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <param name="options">自定义序列化配置</param>
	/// <returns>序列化后的UTF8字节数组</returns>
	/// <exception cref="T:System.ArgumentNullException">当obj或options为null时抛出</exception>
	public static byte[] SerializeToUtf8Bytes(object obj, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		ArgumentNullException.ThrowIfNull(options, "options");
		return JsonSerializer.SerializeToUtf8Bytes(obj, options);
	}

	/// <summary>
	/// 将对象序列化为格式化的UTF8编码字节数组
	/// 使用格式化序列化配置(FormatOptions)
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <returns>序列化后的UTF8字节数组</returns>
	/// <exception cref="T:System.ArgumentNullException">当obj为null时抛出</exception>
	public static byte[] SerializeToUtf8BytesFormat(object obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		return JsonSerializer.SerializeToUtf8Bytes(obj, FormatOptions);
	}

	/// <summary>
	/// 将JSON字符串反序列化为指定类型的对象
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="json">需要反序列化的JSON字符串</param>
	/// <typeparam name="T">目标类型，必须是引用类型且有无参构造函数</typeparam>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当json为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当json为空字符串或仅包含空白字符时抛出</exception>
	public static T Deserialize<T>(string json) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(json, "json");
		ArgumentException.ThrowIfNullOrEmpty(json, "json");
		ArgumentException.ThrowIfNullOrWhiteSpace(json, "json");
		return JsonSerializer.Deserialize<T>(json, DefaultOptions);
	}

	/// <summary>
	/// 将JSON字符串反序列化为指定类型的对象
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="json">需要反序列化的JSON字符串</param>
	/// <param name="options">自定义序列化配置</param>
	/// <typeparam name="T">目标类型，必须是引用类型且有无参构造函数</typeparam>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当json或options为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当json为空字符串或仅包含空白字符时抛出</exception>
	public static T Deserialize<T>(string json, JsonSerializerOptions options) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(json, "json");
		ArgumentException.ThrowIfNullOrEmpty(json, "json");
		ArgumentException.ThrowIfNullOrWhiteSpace(json, "json");
		ArgumentNullException.ThrowIfNull(options, "options");
		return JsonSerializer.Deserialize<T>(json, options);
	}

	/// <summary>
	/// 将JSON字符串反序列化为指定Type类型的对象
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="json">需要反序列化的JSON字符串</param>
	/// <param name="type">目标类型的Type对象</param>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当json或type为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当json为空字符串或仅包含空白字符时抛出</exception>
	public static object Deserialize(string json, Type type)
	{
		ArgumentNullException.ThrowIfNull(json, "json");
		ArgumentException.ThrowIfNullOrEmpty(json, "json");
		ArgumentException.ThrowIfNullOrWhiteSpace(json, "json");
		ArgumentNullException.ThrowIfNull(type, "type");
		return JsonSerializer.Deserialize(json, type, DefaultOptions);
	}

	/// <summary>
	/// 将JSON字符串反序列化为指定Type类型的对象
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="json">需要反序列化的JSON字符串</param>
	/// <param name="type">目标类型的Type对象</param>
	/// <param name="options">自定义序列化配置</param>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当json、type或options为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当json为空字符串或仅包含空白字符时抛出</exception>
	public static object Deserialize(string json, Type type, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(json, "json");
		ArgumentException.ThrowIfNullOrEmpty(json, "json");
		ArgumentException.ThrowIfNullOrWhiteSpace(json, "json");
		ArgumentNullException.ThrowIfNull(type, "type");
		ArgumentNullException.ThrowIfNull(options, "options");
		return JsonSerializer.Deserialize(json, type, options);
	}

	/// <summary>
	/// 从UTF8编码的字节数组反序列化为指定类型的对象
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="utf8Bytes">UTF8编码的JSON字节数组</param>
	/// <typeparam name="T">目标类型，必须是引用类型且有无参构造函数</typeparam>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当utf8Bytes为null时抛出</exception>
	public static T DeserializeFromUtf8Bytes<T>(byte[] utf8Bytes) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(utf8Bytes, "utf8Bytes");
		return JsonSerializer.Deserialize<T>(utf8Bytes, DefaultOptions);
	}

	/// <summary>
	/// 从UTF8编码的字节数组反序列化为指定类型的对象
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="utf8Bytes">UTF8编码的JSON字节数组</param>
	/// <param name="options">自定义序列化配置</param>
	/// <typeparam name="T">目标类型，必须是引用类型且有无参构造函数</typeparam>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当utf8Bytes或options为null时抛出</exception>
	public static T DeserializeFromUtf8Bytes<T>(byte[] utf8Bytes, JsonSerializerOptions options) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(utf8Bytes, "utf8Bytes");
		ArgumentNullException.ThrowIfNull(options, "options");
		return JsonSerializer.Deserialize<T>(utf8Bytes, options);
	}

	/// <summary>
	/// 从UTF8编码的字节数组反序列化为指定Type类型的对象
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="utf8Bytes">UTF8编码的JSON字节数组</param>
	/// <param name="type">目标类型的Type对象</param>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当utf8Bytes或type为null时抛出</exception>
	public static object DeserializeFromUtf8Bytes(byte[] utf8Bytes, Type type)
	{
		ArgumentNullException.ThrowIfNull(utf8Bytes, "utf8Bytes");
		ArgumentNullException.ThrowIfNull(type, "type");
		return JsonSerializer.Deserialize(utf8Bytes, type, DefaultOptions);
	}

	/// <summary>
	/// 从UTF8编码的字节数组反序列化为指定Type类型的对象
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="utf8Bytes">UTF8编码的JSON字节数组</param>
	/// <param name="type">目标类型的Type对象</param>
	/// <param name="options">自定义序列化配置</param>
	/// <returns>反序列化后的对象实例</returns>
	/// <exception cref="T:System.ArgumentNullException">当utf8Bytes、type或options为null时抛出</exception>
	public static object DeserializeFromUtf8Bytes(byte[] utf8Bytes, Type type, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(utf8Bytes, "utf8Bytes");
		ArgumentNullException.ThrowIfNull(type, "type");
		ArgumentNullException.ThrowIfNull(options, "options");
		return JsonSerializer.Deserialize(utf8Bytes, type, options);
	}

	/// <summary>
	/// 尝试将JSON字符串反序列化为指定类型的对象
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="json">需要反序列化的JSON字符串</param>
	/// <param name="result">反序列化成功时的结果对象</param>
	/// <typeparam name="T">目标类型，必须是引用类型且有无参构造函数</typeparam>
	/// <returns>反序列化是否成功</returns>
	public static bool TryDeserialize<T>(string json, out T result) where T : class, new()
	{
		try
		{
			result = Deserialize<T>(json);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}

	/// <summary>
	/// 尝试将JSON字符串反序列化为指定类型的对象
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="json">需要反序列化的JSON字符串</param>
	/// <param name="result">反序列化成功时的结果对象</param>
	/// <param name="options">自定义序列化配置</param>
	/// <typeparam name="T">目标类型，必须是引用类型且有无参构造函数</typeparam>
	/// <returns>反序列化是否成功</returns>
	public static bool TryDeserialize<T>(string json, out T result, JsonSerializerOptions options) where T : class, new()
	{
		try
		{
			result = Deserialize<T>(json, options);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}

	/// <summary>
	/// 尝试将对象序列化为JSON字符串
	/// 使用默认序列化配置(DefaultOptions)
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <param name="result">序列化成功时的JSON字符串</param>
	/// <returns>序列化是否成功</returns>
	public static bool TrySerialize(object obj, out string result)
	{
		try
		{
			result = Serialize(obj);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}

	/// <summary>
	/// 尝试将对象序列化为JSON字符串
	/// 使用自定义序列化配置
	/// </summary>
	/// <param name="obj">需要序列化的对象</param>
	/// <param name="result">序列化成功时的JSON字符串</param>
	/// <param name="options">自定义序列化配置</param>
	/// <returns>序列化是否成功</returns>
	public static bool TrySerialize(object obj, out string result, JsonSerializerOptions options)
	{
		try
		{
			result = Serialize(obj, options);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}
}
