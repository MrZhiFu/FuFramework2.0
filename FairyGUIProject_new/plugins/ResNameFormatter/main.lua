---@type CS.FairyEditor.App
App = App

--- 全局格式化输出函数
fprintf = function(fmt, ...)
    fprint(string.format(fmt, ...))
end

local ResNameFormatter = require(PluginPath..'/ResNameFormatter')

-------Add custom menu-------
local toolMenu = App.menu:GetSubMenu("tool");
toolMenu:AddItem("FormatResName", "ResNameFormatter", 0, false, function()
    ResNameFormatter:Run()
end)

-------do cleanup here-------

function onDestroy()
    toolMenu:RemoveItem("ResNameFormatter")
end