# Luban 配置表生成工具

本目录存放 **Luban**（配置表解决方案）的**源码、构建脚本与构建产物**。

- **来源**：`source/` 是上游 Luban 仓库检出（**MIT**，见 `source/LICENSE`、`source/README.md`）；本项目在其基础上按需改造（见 `source/src`）。
- **被谁使用**：`Config/gen-*.{bat,sh}` 调用 `bin/Luban.dll` 生成配置表代码与数据。**配置源与生成入口在 `Config/`**（详见 `Config/README.md`）。
- **不被 Unity 工程引用**：纯构建期工具。

---

## 目录

| 目录 / 文件 | 说明 |
|---|---|
| `source/` | Luban 上游源码：`src/`（各 .NET 工程）、`docs/`、`scripts/`、`Tools/`、`LICENSE`、`README*.md` |
| `bin/` | 由 `source/src` 构建出的可执行产物（`Luban.dll` 等）。**已入库**——不改源码时可直接使用，无需构建 |
| `build-luban.bat` / `.sh` | 从 `source/src` **重新构建**到 `bin/` |

## 构建（仅在改动 Luban 源码时需要）

```bat
:: Windows（双击或命令行）
Tools\Luban\build-luban.bat
```
```bash
bash Tools/Luban/build-luban.sh
```
> 构建会**先清空 `bin/` 再重建**。

## 使用

不改源码时**无需构建**，直接在 `Config/` 下跑生成脚本：

```bat
cd Config
gen-client-bin.bat     :: 客户端：二进制数据 + C# 代码
gen-client-json.bat    :: 客户端：JSON 变体
gen-server-bin.bat     :: 服务端
gen-server-json.bat    :: 服务端（JSON）
```

## 备注

- 上游仓库：<https://github.com/focus-creative-games/luban>（MIT License）。
- `source/docs/`、`source/scripts/`、`source/Tools/` 为上游自带内容，本项目未直接使用，保留以便对照上游用法与历史。
