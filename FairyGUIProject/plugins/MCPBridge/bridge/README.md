# 通信目录说明

此目录用于 MCP 服务和 FairyGUI 插件之间的通信。

## 子目录

- `commands/` - MCP 服务写入的命令文件
- `results/` - FairyGUI 插件写入的结果文件
- `screenshots/` - 截图输出文件

## 文件格式

### 命令文件 (commands/*.json)

```json
{
  "id": "cmd_xxx",
  "action": "open_component",
  "params": {
    "package_name": "Common",
    "component_name": "Button01"
  }
}
```

### 结果文件 (results/*.json)

```json
{
  "id": "cmd_xxx",
  "status": "success",
  "data": { ... }
}
```