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
    local exportGenPath = Tool:GetExportCodeGenPath(pkgName) --- 导出ViewGen的C#代码路径
    local exportPath = Tool:GetExportCodePath(pkgName)       --- 导出View的C#代码路径
    local namespace = Tool:GetExportCodeNamespace(pkgName)   --- 导出View的C#代码命名空间

    -- Launcher 包：Win 界面代码统一放在 Bootstrap/UI/ 下
    local isLauncher = tostring(pkgName) == "Launcher"
    local aotUiSubDir = isLauncher and "/UI" or ""

    -- 提前计算目标目录（Gen / 手写代码）
    local targetGenDir = Tool:StrFormat(exportGenPath, unityDataPath, pkgName) .. aotUiSubDir
    local targetCsDir = Tool:StrFormat(exportPath, unityDataPath, pkgName)

    if winClsArray and #winClsArray > 0 then
        Tool:CreateDirectory(targetGenDir)  -- 确保 Gen 目录存在

        for _, winCls in ipairs(winClsArray) do
            local winName = winCls.resName

            -------------------------------------WinXxx.Gen.cs----------------------------------------
            Tool:Log("生成界面C#代码----%s.Gen.cs", winName)

            local targetPath = Tool:StrFormat('%s/%s.Gen.cs', targetGenDir, winName) --- 界面代码生成目标路径
            local compArray = Tool:GetCompArray(winCls)

            -- Launcher 包使用独立模板（不继承 ViewBase，手动管理 m_View）
            local templateName = isLauncher and "Template/WinGenLauncherTemplate.txt" or "Template/WinGenTemplate.txt"
            local templateCodeGenPath = Tool:StrFormat("%s/%s", Tool:PluginPath(), templateName)
            local templateCodeGen = Tool:ReadTxt(templateCodeGenPath)  -- 读取模板代码

            -- 定义模板代码中需要填充的关键字
            local dataKeys = {
                '#CompDefine#', -- 界面包含的组件定义关键字（标准模板）
                '#FieldDefine#', -- Launcher 模板：字段声明（含 Controller、组件、动效）
                '#EnumAndMethodDefine#', -- Launcher 模板：枚举定义与 SetController 方法
                '#CompInit#', -- 界面包含的组件初始化赋值关键字
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

            -- A. 将 #CompDefine# 拆分为字段声明与枚举/方法，字段在前（所有包通用）
            local compDefineContent = table.concat(dataTable['#CompDefine#'])
            local fieldLines = {}
            local otherLines = {}
            for line in compDefineContent:gmatch("[^\n]*\n?") do
                if line:match("^\t*private %w+ [%w_]+;\n?$") then
                    table.insert(fieldLines, line)
                elseif line:match("^%s*$") then
                    if #fieldLines > 0 and #otherLines == 0 then
                        -- 字段后的空白暂时跳过
                    else
                        table.insert(otherLines, line)
                    end
                else
                    table.insert(otherLines, line)
                end
            end
            dataTable['#FieldDefine#'] = fieldLines
            while #otherLines > 0 and otherLines[1]:match("^%s*$") do
                table.remove(otherLines, 1)
            end
            while #otherLines > 0 and otherLines[#otherLines]:match("^%s*$") do
                table.remove(otherLines)
            end
            dataTable['#EnumAndMethodDefine#'] = otherLines
            dataTable['#CompDefine#'] = {} -- 拆分后原占位符不再使用

            -- B. Launcher 包特殊处理
            if isLauncher then
                -- B0. 字段/枚举/方法改为 public（独立类，非 partial，外部需要访问）
                for _, k in ipairs({ '#FieldDefine#', '#EnumAndMethodDefine#' }) do
                    local content = table.concat(dataTable[k])
                    if content ~= "" then
                        content = content:gsub("\t\tprivate ", "\t\tpublic ")
                        dataTable[k] = { content }
                    end
                end

                local apiKeys = { '#FieldDefine#', '#EnumAndMethodDefine#', '#CompInit#', '#INITUIEVENT#' }
                for _, k in ipairs(apiKeys) do
                    local content = table.concat(dataTable[k])
                    if content ~= "" then
                        -- 1. UIView.GetController → m_View.GetController
                        content = content:gsub("UIView%.GetController", "m_View.GetController")
                        -- 2. UIView.GetTransition → m_View.GetTransition
                        content = content:gsub("UIView%.GetTransition", "m_View.GetTransition")
                        -- 3. GetChild → m_View.GetChild
                        content = content:gsub("([^%.])GetChild%(", "%1m_View.GetChild(")
                        -- 4. AddUIListener(var.event, handler) → var.event.Set(handler)
                        content = content:gsub("AddUIListener%(([%w_]+)%.(%w+), ([^)]+)%)", function(varName, event, handler)
                            return varName .. "." .. event .. ".Set(" .. handler .. ")"
                        end)
                        dataTable[k] = { content }
                    end
                end
            end

            -- 使用生成的代码替换模板代码中各个关键字（去除末尾多余换行，避免与模板换行叠加）
            for k, v in pairs(dataTable) do
                local content = table.concat(v)
                content = content:gsub("\n+$", "")
                templateCodeGen = templateCodeGen:gsub(k, content)
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

                local csTargetPath = Tool:StrFormat('%s/%s.cs', targetCsDir, winName)

                -- 如果界面逻辑代码文件不存在，则生成
                if not Tool:IsFileExists(csTargetPath) then

                    -- 创建存放代码的文件夹=>.../ViewImpl
                    Tool:CreateDirectory(targetCsDir)

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
                        GenCommon:GenCompEventHandler(dataTable1['#HANDLER#'], compArray, AllClsMap)

                        -- 使用生成的代码替换模板代码中各个关键字
                        for k, v in pairs(dataTable1) do
                            templateCode = templateCode:gsub(k, table.concat(v))
                        end

                        -- 替换命名空间，包名，界面名
                        templateCode = templateCode:gsub('#NAMESPACE#', namespace)
                        templateCode = templateCode:gsub('#PKGNAME#', pkgName)
                        templateCode = templateCode:gsub('#WINNAME#', winName)

                        -- 写入替换完成后的代码文件WinXxx.cs
                        Tool:WriteTxt(csTargetPath, templateCode)
                    end
                end
            end -- if not isLauncher
        end
    end -- if winClsArray

    -- 清理已删除界面的残留代码文件（FGUI 中移除界面后自动同步删除本地代码）
    GenWin:CleanupOrphanedWins(targetGenDir, targetCsDir, winClsArray)
end

--- 清理已从 FGUI 删除的界面对应的代码文件
---@param targetGenDir  string  Gen 文件所在目录
---@param targetCsDir   string  手写 .cs 文件所在目录
---@param winClsArray   table   当前 FGUI 中存在的界面列表
function GenWin:CleanupOrphanedWins(targetGenDir, targetCsDir, winClsArray)
    if not CS.System.IO.Directory.Exists(targetGenDir) then
        return
    end

    -- 构建当前有效界面名集合
    local currentWins = {}
    if winClsArray then
        for _, winCls in ipairs(winClsArray) do
            currentWins[winCls.resName] = true
        end
    end

    local files = CS.System.IO.Directory.GetFiles(targetGenDir)
    if not files or files.Length == 0 then
        return
    end

    for i = 0, files.Length - 1 do
        local filePath = files[i]
        local fileName = filePath:match("([^/\\]+)$")
        if not fileName then
            goto nextWinFile
        end

        -- 匹配 WinXxx.Gen.cs
        local winName = fileName:match("^(Win.+)%.Gen%.cs$")
        if not winName then
            goto nextWinFile
        end

        if not currentWins[winName] then
            Tool:Log("[清理] 删除已移除界面的 Gen 代码: %s", winName)
            CS.System.IO.File.Delete(filePath)
            local metaPath = filePath .. ".meta"
            if Tool:IsFileExists(metaPath) then
                CS.System.IO.File.Delete(metaPath)
            end

            -- 同时删除手写 .cs 文件（如含自定义逻辑请从 git 恢复）
            local csPath = targetCsDir .. "/" .. winName .. ".cs"
            if Tool:IsFileExists(csPath) then
                Tool:Log("[清理] 删除已移除界面的手写代码: %s.cs", winName)
                CS.System.IO.File.Delete(csPath)
                local csMetaPath = csPath .. ".meta"
                if Tool:IsFileExists(csMetaPath) then
                    CS.System.IO.File.Delete(csMetaPath)
                end
            end
        end
        :: nextWinFile ::
    end
end

return GenWin