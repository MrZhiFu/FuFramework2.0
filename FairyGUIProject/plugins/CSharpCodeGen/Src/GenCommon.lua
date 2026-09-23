--- 生成C#代码时的通用功能对象
---@class GenCommon
local GenCommon = {}

--- 组件类型 → 交互事件配置映射（cbNamePattern 中的 %s 会被组件功能名替换）
--- 事件能力参考 FairyGUI 运行时源码：所有 GObject 均有 onClick 等基类事件，此处白名单只收录
--- "该类型实例大概率需要接线" 的默认事件，未收录的类型需要时手动订阅或在此加行
local COMP_EVENT_CONFIG = {
    GSlider = {
        { eventName = "onChanged", cbNamePattern = "On%sChanged" },
    },
    GComboBox = {
        { eventName = "onChanged", cbNamePattern = "On%sChanged" },
    },
    GTextInput = {
        { eventName = "onChanged", cbNamePattern = "On%sChanged" },
        { eventName = "onFocusOut", cbNamePattern = "On%sFocusOut" },
        { eventName = "onSubmit", cbNamePattern = "On%sSubmit" },
    },
    GButton = {
        { eventName = "onClick", cbNamePattern = "On%sClick" },
    },
    GGraph = {
        { eventName = "onClick", cbNamePattern = "On%sClick" },
    },
    GRichTextField = {
        { eventName = "onClick", cbNamePattern = "On%sClick" },
        { eventName = "onClickLink", cbNamePattern = "On%sClickLink", defaultContent = "\t\t\t// ctx.data 为超链接的 href 字符串\n" },
    },
}

--- 交互事件回调的默认参数列表（当前所有事件类型一致）
local DEFAULT_EVENT_ARGS = { { argName = "ctx", argType = "EventContext" } }

--- 生成组件的定义代码：private GButton btnEnter;
---@param dataList table 待填充的代码行数组
---@param compArray table 组件信息数组，元素格式 {comp, resName, resPkg, funName}
---@param AllClsMap table 所有类名映射表（资源名→类信息）
function GenCommon:GenCompDefine(dataList, compArray, AllClsMap)
    if #compArray <= 0 then
        return
    end

    Tool:Log("生成组件的定义代码")
    for _, comp in ipairs(compArray) do
        if Tool:StartWith(comp.comp.name, "_") then
            local comType = Tool:GetCompType(comp.comp, AllClsMap)
            local paramName = Tool:FormatVarName(comp.comp.name)
            local comDef = string.format("\t\tprivate %s %s;\n", comType, paramName)
            table.insert(dataList, comDef)
        end
    end
end

--- 生成自定义组件的URL代码：public const string URL = "ui://mkasn9e4jo110";
---@param dataList table 待填充的代码行数组
---@param compCls CS.FairyEditor.PublishHandler.ClassInfo 组件类信息
function GenCommon:GenCompURL(dataList, compCls)
    Tool:Log("生成组件的URL的C#代码")
    local url = string.format("\t\tpublic const string URL = \"ui://%s%s\";\n\n", compCls.res.owner.id, compCls.resId)
    table.insert(dataList, url)
end

--- 生成动效的定义代码：private Transition xxxAnim;
---@param dataList table 待填充的代码行数组
---@param compCls CS.FairyEditor.PublishHandler.ClassInfo 组件/界面类信息
function GenCommon:GenTransitionDefine(dataList, compCls)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(compCls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local transitionList = desc:Elements("transition")
    if transitionList.Count <= 0 then
        return
    end

    Tool:Log("生成动效的定义C#代码")
    for i = 1, transitionList.Count do
        ---@type CS.FairyGUI.Utils.XML
        local transition = transitionList[i - 1]

        local transitionName = transition:GetAttribute("name")
        transitionName = transitionName:gsub("^_", "")
        table.insert(dataList, string.format("\t\tprivate Transition %s;\n", transitionName .. 'Anim'))
    end
end

--- 生成控制器的定义代码和枚举定义，以及使用枚举的SetController函数C#代码：
--- @param dataList table 需要填充的内容
--- @param compCls table
function GenCommon:GenControllerDefine(dataList, compCls)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(compCls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local controllerList = desc:Elements("controller")
    if controllerList.Count <= 0 then
        return
    end

    Tool:Log("生成控制器的定义代码和枚举定义C#代码")

    -- 尝试从原始 XML 文件读取 alias 和 page remark（GetItemDesc 可能不包含这些编辑器元数据）
    -- FPackageItem 自带 owner.basePath / path / fileName，直接拼接即为原始组件 XML 路径
    local rawXml = nil
    local xmlPath = compCls.res.owner.basePath .. compCls.res.path .. compCls.res.fileName
    if Tool:IsFileExists(xmlPath) then
        rawXml = Tool:ReadTxt(xmlPath)
    end

    -- 第一遍：收集所有控制器的元数据
    local nameList = {}
    local ctrlInfos = {}
    for i = 1, controllerList.Count do
        ---@type CS.FairyGUI.Utils.XML
        local controller = controllerList[i - 1]

        local controllerName = controller:GetAttribute("name")
        table.insert(nameList, controllerName)

        -- 1. 获取显示名：优先从 rawXml 解析 alias，回退 XML 属性
        local displayName = nil
        if rawXml then
            local aliasPattern = '<controller name="' .. controllerName:gsub("([%.%-])", "%%%1") .. '"[^>]*alias="([^"]*)"'
            displayName = rawXml:match(aliasPattern)
        end
        if not displayName or displayName == "" then
            local alias = controller:GetAttribute("alias")
            displayName = (alias and alias ~= "") and alias or controllerName
        end

        -- 2. 构建页面备注映射：优先从 rawXml 解析 <remark>，回退为空
        local remarkMap = {}
        if rawXml then
            local escName = controllerName:gsub("([%.%-])", "%%%1")
            local ctrlStart = rawXml:find('<controller name="' .. escName .. '"')
            if ctrlStart then
                local ctrlSection = rawXml:sub(ctrlStart)
                local ctrlEnd = ctrlSection:find("</controller>")
                if ctrlEnd then
                    ctrlSection = ctrlSection:sub(1, ctrlEnd)
                end
                for page, value in ctrlSection:gmatch('<remark page="(%d+)" value="([^"]*)"') do
                    if value ~= "" then
                        remarkMap[page] = value
                    end
                end
            end
        end
        if next(remarkMap) == nil then
            local remarkList = controller:Elements("remark")
            if remarkList and remarkList.Count > 0 then
                for r = 1, remarkList.Count do
                    local remark = remarkList[r - 1]
                    local page = remark:GetAttribute("page")
                    local remarkValue = remark:GetAttribute("value")
                    if page and remarkValue and remarkValue ~= "" then
                        remarkMap[page] = remarkValue
                    end
                end
            end
        end

        -- 3. 收集页面信息
        local pages = controller:GetAttribute("pages")
        local pageValues = {}
        if pages then
            local valArray = Tool:StrSplit(pages, ",")
            for t = 1, #valArray, 2 do
                local idx = valArray[t]
                local value = valArray[t + 1]
                if value == "" then
                    value = ("N" .. idx)
                end
                local valueComment = remarkMap[idx] or value
                table.insert(pageValues, { idx = idx, value = value, comment = valueComment })
            end
        end

        table.insert(ctrlInfos, {
            name = controllerName,
            displayName = displayName,
            pages = pageValues,
        })
    end

    -- 第二遍：生成所有枚举定义
    for _, info in ipairs(ctrlInfos) do
        table.insert(dataList, Tool:StrFormat("\t\t/// <summary>\n\t\t/// %s\n\t\t/// </summary>\n", info.displayName))
        table.insert(dataList, "\t\tprivate enum E")
        table.insert(dataList, info.name)
        table.insert(dataList, "\n\t\t{\n")

        for _, pv in ipairs(info.pages) do
            table.insert(dataList, Tool:StrFormat("\t\t\t/// <summary>\n\t\t\t/// %s\n\t\t\t/// </summary>\n", pv.comment))
            table.insert(dataList, "\t\t\t")
            local keyName = Tool:FirstCharUpper(pv.value)
            table.insert(dataList, keyName)
            table.insert(dataList, " = ")
            table.insert(dataList, pv.idx)
            table.insert(dataList, ",\n")
        end
        table.insert(dataList, "\t\t}\n\n")
    end

    -- 第三遍：生成所有 SetController 方法
    for _, info in ipairs(ctrlInfos) do
        table.insert(dataList, Tool:StrFormat("\t\t/// <summary>\n\t\t/// 设置 %s 控制器状态\n\t\t/// </summary>\n", info.displayName))
        table.insert(dataList, Tool:StrFormat("\t\tprivate void SetController(E%s e%s) => ", info.name, info.name))
        table.insert(dataList, Tool:StrFormat("%s.SetSelectedIndex((int) e%s);\n", info.name, info.name))
        table.insert(dataList, "\n")
    end

    ------------生成控制器的定义-------------------
    ---如：private Controller TestCtrl;
    for _, name in ipairs(nameList) do
        table.insert(dataList, string.format("\t\tprivate Controller %s;\n", name))
    end
end

--- 生成组件的初始化赋值C#代码：_btnEnter = (btn_Enter)GetChild("_btnEnter");
---@param dataList table 待填充的代码行数组
---@param compArray table 组件信息数组
---@param AllClsMap table 所有类名映射表（资源名→类信息）
function GenCommon:GenCompInit(dataList, compArray, AllClsMap)
    if #compArray <= 0 then
        return
    end

    Tool:Log("生成组件的初始化赋值C#代码")
    for _, comp in ipairs(compArray) do
        if Tool:StartWith(comp.comp.name, "_") and comp.comp.type ~= "Controller" and comp.comp.type ~= "Transition" then
            local comType = Tool:GetCompType(comp.comp, AllClsMap)
            local paramName = Tool:FormatVarName(comp.comp.name)
            local comDef = string.format("\t\t\t%s = (%s)GetChild(\"%s\");\n", paramName, comType, comp.comp.name)
            table.insert(dataList, comDef)
        end
    end
end

--- 生成动效的初始化赋值C#代码：testAnim = WinUI.GetTransition("TestAnim");
---@param dataList table 待填充的代码行数组
---@param compCls CS.FairyEditor.PublishHandler.ClassInfo 组件/界面类信息
function GenCommon:GenTransitionInit(dataList, compCls)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(compCls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local transitionList = desc:Elements("transition")
    if transitionList.Count <= 0 then
        return
    end

    Tool:Log("生成动效的初始化赋值C#代码")
    for i = 1, transitionList.Count do
        ---@type CS.FairyGUI.Utils.XML
        local transition = transitionList[i - 1]

        ---@type string
        local transitionName = transition:GetAttribute("name")
        transitionName = transitionName:gsub("^_", "")

        table.insert(dataList, "\t\t\t")
        table.insert(dataList, transitionName .. 'Anim')
        table.insert(dataList, " = ")

        if Tool:StartWith(compCls.resName, "Win") then
            table.insert(dataList, "WinUI.")
        end

        table.insert(dataList, string.format("GetTransition(\"%s\");\n", transitionName))
    end
end

--- 生成控制器的初始化赋值C#代码：testCtrl = WinUI.GetController("TestCtrl");
---@param dataList table 待填充的代码行数组
---@param compCls CS.FairyEditor.PublishHandler.ClassInfo 组件/界面类信息
function GenCommon:GenControllerInit(dataList, compCls)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(compCls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local controllerList = desc:Elements("controller")
    if controllerList.Count <= 0 then
        return
    end

    Tool:Log("生成控制器的初始化赋值C#代码")
    for i = 1, controllerList.Count do
        ---@type CS.FairyGUI.Utils.XML
        local controller = controllerList[i - 1]
        local controllerName = controller:GetAttribute("name")

        table.insert(dataList, "\t\t\t")
        table.insert(dataList, controllerName)
        table.insert(dataList, " = ")

        if Tool:StartWith(compCls.resName, "Win") then
            table.insert(dataList, "WinUI.")
        end

        table.insert(dataList, string.format("GetController(\"%s\");\n", controllerName))
    end
end

--- 生成GList组件Item的渲染回调函数赋值C#代码：listPlayer.itemRenderer = OnShowListPlayerItem;
---@param dataList table 待填充的代码行数组
---@param compArray table 组件信息数组
---@param AllClsMap table 所有类名映射表（资源名→类信息）
function GenCommon:GenCompListOnRender(dataList, compArray, AllClsMap)
    if #compArray <= 0 then
        return
    end

    Tool:Log("生成GList组件Item的渲染回调函数赋值C#代码")
    for _, comp in ipairs(compArray) do
        if Tool:StartWith(comp.comp.name, "_") then
            local comType = Tool:GetCompType(comp.comp, AllClsMap)
            if comType == "GList" then
                local upName = Tool:FirstCharUpper(Tool:StrSub(comp.comp.name, 2, -1))
                table.insert(dataList, "\t\t\t")
                table.insert(dataList, Tool:FormatVarName(comp.comp.name))
                table.insert(dataList, ".itemRenderer = OnRender")
                table.insert(dataList, upName)
                table.insert(dataList, "Item;\n")
            end
        end
    end
end

--- 生成组件的交互事件添加监听C#代码:AddUIListener(btnEnter.onClick, OnBtnEnterClick);
---@param dataList table 待填充的代码行数组
---@param compArray table 组件信息数组
---@param AllClsMap table 所有类名映射表（资源名→类信息）
function GenCommon:GenCompEvent(dataList, compArray, AllClsMap)
    if #compArray <= 0 then
        return
    end

    Tool:Log("生成组件的交互事件添加监听C#代码")
    for _, comp in ipairs(compArray) do
        local uiEventsNameArray = GenCommon:GetCompRegUIEventName(comp.comp, AllClsMap)
        local upName = Tool:FirstCharUpper(Tool:StrSub(comp.comp.name, 2, -1))
        for _, v in ipairs(uiEventsNameArray) do
            table.insert(dataList, "\t\t\t")
            table.insert(dataList, "AddUIListener(")
            table.insert(dataList, Tool:FormatVarName(comp.comp.name))
            table.insert(dataList, ".")
            table.insert(dataList, v.eventName)
            table.insert(dataList, ", ")
            table.insert(dataList, string.format(v.cbNamePattern, upName))
            table.insert(dataList, ");\n")
        end
    end
end

--- 生成组件的交互事件处理函数C#代码:private void OnBtnEnterClick(EventContext ctx){ }。
-- list组件特殊处理，需要生成渲染GList组件的Item处理函数：private void OnRenderListPlayerItem(int idx, GObject item){ }
---@param dataList table 待填充的代码行数组
---@param compArray table 组件信息数组
---@param AllClsMap table 所有类名映射表（资源名→类信息）
function GenCommon:GenCompEventHandler(dataList, compArray, AllClsMap)
    if #compArray <= 0 then
        return
    end

    Tool:Log("生成组件的交互事件处理函数C#代码")

    for _, comp in ipairs(compArray) do
        local uiEventsNameArray = GenCommon:GetCompRegUIEventName(comp.comp, AllClsMap)
        local upName = Tool:FirstCharUpper(Tool:StrSub(comp.comp.name, 2, -1))

        for _, v in ipairs(uiEventsNameArray) do
            table.insert(dataList, "\t\tprivate void ")
            table.insert(dataList, string.format(v.cbNamePattern, upName))
            table.insert(dataList, "(")

            for _, arg in ipairs(v.args) do
                table.insert(dataList, string.format("%s %s", arg.argType, arg.argName))
            end

            table.insert(dataList, ")\n")
            table.insert(dataList, "\t\t{\n")

            if v.defaultContent then
                table.insert(dataList, v.defaultContent)
            end
            table.insert(dataList, "\t\t\t// todo\n")
            table.insert(dataList, "\t\t}\n\n")
        end

        -- 生成渲染GList组件的Item处理函数：private void OnRenderListPlayerItem(int idx, GObject item){} 
        if comp.comp.type == "GList" then
            GenCommon:GenListOnRenderHandler(dataList, comp.resName, upName)
        end
    end
end

--- 生成渲染GList组件的Item处理函数：private void OnRenderListPlayerItem(int idx, GObject item){}
---@param dataList table 待填充的代码行数组
---@param resName string 组件资源名称，用于类型转换（如 CompBagItem）
---@param upName string 组件功能名（驼峰，如 ListPlayer）
function GenCommon:GenListOnRenderHandler(dataList, resName, upName)
    Tool:Log("生成渲染GList组件<%s>的Item处理函数-%s", resName, "OnRender" .. upName .. "Item")
    table.insert(dataList, "\t\tprivate void OnRender")
    table.insert(dataList, upName)
    table.insert(dataList, "Item(int idx, GObject item)\n")
    table.insert(dataList, "\t\t{\n")
    table.insert(dataList, "\t\t\tif (item is not ")
    table.insert(dataList, resName)
    table.insert(dataList, " compItem) return;\n")
    table.insert(dataList, "\t\t\t//var data = xxxModel:Get")
    table.insert(dataList, upName)
    table.insert(dataList, "DataByIdx(idx);\n")
    table.insert(dataList, "\t\t\t//compItem.SetData(data);\n")
    table.insert(dataList, "\t\t\t// todo\n")
    table.insert(dataList, "\t\t}\n\n")
end

--- 获取不同类型组件的交互事件名称与回调信息
---@param comp CS.FairyEditor.PublishHandler.MemberInfo 组件信息
---@param AllClsMap table 所有类名映射表（资源名→类信息）
---@return table 事件配置数组，元素格式 {eventName, cbNamePattern, args, [defaultContent]}
function GenCommon:GetCompRegUIEventName(comp, AllClsMap)
    local type = Tool:GetCompType(comp, AllClsMap)

    -- GList 的 onClickItem 回调需要按组件名生成默认函数体，单独处理
    if type == "GList" then
        local dataList = {}

        local lowerName = Tool:FirstCharLower(Tool:StrSub(comp.name, 2, -1))
        table.insert(dataList, Tool:StrFormat("\t\t\tvar idx = %s.GetChildIndex((GObject)ctx.data);\n", lowerName))
        table.insert(dataList, Tool:StrFormat("\t\t\tif (%s.isVirtual) idx = %s.ChildIndexToItemIndex(idx);\n", lowerName, lowerName))
        table.insert(dataList, "\t\t\t//var data = xxxModel:GetListDataByIdx(idx);\n")

        return {
            {
                eventName = "onClickItem",
                cbNamePattern = "OnClick%sItem",
                args = DEFAULT_EVENT_ARGS,
                defaultContent = table.concat(dataList),
            }
        }
    end

    -- 其余类型查静态配置表（COMP_EVENT_CONFIG）
    local configs = COMP_EVENT_CONFIG[type]
    if not configs then
        return {}
    end

    local uiEventsNameArray = {}
    for _, config in ipairs(configs) do
        table.insert(uiEventsNameArray, {
            eventName = config.eventName,
            cbNamePattern = config.cbNamePattern,
            args = DEFAULT_EVENT_ARGS,
            defaultContent = config.defaultContent,
        })
    end
    return uiEventsNameArray
end

--- 将 #CompDefine# 生成内容拆分为字段声明与枚举/方法两部分（字段在前，去除首尾多余空行）
--- 拆分后填充 dataDict['#FieldDefine#'] 与 dataDict['#EnumAndMethodDefine#']，'#CompDefine#' 清空不再使用
---@param dataDict table 模板占位符 → 代码行数组 的字典（须已包含 '#CompDefine#' 键）
function GenCommon:SplitCompDefine(dataDict)
    local compDefineContent = table.concat(dataDict['#CompDefine#'])
    local fieldLines = {}
    local otherLines = {}
    for line in compDefineContent:gmatch("[^\n]*\n?") do
        if line:match("^\t*private %w+ [%w_]+;\n?$") then
            table.insert(fieldLines, line)
        elseif line:match("^%s*$") then
            if #fieldLines > 0 and #otherLines == 0 then
                -- 字段后的空白暂时跳过
            else
                table.insert(otherLines, line)
            end
        else
            table.insert(otherLines, line)
        end
    end
    dataDict['#FieldDefine#'] = fieldLines
    while #otherLines > 0 and otherLines[1]:match("^%s*$") do
        table.remove(otherLines, 1)
    end
    while #otherLines > 0 and otherLines[#otherLines]:match("^%s*$") do
        table.remove(otherLines)
    end
    dataDict['#EnumAndMethodDefine#'] = otherLines
    dataDict['#CompDefine#'] = {} -- 拆分后原占位符不再使用
end

--- 清理目录中不再存在于有效名称集合内的孤儿代码文件（连同 .meta）
---@param dir string 目标目录
---@param patterns string[] 文件名匹配模式数组（每个模式需含一个捕获组返回类名，如 "^(Win.+)%.Gen%.cs$"）
---@param currentSet table 当前有效类名集合 {类名=true}
---@param logLabel string 日志标签（如 "界面代码" / "组件手写代码"）
---@param deleteDirIfEmpty boolean|nil 清理后目录为空时是否删除目录及其 .meta
---@return table 被删除的类名列表
function GenCommon:CleanupOrphanedFiles(dir, patterns, currentSet, logLabel, deleteDirIfEmpty)
    local deleted = {}
    if not CS.System.IO.Directory.Exists(dir) then
        return deleted
    end

    local files = CS.System.IO.Directory.GetFiles(dir)
    if files and files.Length > 0 then
        for i = 0, files.Length - 1 do
            local filePath = files[i]
            local fileName = filePath:match("([^/\\]+)$")
            if fileName then
                local clsName = nil
                for _, pattern in ipairs(patterns) do
                    clsName = fileName:match(pattern)
                    if clsName then break end
                end

                if clsName and not currentSet[clsName] then
                    Tool:Log("[清理] 删除已移除%s: %s", logLabel, clsName)
                    Tool:DeleteFileWithMeta(filePath)
                    table.insert(deleted, clsName)
                end
            end
        end
    end

    if deleteDirIfEmpty then
        GenCommon:DeleteDirIfEmpty(dir)
    end

    return deleted
end

--- 如果目录为空（无任何文件/子目录），删除目录及其 .meta
---@param dirPath string 目录路径
function GenCommon:DeleteDirIfEmpty(dirPath)
    if not CS.System.IO.Directory.Exists(dirPath) then
        return
    end

    local remainingFiles = CS.System.IO.Directory.GetFiles(dirPath)
    local remainingDirs = CS.System.IO.Directory.GetDirectories(dirPath)
    if (not remainingFiles or remainingFiles.Length == 0) and (not remainingDirs or remainingDirs.Length == 0) then
        CS.System.IO.Directory.Delete(dirPath)
        Tool:Log("[清理] 目录为空，已删除: %s", dirPath)
        local metaPath = dirPath .. ".meta"
        if Tool:IsFileExists(metaPath) then
            CS.System.IO.File.Delete(metaPath)
        end
    end
end

return GenCommon