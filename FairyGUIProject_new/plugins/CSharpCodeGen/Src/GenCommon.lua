--- 生成C#代码时的通用功能对象
---@class GenCommon
local GenCommon = {}

--- 全局格式化输出函数
fprintf = function(fmt, ...)
    fprint(string.format(fmt, ...))
end

--- 生成自定义组件的定义代码：private GButton btnEnter;
function GenCommon:GenCompDefine(dataTable, compArray, AllClsMap)
    fprintf("生成组件的C#代码")
    if #compArray > 0 then
        for _, comp in ipairs(compArray) do
            if Tool:StartWith(comp.comp.name, "_") then
                local comType = Tool:GetCompType(comp.comp, AllClsMap)
                local paramName = Tool:FormatVarName(comp.comp.name)
                local comDef = string.format("\t\tprivate %s %s;\n", comType , paramName)
                table.insert(dataTable, comDef)
            end
        end
    end
end

--- 生成自定义组件的URL代码：public const string URL = "ui://mkasn9e4jo110";
function GenCommon:GenCompURL(dataTable, cls)
    fprintf("生成组件的URL的C#代码")
    local url = string.format("\t\tpublic const string URL = \"ui://%s%s\";\n", cls.res.owner.id, cls.resId)
     table.insert(dataTable, url)
end

--- 生成动效的定义代码：private Transition xxxAnim;
function GenCommon:GenTransitionDefine(dataTable,cls, AllClsMap)
    fprintf("生成动效的C#代码")
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(cls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local transitionList = desc:Elements("transition")
    local transitionCnt = transitionList.Count
    if transitionCnt > 0 then
        for i = 1, transitionCnt do
            ---@type CS.FairyGUI.Utils.XML
            local transition = transitionList[i - 1]

            local transitionName = transition:GetAttribute("name")
            transitionName = transitionName:gsub("^_", "")
            
            local funName = Tool:GetTransitionFunName(transitionName)

            table.insert(dataTable, string.format("\t\tprivate Transition %s;\n", transitionName..'Anim'))
        end
    end
end

--- 生成控制器的定义代码：private Transition tranXXX;
function GenCommon:GenControllerDefine(dataTable, winCls, content)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(winCls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local controllerList = desc:Elements("controller")
    local controllerCnt = controllerList.Count
    if controllerCnt > 0 then
        local nameList = {}
        for i = 1, controllerCnt do
            ---@type CS.FairyGUI.Utils.XML
            local controller = controllerList[i - 1]

            local controllerName = controller:GetAttribute("name")
            table.insert(nameList, controllerName)

            table.insert(dataTable, "\t\tenum E")
            table.insert(dataTable, controllerName)
            table.insert(dataTable, "\n\t\t{\n")

            local pages = controller:GetAttribute("pages")
            if pages then
                local valArray = Tool:StrSplit(pages, ",")
                for t = 1, #valArray, 2 do
                    local idx = valArray[t]
                    local value = valArray[t + 1]

                    if value == "" then
                        value = ("N"..idx)
                    end
                    
                    table.insert(dataTable, "\t\t\t")
                    local keyName = Tool:FirstCharUpper(value)
                    table.insert(dataTable, keyName)
                    table.insert(dataTable, " = ")
                    table.insert(dataTable, idx)
                    table.insert(dataTable, ",\n")
                end
            end
            table.insert(dataTable, "\t\t}\n\n")
            
        end

        for _, name in ipairs(nameList) do
            table.insert(dataTable, string.format("\t\tprivate Controller %s;\n", name))
        end
    end
end

--- 生成组件的初始化赋值代码：_btnEnter = (btn_Enter)GetChild("_btnEnter");
function GenCommon:GenCompInit(dataTable, compArray, AllClsMap)
    if #compArray > 0 then
        for _, comp in ipairs(compArray) do
            if Tool:StartWith(comp.comp.name,"_") and comp.comp.type ~= "Controller" and comp.comp.type ~= "Transition" then
                local comType = Tool:GetCompType(comp.comp,AllClsMap)        
                local paramName = Tool:FormatVarName(comp.comp.name)
                local comDef = string.format("\t\t\t%s = (%s)GetChild(\"%s\");\n",paramName,comType,comp.comp.name)
                table.insert(dataTable, comDef)
            end
        end
    end
end

--- 生成组件的DoInit函数代码
function GenCommon:GenDoInit(dataTable, compArray, AllClsMap)
    if #compArray > 0 then
        for _, comp in ipairs(compArray) do
            local comType = Tool:GetCompType(comp.comp, AllClsMap)
            local paramName = Tool:FormatVarName(comp.comp.name)
            if Tool:StartWith(comType, "Comp") then
                local comDef = string.format("\t\t\t%s.DoInit(this);\n", paramName)
                table.insert(dataTable, comDef)
            end
        end
    end
end

--- 生成动效的初始化赋值代码：testAnim = GetTransition("TestAnim");
function GenCommon:GenTransitionInit(dataTable, cls, AllClsMap)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(cls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local transitionList = desc:Elements("transition")
    local transitionCnt = transitionList.Count
    if transitionCnt > 0 then
        for i = 1, transitionCnt do
            ---@type CS.FairyGUI.Utils.XML
            local transition = transitionList[i - 1]
            ---@type string
            local transitionName = transition:GetAttribute("name")
            transitionName = transitionName:gsub("^_", "")

            table.insert(dataTable, "\t\t\t")
            table.insert(dataTable, transitionName..'Anim')
            table.insert(dataTable, " = ")

            if Tool:StartWith(cls.resName, "Win") then
                table.insert(dataTable, "contentPane.")
            end
        
            table.insert(dataTable, string.format("GetTransition(\"%s\");\n", transitionName))
        end
    end
end

--- 生成控制器的初始化赋值代码：testCtrl = GetController("TestCtrl");
function GenCommon:GenControllerInit(dataTable, winCls, content)
    local handler = Tool:Handler()

    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(winCls.res)

    ---@type CS.FairyGUI.Utils.XMLList
    local controllerList = desc:Elements("controller")
    local controllerCnt = controllerList.Count
    if controllerCnt > 0 then
        local nameList = {}
        for i = 1, controllerCnt do
            ---@type CS.FairyGUI.Utils.XML
            local controller = controllerList[i - 1]
            local controllerName = controller:GetAttribute("name")

            table.insert(dataTable, "\t\t\t")
            table.insert(dataTable, controllerName)
            table.insert(dataTable, " = ")

            if Tool:StartWith(winCls.resName, "Win") then
                table.insert(dataTable, "contentPane.")
            end

            table.insert(dataTable, string.format("GetController(\"%s\");\n", controllerName))
        end
    end
end

--- 生成GList组件Item的操作代码
function GenCommon:GenCompOperationn(dataTable, compArray, AllClsMap)
    if #compArray > 0 then
        for _, comp in ipairs(compArray) do
            if Tool:StartWith(comp.comp.name, "_") then
                local comType = Tool:GetCompType(comp.comp, AllClsMap)   
                if comType == "GList" then
                    local upName = Tool:FirstCharUpper(Tool:StrSub(comp.comp.name, 2, -1))
                    table.insert(dataTable, "\t\t\t")
                    table.insert(dataTable, Tool:FormatVarName(comp.comp.name))
                    table.insert(dataTable, ".itemRenderer = OnShow")
                    table.insert(dataTable, upName)
                    table.insert(dataTable, "Item;\n")
                end
            end
        end
    end
end

--- 生成组件的交互事件监听代码:AddListener(btnEnter.onClick, OnBtnEnterClick);
function GenCommon:GenCompEvent(dataTable, compArray, AllClsMap)
    if #compArray > 0 then
        for _, comp in ipairs(compArray) do
            local uiEventsNameArray = GenCommon:GetCompRegUIEventNameArray(comp.comp, comp.funName, AllClsMap)
            local upName = Tool:FirstCharUpper(Tool:StrSub(comp.comp.name,2,-1))
            for i, v in pairs(uiEventsNameArray) do
                table.insert(dataTable, "\t\t\t")
                table.insert(dataTable, "AddListener(")
                table.insert(dataTable, Tool:FormatVarName(comp.comp.name))
                table.insert(dataTable, ".")
                table.insert(dataTable, v.eventName)
                table.insert(dataTable, ", ")
                table.insert(dataTable, string.format(v.cbNamePattern, upName))
                table.insert(dataTable, ");\n")
            end
        end
    end
end

--- 生成组件的交互事件处理函数代码:	private void OnBtnEnterClick(EventContext ctx){}
function GenCommon:GenCompEventHandler(dataTable, compArray, AllClsMap)
    if #compArray > 0 then
        for _, comp in ipairs(compArray) do
            local uiEventsNameArray = GenCommon:GetCompRegUIEventNameArray(comp.comp, comp.funName, AllClsMap)
            local upName = Tool:FirstCharUpper(Tool:StrSub(comp.comp.name,2,-1))
            for i, v in pairs(uiEventsNameArray) do
                table.insert(dataTable, "\t\tprivate void ")
                table.insert(dataTable, string.format(v.cbNamePattern, upName))
                table.insert(dataTable, "(")

                for _, arg in ipairs(v.args) do
                    table.insert(dataTable, string.format("%s %s",arg.argType,arg.argName))
                end
                
                table.insert(dataTable, ")\n")
                table.insert(dataTable, "\t\t{\n")

                if v.defaultContent then
                    table.insert(dataTable, v.defaultContent)
                end
                
                table.insert(dataTable, "\n\t\t}\n")
            end

            if comp.comp.type == "GList" then
                GenCommon:AppendShowAndSetListHandlerContent(dataTable, comp.resName, upName)
            end
        end
    end
end

--- 获取不同类型组件的交互事件名称数组
function GenCommon:GetCompRegUIEventNameArray(comp, funName, AllClsMap)
    local uiEventsNameArray = {}
    local type = Tool:GetCompType(comp,AllClsMap)

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
        local dataTable = {}

        table.insert(dataTable, "\t\t\t//var idx = ((GObject)ctx.data)?.ItemIndex;\n")
        table.insert(dataTable, "\t\t\t//var data = xxxModel:Get")
        table.insert(dataTable, "ListDataByIdx(idx);\n")
        table.insert(dataTable, "\t\t\t// todo\n")

        table.insert(uiEventsNameArray, {
            eventName = "onClickItem",
            cbNamePattern = "OnClick%sItem",
            args = {
                {
                    argName = "ctx",
                    argType = "EventContext",
                }
            },
            defaultContent = table.concat(dataTable),
        })
    end

    return uiEventsNameArray
end

--- 添加显示并设置GList组件的Item处理代码
function GenCommon:AppendShowAndSetListHandlerContent(dataTable,  resName,  upName)
    table.insert(dataTable, "\t\tprivate void OnShow")
    table.insert(dataTable, upName)
    table.insert(dataTable, "Item(int idx, GObject item)\n")
    table.insert(dataTable, "\t\t{\n")
    table.insert(dataTable, "\t\t\t//var data = xxxModel:Get")
    table.insert(dataTable, upName)
    table.insert(dataTable, "DataByIdx(idx);\n")
    table.insert(dataTable, string.format("\t\t\t//Make Sure the %s is Exported\n",resName))
    table.insert(dataTable, string.format("\t\t\t//((%s)item)?.Init(data);\n",resName))
    table.insert(dataTable, "\t\t\t// todo\n")
    table.insert(dataTable, "\t\t}\n")
end

return GenCommon