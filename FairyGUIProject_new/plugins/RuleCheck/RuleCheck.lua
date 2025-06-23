local RuleCheck = {}

--- 包 忽略列表
local dicIgnorePkg = {
    Sample = 1
}

local eData = {}
local isAutoSet = false

--- 全局格式化输出函数
fprintf = function(fmt, ...)
    local msg = string.format(fmt, ...)
    fprint(msg)
    
    return msg
end

local basePath
function RuleCheck:Check(pluginPath, autoSet)
    eData = {}

    basePath = pluginPath
    isAutoSet = autoSet
    
    self:CheckTextProperty()

    App.RefreshProject()
end

local function readXml(path)
    local f = io.open(path, "r")
    if f then
        local xml = f:read("*a")
        f:close()
        
        return xml
    end
end

local function writeText(path, content)
    local f = io.open(path, "w")
    if f then
        f:write(content)
        f:close()
    end
end

local function appendText(path, content)
    local f = io.open(path, "a+")
    if f then
        f:write(content)
        f:close()
    end
end

local function writeETime(path)
    local time = os.date("%Y-%m-%d %H:%M:%S", os.time())

    local msg = string.format("\n\n========== %s ==========\n", time)
    appendText(path, msg)
end

local function writeE()
    local path = basePath .. '/check_error.txt'

    writeETime(path)

    for key, data in pairs(eData) do
        if #data == 0 then
            return
        end
    
        table.insert(data, 1, string.format("\n[ %s ]:\n", key))
        local msg = table.concat(data)
    
        appendText(path, msg)
    end
end

local function logF(fmt, ...)
    local msg = string.format(fmt, ...) .. '\n'
    appendText(basePath .. '/check_log.txt', msg)
end

local function logE(title, fmt, ...)
    local data = eData[title]
    if not data then
        data = {}
        eData[title] = data
    end

    local msg = string.format(fmt, ...) .. '\n'
    table.insert(data, msg)
end

---@param xml CS.FairyGUI.Utils.XML
local function travelXml(xml, selector, callback)
    -- logF("name: %s", xml.name)
    if selector(xml) then
        callback(xml)
    end
    
    local elements = xml.elements
    -- logF("elements: %s", xml.elements)
    if elements then
        local rawList = elements.rawList
        local count = rawList.Count
        for i = 0, count - 1 do
            travelXml(rawList[i], selector, callback)
        end
    end
end

function RuleCheck:CheckTextProperty()
    local project = App.project

    -- logF('project[%s] opened: %s', project.name, project.opened)
    if not project.opened then
        return
    end
    
    local allPackages = project.allPackages
    local numPkg = allPackages.Count
    for i = 0, numPkg - 1 do
        ---@type CS.FairyEditor.FPackage
        local pkg = allPackages[i]

        if not dicIgnorePkg[pkg.name] then

            -- logF('pkg: %s', pkg.name)
            local items = pkg.items
            local numItem = items.Count
            for j = 0, numItem - 1 do
                ---@type CS.FairyEditor.FPackageItem
                local item = items[j]
                
                if item.type == 'component' then
                    -- fprintf(string.format("Comp:%s, Pkg:%s", item.fileName, pkg.name))

                    local basePath = pkg.basePath
                    basePath = basePath:gsub('\\', '/'):gsub('////', '/'):gsub('///', '/'):gsub('//', '/')
                    local path = string.format('%s%s%s', basePath, item.path, item.fileName)

                    -- logF('deal item: %s %s', item.name, path)
                    local xmlStr = readXml(path)

                    if xmlStr then
                        local needSave = false
                        
                        ---@type CS.FairyGUI.Utils.XML
                        local xml = CS.FairyGUI.Utils.XML(xmlStr)
                        travelXml(xml, function (arg)
                            ---@type CS.FairyGUI.Utils.XML
                            local node = arg
                            
                            if node.name == 'text' or node.name == 'richtext' then
                                return true
                            end
                        end, function(arg)
                            ---@type CS.FairyGUI.Utils.XML
                            local node = arg
                            
                            if not node:HasAttribute('autoClearText') or
                                    not node:GetAttributeBool('autoClearText') then
                                local rPath = string.format("%s%s%s", pkg.name, item.path, item.fileName:gsub(".xml", ""))
                                logE("text not set autoClearText", "path: %s\tname: %s", rPath, node:GetAttribute('name'))

                                if isAutoSet then
                                    node:SetAttribute('autoClearText', 'true')

                                    needSave = true
                                end
                            end

                            if node:HasAttribute('font') and node:GetAttribute('font') == 'Microsoft YaHei' then
                                node:RemoveAttribute('font')

                                needSave = true
                            end
                        end)

                        if needSave then
                            writeText(path, xml:ToXMLString(true))
                        end
                    else
                        logE("not find item", '%s %s', item.name, path)
                    end
                    -- return
                end
            end

            collectgarbage('collect')
        end
    end

    writeE()
end

return RuleCheck