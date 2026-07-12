--- 生成组件绑定的C#代码
---@class GenBinder
local GenBinder = {}

--- 生成组件绑定C#代码
---@param pkgName string 包名
---@param compClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 组件数组
---@param unityDataPath string Unity路径 “xxx/Assets”
function GenBinder:Gen(pkgName, compClsArray, unityDataPath)
    if not compClsArray or #compClsArray == 0 then
        return
    end

    local exportGenPath = Tool:GetExportCodeGenPath(pkgName)--- 导出ViewGen的C#代码路径
    local targetDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName) --- 导出ViewGen的C#代码目录
    local namespace = Tool:GetExportCodeNamespace(pkgName)   --- 导出View的C#代码命名空间
    Tool:CreateDirectory(targetDir)

    for _, _ in ipairs(compClsArray) do

        local targetPath = Tool:StrFormat('%s/%sBinder.cs', targetDir, pkgName)

        --- 读取代码模板文档
        local templateCodePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/BinderTemplate.txt")
        local templateCode = Tool:ReadTxt(templateCodePath)

        -- 处理模板中的组件绑定部分
        templateCode = GenBinder:BinderComps(templateCode, compClsArray)

        -- 替换命名空间，包名，界面名
        templateCode = templateCode:gsub('#NAMESPACE#', namespace)
        templateCode = templateCode:gsub('#PKGNAME#', pkgName)

        -- 写入最终生成的代码文件
        Tool:WriteTxt(targetPath, templateCode)
    end

    -- 自动更新 HotfixLauncher.cs 或 ProcedureLauncher.cs 中的 BindCustomComps 方法
    GenBinder:UpdateLauncherBinder(pkgName, unityDataPath)
end

--- 生成组件绑定代码内容
---@param content string 原始模板内容
---@param compClsArray table 组件类信息数组
---@return string 处理后的内容
function GenBinder:BinderComps(content, compClsArray)
    if not compClsArray or #compClsArray == 0 then
        return
    end

    local strContent = ""
    local arrStr = {}
    for _, cls in ipairs(compClsArray) do
        if cls.res.exported then
            table.insert(arrStr, "\t\t\tUIObjectFactory.SetPackageItemExtension(")
            local comDef = string.format("%s.URL, typeof(%s));\n", cls.resName, cls.resName)
            table.insert(arrStr, comDef)
        end
        strContent = table.concat(arrStr)
    end
    return content:gsub('#BinderComps#', strContent)
end

--- 自动更新 Launcher 中的 BindCustomComps 方法
--- 如果是 Launcher 包，更新 ProcedureLauncher.cs（AOT）
--- 如果是其他包，更新 HotfixLauncher.cs（Hotfix）
---@param pkgName string 包名
---@param unityDataPath string Unity路径 "xxx/Assets"
function GenBinder:UpdateLauncherBinder(pkgName, unityDataPath)
    local isLauncher = tostring(pkgName) == "Launcher"
    local launcherPath
    local launcherName

    if isLauncher then
        -- AOT 代码路径
        launcherPath = Tool:StrFormat("%s/Scripts/AOT/Procedure/ProcedureLauncher.cs", unityDataPath)
        launcherName = "ProcedureLauncher.cs"
    else
        -- Hotfix 代码路径
        launcherPath = Tool:StrFormat("%s/Scripts/Hotfix/HotfixLauncher.cs", unityDataPath)
        launcherName = "HotfixLauncher.cs"
    end

    -- 检查文件是否存在
    if not Tool:IsFileExists(launcherPath) then
        Tool:Log("%s 不存在，跳过自动更新: %s", launcherName, launcherPath)
        return
    end

    Tool:Log("更新 %s 中的 BindCustomComps 方法...", launcherName)

    local content = Tool:ReadTxt(launcherPath)
    local binderCall = Tool:StrFormat("%sBinder.BindAll();", pkgName)

    -- 检查是否已存在该绑定代码（使用纯文本查找，不使用正则）
    if string.find(content, binderCall, 1, true) then
        Tool:Log("BindCustomComps 中已存在 %s 的绑定代码，跳过", binderCall)
        return
    end

    -- 查找 BindCustomComps 方法体，在 { 之后插入代码
    -- 匹配 private static void BindCustomComps() 后面跟着 { 和换行
    local pattern = "(private static void BindCustomComps%(%)%s*\n?%s*{%s*\n)"
    local replacement = "%1            " .. binderCall .. "\n"

    local newContent, count = content:gsub(pattern, replacement)

    if count > 0 then
        Tool:WriteTxt(launcherPath, newContent)
        Tool:Log("成功在 BindCustomComps 中添加 %s", binderCall)
        return
    end

    -- 尝试匹配没有换行的情况: private static void BindCustomComps() {
    pattern = "(private static void BindCustomComps%(%)%s*{%s*\n)"
    replacement = "%1            " .. binderCall .. "\n"
    newContent, count = content:gsub(pattern, replacement)

    if count > 0 then
        Tool:WriteTxt(launcherPath, newContent)
        Tool:Log("成功在 BindCustomComps 中添加 %s", binderCall)
        return
    end

    -- 尝试匹配方法体在 { 后面没有换行的情况
    pattern = "(private static void BindCustomComps%(%)%s*{%s*)"
    replacement = "%1\n            " .. binderCall .. "\n        "
    newContent, count = content:gsub(pattern, replacement)

    if count > 0 then
        Tool:WriteTxt(launcherPath, newContent)
        Tool:Log("成功在 BindCustomComps 中添加 %s", binderCall)
        return
    end

    Tool:Warning("未能在 %s 中找到 BindCustomComps 方法，请手动添加 %s", launcherName, binderCall)
end

return GenBinder