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
    local exportGenPath = Tool:GetExportCodeGenPath(pkgName) --- 导出ViewGen的C#代码路径
    local exportPath = Tool:GetExportCodePath(pkgName)       --- 导出View的C#代码路径
    local namespace = Tool:GetExportCodeNamespace(pkgName)   --- 导出View的C#代码命名空间

    local compSubDir = "/Comp"

    -- Gen 和 Cs 的基础路径不同：Gen → AutoGen/UI/{pkg}, Cs → Game/UI/{pkg}
    local targetGenDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName) .. compSubDir
    local targetCsDir = Tool:StrFormat(exportPath, unityDataPath, pkgName) .. compSubDir

    if compClsArray and #compClsArray > 0 then
        Tool:CreateDirectory(targetGenDir)  -- 确保 Gen 目录存在

        for _, compCls in ipairs(compClsArray) do
            -------------------------------------CompXxx.Gen.cs----------------------------------------
            Tool:Log("生成组件C#代码----%s", compCls.resName .. ".Gen.cs")

            local targetPath = Tool:StrFormat('%s/%s.Gen.cs', targetGenDir, compCls.resName)
            local compArray = Tool:GetCompArray(compCls)

            local templateName = "Template/CompGenTemplate.txt"
            local templateCodeGenPath = Tool:StrFormat("%s/%s", Tool:PluginPath(), templateName)
            local templateCodeGen = Tool:ReadTxt(templateCodeGenPath) -- 读取模板代码

            -- 定义模板代码中需要填充的关键字
            local dataKeys = {
                '#CompDefine#', -- 组件包含的组件定义关键字（拆分后不再使用）
                '#FieldDefine#', -- 字段声明
                '#EnumAndMethodDefine#', -- 枚举定义与 SetController 方法
                '#CompInit#', -- 组件包含的组件初始化赋值关键字
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

            GenCommon:GenControllerInit(dataDict['#CompInit#'], compCls)-- 控制器的初始化赋值，如：CtrlSelected = WinUI.GetController("CtrlSelected");
            GenCommon:GenCompInit(dataDict['#CompInit#'], compArray, AllClsMap)-- 常用组件的初始化赋值，如：btnLogin = (GButton)GetChild("_btnLogin");
            GenCommon:GenTransitionInit(dataDict['#CompInit#'], compCls)-- 动效的初始化赋值，如：xxxAnim = WinUI.GetTransition("xxxAnim");

            GenCommon:GenCompEvent(dataDict['#INITUIEVENT#'], compArray, AllClsMap)-- 生成组件的交互事件监听代码:AddUIListener(btnEnter.onClick, OnBtnEnterClick);
            GenCommon:GenCompListOnRender(dataDict['#INITUIEVENT#'], compArray, AllClsMap)-- 生成GList组件Item的渲染回调函数赋值：listPlayer.itemRenderer = OnShowListPlayerItem;

            -- 将 #CompDefine# 拆分为字段声明与枚举/方法，字段在前
            GenCommon:SplitCompDefine(dataDict)

            -- 无交互事件时删除 InitUIEvent 的调用与定义块，有则仅移除条件标记（Win 的 InitUIEvent 被手写层 OnInit 调用，不参与此判断）
            if table.concat(dataDict['#INITUIEVENT#']) == "" then
                templateCodeGen = templateCodeGen:gsub("#IF_UIEVENT_CALL#START.-#IF_UIEVENT_CALL#END\n", "")
                templateCodeGen = templateCodeGen:gsub("#IF_UIEVENT_METHOD#START.-#IF_UIEVENT_METHOD#END\n", "")
            else
                templateCodeGen = templateCodeGen:gsub("#IF_UIEVENT_CALL#START\n", ""):gsub("#IF_UIEVENT_CALL#END\n", "")
                templateCodeGen = templateCodeGen:gsub("#IF_UIEVENT_METHOD#START\n", ""):gsub("#IF_UIEVENT_METHOD#END\n", "")
            end


            -- 使用生成的代码替换模板代码中各个关键字（去除末尾多余换行，避免与模板换行叠加）
            for k, v in pairs(dataDict) do
                local content = table.concat(v)
                content = content:gsub("\n+$", "")
                templateCodeGen = templateCodeGen:gsub(k, content)
            end

            -- 替换命名空间，包名，组件名
            templateCodeGen = templateCodeGen:gsub('#NAMESPACE#', namespace)
            templateCodeGen = templateCodeGen:gsub('#COMPNAME#', compCls.resName)
            templateCodeGen = templateCodeGen:gsub('#COMPTYPE#', compCls.superClassName)

            -- 写入替换完成后的代码文件WinXxx.Gen.cs
            Tool:WriteTxt(targetPath, templateCodeGen)

            ------------------------------------------CompXxx.cs----------------------------------------------
            Tool:Log("生成组件逻辑C#代码----%s", compCls.resName .. ".cs")

            local csTargetPath = Tool:StrFormat('%s/%s.cs', targetCsDir, compCls.resName)

            -- 如果组件逻辑代码文件存在，则不再生成
            if Tool:IsFileExists(csTargetPath) then
                Tool:Log("组件代码文件%s已存在，不再生成", compCls.resName)
                goto continue
            end

            -- 创建存放代码的文件夹=>.../ViewImpl/Comp
            Tool:CreateDirectory(targetCsDir)

            local templateCodePath = Tool:StrFormat("%s/%s", Tool:PluginPath(), "Template/CompTemplate.txt")
            local templateCode = Tool:ReadTxt(templateCodePath) -- 读取模板代码

            -- 生成组件的交互事件处理函数代码，如:private void OnBtnEnterClick(EventContext ctx){}
            local handlerLines = {}
            GenCommon:GenCompEventHandler(handlerLines, compArray, AllClsMap)

            -- 有事件处理函数时才生成 region 块，避免空 region
            local handlerContent = table.concat(handlerLines)
            local handlerRegion = ""
            if handlerContent ~= "" then
                handlerRegion = "\t\t#region 交互事件与ListItem渲染回调处理\n\n" .. handlerContent .. "\t\t#endregion\n"
            end
            templateCode = templateCode:gsub('#HANDLER_REGION#', handlerRegion)

            -- 替换命名空间，包名，组件名
            templateCode = templateCode:gsub('#NAMESPACE#', namespace)
            templateCode = templateCode:gsub('#COMPNAME#', compCls.resName)
            templateCode = templateCode:gsub('#COMPTYPE#', compCls.superClassName)

            -- 写入替换完成后的代码文件CompXxx.cs
            Tool:WriteTxt(csTargetPath, templateCode)
            :: continue ::
        end
    end -- if compClsArray

    -- 清理已删除组件的残留代码文件（FGUI 中移除组件后自动同步删除本地代码）
    GenComp:CleanupOrphanedComps(targetGenDir, targetCsDir, compClsArray)
end

--- 清理已从 FGUI 删除的组件对应的代码文件
---@param targetGenDir  string  Gen 文件所在目录（.Gen.cs）
---@param targetCsDir   string  手写 .cs 文件所在目录
---@param compClsArray  table   当前 FGUI 中存在的组件列表
function GenComp:CleanupOrphanedComps(targetGenDir, targetCsDir, compClsArray)
    -- 构建当前有效组件名集合
    local currentComps = {}
    if compClsArray then
        for _, cls in ipairs(compClsArray) do
            currentComps[cls.resName] = true
        end
    end

    -- 清理 Gen 目录中的孤儿 Comp 文件（.Gen.cs 和 .cs）
    GenCommon:CleanupOrphanedFiles(targetGenDir, { "^(Comp.+)%.Gen%.cs$", "^(Comp.+)%.cs$" }, currentComps, "组件代码", true)

    -- 清理手写代码目录中的孤儿 .cs 文件（若与 Gen 目录不同）
    if targetCsDir ~= targetGenDir then
        GenCommon:CleanupOrphanedFiles(targetCsDir, { "^(Comp.+)%.cs$" }, currentComps, "组件手写代码", true)
    end
end

return GenComp