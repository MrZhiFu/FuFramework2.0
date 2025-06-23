---@class Tool 工具方法相关定义
---@field private pluginPath
local Tool = {}

local File = CS.System.IO.File
local Directory = CS.System.IO.Directory

--- 导出界面View的C#代码路径
Tool.ExportViewPath = "%s/Game/Scripts/Runtime/Logics/UI/View/%s/ViewImpl"

--- 导出界面ViewGen的C#代码路径
Tool.ExportViewGenPath = "%s/Game/Scripts/Runtime/Logics/UI/View/%s/ViewGen/"


--- 字符串格式化（封装 string.format，提供一致性调用接口）
---@param str string 格式字符串（参考 string.format）
---@param ... any 可变参数，用于填充格式字符串
---@return string 格式化后的字符串
---@usage Tool:StrFormat("Hello %s!", "World")  --> "Hello World!"
function Tool:StrFormat(str, ...)
    return string.format(str, ...)
end

--- 字符串查找（封装 string.find，提供一致性调用接口）
--- @param s        string  待查找的字符串
--- @param pattern  string  匹配模式（支持正则或纯文本）
--- @param init     number  [可选]起始查找位置，默认 1
--- @param plain    boolean [可选]是否禁用正则，默认 false
--- @return number|nil start  匹配起始位置（未找到返回 nil）
--- @return number|nil end    匹配结束位置（未找到返回 nil）
--- @return string|nil capture 第一个捕获组内容（无捕获组时返回 nil）
--- @usage 
---   local s, e = Tool:StrFind("hello world", "world") --> 7, 11
---   local s, e, cap = Tool:StrFind("2023-12-31", "(%d+)-%d+-%d+") --> 1, 10, "2023"
function Tool:StrFind(s, pattern, init, plain)
    return string.find(s, pattern, init, plain)
end

--- 判断字符串是否以指定子串开头
--- @param str    string 原字符串
--- @param subStr string 要检查的前缀子串
--- @return boolean 是否以该子串开头
--- @usage 
---   Tool:StartWith("你好世界", "你好") --> true
---   Tool:StartWith("hello", "ello") --> false
function Tool:StartWith(str, subStr)
    if type(str) ~= "string" or type(subStr) ~= "string" then
        return false
    end
    return string.sub(str, 1, #subStr) == subStr
end

--- 截取子字符串（封装 string.sub）
--- @param s string 原字符串
--- @param i number 起始位置（支持负数倒数，默认 1）
--- @param j number|nil 结束位置（支持负数倒数，默认到字符串末尾）
--- @return string 截取后的子串
--- @usage
---   Tool:StrSub("abcdef", 2, 4)    --> "bcd"
---   Tool:StrSub("abcdef", -3)      --> "def"
function Tool:StrSub(s, i, j)
    return string.sub(s, i, j)
end

--- 创建目录
---@param path string
function Tool:CreateDirectory(path)
    if not Directory.Exists(path) then
        Directory.CreateDirectory(path)
    end
end

--- 检查文件是否存在
---@param path string
---@return boolean
function Tool:IsFileExists(path)
    return File.Exists(path)
end

--- 格式化变量名（下划线命名转驼峰命名）
---@param varName string 待格式化的变量名（支持包含下划线）
---@return string 转换后的驼峰命名变量名
function Tool:FormatVarName(varName)
    varName = string.gsub(varName, "^_+", "") -- 1. 去除开头的一个或多个下划线
    varName = string.gsub(varName, "_+$", "") -- 2. 去除结尾的一个或多个下划线  
    varName = string.gsub(varName, "_(%a)", function(c)  -- 3. 将下划线后跟字母转为大写字母（驼峰关键步骤）
        return string.upper(c) 
    end)
    return varName
end

--- 日志输出
function Tool:Log(fmt, ...)
    fmt = tostring(fmt)
    fprint(string.format(fmt, ...))
end

--- 读取文本
---@return string
function Tool:ReadTxt(path)
    return File.ReadAllText(path)
end

--- 写入文本
---@param path string
---@param content string
function Tool:WriteTxt(path, content)
    File.WriteAllText(path, content)
end

--- 设置插件路径
---@param path string
function Tool:SetPluginPath(path)
    Tool.pluginPath = path
end

--- 获得插件路径
---@return string
function Tool:PluginPath()
    return Tool.pluginPath
end

--- 设置句柄
---@param handler CS.FairyEditor.PublishHandler
function Tool:SetHandler(handler)
    Tool.handler = handler
end

--- 获得句柄
---@return CS.FairyEditor.PublishHandler
function Tool:Handler()
    return Tool.handler
end

--- 字符串分割
---@param content string 要分割的字符串
---@param pattern string 分隔符（支持正则或纯文本）
---@param plain boolean [可选]是否禁用正则匹配（默认false）
---@return string[]
---   Tool:StrSplit("a,b,c", ",") --> {"a","b","c"}
---   Tool:StrSplit("1|2|3", "|", true) --> {"1","2","3"}
function Tool:StrSplit(content, pattern, plain)
    local index = 1
    local splitIndex = 1
    local result = {}
    while true do
        local lastIndex = string.find(content, pattern, index, plain)
        if not lastIndex then
            result[splitIndex] = string.sub(content, index, string.len(content))
            break
        end
        result[splitIndex] = string.sub(content, index, lastIndex - 1)
        index = lastIndex + string.len(pattern)
        splitIndex = splitIndex + 1
    end
    return result
end

--- 是否是需要导出的组件(组件以下划线结尾的是需要导出的)
---@param member CS.FairyEditor.PublishHandler.MemberInfo
---@return boolean
function Tool:IsExportedComp(member)
    return Tool:StrFind(member.name, '_') == 1
end

--- 获取窗口中指定列表组件（List）引用的默认项资源
---@param winCls CS.FairyEditor.PublishHandler.ClassInfo 窗口类信息
---@param member CS.FairyEditor.PublishHandler.MemberInfo 成员信息（列表组件）
---@return CS.FairyEditor.FPackageItem|nil 列表引用的默认资源项
function Tool:GetListRefRes(winCls, member)
    -- 1. 获取发布处理器实例
    local handler = Tool:Handler()
    self:Log("[GetListRefRes] 获取发布处理器实例", "DEBUG")

    -- 2. 获取窗口类的XML结构描述文件
    ---@type CS.FairyGUI.Utils.XML
    local desc = handler:GetItemDesc(winCls.res)
    self:Log(string.format("[GetListRefRes] 加载资源描述XML，资源ID: %s", winCls.res.id), "INFO")

    -- 3. 获取显示列表节点
    local displayList = desc:GetNode("displayList")
    if not displayList then
        self:Log("[GetListRefRes] 未找到displayList节点", "WARNING")
        return nil
    end
    self:Log(string.format("[GetListRefRes] displayList节点包含 %d 个子元素", displayList.elements.Count), "DEBUG")

    -- 4. 遍历列表所有显示元素
    local cnt = displayList.elements.Count
    for i = 1, cnt do
        -- C#集合索引从0开始，需要-1
        ---@type CS.FairyGUI.Utils.XML
        local element = displayList.elements[i - 1]
        local elementName = element:GetAttribute("name") or ""
        self:Log(string.format("[GetListRefRes] 检查元素[%d/%d]: %s", i, cnt, elementName), "TRACE")

        -- 5. 匹配目标组件
        if elementName == member.name then
            self:Log(string.format("[GetListRefRes] 找到匹配列表组件: %s", member.name), "SUCCESS")
            
            -- 6. 获取列表默认项资源ID
            local defaultItemId = element:GetAttribute("defaultItem")
            if not defaultItemId then
                self:Log("[GetListRefRes] 该列表组件未设置defaultItem属性", "WARNING")
                return nil
            end
            self:Log(string.format("[GetListRefRes] 默认项资源ID: %s", defaultItemId), "INFO")

            -- 7. 通过资源URL获取具体资源项
            ---@type CS.FairyEditor.FPackageItem
            local defaultItem = handler.project:GetItemByURL(defaultItemId)
            if defaultItem then
                self:Log(string.format(
                    "[GetListRefRes] 成功加载资源: %s (类型: %s, 尺寸: %dx%d)", 
                    defaultItem.name, 
                    defaultItem.type,
                    defaultItem.width,
                    defaultItem.height
                ), "SUCCESS")
            else
                self:Log(string.format("[GetListRefRes] 资源加载失败: %s", defaultItemId), "ERROR")
            end
            
            return defaultItem
        end
    end

    -- 8. 未找到匹配组件
    self:Log(string.format("[GetListRefRes] 未找到匹配的列表组件: %s", member.name), "WARNING")
    return nil
end

--- 获取组件功能名（智能转换组件名为驼峰式功能名）
--- @param comp CS.FairyEditor.PublishHandler.MemberInfo 组件信息
--- @return string 转换后的功能名（如 "_btnOK" → "Ok"）
--- local name = Tool:GetCompFunName({type="GButton", name="_btnOK"}) -- 返回 "Ok"
--- local name = Tool:GetCompFunName({type="GList", name="_listItem1"}) -- 返回 "Item1"
function Tool:GetCompFunName(comp)
    -- 参数校验
    if not comp or not comp.name or comp.name == "" then
        return ""
    end

    -- 组件类型与对应前缀映射表（更高效的查找方式）
    local prefixMap = {
        GButton = {"_btn", "_Btn"},
        GList = {"_list", "_List"},
        GSlider = {"_slider", "_Slider", "_sld"},
        GComboBox = {
            "_comboBox", "_combobox", "_Combobox", 
            "_ComboBox", "_combo", "_Combo", "_com"
        },
        GTextInput = {
            "_input", "_Input", "_txtInput", "_TxtInput",
            "_textInput", "_TextInput", "_text", "_Text",
            "_txt", "_Txt"
        }
    }

    -- 1. 获取待移除前缀列表
    local removeList = {}
    if comp.type and prefixMap[comp.type] then
        removeList = Tool:TableCopy(prefixMap[comp.type])
    end
    table.insert(removeList, "_") -- 默认移除单独的下划线

    -- 2. 移除所有匹配前缀
    local name = comp.name
    for _, prefix in ipairs(removeList) do
        -- 使用 ^ 确保只匹配前缀
        name = string.gsub(name, "^" .. prefix, "")
    end

    -- 3. 处理结果
    if name == "" then
        -- 如果移除前缀后为空，返回组件类型作为后备
        return comp.type and comp.type:gsub("^G", "") or ""
    end

    -- 4. 首字母大写处理（保留后续数字和大小写）
    return Tool:FirstCharUpper(name)
end

--- 辅助函数：浅拷贝表
--- @param t table 源表
--- @return table 新表
function Tool:TableCopy(t)
    local res = {}
    for k, v in pairs(t) do res[k] = v end
    return res
end

--- 首字母大写
---@param str string
---@return string
function Tool:FirstCharUpper(str)
    if not str or str == "" then
        return ""
    end

    local first = string.sub(str, 1, 1)
    if tonumber(first) then
        return "N" .. str
    end

    return string.gsub(str, "^%l", string.upper)
end

--- 首字母小写
---@param str string
---@return string
function Tool:FirstCharLower(str)
    if not str or str == "" then
        return ""
    end

    local first = string.sub(str, 1, 1)
    if tonumber(first) then
        return "N" .. str
    end

    return string.gsub(str, "^%u", string.lower)
end

--- 获取窗口类中所有导出组件的结构化信息
--- @param winCls CS.FairyEditor.PublishHandler.ClassInfo 窗口类信息
--- @return table 包含组件信息的数组，每个元素格式为：
--- {comp:MemberInfo, resName:string, resPkg:string, funName:string}
function Tool:GetCompArray(winCls)
    local compArray = {}

    ---@type CS.FairyEditor.PublishHandler.MemberInfo[]
    local members = winCls.members
    for _, member in pairs(members) do
        if Tool:IsExportedComp(member) then
            local resName = ""
            local resPkg = ""
            if member.res then
                resName = member.res.name
                resPkg = member.res.owner.name
            elseif member.type == "GList" then
                --- 获得列表引用的组件
                local defaultItem = Tool:GetListRefRes(winCls, member)
                if defaultItem then
                    resName = defaultItem.name
                    resPkg = defaultItem.owner.name
                end
            end

            table.insert(compArray, {
                comp = member,
                resName = resName,
                resPkg = resPkg,
                funName = Tool:GetCompFunName(member),
            })
        end
    end

    return compArray
end

--- 获得控制器功能名
--- CtrOK -> OK, _ctrItem -> Item
---@param name
---@return string
---@private
function Tool:GetControllerFunName(name)
    local removeList = { "Ctrl", "ctrl", "Ctr", "ctr", "_" }

    for _, v in ipairs(removeList) do
        if name and name ~= "" then
            name = string.gsub(name, v, "")
        end
    end

    if name and name ~= "" then
        name = Tool:FirstCharUpper(name)
    end

    return name
end

--- 获得动效功能名
--- transitionOK -> OK, _transItem -> Item
---@param name
---@return string
---@private
function Tool:GetTransitionFunName(name)
    local removeList = { "Transition", "transition", "Trans", "trans", "Tran", "tran", "_" }

    for _, v in ipairs(removeList) do
        if name and name ~= "" then
            name = string.gsub(name, v, "")
        end
    end

    if name and name ~= "" then
        name = Tool:FirstCharUpper(name)
    end

    return name
end

--- 获得列表变量名
---@param funName string
---@return string
function Tool:GetListFieldName(funName)
    if funName == "" then
        return "value"
    end

    return Tool:FirstCharLower(funName)
end

---获取组件类型
---@param comp MemberInfo  组件信息对象
---@param AllClsMap table 类名映射表（资源名→类信息）
---@return string 组件类型，优先返回组件资源定义的类型，其次返回基础组件类型
function Tool:GetCompType(comp, AllClsMap)
    if comp.res then
        local cls = AllClsMap[comp.res.name]
        if cls then
            if not cls.res.exported or not Tool:StartWith(cls.resName, "Comp") then
                return cls.superClassName
            end
        end
        if Tool:StartWith(comp.res.name, "Comp") then
            return comp.res.name
        end
    end
    return comp.type
end

--- 检查组件资源是否被导出
--- @param comp MemberInfo 组件信息
--- @param AllClsMap table 类映射表
--- @return boolean
function Tool:IsCompResExported(comp, AllClsMap)
    -- 根据组件类型查找类定义
    local cls = AllClsMap[comp.type]

    -- 如果找到类定义，返回其导出状态
    if cls then
        return cls.res.exported
    end
    
    -- 默认返回false
    return false
end

return Tool