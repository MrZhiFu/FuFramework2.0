--- 组件的C#代码生成
---@class GenComp
local GenComp = {}

--- 生成组件的C#代码
---注意：组件逻辑代码CompXxx.cs不会多次生成，只会在首次导出时生成一次, 而组件非逻辑代码CompXxx.Gen.cs会在每次导出时重新生成。
---@param pkgName string 包名
---@param compClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 所有组件类
---@param AllClsMap table 所有组件与组件的Map--key-资源名称--value-资源对应的组件或组件
---@param unityDataPath string Unity工程路径 “xxx/Assets”
function GenComp:Gen(pkgName, compClsArray, AllClsMap, unityDataPath)
    if not compClsArray or #compClsArray == 0 then
        return
    end

    local exportGenPath = Tool:GetExportCodeGenPath(pkgName) --- 导出ViewGen的C#代码路径
    local exportPath = Tool:GetExportCodePath(pkgName)       --- 导出View的C#代码路径
    local namespace = Tool:GetExportCodeNamespace(pkgName)   --- 导出View的C#代码命名空间

    for _, compCls in ipairs(compClsArray) do
        -------------------------------------CompXxx.Gen.cs----------------------------------------
        Tool:Log("生成组件C#代码----%s", compCls.resName .. ".Gen.cs")

        local targetDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName) .. "/Comp"

        -- 创建存放代码的文件夹=>.../ViewGen/Comp
        Tool:CreateDirectory(targetDir)

        local targetPath = Tool:StrFormat('%s/%s.Gen.cs', targetDir, compCls.resName)
        local compArray = Tool:GetCompArray(compCls)
        local templateCodeGenPath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompGenTemplate.txt")
        local templateCodeGen = Tool:ReadTxt(templateCodeGenPath) -- 读取模板代码

        -- 定义模板代码中需要填充的关键字
        local dataKeys = {
            '#CompDefine#', -- 组件包含的组件定义关键字
            '#CompInit#', -- 组件包含的组件初始化赋值关键字
            '#CustomCompInit#', -- 自定义组件的初始化Init函数代码
            '#INITUIEVENT#', -- 组件可交互组件事件初始化
        }

        ---@type table<string, string[]>  key-模板代码关键字, value-生成的代码数组
        local dataDict = {}
        for _, key in ipairs(dataKeys) do
            dataDict[key] = {}
        end

        GenCommon:GenCompURL(dataDict['#CompDefine#'], compCls)-- 生成自定义组件的URL代码，如：public const string URL = "ui://mkasn9e4jo110";
        GenCommon:GenControllerDefine(dataDict['#CompDefine#'], compCls)-- 生成控制器的定义代码和枚举定义，如：private Controller CtrlSelected;
        GenCommon:GenCompDefine(dataDict['#CompDefine#'], compArray, AllClsMap)-- 生成组件的定义代码，如：private GButton btnEnter;
        GenCommon:GenTransitionDefine(dataDict['#CompDefine#'], compCls)-- 生成动效的定义代码，如：private Transition xxxAnim;

        GenCommon:GenControllerInit(dataDict['#CompInit#'], compCls)-- 控制器的初始化赋值，如：CtrlSelected = UIView.GetController("CtrlSelected");
        GenCommon:GenCompInit(dataDict['#CompInit#'], compArray, AllClsMap)-- 常用组件的初始化赋值，如：btnLogin = (GButton)GetChild("_btnLogin");
        GenCommon:GenTransitionInit(dataDict['#CompInit#'], compCls)-- 动效的初始化赋值，如：xxxAnim = UIView.GetTransition("xxxAnim");
        --GenCommon:GenCustomCompInit(dataDict['#CustomCompInit#'], compArray, AllClsMap, true)--生成自定义组件的初始化Init函数代码：compXXX.Init(this)，注入该组件属于的组件View

        GenCommon:GenCompEvent(dataDict['#INITUIEVENT#'], compArray, AllClsMap)-- 生成组件的交互事件监听代码:AddUIListener(btnEnter.onClick, OnBtnEnterClick);
        GenCommon:GenCompListOnRender(dataDict['#INITUIEVENT#'], compArray, AllClsMap)-- 生成GList组件Item的渲染回调函数赋值：listPlayer.itemRenderer = OnShowListPlayerItem;

        -- 使用生成的代码替换模板代码中各个关键字
        for k, v in pairs(dataDict) do
            templateCodeGen = templateCodeGen:gsub(k, table.concat(v))
        end

        -- 替换命名空间，包名，组件名
        templateCodeGen = templateCodeGen:gsub('#NAMESPACE#', namespace)
        templateCodeGen = templateCodeGen:gsub('#COMPNAME#', compCls.resName)
        templateCodeGen = templateCodeGen:gsub('#COMPTYPE#', compCls.superClassName)

        -- 写入替换完成后的代码文件WinXxx.Gen.cs
        Tool:WriteTxt(targetPath, templateCodeGen)

        ------------------------------------------CompXxx.cs----------------------------------------------
        Tool:Log("生成组件逻辑C#代码----%s", compCls.resName .. ".cs")

        targetDir = Tool:StrFormat(exportPath, unityDataPath, pkgName) .. "/Comp"
        targetPath = Tool:StrFormat('%s/%s.cs', targetDir, compCls.resName)

        -- 如果组件逻辑代码文件存在，则不再生成
        if Tool:IsFileExists(targetPath) then
            Tool:Log("组件代码文件%s已存在，不再生成", compCls.resName)
            goto continue
        end

        -- 创建存放代码的文件夹=>.../ViewImpl/Comp
        Tool:CreateDirectory(targetDir)

        local templateCodePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompTemplate.txt")
        local templateCode = Tool:ReadTxt(templateCodePath) -- 读取模板代码

        -- 定义模板代码中需要填充的关键字
        local dataKeys1 = {
            '#HANDLER#', -- 交互事件处理函数关键子
        }

        local dataTable1 = {}
        for _, key in ipairs(dataKeys1) do
            dataTable1[key] = {}
        end

        -- 生成组件的交互事件处理函数代码，如:private void OnBtnEnterClick(EventContext ctx){}
        GenCommon:GenCompEventHandler(dataTable1['#HANDLER#'], compArray, AllClsMap)

        -- 使用生成的代码替换模板代码中各个关键字
        for k, v in pairs(dataTable1) do
            templateCode = templateCode:gsub(k, table.concat(v))
        end

        -- 替换命名空间，包名，组件名
        templateCode = templateCode:gsub('#NAMESPACE#', namespace)
        templateCode = templateCode:gsub('#COMPNAME#', compCls.resName)
        templateCode = templateCode:gsub('#COMPTYPE#', compCls.superClassName)

        -- 写入替换完成后的代码文件WinXxx.cs
        Tool:WriteTxt(targetPath, templateCode)
        :: continue ::
    end
end

return GenComp