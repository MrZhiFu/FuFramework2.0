--- 生成代码前的准备工作
---@class GenReady
local GenReady = {}

--- 初始化
--- 1.加载Tool工具对象，并为Tool指定FGUI发布处理器对象与该插件路径
--- 2.加载GenCommon生成时的通用功能对象
---@param handler CS.FairyEditor.PublishHandler FGUI发布处理器对象
---@param pluginPath string 该插件路径
function GenReady:Init(handler, pluginPath)
    ---@type Tool
    Tool = require(pluginPath .. '/Src/Tool')
    Tool:Log("导出插件初始化...")
    Tool:SetHandler(handler)
    Tool:SetPluginPath(pluginPath)

    ---@type GenCommon
    GenCommon = require(pluginPath .. '/Src/GenCommon')
end

--- 导出路径是否有效
---@return boolean
function GenReady:IsExportPathOK()
    local handler = Tool:Handler()
    local exportPath = handler.exportPath
    exportPath = exportPath:gsub('\\', '/')

    local _, idx = Tool:StrFind(exportPath, "Assets")
    if not idx then
        Tool:Log('路径不在Assets目录下')
        return false
    end

    return true
end

--- 获得Unity工程目录 “xxx/Assets”
---@param handler CS.FairyEditor.PublishHandler FUI发布处理器对象
---@return string
function GenReady:GetUnityDataPath(handler)
    local exportCodePath = handler.exportCodePath
    exportCodePath = exportCodePath:gsub('\\', '/')

    local _, idx = Tool:StrFind(exportCodePath, "Assets")
    if not idx then
        Tool:Log('路径不在Assets目录下')
        return
    end

    local unityDataPath = Tool:StrSub(exportCodePath, 1, idx)
    unityDataPath = unityDataPath:gsub('\\', '/')
    Tool:Log('Unity数据路径: %s', unityDataPath)

    return unityDataPath
end

--- 获得所有界面数组和组件数组
---@param handler CS.FairyEditor.PublishHandler FUI发布处理器对象
---@return CS.FairyEditor.PublishHandler.ClassInfo[], CS.FairyEditor.PublishHandler.ClassInfo[], table<string, CS.FairyEditor.PublishHandler.ClassInfo>  界面数组, 组件数组, 所有类型的Map-包括所有界面与组件--key-资源名称，value-资源对应的界面或组件
function GenReady:GetClsArray(handler)
    local winClsArray = {}
    local compClsArray = {}
    local allClsMap = {}

    ---@type CS.FairyEditor.GlobalPublishSettings.CodeGenerationConfig
    local settings = handler.project:GetSettings("Publish").codeGeneration
    settings.ignoreNoname = false

    --- 获得所有导出的界面和有引用的组件
    ---@type CS.FairyEditor.PublishHandler.ClassInfo[]
    local classes = handler:CollectClasses(settings.ignoreNoname, settings.ignoreNoname, nil)
    for i = 1, classes.Count do
        local clsInfo = classes[i - 1]

        --检查资源依赖是否正确
        local depIsRight = self:CheckDependency(clsInfo, handler)
        if not depIsRight then
            return nil, nil, nil
        end

        allClsMap[clsInfo.resName] = clsInfo
        if Tool:StrFind(clsInfo.resName, 'Win') == 1 then
            table.insert(winClsArray, clsInfo)
        elseif Tool:StrFind(clsInfo.resName, 'Comp') == 1 then
            if Tool:StrFind(clsInfo.res.path, "Comps") ~= nil then
                table.insert(compClsArray, clsInfo)
            end
        end
    end

    return winClsArray, compClsArray, allClsMap
end

--- 检查资源依赖是否正确,即资源依赖的包只能是当前发布的或以Common开头的包
---@param itemClsInfo CS.FairyEditor.PublishHandler.ClassInfo 资源项
---@param handler CS.FairyEditor.PublishHandler FUI发布处理器对象
---@return boolean 依赖是否正确
function GenReady:CheckDependency(itemClsInfo, handler)
    for _, member in pairs(itemClsInfo.members) do
        if Tool:IsExportedComp(member) then
            local pkgName = ""
            if member.res then
                pkgName = member.res.owner.name
            elseif member.type == "GList" then
                --- 获得列表引用的组件
                local defaultItem = Tool:GetListRefRes(itemClsInfo, member)
                if defaultItem then
                    pkgName = defaultItem.owner.name
                end
            end

            if pkgName ~= handler.pkg.name and Tool:StrFind(pkgName, 'Common') == nil then
                Tool:Error('资源依赖错误：%s，依赖的包只能是当前发布的或以Common开头的包，请将资源拖入正确的包中', itemClsInfo.name)
                return false
            end
        end
    end

    return true
end

return GenReady