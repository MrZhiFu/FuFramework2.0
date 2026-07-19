# xlsx → CSV 独立转换工具 设计文档

日期: 2025-07-20

## 目标

提供一个独立于 Luban 管线的纯 xlsx → CSV 转换工具。不依赖 Luban schema/类型系统，直接将 Excel 的每个 Sheet 导出为纯 CSV 文件。

## 技术选型

- **语言**: Python 3
- **依赖**: `openpyxl`（读写 xlsx）、`csv`（标准库）
- **入口**: `Config/xlsx2csv.bat` → `Config/Tools/xlsx2csv.py`

## 输入 → 输出

```
Config/Excels/
├── Tables/                        →  Config/Excels/CSV/Tables/
│   ├── C-Achievement-成就表.xlsx  →    ├── C-Achievement-成就表_Sheet1.csv
│   │   ├── Sheet1                 →    ├── C-Achievement-成就表_Sheet2.csv
│   │   └── Sheet2                 →    └── ...
│   └── D-Item-道具表.xlsx         →    └── D-Item-道具表_Sheet1.csv
└── Local/
    └── L-Localization-成就.xlsx    →  Config/Excels/CSV/Local/
                                         └── L-Localization-成就_Sheet1.csv
```

### 规则

- 扫描 `Excels/` 下所有子目录，跳过输出目录自身
- 每个 Sheet → 一个 CSV 文件，命名：`{xlsx文件名去扩展名}_{Sheet名}.csv`
- 第一行为列头
- 跳过完全为空的数据行

## CSV 格式

- **编码**: UTF-8 with BOM（Windows Excel 双击不乱码）
- **分隔符**: 逗号 `,`
- **引号规则**: 字段含逗号/换行时自动加双引号（Python csv 模块默认行为）
- **换行**: `\r\n`（Windows 兼容）

## 命令行接口

```bash
# 基本用法（输出到 Excels/CSV/）
python Tools/xlsx2csv.py

# 指定输入/输出目录
python Tools/xlsx2csv.py --input ./Excels --output ./CSV_Output

# 只处理指定子目录
python Tools/xlsx2csv.py --subdirs Tables,Local
```

## 文件结构

```
Config/
├── xlsx2csv.bat              # Windows 双击入口
└── Tools/
    └── xlsx2csv.py           # 核心脚本
```

## 错误处理

- 缺少 openpyxl 时给出 `pip install openpyxl` 提示
- 单个文件失败不中断整个批处理，打印警告并继续
- 处理完毕打印统计：成功/失败文件数
