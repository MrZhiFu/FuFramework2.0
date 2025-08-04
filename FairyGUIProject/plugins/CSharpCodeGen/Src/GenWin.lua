--- 界面的C#代码生成
---@class GenWin
local GenWin = {}

--- 生成界面的C#代码
---注意：界面逻辑代码WinXxx.cs不会多次生成，只会在首次导出时生成一次, 而界面非逻辑代码WinXxx.Gen.cs会在每次导出时重新生成。
---@param pkgName string 包名
---@param winClsArray CS.FairyEditor.PublishHandler.ClassInfo[] 所有界面类
---@param AllClsMap table 所有界面与组件的Map--key-资源名称--value-资源对应的界面或组件
---@param unityDataPath string Unity工程路径 “xxx/Assets”
function GenWin:Gen(pkgName, winClsArray, AllClsMap, unityDataPath)
    if not winClsArray or #winClsArray == 0 then
        return
    end

    local exportGenPath = Tool:GetExportCodeGenPath(pkgName) --- 导出ViewGen的C#代码路径
    local exportPath = Tool:GetExportCodePath(pkgName)       --- 导出View的C#代码路径
    local namespace = Tool:GetExportCodeNamespace(pkgName)   --- 导出View的C#代码命名空间

    for _, winCls in ipairs(winClsArray) do
        -------------------------------------WinXxx.Gen.cs----------------------------------------
        Tool:Log("生成界面C#代码----%s.Gen.cs", winCls.resName)

        local targetDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName)

        -- 创建存放代码的文件夹=>.../ViewGen
        Tool:CreateDirectory(targetDir)

        local targetPath = Tool:StrFormat('%s/%s.Gen.cs', targetDir, winCls.resName) --- 界面代码生成目标路径
        local compArray = Tool:GetCompArray(winCls)

        local templateCodeGenPath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/WinGenTemplate.txt")
        local templateCodeGen = Tool:ReadTxt(templateCodeGenPath)  -- 读取模板代码

        -- 定义模板代码中需要填充的关键字
        local dataKeys = {
            '#CompDefine#', -- 界面包含的组件定义关键字
            '#CompInit#', -- 界面包含的组件初始化赋值关键字
            '#CustomCompInit#', -- 自定义组件的初始化Init函数代码
            '#INITUIEVENT#', -- 界面可交互组件事件初始化
        }

        -- 定义关键字对应的填充内容字典
        local dataTable = {}
        for _, key in ipairs(dataKeys) do
            dataTable[key] = {}
        end

        GenCommon:GenControllerDefine(dataTable['#CompDefine#'], winCls)-- 生成控制器的定义代码和枚举定义，如：private Controller CtrlSelected;
        GenCommon:GenCompDefine(dataTable['#CompDefine#'], compArray, AllClsMap)-- 生成组件的定义代码，如：private GButton btnEnter;
        GenCommon:GenTransitionDefine(dataTable['#CompDefine#'], winCls)           -- 生成动效的定义代码，如：private Transition xxxAnim;

        GenCommon:GenControllerInit(dataTable['#CompInit#'], winCls)-- 控制器的初始化赋值，如：CtrlSelected = UIView.GetController("CtrlSelected");
        GenCommon:GenCompInit(dataTable['#CompInit#'], compArray, AllClsMap)-- 常用组件的初始化赋值，如：btnLogin = (GButton)GetChild("_btnLogin");
        GenCommon:GenTransitionInit(dataTable['#CompInit#'], winCls)-- 动效的初始化赋值，如：xxxAnim = UIView.GetTransition("xxxAnim");
        --GenCommon:GenCustomCompInit(dataTable['#CustomCompInit#'], compArray, AllClsMap, false)--生成自定义组件的初始化Init函数代码：compXXX.Init(this)，注入该组件属于的界面View

        GenCommon:GenCompEvent(dataTable['#INITUIEVENT#'], compArray, AllClsMap)-- 生成组件的交互事件监听代码:AddUIListener(btnEnter.onClick, OnBtnEnterClick);
        GenCommon:GenCompListOnRender(dataTable['#INITUIEVENT#'], compArray, AllClsMap)-- 生成GList组件Item的渲染回调函数赋值：listPlayer.itemRenderer = OnShowListPlayerItem;

        -- 使用生成的代码替换模板代码中各个关键字
        for k, v in pairs(dataTable) do
            templateCodeGen = templateCodeGen:gsub(k, table.concat(v))
        end

        -- 替换命名空间，包名，界面名
        templateCodeGen = templateCodeGen:gsub('#NAMESPACE#', namespace)
        templateCodeGen = templateCodeGen:gsub('#PKGNAME#', pkgName)
        templateCodeGen = templateCodeGen:gsub('#WINNAME#', winCls.resName)

        -- 写入替换完成后的代码文件WinXxx.Gen.cs
        Tool:WriteTxt(targetPath, templateCodeGen)

        -------------------------------------WinXxx.cs----------------------------------------
        Tool:Log("生成界面逻辑C#代码----%s.cs", winCls.resName)

        targetDir = Tool:StrFormat(exportPath, unityDataPath, pkgName)
        targetPath = Tool:StrFormat('%s/%s.cs', targetDir, winCls.resName)

        -- 如果界面逻辑代码文件存在，则不再生成
        if Tool:IsFileExists(targetPath) then
            Tool:Log("界面代码文件%s已存在，不再生成", winCls.resName)
            return
        end

        -- 创建存放代码的文件夹=>.../ViewImpl
        Tool:CreateDirectory(targetDir)

        -- 如果设置为导出，则生成界面代码文件WinXxx.cs
        if winCls.res.exported then
            local templateCodePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/WinTemplate.txt")
            local templateCode = Tool:ReadTxt(templateCodePath)  -- 读取模板代码

            local dataKeys1 = {
                '#HANDLER#', -- 交互事件处理函数关键子
            }

            local dataTable1 = {}
            for _, key in ipairs(dataKeys1) do
                dataTable1[key] = {}
            end

            -- 生成组件的交互事件处理函数代码，如:	private void OnBtnEnterClick(EventContext ctx){}
            GenCommon:GenCompEventHandler(dataTable1['#HANDLER#'], compArray, AllClsMap, templateCode)

            -- 使用生成的代码替换模板代码中各个关键字
            for k, v in pairs(dataTable1) do
                templateCode = templateCode:gsub(k, table.concat(v))
            end

            -- 替换命名空间，包名，界面名
            templateCode = templateCode:gsub('#NAMESPACE#', namespace)
            templateCode = templateCode:gsub('#PKGNAME#', pkgName)
            templateCode = templateCode:gsub('#WINNAME#', winCls.resName)

            -- 写入替换完成后的代码文件WinXxx.cs
            Tool:WriteTxt(targetPath, templateCode)
        end
    end
end

return GenWin