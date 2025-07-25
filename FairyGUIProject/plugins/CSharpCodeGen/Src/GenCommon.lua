--- 生成C#代码时的通用功能对象
---@class GenCommon
local GenCommon = {}

--- 生成组件的定义代码：private GButton btnEnter;
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
function GenCommon:GenCompURL(dataList, compCls)
    Tool:Log("生成组件的URL的C#代码")
    local url = string.format("\t\tpublic const string URL = \"ui://%s%s\";\n\n", compCls.res.owner.id, compCls.resId)
    table.insert(dataList, url)
end

--- 生成动效的定义代码：private Transition xxxAnim;
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

    local nameList = {}
    for i = 1, controllerList.Count do
        ---@type CS.FairyGUI.Utils.XML
        local controller = controllerList[i - 1]

        local controllerName = controller:GetAttribute("name")
        table.insert(nameList, controllerName)

        ------------生成控制器对应的枚举定义-------------------
        ------ 如：enum ECtrlSelected
        ------       {
        ------            No = 0,
        ------            Yes = 1,
        ------       }
        table.insert(dataList, "\t\tprivate enum E")
        table.insert(dataList, controllerName)
        table.insert(dataList, "\n\t\t{\n")

        local pages = controller:GetAttribute("pages")
        if pages then
            local valArray = Tool:StrSplit(pages, ",")
            for t = 1, #valArray, 2 do
                local idx = valArray[t]
                local value = valArray[t + 1]

                if value == "" then
                    value = ("N" .. idx)
                end

                table.insert(dataList, "\t\t\t")
                local keyName = Tool:FirstCharUpper(value)
                table.insert(dataList, keyName)
                table.insert(dataList, " = ")
                table.insert(dataList, idx)
                table.insert(dataList, ",\n")
            end
        end
        table.insert(dataList, "\t\t}\n\n")

        ------------生成控制器的SetController函数----------------
        --- 如：private void SetController(ETestCtrl eTestCtrl) => CtrlSelected.SetSelectedIndex((int) eTestCtrl);
        table.insert(dataList, Tool:StrFormat("\t\tprivate void SetController(E%s e%s) => ", controllerName, controllerName))
        table.insert(dataList, Tool:StrFormat("%s.SetSelectedIndex((int) e%s);\n", controllerName, controllerName))
        table.insert(dataList, "\n")
    end

    ------------生成控制器的定义-------------------
    ---如：private Controller CtrlSelected
    for _, name in ipairs(nameList) do
        table.insert(dataList, string.format("\t\tprivate Controller %s;\n", name))
    end
end

--- 生成组件的初始化赋值C#代码：_btnEnter = (btn_Enter)GetChild("_btnEnter");
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

--- 生成自定义组件的初始化Init函数C#代码：compXXX.InitView(this.uiView)，注入该组件所属的界面
function GenCommon:GenCustomCompInit(dataList, compArray, AllClsMap, isComp)
    if #compArray <= 0 then
        return
    end

    Tool:Log("生成自定义组件的初始化InitView函数C#代码")
    for _, comp in ipairs(compArray) do
        local comType = Tool:GetCompType(comp.comp, AllClsMap)
        local paramName = Tool:FormatVarName(comp.comp.name)
        if Tool:StartWith(comType, "Comp") then
            local comDef = string.format("\t\t\t%s.InitView(this);", paramName)
            if isComp then
                -- 如果是自定义组件里面的自定义组件，传递uiView到InitView方法中
                comDef = string.format("\t\t\t%s.InitView(this.uiView);", paramName)
            end
            table.insert(dataList, comDef)
        end
    end
end

--- 生成动效的初始化赋值C#代码：testAnim = UIView.GetTransition("TestAnim");
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
function GenCommon:GenListOnRenderHandler(dataList, resName, upName)
    Tool:Log("生成渲染GList组件<%s>的Item处理函数-%s", resName, "OnRender" .. upName .. "Item")
    table.insert(dataList, "\t\tprivate void OnRender")
    table.insert(dataList, upName)
    table.insert(dataList, "Item(int idx, GObject item)\n")
    table.insert(dataList, "\t\t{\n")
    table.insert(dataList, "\t\t\tif (item is not ")
    table.insert(dataList, upName)
    table.insert(dataList, " compItem) return;")
    table.insert(dataList, "\t\t\t//var data = xxxModel:Get")
    table.insert(dataList, upName)
    table.insert(dataList, "DataByIdx(idx);\n")
    table.insert(dataList, "\t\t\tcompItem.InitView(this);\n")
    table.insert(dataList, "\t\t\t//compItem.SetData(data);\n")
    table.insert(dataList, "\t\t\t// todo\n")
    table.insert(dataList, "\t\t}\n\n")
end

--- 获取不同类型组件的交互事件名称
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
        
        local lowerName = Tool:FirstCharLower("%s")
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

return GenCommon