---@class RuleCheck
---组件属性规范检查器
---
---使用方法：打开编辑器，在菜单栏中点击“工具”->“自定义-组件属性规范检查器”；
---
---1.检查文本属性：是否设置了"自动清除文本"属性，没有则记录错误。同时看是否需要自动设置，是则设置自动清除文本属性，并保存
---
---2.检查文本属性：是否设置了"Microsoft YaHei"字体，是则移除字体属性，并保存。(主要是为了在Unity ConfigSetting中统一设置字体，
---  所以这里有个软规范，如果需要统一使用Unity ConfigSetting中的字体，需要在FGUI编辑器中设置为"Microsoft YaHei"字体)
---
---3.记录错误日志到文件
---
---4.目前只检查了文本组件的相关属性，后续可添加更多自定义检查项
local RuleCheck = {}

local dicIgnorePkg = { Sample = 1 } --- 忽略检查的包
local errorData = {} --- 错误数据
local isAutoSetClearAttr = false --- 是否自动设置自动清除文本属性
local pluginPath --- 插件路径

---初始化
---@param pluginPath_ string 插件路径
---@param isAutoSetClearAttr_ boolean 是否自动设置自动清除文本属性
function RuleCheck:Init(pluginPath_, isAutoSetClearAttr_)
    Tool = require(pluginPath_ .. '/Tool')
    errorData = {}
    pluginPath = pluginPath_
    isAutoSetClearAttr = isAutoSetClearAttr_
end

---执行检查
function RuleCheck:Check()
    self:CheckTextProperty() -- 检查文本属性
    App.RefreshProject() -- 刷新项目
end

---读取文件XML内容
---@param path string 文件路径
---@return string 文件内容
local function readXml(path)
    local f = io.open(path, "r")
    if f then
        local xml = f:read("*a")
        f:close()
        return xml
    end
end

---写入文件内容
---@param path string 文件路径
---@param content string 文件内容
local function writeText(path, content)
    local f = io.open(path, "w")
    if f then
        f:write(content)
        f:close()
    end
end

---追加内容到文件尾
---@param path string 文件路径
---@param content string 文件内容
local function appendText(path, content)
    local f = io.open(path, "a+")
    if f then
        f:write(content)
        f:close()
    end
end

---追加发生错误的时间信息到文件尾
---@param path string 文件路径
local function writeErrorTime(path)
    local time = os.date("%Y-%m-%d %H:%M:%S", os.time())
    local msg = string.format("\n\n========== %s ==========\n", time)
    appendText(path, msg)
end

---写入错误日志到文件
local function writeToErrorLog()
    local path = pluginPath .. '/check_error.txt'
    Tool:Log("写入错误日志到文件: %s", path)
    writeErrorTime(path)

    for key, data in pairs(errorData) do
        if #data == 0 then
            return
        end

        table.insert(data, 1, string.format("\n[ %s ]:\n", key))
        local msg = table.concat(data)
        appendText(path, msg)
    end
end

---记录到日志
---@param fmt string 格式化字符串
---@param ... any 格式化参数
local function logToLogTxt(fmt, ...)
    local msg = string.format(fmt, ...) .. '\n'
    appendText(pluginPath .. '/check_log.txt', msg)
end

---记录错误信息到errorData
---@param title string 错误标题
---@param fmt string 格式化字符串
---@param ... any 格式化参数
local function logToErrorData(title, fmt, ...)
    local data = errorData[title]
    if not data then
        data = {}
        errorData[title] = data
    end

    local msg = string.format(fmt, ...) .. '\n'
    table.insert(data, msg)
end

---遍历XML节点，并执行选中节点的回调函数
---@param xml CS.FairyGUI.Utils.XML
---@param selector function(xml:CS.FairyGUI.Utils.XML):boolean 选择器
---@param callback function(xml:CS.FairyGUI.Utils.XML) 回调函数
local function travelXml(xml, selector, callback)
    -- logToLogTxt("name: %s", xml.name)
    if selector(xml) then
        callback(xml)
    end

    local elements = xml.elements
    -- logToLogTxt("elements: %s", xml.elements)
    if elements then
        local rawList = elements.rawList
        local count = rawList.Count
        for i = 0, count - 1 do
            travelXml(rawList[i], selector, callback)
        end
    end
end

---检查文本属性
---1. 是否设置了"自动清除文本"属性，没有则记录错误。同时看是否需要自动设置，是则设置自动清除文本属性，并保存
---2. 是否设置了"Microsoft YaHei"字体，没有则移除字体属性，并保存
function RuleCheck:CheckTextProperty()
    local project = App.project

    -- logToLogTxt('project[%s] opened: %s', project.name, project.opened)
    if not project.opened then
        return
    end

    local allPackages = project.allPackages
    local errorCnt = 0 -- 错误计数

    ---遍历所有包
    for i = 0, allPackages.Count - 1 do
        ---@type CS.FairyEditor.FPackage
        local pkg = allPackages[i]

        ---是否是忽略检查的包，否则检查
        if not dicIgnorePkg[pkg.name] then

            -- logToLogTxt('pkg: %s', pkg.name)
            local items = pkg.items

            ---遍历包下的所有Item
            for j = 0, items.Count - 1 do
                ---@type CS.FairyEditor.FPackageItem
                local item = items[j]

                ---是否是组件，是则检查文本属性
                if item.type == 'component' then
                    local basePath = pkg.basePath
                    basePath = basePath:gsub('\\', '/'):gsub('////', '/'):gsub('///', '/'):gsub('//', '/')
                    local path = string.format('%s%s%s', basePath, item.path, item.fileName)

                    -- logToLogTxt('deal item: %s %s', item.name, path)
                    local xmlStr = readXml(path)

                    if xmlStr then
                        local needSave = false

                        ---@type CS.FairyGUI.Utils.XML
                        local xml = CS.FairyGUI.Utils.XML(xmlStr)

                        ---遍历XML节点，并执行选中节点的回调函数，这里检查文本/富文本节点
                        travelXml(xml, function(xmlNode)
                            ---@type CS.FairyGUI.Utils.XML
                            local node = xmlNode
                            if node.name == 'text' or node.name == 'richtext' then
                                return true
                            end
                        end, function(xmlNode)
                            ---@type CS.FairyGUI.Utils.XML
                            local txtNode = xmlNode

                            local rPath = string.format("%s%s%s", pkg.name, item.path, item.fileName:gsub(".xml", ""))
                            local attrName = string.format("path: %s\tname: %s", rPath, txtNode:GetAttribute('name'))

                            ---是否设置了"自动清除文本"属性，没有则记录错误
                            if not txtNode:HasAttribute('autoClearText') or not txtNode:GetAttributeBool('autoClearText') then
                                errorCnt = errorCnt + 1
                                local errorTitle = "文本没有设置自动清除文本属性：%s"
                                logToErrorData(errorTitle, attrName) -- 记录错误信息到errorData

                                -- 是否自动设置“自动清除文本”属性，是则设置，并将是否需要保存设置true
                                if isAutoSetClearAttr then
                                    txtNode:SetAttribute('autoClearText', 'true')
                                    needSave = true
                                    errorTitle = errorTitle ..", 并自动设置了“自动清除文本”属性"
                                end
                                
                                local errorMsg = string.format(errorTitle, attrName)
                                Tool:Warning("Error---:%s", errorMsg)
                            end

                            ---是否设置了"Microsoft YaHei"字体，是则移除字体属性，并将是否需要保存设置true
                            if txtNode:HasAttribute('font') and txtNode:GetAttribute('font') == 'Microsoft YaHei' then
                                errorCnt = errorCnt + 1
                                local errorTitle = "文本设置了Microsoft YaHei字体：%s"
                                logToErrorData(errorTitle, attrName) -- 记录错误信息到errorData

                                -- 是否自动设置“自动清除文本”属性，是则设置，并将是否需要保存设置true
                                if isAutoSetClearAttr then
                                    txtNode:RemoveAttribute('font')
                                    errorTitle = errorTitle ..", 并自动清除了字体"
                                    needSave = true
                                end
                                
                                local errorMsg = string.format(errorTitle, attrName)
                                Tool:Warning("Error---:%s", errorMsg)
                            end
                        end)

                        -- 如果更改需要保存，则将更改后的XML保存替换原XMl
                        if needSave and errorCnt > 0 then
                            writeText(path, xml:ToXMLString(true))
                        end
                    else
                        logToErrorData("没有找到Item:", '%s %s', item.name, path)
                    end
                end
            end

            collectgarbage('collect')
        end
    end

    -- 写入错误日志到文件
    if errorCnt > 0 then
        writeToErrorLog()
    else
        Tool:Log("没有检查到错误规范的组件属性")
    end
end

return RuleCheck