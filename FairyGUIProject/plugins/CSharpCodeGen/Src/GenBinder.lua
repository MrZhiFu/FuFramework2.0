--- 生成组件绑定的C#代码
---@class GenBinder
local GenBinder = {}

--- 生成组件绑定C#代码（汇总到 CustomCompBind.cs）
---@param pkgName string 包名
---@param compClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 组件数组
---@param unityDataPath string Unity路径 "xxx/Assets"
function GenBinder:Gen(pkgName, compClsArray, unityDataPath)
    if not compClsArray or #compClsArray == 0 then
        return
    end

    local exportGenPath = Tool:GetExportCodeGenPath(pkgName)
    local isLauncher = tostring(pkgName) == "Launcher"
    local namespace = Tool:GetExportCodeNamespace(pkgName)

    -- CustomCompBind.cs 放在 UI 目录根下（不区分包子目录）
    local customCompBindDir
    if isLauncher then
        customCompBindDir = Tool:StrFormat("%s/Scripts/AOT/Bootstrap/UI", unityDataPath)
    else
        -- exportGenPath 为 "%s/Scripts/Hotfix/Game/AutoGen/UI/%s/"
        -- 格式化为空字符串去掉末尾 %s/，再清理多余斜杠
        customCompBindDir = Tool:StrFormat(exportGenPath, unityDataPath, ""):gsub("/+$", "")
    end

    Tool:CreateDirectory(customCompBindDir)
    local targetPath = Tool:StrFormat("%s/CustomCompBind.cs", customCompBindDir)

    -- 生成当前包的 BindXxx 方法
    local methodCode = GenBinder:GenBindMethod(pkgName, compClsArray)

    -- 创建或更新 CustomCompBind.cs
    if Tool:IsFileExists(targetPath) then
        GenBinder:UpdateCustomCompBind(targetPath, pkgName, methodCode, namespace)
    else
        GenBinder:CreateCustomCompBind(targetPath, pkgName, methodCode, namespace)
    end

    -- 删除旧的 xxxBinder.cs
    GenBinder:DeleteOldBinder(pkgName, exportGenPath, unityDataPath, isLauncher)

    -- 自动更新 Launcher 中的调用（XxxBinder.BindAll → CustomCompBind.BindAll）
    GenBinder:UpdateLauncherBinder(pkgName, unityDataPath)
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

    local hasExported = false
    for _, cls in ipairs(compClsArray) do
        if cls.res.exported then
            table.insert(lines, Tool:StrFormat("\t\t\tUIObjectFactory.SetPackageItemExtension(%s.URL, typeof(%s));", cls.resName, cls.resName))
            hasExported = true
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
        if not summaryStart then break end

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

    return {methods = methods, order = order}
end

--- 生成 BindAll 方法体中的调用列表
---@param order table 包名数组
---@return string
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
---@return string
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
    local order = {pkgName}

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

--- 删除旧的 xxxBinder.cs 文件
---@param pkgName string 包名
---@param exportGenPath string 导出路径格式串
---@param unityDataPath string Unity路径
---@param isLauncher boolean
function GenBinder:DeleteOldBinder(pkgName, exportGenPath, unityDataPath, isLauncher)
    local oldBinderPath
    if isLauncher then
        oldBinderPath = Tool:StrFormat("%s/Scripts/AOT/Bootstrap/UI/%sBinder.cs", unityDataPath, pkgName)
    else
        local oldBinderDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName)
        oldBinderPath = Tool:StrFormat("%s/%sBinder.cs", oldBinderDir, pkgName)
    end

    if Tool:IsFileExists(oldBinderPath) then
        CS.System.IO.File.Delete(oldBinderPath)
        Tool:Log("已删除旧 Binder 文件: %s", oldBinderPath)

        -- 同时删除 .meta 文件
        local metaPath = oldBinderPath .. ".meta"
        if Tool:IsFileExists(metaPath) then
            CS.System.IO.File.Delete(metaPath)
        end
    end
end

--- 自动更新 Launcher 中的绑定调用
--- 对于 Launcher 包：更新 BootstrapProcess.cs (AOT)
--- 对于其他包：更新 HotfixLauncher.cs (Hotfix)
---@param pkgName string 包名
---@param unityDataPath string Unity路径 "xxx/Assets"
function GenBinder:UpdateLauncherBinder(pkgName, unityDataPath)
    local isLauncher = tostring(pkgName) == "Launcher"
    local launcherPath
    local launcherName

    if isLauncher then
        launcherPath = Tool:StrFormat("%s/Scripts/AOT/Bootstrap/BootstrapProcess.cs", unityDataPath)
        launcherName = "BootstrapProcess.cs"
    else
        launcherPath = Tool:StrFormat("%s/Scripts/Hotfix/HotfixLauncher.cs", unityDataPath)
        launcherName = "HotfixLauncher.cs"
    end

    if not Tool:IsFileExists(launcherPath) then
        Tool:Warning("%s 不存在，跳过自动更新", launcherName)
        return
    end

    local content = Tool:ReadTxt(launcherPath)

    if isLauncher then
        -- AOT：将 LauncherBinder.BindAll() 替换为 CustomCompBind.BindAll()
        if content:find("CustomCompBind.BindAll()", 1, true) then
            Tool:Log("%s 中已存在 CustomCompBind.BindAll()，跳过", launcherName)
            return
        end
        local newContent, count = content:gsub("LauncherBinder%.BindAll%(%)", "CustomCompBind.BindAll()")
        if count > 0 then
            Tool:WriteTxt(launcherPath, newContent)
            Tool:Log("已将 %s 中的 LauncherBinder.BindAll() 替换为 CustomCompBind.BindAll()", launcherName)
        else
            Tool:Warning("未能在 %s 中找到 LauncherBinder.BindAll()，请手动替换", launcherName)
        end
    else
        -- Hotfix：确保 CustomCompBind.BindAll() 存在，清理旧调用
        if content:find("CustomCompBind.BindAll()", 1, true) then
            -- 已迁移，只需确保旧调用被清理
            local cleaned, count = content:gsub("[ \t]*%w+Binder%.BindAll%(%)%s*\n", "")
            if count > 0 then
                Tool:WriteTxt(launcherPath, cleaned)
                Tool:Log("已清理 %s 中的 %d 条旧 Binder 调用", launcherName, count)
            else
                Tool:Log("%s 中已存在 CustomCompBind.BindAll()，跳过", launcherName)
            end
            return
        end

        -- 清理旧的 XxxBinder.BindAll() 调用
        local cleaned, oldCount = content:gsub("[ \t]*%w+Binder%.BindAll%(%)%s*\n", "")

        -- 同时清理遗留的 BindCustomComps 空壳方法
        cleaned = cleaned:gsub("\n[ \t]*//@formatter:off[^\n]*\n[ \t]*///[^\n]*\n[ \t]*///[^\n]*\n[ \t]*///[^\n]*\n[ \t]*///[^\n]*\n[ \t]*private static void BindCustomComps%(%)%s*\n[ \t]*{%s*\n[ \t]*CustomCompBind%.BindAll%(%)%s*;%s*\n[ \t]*}%s*\n[ \t]*//@formatter:on%s*\n", "")
        cleaned = cleaned:gsub("[ \t]*BindCustomComps%(%)%s*;%s*\n", "")

        -- 找到适合插入的位置（"绑定...自定义组件" 注释行之后，或 LoadUIAsync 之后）
        local insertPos = nil
        local marker = cleaned:find("绑定.*Fui.*自定义组件")
        if marker then
            -- 找到该注释所在行的末尾
            local lineEnd = cleaned:find("\n", marker)
            if lineEnd then
                -- 如果下一行是 BindCustomComps(); 则替换它，否则在之后插入
                local nextLine = cleaned:sub(lineEnd + 1)
                if nextLine:match("^[ \t]*CustomCompBind.BindAll") then
                    -- 已经存在（不太可能但检查一下）
                    insertPos = nil
                else
                    insertPos = lineEnd
                end
            end
        end

        if insertPos then
            local newContent = cleaned:sub(1, insertPos) .. "\n            CustomCompBind.BindAll();" .. cleaned:sub(insertPos + 1)
            Tool:WriteTxt(launcherPath, newContent)
            Tool:Log("已在 %s 中插入 CustomCompBind.BindAll()（清理 %d 条旧调用）", launcherName, oldCount)
        elseif oldCount > 0 then
            -- 有旧调用被清理但没找到插入位置，直接替换第一个旧调用的位置
            Tool:WriteTxt(launcherPath, cleaned)
            Tool:Warning("已在 %s 中清理 %d 条旧 Binder 调用，但未找到合适的插入位置，请手动添加 CustomCompBind.BindAll()", launcherName, oldCount)
        else
            Tool:Warning("未能在 %s 中找到旧 Binder 调用或绑定注释，请手动添加 CustomCompBind.BindAll()", launcherName)
        end
    end
end

return GenBinder
