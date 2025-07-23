---@class Tool 工具方法相关定义
---@field private pluginPath
local Tool = {}

--- 日志输出
function Tool:Log(fmt, ...)
    fmt = tostring(fmt)
    fprint(string.format(fmt, ...))
end

return Tool