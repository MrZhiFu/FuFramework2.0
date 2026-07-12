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
        -- Launcher包特殊处理：FGUI资源名为WinLauncher，C#类名使用BootstrapView
        local winName = winCls.resName
        if tostring(pkgName) == "Launcher" then
            winName = "BootstrapView"
        end

        -------------------------------------WinXxx.Gen.cs----------------------------------------
        Tool:Log("生成界面C#代码----%s.Gen.cs", winName)

        local targetDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName)

        -- 创建存放代码的文件夹=>.../ViewGen
        Tool:CreateDirectory(targetDir)

        local targetPath = Tool:StrFormat('%s/%s.Gen.cs', targetDir, winName) --- 界面代码生成目标路径
        local compArray = Tool:GetCompArray(winCls)

        -- Launcher 包使用独立模板（不继承 ViewBase，手动管理 m_View）
        local isLauncher = tostring(pkgName) == "Launcher"
        local templateName = isLauncher and "Template/WinGenLauncherTemplate.txt" or "Template/WinGenTemplate.txt"
        local templateCodeGenPath = Tool:StrFormat("%s/%s", Tool:PluginPath(), templateName)
        local templateCodeGen = Tool:ReadTxt(templateCodeGenPath)  -- 读取模板代码

        -- 定义模板代码中需要填充的关键字
        local dataKeys = {
            '#CompDefine#', -- 界面包含的组件定义关键字（标准模板）
            '#FieldDefine#', -- Launcher 模板：字段声明（含 Controller、组件、动效）
            '#EnumAndMethodDefine#', -- Launcher 模板：枚举定义与 SetController 方法
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

        GenCommon:GenCompEvent(dataTable['#INITUIEVENT#'], compArray, AllClsMap)-- 生成组件的交互事件监听代码:AddUIListener(btnEnter.onClick, OnBtnEnterClick);
        GenCommon:GenCompListOnRender(dataTable['#INITUIEVENT#'], compArray, AllClsMap)-- 生成GList组件Item的渲染回调函数赋值：listPlayer.itemRenderer = OnShowListPlayerItem;

        -- Launcher 包特殊处理
        if isLauncher then
            -- A. 将 #CompDefine# 拆分为字段声明与枚举/方法，字段在前
            local compDefineContent = table.concat(dataTable['#CompDefine#'])
            local fieldLines = {}
            local otherLines = {}
            for line in compDefineContent:gmatch("[^\n]*\n?") do
                if line:match("^\t*private %w+ [%w_]+;\n?$") then
                    table.insert(fieldLines, line)
                elseif line:match("^%s*$") then
                    -- 空白行：归入下一组（不归属任何一组，按顺序追加到当前组）
                    if #fieldLines > 0 and #otherLines == 0 then
                        -- 字段后的空白暂时跳过，待合并时处理
                    else
                        table.insert(otherLines, line)
                    end
                else
                    table.insert(otherLines, line)
                end
            end
            dataTable['#FieldDefine#'] = fieldLines
            -- 清理 otherLines 首尾空白
            while #otherLines > 0 and otherLines[1]:match("^%s*$") do
                table.remove(otherLines, 1)
            end
            while #otherLines > 0 and otherLines[#otherLines]:match("^%s*$") do
                table.remove(otherLines)
            end
            dataTable['#EnumAndMethodDefine#'] = otherLines
            dataTable['#CompDefine#'] = {} -- Launcher 模板不使用此占位符

            -- B. API 调用改为 m_View.xxx（不修改字段命名，Gen.cs 保持 FGUI 标准匈牙利风格）
            local apiKeys = {'#FieldDefine#', '#EnumAndMethodDefine#', '#CompInit#', '#INITUIEVENT#'}
            for _, k in ipairs(apiKeys) do
                local content = table.concat(dataTable[k])
                if content ~= "" then
                    -- 1. UIView.GetController → m_View.GetController
                    content = content:gsub("UIView%.GetController", "m_View.GetController")
                    -- 2. UIView.GetTransition → m_View.GetTransition
                    content = content:gsub("UIView%.GetTransition", "m_View.GetTransition")
                    -- 3. GetChild → m_View.GetChild（避免重复替换）
                    content = content:gsub("([^%.])GetChild%(", "%1m_View.GetChild(")
                    -- 4. AddUIListener(var.event, handler) → var.event.Set(handler)
                    content = content:gsub("AddUIListener%(([%w_]+)%.(%w+), ([^)]+)%)", function(varName, event, handler)
                        return varName .. "." .. event .. ".Set(" .. handler .. ")"
                    end)
                    dataTable[k] = {content}
                end
            end
        end

        -- 使用生成的代码替换模板代码中各个关键字
        for k, v in pairs(dataTable) do
            templateCodeGen = templateCodeGen:gsub(k, table.concat(v))
        end

        -- 替换命名空间，包名，界面名
        templateCodeGen = templateCodeGen:gsub('#NAMESPACE#', namespace)
        templateCodeGen = templateCodeGen:gsub('#PKGNAME#', pkgName)
        templateCodeGen = templateCodeGen:gsub('#WINNAME#', winName)

        -- 写入替换完成后的代码文件WinXxx.Gen.cs
        Tool:WriteTxt(targetPath, templateCodeGen)

        -------------------------------------WinXxx.cs----------------------------------------
        -- Launcher 包的 BootstrapView.cs 为手写代码，不自动生成
        if not isLauncher then
            Tool:Log("生成界面逻辑C#代码----%s.cs", winName)

            targetDir = Tool:StrFormat(exportPath, unityDataPath, pkgName)
            targetPath = Tool:StrFormat('%s/%s.cs', targetDir, winName)

            -- 如果界面逻辑代码文件不存在，则生成
            if not Tool:IsFileExists(targetPath) then

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
                    templateCode = templateCode:gsub('#WINNAME#', winName)

                    -- 写入替换完成后的代码文件WinXxx.cs
                    Tool:WriteTxt(targetPath, templateCode)
                end
            end
        end -- if not isLauncher
    end
end

return GenWin