# RedDotBind - 红点绑定编辑器插件

FairyGUI 编辑器的红点系统可视化绑定工具。无需手写 `customData`，在 Inspector 面板中一键创建红点组件、编辑红点 ID。

## 功能

| 操作 | 说明 |
|------|------|
| 创建红点 | 选中根组件，点击"创建红点"按钮，自动在右上角生成通用红点子节点 |
| 编辑红点 ID | 选中红点组件或其父组件，输入红点 ID，失焦自动保存到 `customData` |

## Inspector 面板

插件注册两个 Inspector，根据选中对象自动切换：

| Inspector | FGUI 组件 | 触发条件 | 功能 |
|-----------|-----------|----------|------|
| 红点设置 | `Create` | 空选 / 选中根组件 | 创建红点按钮 |
| 红点 ID | `SetId` | 选中红点组件 / 含红点子的父组件 | 红点 ID 输入框 |

## 数据存储

红点 ID 以 `red_dot:<id>` 格式存储在对象的 `customData` 字段中：

```
red_dot:Shop_Main|other_key:value
```

写入通过 `docElement:SetProperty()` 完成，支持编辑器撤销/重做（Ctrl+Z）。

## 文件结构

```
RedDotBind/
├── package.json       # 插件元信息
├── main.lua           # 插件主逻辑（Inspector 注册、红点创建/编辑）
├── common.lua         # 公共方法库（customData 读写）
├── icon.png           # 插件图标
├── RedDot_fui.bytes   # FGUI 编译包（Create / SetId 两个组件）
└── README.md          # 本文件
```

## 依赖

- FairyGUI 编辑器
- 通用红点组件资源（URL: `ui://ats3vms3ubwa2u`）

## 作者

Mrfu
