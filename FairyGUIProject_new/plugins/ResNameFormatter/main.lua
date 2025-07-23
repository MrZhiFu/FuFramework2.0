---@type CS.FairyEditor.App
App = App

Tool = require(PluginPath .. '/Tool')

---@type ResNameFormatter
local ResNameFormatter = require(PluginPath..'/ResNameFormatter')

---添加工具栏自定义检查菜单项
local toolMenu = App.menu:GetSubMenu("tool");
toolMenu:AddItem("自定义-资源名称格式化器", "自定义-资源名称格式化器", 0, false, function()
    Tool:Log("[ResNameFormatter]资源格式化开始......")
    ResNameFormatter:Init(PluginPath)
    ResNameFormatter:Run()
    Tool:Log("[ResNameFormatter]资源格式化完成......")
end)

-- 销毁菜单项
function onDestroy()
    toolMenu:RemoveItem("自定义-资源名称格式化器")
end