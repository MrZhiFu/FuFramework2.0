# Web Pb 请求错误响应识别 — 短设计

日期：2026-11-26
状态：已批准

## 意图与原因

服务端对协议层错误一律返回 `HTTP 200 + application/json` 错误信封（`{"code":...,"message":...,"data":null}`，已实测确认响应头为 `application/json; charset=utf-8`）。客户端 Pb 链路把 200 一律当 protobuf 成功处理，将 JSON 字节喂给 protobuf 反序列化，抛出误导性的 `Invalid wire-type` 异常，掩盖了服务端真实错误（如登录接口实际返回 `code=500 Internal server error`）。

修复方式：客户端识别 JSON 错误信封并抛出携带 `code/message` 原文的清晰异常。服务端不改。

## 目标 / 非目标

- **目标**：`WebModule.Post<T>` 收到 JSON 错误响应时，抛出含 URL、code、message 的 `InvalidOperationException`，与现有异常契约一致。
- **非目标**：不改服务端错误响应形态（方向 B 被否，改动小者优先）；不动 JSON 请求流（`PostToString` 等本来就把 200+JSON 当正常数据）。
- **反目标**：不引入新的 JSON 解析类型——复用现有 `HttpJsonResult` DTO（`Result/HttpJsonResultHelper.cs:40`），字段与服务端信封完全对齐。

## 方案取舍

- **方案 A（采纳）**：客户端响应 Content-Type 判别 + 现有 DTO 解析 + 抛清晰异常。只动客户端一层，顺带透出服务端真实错误。
- **方案 B（否决）**：服务端协议层错误改返回非 2xx，走客户端现有失败分支。改动面涉及服务端协议契约，风险大收益低；且已部署的响应形态短期难统一。

## 改动点（3 个文件，约 30 行）

1. `Result/WebBufferResult.cs` — 加 `ContentType` 属性，构造函数加可选参数（默认 `null`，不影响另一调用方 `WebJsonBytesData`）。
2. `Data/WebPbData.cs` — `Complete` 中取 `request.GetResponseHeader("Content-Type")` 随结果上抛。
3. `WebModule.API.cs` — `Post<T>` 反序列化 protobuf 前：Content-Type 含 `"json"` 则 UTF-8 解码 → `HttpJsonResult` 解析 → 抛 `InvalidOperationException`（消息带 `url + code + message`）；JSON 解析失败则附响应正文前 100 字符。
4. 顺手项：现有 protobuf 反序列化失败 catch 的报错信息附响应正文片段，便于下次直读垃圾响应内容。

## 留给执行者的判定

- Content-Type 判别用"包含 `json`"的小写不敏感匹配（服务端实测值为 `application/json; charset=utf-8`）。
- 异常类型沿用 `Post<T>` 现有契约的 `InvalidOperationException`。

## 已知后续（不在本次范围）

修复后登录接口会显式暴露服务端 `code=500`，那是服务端 handler 侧的另一问题，是否追查届时再定。
