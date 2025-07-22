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

    --- 导出ViewGen的C#代码路径
    local exportGenPath = Tool:GetExportViewGenPath(pkgName)

    for _, _ in ipairs(compClsArray) do
        local dir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName)
        Tool:CreateDirectory(dir)

        local path = Tool:StrFormat('%s/%sBinder.cs', dir, pkgName)

        --- 读取代码模板文档
        local templatePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/BinderTemplate.txt")
        local template = Tool:ReadTxt(templatePath)

        -- 处理模板中的组件绑定部分
        template = GenBinder:BinderComps(template, compClsArray)

        ---替换模板中的占位符：包名，组件名，类型
        template = template:gsub('#PKGNAME#', pkgName)

        -- 写入最终生成的代码文件
        Tool:WriteTxt(path, template)
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