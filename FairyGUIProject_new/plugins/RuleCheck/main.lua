---@type CS.FairyEditor.App
App = App

--- 全局格式化输出函数
fprintf = function(fmt, ...)
    fprint(string.format(fmt, ...))
end

local RuleCheck = require(PluginPath..'/RuleCheck')

local function doRuleCheck(autoSet)
    fprint("Rule Check .. start!")
    RuleCheck:Check(PluginPath, autoSet)
    fprint("Rule Check .. finish!")
end

-------Add custom menu-------
local toolMenu = App.menu:GetSubMenu("tool");
toolMenu:AddItem("RuleCheck", "RuleCheck", 0, true, nil)
local tMenu = toolMenu:GetSubMenu("RuleCheck")
tMenu:AddItem("OnlyCheck", "OnlyCheck", 0, false, function(menuItem)
    doRuleCheck(false)
end)
tMenu:AddItem("AutoSet", "AutoSet", 1, false, function(menuItem)
    doRuleCheck(true)
end)

-------do cleanup here-------

function onDestroy()
    toolMenu:RemoveItem("RuleCheck")
end