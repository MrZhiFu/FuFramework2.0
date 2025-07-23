---@type CS.FairyEditor.App
App = App

Tool = require(PluginPath .. '/Tool')

---@type RuleCheck
local RuleCheck = require(PluginPath..'/RuleCheck')

--- 执行规则检查
---@param isAutoSetClearAttr boolean 是否自动设置"自动清除文本"属性
local function doRuleCheck(isAutoSetClearAttr)
    Tool:Log("[RuleCheck]规范检查器检查开始......")
    RuleCheck:Init(PluginPath, isAutoSetClearAttr)
    RuleCheck:Check()
    Tool:Log("[RuleCheck]规范检查器检查完成......")
end

---添加工具栏自定义检查菜单项
local toolMenu = App.menu:GetSubMenu("tool");
toolMenu:AddItem("自定义-组件属性规范检查器", "自定义-组件属性规范检查器", 0, true, nil)

local tMenu = toolMenu:GetSubMenu("自定义-组件属性规范检查器")

---添加自定义检查菜单项子项-检查文本属性
tMenu:AddItem("检查文本属性", "检查文本属性", 0, false, function(_)
    doRuleCheck(false)
end)

-- 添加自定义检查菜单项子项-检查并自动设置"自动清除“和字体文本属性
tMenu:AddItem("检查并自动设置文本属性", "检查并自动设置文本属性", 1, false, function(_)
    doRuleCheck(true)
end)

-- 销毁菜单项
function onDestroy()
    toolMenu:RemoveItem("自定义-组件属性规范检查器")
end