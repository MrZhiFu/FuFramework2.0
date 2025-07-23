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

--- 获得所有待导出的界面数组和组件数组
---@param handler CS.FairyEditor.PublishHandler FUI发布处理器对象
---@return CS.FairyEditor.PublishHandler.ClassInfo[], CS.FairyEditor.PublishHandler.ClassInfo[], table<string, CS.FairyEditor.PublishHandler.ClassInfo>  界面数组, 组件数组, 所有类型的Map-包括所有界面与组件--key-资源名称，value-资源对应的界面或组件
function GenReady:GetClsArray(handler)
    local winClsArray = {}
    local compClsArray = {}
    local allClsMap = {}

    ---@type CS.FairyEditor.GlobalPublishSettings.CodeGenerationConfig
    local settings = handler.project:GetSettings("Publish").codeGeneration
    settings.ignoreNoname = false

    --- 获得当前包的所有待导出对象
    ---@type CS.FairyEditor.PublishHandler.ClassInfo[]
    local classes = handler:CollectClasses(settings.ignoreNoname, settings.ignoreNoname, nil)
    for i = 1, classes.Count do
        local clsInfo = classes[i - 1]
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

--- 检查依赖组件是否在任意Common包或当前正在发布的包中
---@param handler CS.FairyEditor.PublishHandler
---@return boolean 所有依赖合法返回true，否则返回false
function GenReady:CheckDependencies(handler)
    local project = handler.project
    local currentPkg = handler.pkg  -- 当前正在发布的包

    ---@type CS.FairyEditor.PublishHandler.ClassInfo[]
    local classes = handler:CollectClasses()  -- 所有待导出的组件

    if not classes then
        return true  -- 无组件需要检查，默认合法
    end

    -- 收集所有名称包含 "Common" 的包（不区分大小写）
    local commonPackages = {}
    for _, pkg in pairs(project.allPackages) do
        local pkgName = string.lower(pkg.name)
        local isCommon = Tool:StrFind(pkgName, "common") ~= nil
        if isCommon then
            table.insert(commonPackages, pkg)
        end
    end

    if #commonPackages == 0 then
        Tool:Log("[GenReady] 未找到任何包含 'Common' 的包，跳过依赖检查")
        return true  -- 无Common包，默认合法（或根据需求返回false）
    end

    local allDependenciesValid = true  -- 初始假设全部合法

    -- 遍历检查包内的所有组件
    for _, classInfo in pairs(classes) do
        local compName = classInfo.className
        
        -- 遍历检查直接属于当前组件的子组件的依赖项是否合法
        ---@type CS.FairyEditor.PublishHandler.MemberInfo[]
        local members = classInfo.members  -- 该组件的所有依赖成员
        for _, memberInfo in pairs(members) do
            -- 跳过非资源类型成员
            if memberInfo.res == nil then
                goto continue
            end

            local memberName = memberInfo.name
            local memberType = memberInfo.type
            local memberURL = memberInfo.res:GetURL()

            --- 获取该依赖项的包信息
            ---@type CS.FairyEditor.FPackageItem
            local memberItem = project:GetItemByURL(memberURL)
            if not memberItem then
                Tool:Error("[GenReady] 依赖项未找到: %s (组件: %s, 类型: %s)", memberName, compName, memberType)
                allDependenciesValid = false
                goto continue
            end

            local memberPkg = memberItem.owner  -- 依赖项所在的包
            local isCurPkg = (memberPkg == currentPkg)

            -- 检查是否在任意 Common 包中
            if not isCurPkg then
                for _, commonPkg in ipairs(commonPackages) do
                    if memberPkg == commonPkg then
                        isCurPkg = true
                        break
                    end
                end
            end

            -- 如果既不在 Common 包，也不在当前包，则标记为非法
            if not isCurPkg then
                Tool:Error("[GenReady] %s中存在非法依赖：组件: %s, 类型: %s, 所在包: %s",compName, memberName, memberType, memberPkg.name)
                allDependenciesValid = false
            end

            :: continue ::
        end
    end

    return allDependenciesValid
end

return GenReady