--- 组件的C#代码生成
---@class GenComp
local GenComp = {}

--- 生成组件的C#代码
---@param pkgName string 包名
---@param compClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 所有组件类
---@param AllClsMap table 所有界面与组件的Map--key-资源名称--value-资源对应的界面或组件
---@param unityDataPath string Unity工程路径 “xxx/Assets”
function GenComp:Gen(pkgName, compClsArray, AllClsMap, unityDataPath)
    if not compClsArray or #compClsArray == 0 then
        return
    end

    local exportGenPath = Tool:GetExportViewGenPath(pkgName) --- 导出ViewGen的C#代码路径
    local exportPath = Tool:GetExportViewPath(pkgName)       --- 导出View的C#代码路径

    for _, cls in ipairs(compClsArray) do
        -------------------------------------CompXxx.Gen.cs----------------------------------------
        Tool:Log("生成组件C#代码--%s", cls.resName .. ".Gen.cs")

        local dir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName)
        dir = dir .. "/Comp"
        Tool:CreateDirectory(dir) -- 创建存放代码的文件夹=>.../ViewGen/Comp

        local path = Tool:StrFormat('%s/%s.Gen.cs', dir, cls.resName)
        local compArray = Tool:GetCompArray(cls)
        local templatePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompGenTemplate.txt")
        local template = Tool:ReadTxt(templatePath) -- 读取模板代码

        -- 定义模板代码中需要填充的关键字
        local dataKeys = {
            '#CompDefine#', -- 界面包含的组件定义关键字
            '#CompInit#', -- 界面包含的组件初始化赋值关键字
            '#CustomCompInit#', -- 自定义组件的初始化Init函数代码
            '#INITUIEVENT#', -- 界面可交互组件事件初始化
        }

        local dataTable = {}
        for _, key in ipairs(dataKeys) do
            dataTable[key] = {}
        end

        GenCommon:GenCompURL(dataTable['#CompDefine#'], cls)-- 生成自定义组件的URL代码，如：public const string URL = "ui://mkasn9e4jo110";
        GenCommon:GenControllerDefine(dataTable['#CompDefine#'], cls)-- 生成控制器的定义代码和枚举定义，如：private Controller CtrlSelected;
        GenCommon:GenCompDefine(dataTable['#CompDefine#'], compArray, AllClsMap)-- 生成组件的定义代码，如：private GButton btnEnter;
        GenCommon:GenTransitionDefine(dataTable['#CompDefine#'], cls)-- 生成动效的定义代码，如：private Transition xxxAnim;

        GenCommon:GenControllerInit(dataTable['#CompInit#'], cls)-- 控制器的初始化赋值，如：CtrlSelected = UIView.GetController("CtrlSelected");
        GenCommon:GenCompInit(dataTable['#CompInit#'], compArray, AllClsMap)-- 常用组件的初始化赋值，如：btnLogin = (GButton)GetChild("_btnLogin");
        GenCommon:GenTransitionInit(dataTable['#CompInit#'], cls)-- 动效的初始化赋值，如：xxxAnim = UIView.GetTransition("xxxAnim");
        GenCommon:GenCustomCompInit(dataTable['#CustomCompInit#'], compArray, AllClsMap, true)--生成自定义组件的初始化Init函数代码：compXXX.Init(this)，注入该组件属于的界面View

        GenCommon:GenCompEvent(dataTable['#INITUIEVENT#'], compArray, AllClsMap)-- 生成组件的交互事件监听代码:AddUIListener(btnEnter.onClick, OnBtnEnterClick);
        GenCommon:GenCompListOnRender(dataTable['#INITUIEVENT#'], compArray, AllClsMap)-- 生成GList组件Item的渲染回调函数赋值：listPlayer.itemRenderer = OnShowListPlayerItem;

        -- 使用生成的代码替换模板代码中各个关键字
        for k, v in pairs(dataTable) do
            template = template:gsub(k, table.concat(v))
        end

        -- 替换包名，界面名
        template = template:gsub('#COMPNAME#', cls.resName)
        template = template:gsub('#COMPTYPE#', cls.superClassName)

        -- 写入替换完成后的代码文件WinXxx.Gen.cs
        Tool:WriteTxt(path, template)

        ------------------------------------------CompXxx.cs----------------------------------------------
        Tool:Log("生成组件C#代码--%s", cls.resName .. ".cs")

        dir = Tool:StrFormat(exportPath, unityDataPath, pkgName)
        dir = dir .. "/Comp"
        Tool:CreateDirectory(dir) -- 创建存放代码的文件夹=>.../ViewImpl/Comp
        path = Tool:StrFormat('%s/%s.cs', dir, cls.resName)

        if not Tool:IsFileExists(path) then
            local compArray1 = Tool:GetCompArray(cls)
            local templatePath1 = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompTemplate.txt")
            local template1 = Tool:ReadTxt(templatePath1) -- 读取模板代码

            -- 定义模板代码中需要填充的关键字
            local dataKeys1 = {
                '#HANDLER#', -- 交互事件处理函数关键子
            }

            local dataTable1 = {}
            for _, key in ipairs(dataKeys1) do
                dataTable1[key] = {}
            end

            -- 生成组件的交互事件处理函数代码，如:private void OnBtnEnterClick(EventContext ctx){}
            GenCommon:GenCompEventHandler(dataTable1['#HANDLER#'], compArray1, AllClsMap)

            -- 使用生成的代码替换模板代码中各个关键字
            for k, v in pairs(dataTable1) do
                template1 = template1:gsub(k, table.concat(v))
            end

            -- 替换包名，界面名
            template1 = template1:gsub('#COMPNAME#', cls.resName)
            template1 = template1:gsub('#COMPTYPE#', cls.superClassName)
            -- 写入替换完成后的代码文件WinXxx.cs
            Tool:WriteTxt(path, template1)
        end
    end
end

return GenComp