# Web Pb 请求错误响应识别 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `WebModule.Post<T>` 识别服务端 `HTTP 200 + JSON 错误信封` 响应，抛出携带 code/message 原文的清晰异常，不再把 JSON 字节误当 protobuf 反序列化。

**Architecture:** 响应头 `Content-Type` 从 `WebPbData.Complete` 一路带到 `WebBufferResult.ContentType`，`Post<T>` 在 protobuf 反序列化前判别：含 `json` 则按现有 `HttpJsonResult` DTO 解析并抛 `InvalidOperationException`（沿用现有异常契约）。另在 protobuf 反序列化失败的报错中附正文前 100 字符，便于直读垃圾响应。

**Tech Stack:** Unity C# / UniTask / protobuf-net（项目自带 GoogleProtocolBuffers Runtime）/ Newtonsoft.Json（已有引用）

**Spec:** `Docs/superpowers/specs/2026-11-26-web-pb-error-envelope-design.md`

## Global Constraints

- 铁律：无 LINQ、无反射、无原生 Task/Coroutine，UniTask 全程；CancellationToken 必传不适用本次（不新增异步 API）。
- 缩进一律 Tab（与现有文件一致）。
- 异常类型沿用 `Post<T>` 现有契约：`InvalidOperationException`。
- Content-Type 判别为"包含 `json`"的大小写不敏感匹配（服务端实测值 `application/json; charset=utf-8`）。
- 验证方式：`unity-cli refresh_assets` → `unity-cli get_compilation_state` 确认 0 错误（项目无单测基建）；最终 Play 模式实测。
- 不自动提交：任务完成后列清单询问用户确认（`Docs/Git提交规范.md`）。

## Review Focus

无自动化测试，以下输入靠终审与 Play 实测盯住：

1. **响应缺失 Content-Type 头**（`GetResponseHeader` 返回 null）→ 走 protobuf 正常路径，不误判（Task 1 传 null + Task 2 判空保证）。
2. **正常 protobuf 200 响应回归** → 行为与改前完全一致（Play 实测登录在 server 正常时仍能成功；本轮 server 返回 500，无法覆盖此路径，遗留项）。
3. **JSON 但非信封格式**（如 HTML 错误页）→ 异常信息附"非信封格式 + 正文片段"，不吞内容（`DescribeJsonEnvelope` catch 分支）。
4. **正文极长** → 预览截断至 100 字符加省略号，异常信息不刷屏（`PreviewBody`）。
5. **JSON 信封 `code=0`**（未来服务端若以信封包成功响应）→ 当前契约下仍按错误信封抛（信息含 `code=0`），与设计一致：服务端成功响应恒为 protobuf，此形态不应出现，出现即暴露异常。

---

### Task 1: WebBufferResult 增加 ContentType 并由 WebPbData 传递

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/Result/WebBufferResult.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/Data/WebPbData.cs:45-48`

**Interfaces:**
- Consumes: `UnityWebRequest.GetResponseHeader(string)`（缺失该头时返回 null）。
- Produces: `WebBufferResult.ContentType`（`string` 属性，null 表示缺失）；构造函数签名变为 `WebBufferResult(object userData, byte[] result, string contentType = null)`。默认参数保证另一调用方 `WebJsonBytesData.cs:57` 无需改动。

- [ ] **Step 1: WebBufferResult 增加 ContentType 属性与构造参数**

属性置于 `Result` 与 `UserData` 之间；构造函数追加可选第三参数并赋值。XML 注释风格与文件现有一致（含 `<param>` 说明"缺失为 null"）。

- [ ] **Step 2: WebPbData.Complete 传递 Content-Type**

`WebPbData.cs:47` 改为：

```csharp
			m_CompletionSource.TrySetResult(new WebBufferResult(UserData, request.downloadHandler.data, request.GetResponseHeader("Content-Type")));
```

- [ ] **Step 3: 编译验证**

```
unity-cli refresh_assets
unity-cli get_compilation_state
```

预期：编译成功，0 错误。

---

### Task 2: Post<T> 识别 JSON 错误信封并抛清晰异常

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/WebModule.API.cs`（usings；`Post<T>` 189-228 行；partial class 尾部新增两个私有静态 helper；`Post<T>` XML remarks 同步）

**Interfaces:**
- Consumes: Task 1 的 `WebBufferResult.ContentType`；现有 `HttpJsonResult`（同命名空间 `Hotfix.Framework.Web`，字段 `Code`/`Message`/`Data`，来自 `Result/HttpJsonResultHelper.cs`，无需新增 using）。
- Produces: 无新公共 API；新增私有静态 `DescribeJsonEnvelope(string responseText) -> string`、`PreviewBody(string body) -> string`。

- [ ] **Step 1: 补 usings**

`WebModule.API.cs` 顶部追加 `using System.Text;` 与 `using Newtonsoft.Json;`（按字母序插入现有 using 块）。

- [ ] **Step 2: Post<T> 在 protobuf 反序列化前增加信封判别**

插入位置：`webBufferResult.IsNull()` 检查之后、`SerializerHelper.Deserialize<MessageHttpObject>` 之前：

```csharp
			// 服务端协议层错误以 HTTP 200 + JSON 错误信封返回（Content-Type 含 json），
			// 不识别则 JSON 字节会被误当 protobuf 反序列化，抛出误导性的 wire-type 异常并掩盖服务端真实错误
			var contentType = webBufferResult.ContentType;
			if (contentType != null && contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				var responseText = Encoding.UTF8.GetString(webBufferResult.Result);
				throw new InvalidOperationException($"响应为 JSON 错误信封而非 protobuf: {url} ({DescribeJsonEnvelope(responseText)})");
			}
```

- [ ] **Step 3: protobuf 反序列化失败的 catch 附正文片段**

`Post<T>` 现有 catch（约 200-203 行）的异常消息追加 `正文片段`：

```csharp
			catch (Exception e)
			{
				// 附正文片段：垃圾响应（如误发的 JSON/HTML）一眼可辨，不再只报 wire-type
				throw new InvalidOperationException($"响应消息头反序列化失败: {url} ({e.Message}) 正文片段: {PreviewBody(Encoding.UTF8.GetString(webBufferResult.Result))}", e);
			}
```

- [ ] **Step 4: partial class 尾部（`#endregion` 之后、类结束大括号之前）新增两个 helper**

```csharp
		/// <summary>
		/// 描述 JSON 错误信封内容：解析成功返回 code 与 message 原文，解析失败返回非信封格式标注与正文预览。
		/// </summary>
		/// <param name="responseText">响应正文。</param>
		/// <returns>单行描述文本。</returns>
		private static string DescribeJsonEnvelope(string responseText)
		{
			if (string.IsNullOrWhiteSpace(responseText))
				return "响应体为空";

			try
			{
				var envelope = JsonConvert.DeserializeObject<HttpJsonResult>(responseText);
				return $"code={envelope.Code}, message={envelope.Message}";
			}
			catch
			{
				return $"非信封格式, 正文片段: {PreviewBody(responseText)}";
			}
		}

		/// <summary>
		/// 取正文前 100 字符的单行预览，用于内联到异常信息。
		/// </summary>
		/// <param name="body">响应正文。</param>
		/// <returns>单行预览文本，超长以省略号结尾。</returns>
		private static string PreviewBody(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return string.Empty;

			var singleLine = body.Replace("\r", "").Replace("\n", " ");
			return singleLine.Length <= 100 ? singleLine : singleLine.Substring(0, 100) + "…";
		}
```

- [ ] **Step 5: 同步 Post<T> XML remarks**

`Post<T>` 的 `<remarks>` 协议失败清单补入"响应为 JSON 错误信封"，置于清单首位：

```
		/// 协议失败（响应为 JSON 错误信封、响应消息头反序列化失败、消息为空或 Id 无效、响应类型与 T 不匹配、消息体反序列化失败）抛
```

- [ ] **Step 6: 编译验证**

```
unity-cli refresh_assets
unity-cli get_compilation_state
```

预期：编译成功，0 错误。

---

### Task 3: Play 模式实测与提交确认

**Files:** 无代码改动；验证 + 提交沟通。

**Interfaces:**
- Consumes: Task 2 的新异常路径；正在运行的本地服务端（`FuFramework.Launcher.exe`，28080，登录接口实测返回 `code=500`）。

- [ ] **Step 1: Play 模式实测登录**

进入 Play 模式点击登录按钮，检查 Console。预期输出（经 `FuiEventRegister` 统一记录）：

```
FUI异步事件回调异常: System.InvalidOperationException: 响应为 JSON 错误信封而非 protobuf: http://127.0.0.1:28080/game/api/req_user_login (code=500, message=Internal server error.)
```

不再出现 `Invalid wire-type` 字样。若异常消息显示"非信封格式 + 正文片段"，说明信封解析分支异常，回 Task 2 排查。

- [ ] **Step 2: 列清单询问用户提交**

改动文件清单：

- `Unity/Assets/Scripts/Hotfix/Framework/Web/Result/WebBufferResult.cs`
- `Unity/Assets/Scripts/Hotfix/Framework/Web/Data/WebPbData.cs`
- `Unity/Assets/Scripts/Hotfix/Framework/Web/WebModule.API.cs`
- `Docs/superpowers/specs/2026-11-26-web-pb-error-envelope-design.md`
- `Docs/superpowers/plans/2026-11-26-web-pb-error-envelope.md`

建议提交信息（经用户确认后执行，一次提交）：`[AI]fix: 修复 Web Pb 请求将服务端 JSON 错误信封误判为 protobuf 的问题`
