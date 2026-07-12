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

    -- Launcher 包使用独立模板与目录结构（不含 Hotfix 依赖）
    local isLauncher = tostring(pkgName) == "Launcher"
    local compSubDir = isLauncher and "/UI_AutoGen/Comp" or "/Comp"

    -- 提前计算目标目录（Gen / 手写代码），归一化多余斜杠
    local targetGenDir = (Tool:StrFormat(exportGenPath, unityDataPath, pkgName) .. compSubDir):gsub("/+", "/")
    local targetCsDir = (Tool:StrFormat(exportPath, unityDataPath, pkgName) .. compSubDir):gsub("/+", "/")

    if compClsArray and #compClsArray > 0 then
        Tool:CreateDirectory(targetGenDir)  -- 确保 Gen 目录存在

    for _, compCls in ipairs(compClsArray) do
        -------------------------------------CompXxx.Gen.cs----------------------------------------
        Tool:Log("生成组件C#代码----%s", compCls.resName .. ".Gen.cs")

        local targetDir = targetGenDir

        local targetPath = Tool:StrFormat('%s/%s.Gen.cs', targetDir, compCls.resName)
        local compArray = Tool:GetCompArray(compCls)

        -- Launcher 包使用独立模板（不含 Hotfix 依赖：ICustomComp、ViewBase、FuFramework.UI/Event 等）
        local templateName = isLauncher and "Template/CompGenLauncherTemplate.txt" or "Template/CompGenTemplate.txt"
        local templateCodeGenPath = Tool:StrFormat("%s/%s", Tool:PluginPath(), templateName)
        local templateCodeGen = Tool:ReadTxt(templateCodeGenPath) -- 读取模板代码

        -- 定义模板代码中需要填充的关键字
        local dataKeys = {
            '#CompDefine#', -- 组件包含的组件定义关键字（拆分后不再使用）
            '#FieldDefine#', -- 字段声明
            '#EnumAndMethodDefine#', -- 枚举定义与 SetController 方法
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

        -- 将 #CompDefine# 拆分为字段声明与枚举/方法，字段在前
        local compDefineContent = table.concat(dataDict['#CompDefine#'])
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
        dataDict['#FieldDefine#'] = fieldLines
        while #otherLines > 0 and otherLines[1]:match("^%s*$") do
            table.remove(otherLines, 1)
        end
        while #otherLines > 0 and otherLines[#otherLines]:match("^%s*$") do
            table.remove(otherLines)
        end
        dataDict['#EnumAndMethodDefine#'] = otherLines
        dataDict['#CompDefine#'] = {}

        -- Launcher 包：API 调用改为原生 FGUI 风格（无 uiView 间接层）
        if isLauncher then
            local apiKeys = {'#INITUIEVENT#'}
            for _, k in ipairs(apiKeys) do
                local content = table.concat(dataDict[k])
                if content ~= "" then
                    -- AddUIListener(var.event, handler) → var.event.Set(handler)
                    content = content:gsub("AddUIListener%(([%w_]+)%.(%w+), ([^)]+)%)", function(varName, event, handler)
                        return varName .. "." .. event .. ".Set(" .. handler .. ")"
                    end)
                    dataDict[k] = {content}
                end
            end
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

        -- 写入替换完成后的代码文件CompXxx.cs
        Tool:WriteTxt(csTargetPath, templateCode)
        :: continue ::
    end
    end -- if compClsArray

    -- 清理已删除组件的残留代码文件（FGUI 中移除组件后自动同步删除本地代码）
    GenComp:CleanupOrphanedComps(targetGenDir, targetCsDir, compClsArray)
end

--- 清理已从 FGUI 删除的组件对应的代码文件
---@param targetGenDir  string  Gen 文件所在目录
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
    if CS.System.IO.Directory.Exists(targetGenDir) then
        local files = CS.System.IO.Directory.GetFiles(targetGenDir)
        if files and files.Length > 0 then
            for i = 0, files.Length - 1 do
                local filePath = files[i]
                local fileName = filePath:match("([^/\\]+)$")
                if not fileName then goto nextGenFile end

                -- 匹配 CompXxx.Gen.cs 或 CompXxx.cs
                local compName = fileName:match("^(Comp.+)%.Gen%.cs$")
                if not compName then
                    compName = fileName:match("^(Comp.+)%.cs$")
                end
                if compName and not currentComps[compName] then
                    Tool:Log("[清理] 删除已移除组件的代码: %s", compName)
                    CS.System.IO.File.Delete(filePath)
                    local metaPath = filePath .. ".meta"
                    if Tool:IsFileExists(metaPath) then
                        CS.System.IO.File.Delete(metaPath)
                    end
                end
                :: nextGenFile ::
            end
        end
        -- 清理后如果目录为空则删除
        GenComp:DeleteDirIfEmpty(targetGenDir)
    end

    -- 清理手写代码目录中的孤儿 .cs 文件（若与 Gen 目录不同）
    if targetCsDir ~= targetGenDir and CS.System.IO.Directory.Exists(targetCsDir) then
        local files = CS.System.IO.Directory.GetFiles(targetCsDir)
        if files and files.Length > 0 then
            for i = 0, files.Length - 1 do
                local filePath = files[i]
                local fileName = filePath:match("([^/\\]+)$")
                if not fileName then goto nextCsFile end

                local compName = fileName:match("^(Comp.+)%.cs$")
                if compName and not currentComps[compName] then
                    -- 检查是否还有对应的 .Gen.cs（可能在另一个目录）
                    local genPath = targetGenDir .. "/" .. compName .. ".Gen.cs"
                    if not Tool:IsFileExists(genPath) then
                        Tool:Log("[清理] 删除已移除组件的手写代码: %s.cs", compName)
                        CS.System.IO.File.Delete(filePath)
                        local metaPath = filePath .. ".meta"
                        if Tool:IsFileExists(metaPath) then
                            CS.System.IO.File.Delete(metaPath)
                        end
                    end
                end
                :: nextCsFile ::
            end
        end
        -- 清理后如果目录为空则删除
        GenComp:DeleteDirIfEmpty(targetCsDir)
    end
end

--- 如果目录为空（无任何文件/子目录），删除目录及其 .meta
---@param dirPath string 目录路径
function GenComp:DeleteDirIfEmpty(dirPath)
    if not CS.System.IO.Directory.Exists(dirPath) then
        return
    end

    local remainingFiles = CS.System.IO.Directory.GetFiles(dirPath)
    local remainingDirs = CS.System.IO.Directory.GetDirectories(dirPath)
    if (not remainingFiles or remainingFiles.Length == 0) and (not remainingDirs or remainingDirs.Length == 0) then
        CS.System.IO.Directory.Delete(dirPath)
        Tool:Log("[清理] 目录为空，已删除: %s", dirPath)
        local metaPath = dirPath .. ".meta"
        if Tool:IsFileExists(metaPath) then
            CS.System.IO.File.Delete(metaPath)
        end
    end
end

return GenComp