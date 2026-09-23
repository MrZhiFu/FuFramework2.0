--- 生成组件绑定的C#代码
---@class GenBinder
local GenBinder = {}

--- 生成组件绑定C#代码（汇总到 CustomCompBind.cs）
---@param pkgName string 包名
---@param compClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 组件数组
---@param unityDataPath string Unity路径 "xxx/Assets"
function GenBinder:Gen(pkgName, compClsArray, unityDataPath)
    local exportGenPath = Tool:GetExportCodeGenPath(pkgName)
    local namespace = Tool:GetExportCodeNamespace(pkgName)

    -- CustomCompBind.cs 放在 UI 目录根下（不区分包子目录）
    -- exportGenPath 为 "%s/Scripts/Hotfix/Game/AutoGen/UI/%s"
    -- 格式化为空字符串去掉末尾 %s/，再清理尾斜杠
    local customCompBindDir = Tool:StrFormat(exportGenPath, unityDataPath, ""):gsub("/+$", "")

    Tool:CreateDirectory(customCompBindDir)
    local targetPath = Tool:StrFormat("%s/CustomCompBind.cs", customCompBindDir)

    if not compClsArray or #compClsArray == 0 then
        -- 包中已无自定义组件，从 CustomCompBind.cs 中移除该包的方法
        if Tool:IsFileExists(targetPath) then
            GenBinder:RemovePackageMethod(targetPath, pkgName, namespace)
        end
        return
    end

    -- 生成当前包的 BindXxx 方法
    local methodCode = GenBinder:GenBindMethod(pkgName, compClsArray)

    -- 创建或更新 CustomCompBind.cs
    if Tool:IsFileExists(targetPath) then
        GenBinder:UpdateCustomCompBind(targetPath, pkgName, methodCode, namespace)
    else
        GenBinder:CreateCustomCompBind(targetPath, pkgName, methodCode, namespace)
    end
end

--- 生成单个包的 BindXxx 方法代码
---@param pkgName string 包名
---@param compClsArray table 组件类信息数组
---@return string 方法代码
function GenBinder:GenBindMethod(pkgName, compClsArray)
    local lines = {}
    table.insert(lines, Tool:StrFormat("\t\t/// <summary>"))
    table.insert(lines, Tool:StrFormat("\t\t/// 绑定%s包下的自定义组件", pkgName))
    table.insert(lines, "\t\t/// </summary>")
    table.insert(lines, Tool:StrFormat("\t\tprivate static void Bind%s()", pkgName))
    table.insert(lines, "\t\t{")
    table.insert(lines, Tool:StrFormat('\t\t\tFuLogger.LogInfo("绑定包-{%s}下的所有自定义组件");', pkgName))

    for _, cls in ipairs(compClsArray) do
        if cls.res.exported then
            table.insert(lines, Tool:StrFormat("\t\t\tUIObjectFactory.SetPackageItemExtension(%s.URL, typeof(%s));", cls.resName, cls.resName))
        end
    end

    table.insert(lines, "\t\t}")
    return table.concat(lines, "\n")
end

--- 解析已有 CustomCompBind.cs，提取所有 BindXxx 方法
---@param content string 文件内容
---@return table {methods = {[pkgName] = methodText}, order = {pkgName1, ...}}
function GenBinder:ParseCustomCompBind(content)
    local methods = {}
    local order = {}
    local pos = 1

    while true do
        -- 查找方法级注释（2 个 tab）
        local summaryStart, summaryEnd = content:find("\t\t/// <summary>[^\n]*\n", pos)
        if not summaryStart then
            break
        end

        -- 从注释末尾查找方法签名
        local sigStart, sigEnd, pkgName = content:find("\t\tprivate static void Bind(%w+)()", summaryEnd)
        if sigStart then
            -- 查找方法体起始大括号
            local openBrace = content:find("{", sigEnd, true)
            if openBrace then
                -- 匹配大括号找到方法结束位置
                local depth = 0
                local closeBrace = openBrace
                for i = openBrace, #content do
                    local c = content:sub(i, i)
                    if c == "{" then
                        depth = depth + 1
                    elseif c == "}" then
                        depth = depth - 1
                        if depth == 0 then
                            closeBrace = i
                            break
                        end
                    end
                end

                local methodText = content:sub(summaryStart, closeBrace)
                methods[pkgName] = methodText
                table.insert(order, pkgName)
                pos = closeBrace + 1
            else
                break
            end
        else
            -- 不是方法级注释，跳过
            pos = summaryEnd + 1
        end
    end

    return { methods = methods, order = order }
end

--- 生成 BindAll 方法体中的调用列表
---@param order table 包名数组
---@return string BindAll 方法体内的调用代码
function GenBinder:GenBindAllCalls(order)
    local lines = {}
    for _, pkg in ipairs(order) do
        table.insert(lines, Tool:StrFormat("\t\t\tBind%s();", pkg))
    end
    return table.concat(lines, "\n")
end

--- 从模板构建完整 CustomCompBind.cs 内容
---@param namespace string 命名空间
---@param methods table {[pkgName] = methodText}
---@param order table 包名数组
---@return string 完整的 CustomCompBind.cs 文件内容
function GenBinder:BuildCustomCompBind(namespace, methods, order)
    local templatePath = Tool:StrFormat("%s/Template/CustomCompBindTemplate.txt", Tool:PluginPath())
    local template = Tool:ReadTxt(templatePath)

    local bindAllCalls = GenBinder:GenBindAllCalls(order)
    local bindMethodsArr = {}
    for _, pkg in ipairs(order) do
        table.insert(bindMethodsArr, methods[pkg])
    end
    local bindMethodsText = table.concat(bindMethodsArr, "\n\n")

    template = template:gsub('#NAMESPACE#', namespace)
    template = template:gsub('#BIND_ALL_CALLS#', bindAllCalls)
    template = template:gsub('#BIND_METHODS#', bindMethodsText)

    return template
end

--- 新建 CustomCompBind.cs
---@param targetPath string 目标文件路径
---@param pkgName string 包名
---@param methodCode string BindXxx 方法代码
---@param namespace string 命名空间
function GenBinder:CreateCustomCompBind(targetPath, pkgName, methodCode, namespace)
    local methods = {}
    methods[pkgName] = methodCode
    local order = { pkgName }

    local content = GenBinder:BuildCustomCompBind(namespace, methods, order)
    Tool:WriteTxt(targetPath, content)
    Tool:Log("创建 CustomCompBind.cs，包含包: %s", pkgName)
end

--- 更新已有 CustomCompBind.cs（新增或替换当前包的方法）
---@param targetPath string 目标文件路径
---@param pkgName string 包名
---@param methodCode string BindXxx 方法代码
---@param namespace string 命名空间
function GenBinder:UpdateCustomCompBind(targetPath, pkgName, methodCode, namespace)
    local content = Tool:ReadTxt(targetPath)
    local parsed = GenBinder:ParseCustomCompBind(content)

    -- 新增或替换方法
    if not parsed.methods[pkgName] then
        table.insert(parsed.order, pkgName)
    end
    parsed.methods[pkgName] = methodCode

    -- 按字母排序
    table.sort(parsed.order)

    local newContent = GenBinder:BuildCustomCompBind(namespace, parsed.methods, parsed.order)
    Tool:WriteTxt(targetPath, newContent)
    Tool:Log("更新 CustomCompBind.cs，当前包含包: %s", table.concat(parsed.order, ", "))
end

--- 从 CustomCompBind.cs 中移除指定包的 BindXxx 方法
--- 当包中所有自定义组件被删除时调用
---@param targetPath string CustomCompBind.cs 文件路径
---@param pkgName string 包名
---@param namespace string 命名空间
function GenBinder:RemovePackageMethod(targetPath, pkgName, namespace)
    local content = Tool:ReadTxt(targetPath)
    local parsed = GenBinder:ParseCustomCompBind(content)

    if not parsed.methods[pkgName] then
        Tool:Log("CustomCompBind.cs 中不存在 Bind%s 方法，跳过清理", pkgName)
        return
    end

    -- 移除方法
    parsed.methods[pkgName] = nil
    for i, name in ipairs(parsed.order) do
        if name == pkgName then
            table.remove(parsed.order, i)
            break
        end
    end

    if #parsed.order == 0 then
        -- CustomCompBind 永久保留，清空 BindAll 和所有方法
        local newContent = GenBinder:BuildCustomCompBind(namespace, {}, {})
        Tool:WriteTxt(targetPath, newContent)
        Tool:Log("[清理] 已从 CustomCompBind.cs 中移除 Bind%s 方法，当前无任何绑定方法", pkgName)
    else
        local newContent = GenBinder:BuildCustomCompBind(namespace, parsed.methods, parsed.order)
        Tool:WriteTxt(targetPath, newContent)
        Tool:Log("[清理] 已从 CustomCompBind.cs 中移除 Bind%s 方法，当前包含包: %s", pkgName, table.concat(parsed.order, ", "))
    end
end

return GenBinder
