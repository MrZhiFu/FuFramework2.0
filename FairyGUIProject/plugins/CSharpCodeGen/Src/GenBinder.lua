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

return GenBinder