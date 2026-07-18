--- 生成C#代码时的通用功能对象
---@class GenCommon
local GenCommon = {}

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
    local rawXml = nil
    local pkgName = compCls.res.owner.name
    local compName = compCls.resName
    local pluginPath = Tool:PluginPath()
    -- pluginPath 示例: .../FairyGUIProject/plugins/CSharpCodeGen/
    -- 回退两级得到 FGUI 项目根目录
    local projectPath = pluginPath:gsub("[/\\]plugins[/\\]CSharpCodeGen[/\\]?$", "")
    if projectPath and projectPath ~= pluginPath then
        local xmlPath = nil
        -- 通过 package.xml 查找组件所在的子目录路径
        local pkgXmlPath = projectPath .. "/assets/" .. pkgName .. "/package.xml"
        if Tool:IsFileExists(pkgXmlPath) then
            local pkgXml = Tool:ReadTxt(pkgXmlPath)
            -- package.xml 格式: <component ... name="CompBagItemInfo.xml" path="/Comp/" .../>
            local escName = compName:gsub("([%.%-])", "%%%1")
            local pkgPath = pkgXml:match('name="' .. escName .. '%.xml"[^>]*path="([^"]*)"')
            if pkgPath then
                -- path 格式如 "/Comp/" → 去掉首尾斜杠 → "Comp"
                pkgPath = pkgPath:gsub("^/", ""):gsub("/$", "")
                if pkgPath ~= "" then
                    xmlPath = projectPath .. "/assets/" .. pkgName .. "/" .. pkgPath .. "/" .. compName .. ".xml"
                end
            end
        end
        -- 回退：组件在包根目录
        if not xmlPath then
            xmlPath = projectPath .. "/assets/" .. pkgName .. "/" .. compName .. ".xml"
        end
        if Tool:IsFileExists(xmlPath) then
            rawXml = Tool:ReadTxt(xmlPath)
        end
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

--- 生成动效的初始化赋值C#代码：testAnim = UIView.GetTransition("TestAnim");
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
            table.insert(dataList, "UIView.")
        end

        table.insert(dataList, string.format("GetTransition(\"%s\");\n", transitionName))
    end
end

--- 生成控制器的初始化赋值C#代码：testCtrl = UIView.GetController("TestCtrl");
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
            table.insert(dataList, "UIView.")
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
        for i, v in pairs(uiEventsNameArray) do
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

        for i, v in pairs(uiEventsNameArray) do
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
    table.insert(dataList, "\t\t\tcompItem.Init(this);\n")
    table.insert(dataList, "\t\t\t//compItem.SetData(data);\n")
    table.insert(dataList, "\t\t\t// todo\n")
    table.insert(dataList, "\t\t}\n\n")
end

--- 获取不同类型组件的交互事件名称与回调信息
---@param comp CS.FairyEditor.PublishHandler.MemberInfo 组件信息
---@param AllClsMap table 所有类名映射表（资源名→类信息）
---@return table 事件配置数组，元素格式 {eventName, cbNamePattern, args, [defaultContent]}
function GenCommon:GetCompRegUIEventName(comp, AllClsMap)
    local uiEventsNameArray = {}
    local type = Tool:GetCompType(comp, AllClsMap)

    -- 滑动条
    if type == "GSlider" then
        table.insert(uiEventsNameArray, {
            eventName = "onChanged",
            cbNamePattern = "On%sChanged",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
        })

        -- 复选框
    elseif type == "GComboBox" then
        table.insert(uiEventsNameArray, {
            eventName = "onChanged",
            cbNamePattern = "On%sChanged",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
        })

        -- 输入框
    elseif type == "GTextInput" then
        table.insert(uiEventsNameArray, {
            eventName = "onChanged",
            cbNamePattern = "On%sChanged",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
        })
        table.insert(uiEventsNameArray, {
            eventName = "onFocusOut",
            cbNamePattern = "On%sFocusOut",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
        })

        -- 按钮
    elseif type == "GButton" then
        table.insert(uiEventsNameArray, {
            eventName = "onClick",
            cbNamePattern = "On%sClick",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
        })

        -- 列表
    elseif type == "GList" then
        local dataList = {}

        local lowerName = Tool:FirstCharLower(Tool:StrSub(comp.name, 2, -1))
        table.insert(dataList, Tool:StrFormat("\t\t\tvar idx = %s.GetChildIndex((GObject)ctx.data);\n", lowerName))
        table.insert(dataList, Tool:StrFormat("\t\t\tif (%s.isVirtual) idx = %s.ChildIndexToItemIndex(idx);\n", lowerName, lowerName))
        table.insert(dataList, "\t\t\t//var data = xxxModel:Get")
        table.insert(dataList, "ListDataByIdx(idx);\n")

        table.insert(uiEventsNameArray, {
            eventName = "onClickItem",
            cbNamePattern = "OnClick%sItem",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
            defaultContent = table.concat(dataList),
        })
    end

    return uiEventsNameArray
end

--- 生成红点注册代码的入口函数
--- 遍历组件的 displayList，查找含 i18n= 自定义数据的元素，生成 compXxx.Register(uiView, ERedDotKey.Xxx);
---@param dataList table 待填充的代码行数组
---@param compCls CS.FairyEditor.PublishHandler.ClassInfo 组件或界面类信息
function GenCommon:GenRedDotRegister(dataList, compCls)
    local handler = Tool:Handler()
    local desc = handler:GetItemDesc(compCls.res)
    local displayList = desc:GetNode("displayList")
    if not displayList then
        return
    end
    self:FindRedDotComps(displayList, dataList)
end

--- 递归遍历XML节点，查找含 i18n= 自定义数据的元素
--- 匹配 i18n=ERedDotKey.Xxx 格式，生成 compXxx.Register(uiView, ERedDotKey.Xxx);
---@param xmlNode CS.FairyGUI.Utils.XML
---@param dataList table
function GenCommon:FindRedDotComps(xmlNode, dataList)
    local elements = xmlNode.elements
    local cnt = elements.Count
    for i = 1, cnt do
        local element = elements[i - 1]
        local name = element:GetAttribute("name") or ""
        local customData = element:GetAttribute("customData") or ""

        -- 匹配 i18n=ERedDotKey.Xxx 格式
        local enumName = customData:match("i18n=(ERedDotKey%.%w+)")
        if enumName then
            local varName = Tool:FormatVarName(name)
            table.insert(dataList, string.format(
                "\t\t\t%s.Register(uiView, %s);\n", varName, enumName))
        end

        -- 递归处理子元素
        self:FindRedDotComps(element, dataList)
    end
end

return GenCommon