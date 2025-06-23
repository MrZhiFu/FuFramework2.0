--- 界面的C#代码生成
---@class GenWin
local GenWin = {}

--- 生成界面的C#代码
---@param pkgName string 包名
---@param winClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 所有界面类
---@param unityDataPath string Unity工程路径 “xxx/Assets”
---@param AllClsMap talbe 所有界面与组件的Map--key-资源名称--value-资源对应的界面或组件
function GenWin:Gen(pkgName, winClsArray, unityDataPath, AllClsMap)
    fprintf("生成界面的C#代码  %s %s", pkgName, winClsArray, unityDataPath)
    for _, cls in ipairs(winClsArray) do
        local dir = Tool:StrFormat(Tool.ExportViewGenPath, unityDataPath, pkgName)

        Tool:CreateDirectory(dir)

        -------------------------------------WinXxx.Gen.cs----------------------------------------
        local path = Tool:StrFormat('%s/%s.Gen.cs', dir, cls.resName)
        
        local compArray = Tool:GetCompArray(cls)
        local templatePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/WinGenTemplate.txt")
        local content = Tool:ReadTxt(templatePath)
        local dataKeys = {
            '#CompDefine#',
            '#CompInit#',
            '#CompDoInit#',
            '#INITUIEVENT#',
        }
        local dataTable = {}
        for _,key in ipairs(dataKeys) do
            dataTable[key] = {}
        end
        
        GenCommon:GenControllerDefine(dataTable['#CompDefine#'], cls, AllClsMap)
        GenCommon:GenCompDefine(dataTable['#CompDefine#'], compArray, AllClsMap)
        GenCommon:GenTransitionDefine(dataTable['#CompDefine#'], cls, AllClsMap)

        GenCommon:GenControllerInit(dataTable['#CompInit#'], cls, AllClsMap)
        GenCommon:GenCompInit(dataTable['#CompInit#'], compArray, AllClsMap)
        GenCommon:GenTransitionInit(dataTable['#CompInit#'], cls, AllClsMap)
        GenCommon:GenDoInit(dataTable['#CompDoInit#'], compArray, AllClsMap)
         
        GenCommon:GenCompEvent(dataTable['#INITUIEVENT#'], compArray, AllClsMap)
        GenCommon:GenCompOperationn(dataTable['#INITUIEVENT#'], compArray, AllClsMap)

        for k,v in pairs(dataTable) do
            content = content:gsub(k, table.concat(v))
        end
        content = content:gsub('#PKGNAME#', pkgName)
        content = content:gsub('#WINNAME#', cls.resName)
        
        Tool:WriteTxt(path, content)

        -------------------------------------WinXxx.cs----------------------------------------
        dir = Tool:StrFormat(Tool.ExportViewPath, unityDataPath, pkgName)
        Tool:CreateDirectory(dir)
        path = Tool:StrFormat('%s/%s.cs', dir, cls.resName)
        if cls.res.exported and not Tool:IsFileExists(path) then
            local templatePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/WinTemplate.txt")
            local content = Tool:ReadTxt(templatePath)

            local dataKeys = {
                '#HANDLER#',
            }

            local dataTable = {}
            for _,key in ipairs(dataKeys) do
                dataTable[key] = {}
            end

            GenCommon:GenCompEventHandler(dataTable['#HANDLER#'], compArray, AllClsMap)

            for k,v in pairs(dataTable) do
                content = content:gsub(k, table.concat(v))
            end
            
            --- 替换包名，界面名
            content = content:gsub('#PKGNAME#', pkgName)
            content = content:gsub('#WINNAME#', cls.resName)
            
            Tool:WriteTxt(path, content)
        end
    end
end

return GenWin