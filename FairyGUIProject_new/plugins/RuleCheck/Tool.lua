---@class Tool 工具方法相关定义
---@field private pluginPath
local Tool = {}

--- 日志输出
function Tool:Log(fmt, ...)
    fmt = tostring(fmt)
    fprint(string.format(fmt, ...))
end

--- 警告日志输出
function Tool:Warning(fmt, ...)
    fmt = tostring(fmt)
    App.consoleView:LogWarning(string.format(fmt, ...))
end

--- 错误日志输出
function Tool:Error(fmt, ...)
    fmt = tostring(fmt)
    App.consoleView:LogError(string.format(fmt, ...))
end

return Tool