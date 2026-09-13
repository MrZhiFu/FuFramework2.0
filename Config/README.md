# Config 配置表

本目录是**项目配置表的源数据与生成入口**（基于 [Luban](https://github.com/focus-creative-games/luban)，MIT 许可）。

> **Luban 工具本体**（源码 / 构建脚本 / 可执行产物）已移至 **`Tools/Luban/`** —— 详见 `Tools/Luban/README.md`。

---

## 目录结构

| 项 | 说明 |
|---|---|
| `Defines/` | 表结构定义（schema，XML） |
| `Excels/` | 配置源数据（`.xlsx`） |
| `luban.conf` | Luban 工程配置：表定义、数据目录、导出目标（`client` / `server`） |
| `gen-*.bat` / `gen-*.sh` | 生成脚本（见下） |
| `配置表定义相关说明/` | 表定义相关的图文说明 |

## 生成脚本

**必须在 `Config/` 目录下运行**（脚本以相对路径引用 `./Luban.conf` 与 `../Tools/Luban/bin/Luban.dll`）。

| 脚本 | 目标 | 数据产出 | 代码产出 |
|---|---|---|---|
| `gen-client-bin.bat` / `.sh` | 客户端 | `Unity/Assets/Bundles/Config` | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/{Generate,LanguageKey}` |
| `gen-client-json.bat` / `.sh` | 客户端 | 同上（JSON 变体） | 同上 |
| `gen-server-bin.bat` / `.sh` | 服务端 | `Server/FuFramework.Config/Json` | `Server/FuFramework.Config/Config` |
| `gen-server-json.bat` / `.sh` | 服务端 | 同上（JSON 变体） | 同上 |

**前置**：一般**无需构建**（`Tools/Luban/bin/Luban.dll` 已入库）；仅当改动过 Luban 源码时，先跑 `Tools/Luban/build-luban.bat` 重新构建。

## 工作流（改表）

1. 编辑 `Excels/*.xlsx`（如涉及结构变更，同时改 `Defines/`）；
2. 在 `Config/` 下运行对应的 `gen-*.bat`；
3. 回 Unity 触发重新编译；
4. 提交生成的代码与数据。

---

## 修改和增加

### 增加本地化的文件夹配置支持

在项目中。经常会出现语言表在协作的时候有冲突。这个时候就需要按照每个模块来做表格文件本身的分离，已经不是Sheet的分离的问题了。所以将本地化文件夹配置支持文件夹下的所有文件都识别为本地化文件。

#### 示例配置

- l10n.provider=`fuframework`

这里必须为 `fuframework` 否则会导致本地化文件识别失败。

- l10n.textFile.path=`./Excels/Local/`

这里值必须为 文件夹路径 否则会导致本地化文件识别失败。

导出参数参考

```
--xargs l10n.provider=fuframework --xargs l10n.textFile.keyFieldName=key  --xargs l10n.textFile.path=./Excels/Local/
```

### 增加自动导表的文件名称扩展识别

#### 导出参数(必须配置)

```
--xargs tableImporter.name=fuframework
```

#### 说明

格式 [任意字母]-[导出的表名称]-[导出的组名]-[表名称注释].xlsx

表格以任意字母-开头。

中间部分的表名称为英文且不能有空格可以有下划线

导出的组名称必须是定义的`s`、 `c` 之一，可选

后面表名称注释可以接任意长度。程序只取第一个`-` 和第二个`-` 之间的内容加上 `Tb` 为最终表名称。

#### 示例

##### 导出的表名称

L-Localization.xlsx => `Tb`Localization

C-Achievement-成就表.xlsx => `Tb`Achievement

C-Achievement-成就表-AAA.xlsx => `Tb`Achievement

C-Achievement-成就表-AAA-BBB.xlsx => `Tb`Achievement

C-Achievement-成就表-AAA-BBB-CCC.xlsx => `Tb`Achievement

##### 导出的组表名称

C-Achievement-s-成就表.xlsx => `Tb`Achievement, 当前导出目标为 `s` 时才会导出

C-Achievement-c-成就表-AAA.xlsx => `Tb`Achievement, 当前导出目标为 `c` 时才会导出

C-Achievement-s-成就表-AAA-BBB.xlsx => `Tb`Achievement, 当前导出目标为 `s` 时才会导出

C-Achievement-c-成就表-AAA-BBB-CCC.xlsx => `Tb`Achievement, 当前导出目标为 `c` 时才会导出

## 上游参考

- Luban 官方文档：<https://luban.doc.code-philosophy.com/>
- 上游源码（本项目检出）：`Tools/Luban/source/`（含其 `README.md`、`LICENSE`）
- 示例项目：<https://github.com/focus-creative-games/luban_examples>

## License

Luban 采用 [MIT](https://github.com/focus-creative-games/luban/blob/main/LICENSE) 许可。
