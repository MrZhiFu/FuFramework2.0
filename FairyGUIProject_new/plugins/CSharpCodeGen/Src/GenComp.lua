--- 组件的C#代码生成
---@class GenComp
local GenComp = {}

--- 生成组件的C#代码
---@param pkgName string 包名
---@param compClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 所有组件类
---@param unityDataPath string Unity工程路径 “xxx/Assets”
---@param AllClsMap talbe 所有界面与组件的Map--key-资源名称--value-资源对应的界面或组件
function GenComp:Gen(pkgName, compClsArray, unityDataPath, AllClsMap)
    fprintf("生成组件的C#代码 %s %s %s", pkgName, compClsArray, unityDataPath)
    for _, cls in ipairs(compClsArray) do
        local dir = Tool:StrFormat(Tool.ExportViewGenPath, unityDataPath, pkgName)
        dir = dir .. "/Comp"
        Tool:CreateDirectory(dir)

        -------------------------------------CompXxx.Gen.cs----------------------------------------
        local path = Tool:StrFormat('%s/%s.Gen.cs', dir, cls.resName)
        local compArray = Tool:GetCompArray(cls)
        local templatePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompGenTemplate.txt")
        local content = Tool:ReadTxt(templatePath)

        local dataKeys = {
            '#CompDefine#',
            '#CompInit#',
            '#INITUIEVENT#',
        }

        local dataTable = {}
        for _, key in ipairs(dataKeys) do
            dataTable[key] = {}
        end

        GenCommon:GenCompURL(dataTable['#CompDefine#'], cls)
        GenCommon:GenControllerDefine(dataTable['#CompDefine#'], cls, AllClsMap)
        GenCommon:GenCompDefine(dataTable['#CompDefine#'], compArray, AllClsMap)
        GenCommon:GenTransitionDefine(dataTable['#CompDefine#'], cls, AllClsMap)

        GenCommon:GenControllerInit(dataTable['#CompInit#'], cls, AllClsMap)
        GenCommon:GenCompInit(dataTable['#CompInit#'], compArray, AllClsMap)
        GenCommon:GenTransitionInit(dataTable['#CompInit#'], cls, AllClsMap)

        GenCommon:GenCompEvent(dataTable['#INITUIEVENT#'], compArray, AllClsMap)
        GenCommon:GenCompOperationn(dataTable['#INITUIEVENT#'], compArray, AllClsMap)

        for k, v in pairs(dataTable) do
            content = content:gsub(k, table.concat(v))
        end

        content = content:gsub('#COMPNAME#', cls.resName)
        content = content:gsub('#COMPTYPE#', cls.superClassName)

        Tool:WriteTxt(path, content)

        ------------------------------------------CompXxx.cs----------------------------------------------
        dir = Tool:StrFormat(Tool.ExportViewPath, unityDataPath, pkgName)
        dir = dir .. "/Comp"
        Tool:CreateDirectory(dir)
        path = Tool:StrFormat('%s/%s.cs', dir, cls.resName)

        if not Tool:IsFileExists(path) then
            local compArray = Tool:GetCompArray(cls)

            local templatePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompTemplate.txt")
            local content = Tool:ReadTxt(templatePath)

            local dataKeys = {
                '#HANDLER#',
            }

            local dataTable = {}
            for _, key in ipairs(dataKeys) do
                dataTable[key] = {}
            end

            GenCommon:GenCompEventHandler(dataTable['#HANDLER#'], compArray, AllClsMap)

            for k, v in pairs(dataTable) do
                content = content:gsub(k, table.concat(v))
            end

            content = content:gsub('#COMPNAME#', cls.resName)
            content = content:gsub('#COMPTYPE#', cls.superClassName)

            Tool:WriteTxt(path, content)
        end
    end
end

return GenComp