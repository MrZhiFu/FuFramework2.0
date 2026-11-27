-- MCPBridge 命令处理器
-- 处理来自 MCP 服务的命令并返回结果

local CommandHandler = {}

-- 已处理的命令 ID 集合（防止重复处理）
local processedCommands = {}

-- ========== 辅助函数 ==========

-- 辅助函数：从 C# Dictionary 获取值
local function getDictValue(dict, key)
    local value = nil
    pcall(function()
        value = dict:get_Item(key)
    end)
    return value
end

-- 辅助函数：将 C# Dictionary/Hashtable 转换为 Lua table
local function csharpToLua(obj)
    if obj == nil then return nil end

    local t = type(obj)
    if t == "string" or t == "number" or t == "boolean" then
        return obj
    end

    -- 检查是否是 C# Dictionary/Hashtable
    if obj.Keys then
        local result = {}
        local enumerator = obj.Keys:GetEnumerator()
        while enumerator:MoveNext() do
            local key = enumerator.Current
            local value = obj:get_Item(key)
            result[key] = csharpToLua(value)
        end
        return result
    end

    return obj
end

-- 辅助函数：使用 GetScreenShot 截取 DisplayObject 并保存为 PNG
-- 支持可选的裁剪区域（cropX, cropY, cropW, cropH）
-- 返回 true 表示成功，false 表示失败
local function captureDisplayObject(displayObj, screenshotPath, scale, cropX, cropY, cropW, cropH)
    scale = scale or 1
    -- 调用 GetScreenShot 获取 Texture2D
    local texture = displayObj:GetScreenShot(nil, scale)
    if not texture then
        error("GetScreenShot 返回 nil，无法截取")
    end

    -- 调试：打印实际返回的 texture 尺寸 vs displayObject 尺寸
    pcall(function()
        fprint(string.format("[MCPBridge] DisplayObject %sx%s, Texture %sx%s",
            tostring(displayObj.width or 0), tostring(displayObj.height or 0),
            tostring(texture.width or 0), tostring(texture.height or 0)))
    end)

    local dbgTexW, dbgTexH, dbgObjW, dbgObjH = 0, 0, 0, 0
    local dbgApplied, dbgNote = false, "no_crop"
    local dbgX0, dbgY0, dbgCW, dbgCH = 0, 0, 0, 0

    local ok, err = pcall(function()
        local texW, texH = texture.width, texture.height
        local saveTexture = texture
        local croppedTex = nil
        dbgTexW, dbgTexH = texW or 0, texH or 0
        dbgObjW, dbgObjH = displayObj.width or 0, displayObj.height or 0

        -- 裁剪坐标是显示对象的设计坐标，而 GetScreenShot 出的纹理尺寸随 scale 变；
        -- 比例用实测尺寸反推，不假设 GetScreenShot 的缩放语义
        local fw = (displayObj.width and displayObj.width > 0) and (texW / displayObj.width) or 1
        local fh = (displayObj.height and displayObj.height > 0) and (texH / displayObj.height) or 1

        if cropX and cropY and cropW and cropH and cropW > 0 and cropH > 0 then
            local x0 = math.floor(cropX * fw + 0.5)
            local yTop = math.floor(cropY * fh + 0.5)
            local cw = math.floor(cropW * fw + 0.5)
            local ch = math.floor(cropH * fh + 0.5)

            -- 设备屏相对容器可为负偏移、也可大于纹理：先钳进纹理范围再取像素，
            -- 否则 GetPixels 抛 out of bounds（预览截图此前每次都在这一步失败）
            if x0 < 0 then cw = cw + x0; x0 = 0 end
            if yTop < 0 then ch = ch + yTop; yTop = 0 end
            if x0 + cw > texW then cw = texW - x0 end
            if yTop + ch > texH then ch = texH - yTop end

            if cw > 0 and ch > 0 then
                local cropOk, cropErr = pcall(function()
                    -- Unity Texture2D 的 Y 轴是从下到上，需要翻转
                    local y0 = texH - yTop - ch
                    local pixels = texture:GetPixels(x0, y0, cw, ch)
                    croppedTex = CS.UnityEngine.Texture2D(cw, ch)
                    croppedTex:SetPixels(pixels)
                    croppedTex:Apply()
                    saveTexture = croppedTex
                end)
                if cropOk then
                    dbgApplied, dbgNote = true, "cropped"
                    dbgX0, dbgY0, dbgCW, dbgCH = x0, texH - yTop - ch, cw, ch
                    fprint(string.format("[MCPBridge] 裁剪到 %dx%d at (%d,%d)", cw, ch, x0, yTop))
                else
                    dbgNote = "crop_failed"
                    fprint("[MCPBridge] 裁剪失败，改存整图: " .. tostring(cropErr))
                    croppedTex = nil
                    saveTexture = texture
                end
            else
                dbgNote = "crop_out_of_bounds"
                fprint("[MCPBridge] 裁剪矩形越出纹理范围，改存整图")
            end
        end

        -- 编码为 PNG
        local pngBytes = CS.UnityEngine.ImageConversion.EncodeToPNG(saveTexture)
        -- 写入文件
        CS.System.IO.File.WriteAllBytes(screenshotPath, pngBytes)

        if croppedTex then
            CS.UnityEngine.Object.Destroy(croppedTex)
        end
    end)

    -- 裁剪参数随图落盘：图是"裁错了"还是"界面本身缺内容"，只看图分辨不出，
    -- 而两者的处置完全相反，必须留下实测依据
    pcall(function()
        local sidecar = screenshotPath:gsub("%.[Pp][Nn][Gg]$", "") .. ".debug.json"
        local note = tostring(dbgNote):gsub('[\\"]', '')
        local errText = tostring(err or ""):gsub('[\\"]', ' '):gsub('[\r\n]', ' ')
        CS.System.IO.File.WriteAllText(sidecar, string.format(
            '{"obj_wh":[%d,%d],"tex_wh":[%d,%d],"req_crop_xywh":[%s,%s,%s,%s],"used_rect_x_y_w_h":[%d,%d,%d,%d],"crop_applied":%s,"note":"%s","scale":%s,"error":"%s"}',
            math.floor(tonumber(dbgObjW) or 0), math.floor(tonumber(dbgObjH) or 0),
            math.floor(tonumber(dbgTexW) or 0), math.floor(tonumber(dbgTexH) or 0),
            tostring(cropX or 0), tostring(cropY or 0), tostring(cropW or 0), tostring(cropH or 0),
            dbgX0, dbgY0, dbgCW, dbgCH, tostring(dbgApplied), note, tostring(scale), errText))
    end)

    -- 释放 Texture2D
    CS.UnityEngine.Object.Destroy(texture)

    if not ok then
        error("截图保存失败: " .. tostring(err))
    end
    return true
end

-- ========== 初始化 ==========

-- ========== bridge 实例仲裁 ==========
-- 同工程多实例共享同一 bridge 目录会互相抢占命令（曾实测：一无锁实例工程未就绪时轮流
-- 抢走发布命令，package not found 随机出现）。仅 owner 实例执行 poll；owner 以 PID 文件
-- 标识，owner 进程死亡时其余实例低频重查接管，写后回读缩小双实例同时接管的竞态窗口。
-- 放在 command_handler 而非 main.lua：plugin reload 即可让既有实例获得锁行为，无需重启编辑器
local BridgeMyPid = CS.System.Diagnostics.Process.GetCurrentProcess().Id
local BridgeOwnerFile = nil
local BridgeIsOwner = false

local function bridgeProcessAlive(pid)
    local ok, p = pcall(function()
        return CS.System.Diagnostics.Process.GetProcessById(pid)
    end)
    return ok and p ~= nil
end

local function bridgeAcquireOwnership()
    if not BridgeOwnerFile then
        return false
    end
    local ownerPid = 0
    pcall(function()
        local f = io.open(BridgeOwnerFile, "r")
        if f then
            local c = f:read("*a")
            f:close()
            ownerPid = tonumber(c) or 0
        end
    end)
    if ownerPid ~= 0 and ownerPid ~= BridgeMyPid and bridgeProcessAlive(ownerPid) then
        return false
    end
    pcall(function()
        CS.System.IO.File.WriteAllText(BridgeOwnerFile, tostring(BridgeMyPid))
    end)
    local writtenPid = 0
    pcall(function()
        local f = io.open(BridgeOwnerFile, "r")
        if f then
            local c = f:read("*a")
            f:close()
            writtenPid = tonumber(c) or 0
        end
    end)
    return writtenPid == BridgeMyPid
end

-- 让位（编辑器关闭时调用）：仅当 owner 文件仍登记本进程时删除，加速其余实例接管；
-- 崩溃路径无此调用，由其余实例的存活检测兜底
function CommandHandler.ReleaseBridgeOwnership()
    if BridgeIsOwner and BridgeOwnerFile then
        pcall(function()
            local f = io.open(BridgeOwnerFile, "r")
            if f then
                local c = f:read("*a")
                f:close()
                if (tonumber(c) or 0) == BridgeMyPid then
                    CS.System.IO.File.Delete(BridgeOwnerFile)
                end
            end
        end)
    end
end

-- 初始化通信目录
function CommandHandler.initBridge(bridgePath)
    -- 转换路径分隔符（Windows 兼容）
    bridgePath = bridgePath:gsub("/", "\\")
    local dirs = {"\\commands", "\\results", "\\screenshots"}
    for _, dir in ipairs(dirs) do
        local fullPath = bridgePath .. dir
        -- 使用 C# System.IO 创建目录
        if not CS.System.IO.Directory.Exists(fullPath) then
            CS.System.IO.Directory.CreateDirectory(fullPath)
        end
    end
    BridgeOwnerFile = bridgePath .. "\\bridge_owner"
    BridgeIsOwner = bridgeAcquireOwnership()
    if BridgeIsOwner then
        fprint("[MCPBridge] bridge owner: PID " .. BridgeMyPid)
    else
        fprint("[MCPBridge] 旁观模式: bridge 由其他实例持有，本实例不执行命令")
    end
    -- 启动即写一次心跳，避免冷启动 ~5 秒窗口内 .alive 缺失导致 status 误报 degraded
    -- poll 仍每 50 次（5 秒）刷新；此处仅补首次写入，保证插件一就绪 .alive 即存在
    pcall(function()
        CS.System.IO.File.WriteAllText(bridgePath .. "\\.alive", tostring(os.time()))
    end)
    fprint("[MCPBridge] 通信目录已初始化: " .. bridgePath)
end

-- bridge 通信产物清理：results（CLI 秒级读取，10 分钟后过期）、screenshots（保留 24 小时
-- 供人工查看）、cmd_audit.log（超 1MB 轮转）。命令文件由消费方即删，不在此列
local UNIX_EPOCH_TICKS = 621355968000000000
local function cleanupBridgeArtifacts(bridgePath)
    local now = os.time()
    local function sweep(subDir, pattern, maxAgeSec)
        local dir = bridgePath .. "\\" .. subDir
        if not CS.System.IO.Directory.Exists(dir) then return end
        local okF, files = pcall(function() return CS.System.IO.Directory.GetFiles(dir, pattern) end)
        if not okF or not files then return end
        for i = 0, files.Length - 1 do
            pcall(function()
                local t = CS.System.IO.File.GetLastWriteTime(files[i])
                local unixSec = math.floor((t.Ticks - UNIX_EPOCH_TICKS) / 10000000)
                if now - unixSec > maxAgeSec then
                    CS.System.IO.File.Delete(files[i])
                end
            end)
        end
    end
    sweep("results", "*.json", 600)
    sweep("screenshots", "*.png", 86400)
    pcall(function()
        local audit = bridgePath .. "\\cmd_audit.log"
        local info = CS.System.IO.FileInfo(audit)
        if info.Exists and info.Length > 1048576 then
            CS.System.IO.File.Delete(audit)
        end
    end)
end

-- ========== 轮询 ==========

-- 轮询命令
function CommandHandler.poll(bridgePath)
    -- 实例仲裁：非 owner 实例不处理命令（每约 5 秒重查一次，owner 死亡后接管）
    CommandHandler._ownerCheckCount = (CommandHandler._ownerCheckCount or 0) + 1
    if CommandHandler._ownerCheckCount % 50 == 1 then
        local newOwner = bridgeAcquireOwnership()
        if newOwner ~= BridgeIsOwner then
            BridgeIsOwner = newOwner
            if BridgeIsOwner then
                fprint("[MCPBridge] 已接管 bridge（owner PID " .. BridgeMyPid .. "）")
            end
        end
    end
    if not BridgeIsOwner then
        return
    end

    -- 通信产物低频清理（每约 60 秒一次）
    CommandHandler._cleanupCount = (CommandHandler._cleanupCount or 0) + 1
    if CommandHandler._cleanupCount % 600 == 0 then
        pcall(function() cleanupBridgeArtifacts(bridgePath) end)
    end

    -- 心跳：每约 5 秒（50次轮询）写入时间戳，供 fairygui-cli instances list 发现在线实例
    CommandHandler._heartbeatCount = (CommandHandler._heartbeatCount or 0) + 1
    if CommandHandler._heartbeatCount % 50 == 0 then
        pcall(function()
            CS.System.IO.File.WriteAllText(bridgePath .. "/.alive", tostring(os.time()))
        end)
    end

    -- 转换路径分隔符（Windows 兼容）
    local commandsDir = bridgePath:gsub("/", "\\") .. "\\commands"

    -- 检查目录是否存在
    if not CS.System.IO.Directory.Exists(commandsDir) then
        return
    end

    -- 遍历命令文件
    local files = CS.System.IO.Directory.GetFiles(commandsDir, "*.json")
    if not files or files.Length == 0 then
        return
    end

    fprint("[MCPBridge] 发现 " .. files.Length .. " 个待处理命令")

    for i = 0, files.Length - 1 do
        local filePath = files[i]
        local fileName = CS.System.IO.Path.GetFileNameWithoutExtension(filePath)

        -- 跳过已处理的命令
        if processedCommands[fileName] then
            goto continue
        end

        -- 读取并执行命令
        local success, content = pcall(function()
            local f = io.open(filePath, "r")
            if f then
                local txt = f:read("*a")
                f:close()
                return txt
            end
            return nil
        end)

        if success and content then
            -- 命令审计：落盘原始命令内容（fprint 日志缓冲区对部分环境不生效，排查命令格式用）
            pcall(function()
                CS.System.IO.File.AppendAllText(bridgePath .. "\\cmd_audit.log", os.date("%H:%M:%S ") .. content .. "\n")
            end)
            -- 调试：打印原始内容
            fprint("[MCPBridge] 原始内容长度: " .. #content)
            fprint("[MCPBridge] 原始内容: " .. string.sub(content, 1, 200))

            -- 解析 JSON
            local jsonOk, cmd = pcall(function()
                return CS.FairyEditor.JsonUtil.DecodeJson(content)
            end)

            if not jsonOk then
                fprint("[MCPBridge] JSON 解析失败: " .. tostring(cmd))
            end

            if jsonOk and cmd then
                -- FairyGUI 的 JsonUtil.DecodeJson 返回 C# Dictionary
                local action = nil
                local cmdId = nil
                local params = {}

                -- 遍历 Keys 集合获取值
                if cmd.Keys then
                    local enumerator = cmd.Keys:GetEnumerator()
                    while enumerator:MoveNext() do
                        local key = enumerator.Current
                        local value = nil
                        pcall(function()
                            value = cmd:get_Item(key)
                        end)
                        if key == "action" then action = value end
                        if key == "id" then cmdId = value end
                        if key == "params" then params = csharpToLua(value) or {} end
                    end
                end

                fprint("[MCPBridge] 收到命令: " .. (action or "unknown"))

                -- 执行命令
                local result = CommandHandler.execute(cmd, bridgePath)

                -- 写入结果
                CommandHandler.writeResult(bridgePath, fileName, result)

                -- 命令执行完毕后重置 runInBackground
                -- 确保即使 F5 等模式在 handler 内部覆盖了此值，也能恢复
                CS.UnityEngine.Application.runInBackground = true

                -- 标记为已处理
                processedCommands[fileName] = true

                -- 清理旧记录（保留最近 100 个）
                local count = 0
                for _ in pairs(processedCommands) do count = count + 1 end
                if count > 100 then
                    processedCommands = {}
                end
            end
        end

        -- 删除命令文件
        pcall(function()
            CS.FairyEditor.IOUtil.DeleteFile(filePath, false)
        end)

        ::continue::
    end
end

-- ========== 命令执行 ==========

-- 执行命令
function CommandHandler.execute(cmd, bridgePath)
    -- FairyGUI 的 JsonUtil.DecodeJson 返回 C# Dictionary
    local action = getDictValue(cmd, "action")
    local rawParams = getDictValue(cmd, "params") or {}
    local cmdId = getDictValue(cmd, "id")

    -- 将 params 转换为 Lua table
    local params = csharpToLua(rawParams)

    -- 命令处理器映射
    local handlers = {
        ["activate"] = CommandHandler.handleActivate,
        ["reload"] = CommandHandler.handleReload,
        ["open_component"] = CommandHandler.handleOpenComponent,
        ["preview"] = CommandHandler.handlePreview,
        ["screenshot"] = CommandHandler.handleScreenshot,
        ["get_selection"] = CommandHandler.handleGetSelection,
        ["save"] = CommandHandler.handleSave,
        ["close"] = CommandHandler.handleClose,
        ["get_component_info"] = CommandHandler.handleGetComponentInfo,
        ["list_packages"] = CommandHandler.handleListPackages,
        ["list_components"] = CommandHandler.handleListComponents,
        ["start_test"] = CommandHandler.handleStartTest,
        ["stop_test"] = CommandHandler.handleStopTest,
        ["switch_device"] = CommandHandler.handleSwitchDevice,
        ["capture_preview"] = CommandHandler.handleCapturePreview,
        ["list_devices"] = CommandHandler.handleListDevices,
        ["switch_controller"] = CommandHandler.handleSwitchController,
        ["list_controllers"] = CommandHandler.handleListControllers,
        ["select_element"] = CommandHandler.handleSelectElement,
        ["probe_plugin_api"] = CommandHandler.handleProbePluginApi,
        ["probe_publish"] = CommandHandler.handleProbePublish,
        ["probe_toolbar"] = CommandHandler.handleProbeToolbar,
        ["probe_publish_dialog"] = CommandHandler.handleProbePublishDialog,
        ["publish_all_native"] = CommandHandler.handlePublishAllNative,
        ["probe_publish_dialog_ui"] = CommandHandler.handlePublishDialogProbe,
        ["fire_toolbar"] = CommandHandler.handleFireToolbar,
        ["probe_mainview"] = CommandHandler.handleProbeMainView,
        ["search_tree"] = CommandHandler.handleSearchTree,
        ["publish_dialog_button"] = CommandHandler.handlePublishDialogButton,
        ["open_publish_settings"] = CommandHandler.handleOpenPublishSettings,
        ["publish_package"] = CommandHandler.handlePublishPackage,
        ["publish_all"] = CommandHandler.handlePublishAll,
        ["dev_logs"] = CommandHandler.handleDevLogs,
        ["dev_text_props"] = CommandHandler.handleDevTextProps,
        ["dev_set_test"] = CommandHandler.handleDevSetTest,
        ["dev_list_plugins"] = CommandHandler.handleDevListPlugins,
        ["dev_onupdate_test"] = CommandHandler.handleDevOnUpdateTest,
        ["dev_ctrl_probe"] = CommandHandler.handleDevCtrlProbe,
        ["dev_identity_probe"] = CommandHandler.handleDevIdentityProbe,
        -- 日志管理（暂未启用）
        ["get_logs"] = CommandHandler.handleGetLogs,
        ["clear_logs"] = CommandHandler.handleClearLogs,
        -- ["probe_logs"] = CommandHandler.handleProbeLogs,
    }

    local handler = handlers[action]
    if not handler then
        return {
            id = cmdId,
            status = "error",
            error = "未知命令: " .. (action or "nil")
        }
    end

    -- 执行处理器
    local success, result = pcall(function()
        return handler(params, bridgePath)
    end)

    if success then
        return {
            id = cmdId,
            status = "success",
            data = result
        }
    else
        return {
            id = cmdId,
            status = "error",
            error = tostring(result)
        }
    end
end

-- 写入结果
function CommandHandler.writeResult(bridgePath, cmdId, result)
    local resultPath = bridgePath:gsub("/", "\\") .. "\\results\\" .. cmdId .. ".json"

    -- 手动构建 JSON 字符串
    local function tableToJson(t, indent)
        indent = indent or 0
        local spaces = string.rep("  ", indent)
        local nextSpaces = string.rep("  ", indent + 1)

        if type(t) ~= "table" then
            if type(t) == "string" then
                -- 转义特殊字符
                local escaped = t
                    :gsub("\\", "\\\\")
                    :gsub('"', '\\"')
                    :gsub("\n", "\\n")
                    :gsub("\r", "\\r")
                    :gsub("\t", "\\t")
                return '"' .. escaped .. '"'
            elseif type(t) == "number" or type(t) == "boolean" then
                return tostring(t)
            else
                return "null"
            end
        end

        -- 检查是否是数组
        local isArray = true
        local maxIndex = 0
        for k, v in pairs(t) do
            if type(k) ~= "number" or k <= 0 or math.floor(k) ~= k then
                isArray = false
                break
            end
            if k > maxIndex then maxIndex = k end
        end

        if isArray and maxIndex > 0 then
            local parts = {}
            for i = 1, maxIndex do
                parts[#parts + 1] = nextSpaces .. tableToJson(t[i], indent + 1)
            end
            return "[\n" .. table.concat(parts, ",\n") .. "\n" .. spaces .. "]"
        else
            local parts = {}
            for k, v in pairs(t) do
                local keyStr = type(k) == "string" and k or tostring(k)
                parts[#parts + 1] = nextSpaces .. '"' .. keyStr .. '": ' .. tableToJson(v, indent + 1)
            end
            return "{\n" .. table.concat(parts, ",\n") .. "\n" .. spaces .. "}"
        end
    end

    local success, err = pcall(function()
        local content = tableToJson(result)
        local f = io.open(resultPath, "w")
        if f then
            f:write(content)
            f:close()
        else
            fprint("[MCPBridge] 无法写入结果文件: " .. resultPath)
        end
    end)

    if not success then
        fprint("[MCPBridge] 写入结果失败: " .. tostring(err))
    end
end

-- ========== 命令处理器实现 ==========

-- 激活编辑器
function CommandHandler.handleActivate(params, bridgePath)
    -- Lua 侧无法直接激活窗口，Python 端通过 Win32 API 负责激活
    -- 此处仅设置 runInBackground 确保定时器持续运行
    local ok, err = pcall(function()
        CS.UnityEngine.Application.runInBackground = true
    end)
    if ok then
        return { activated = false, reason = "Lua cannot activate window; Python Win32 handles activation", runInBackground = true }
    else
        return { activated = false, reason = "failed to set runInBackground: " .. tostring(err) }
    end
end

-- 刷新资源
function CommandHandler.handleReload(params, bridgePath)
    if params.package_name then
        local pkg = App.project:GetPackageByName(params.package_name)
        if pkg then
            -- FIX-1: 逐级尝试所有刷新方式，不再在第一次成功 pcall 后停止
            -- pkg:Touch() 可能返回成功但编辑器无视觉变化，需要组合多种方式
            local methodsAttempted = {}
            local methodsSucceeded = {}

            -- 方式1: pkg:Touch() — 标记包为需要刷新
            local ok1 = pcall(function() pkg:Touch() end)
            table.insert(methodsAttempted, "pkg:Touch()")
            if ok1 then
                table.insert(methodsSucceeded, "pkg:Touch()")
                fprint("[MCPBridge] reload: pkg:Touch() ok")
            end

            -- 方式2: pkg:Reload()（如果存在）
            local ok2 = pcall(function() pkg:Reload() end)
            table.insert(methodsAttempted, "pkg:Reload()")
            if ok2 then
                table.insert(methodsSucceeded, "pkg:Reload()")
                fprint("[MCPBridge] reload: pkg:Reload() ok")
            end

            -- 方式3: 遍历 pkg.items 并对每个 item 调用 Touch()
            local ok3 = pcall(function()
                local items = pkg.items
                if items and items.Count > 0 then
                    for i = 0, items.Count - 1 do
                        local item = items[i]
                        pcall(function() item:Touch() end)
                    end
                end
            end)
            table.insert(methodsAttempted, "item:Touch() all")
            if ok3 then
                table.insert(methodsSucceeded, "item:Touch() all")
                fprint("[MCPBridge] reload: item:Touch() on all items ok")
            end

            -- 方式4: App.project:RefreshPackage(pkg)
            local ok4 = pcall(function() App.project:RefreshPackage(pkg) end)
            table.insert(methodsAttempted, "project:RefreshPackage")
            if ok4 then
                table.insert(methodsSucceeded, "project:RefreshPackage")
                fprint("[MCPBridge] reload: project:RefreshPackage() ok")
            end

            -- 方式5: 延迟 0.2s 后再执行一次 Touch()（给编辑器时间处理前面的标记）
            local ok5 = pcall(function()
                CS.FairyGUI.Timers.inst:Add(0.2, 1, function()
                    pcall(function() pkg:Touch() end)
                    fprint("[MCPBridge] reload: delayed pkg:Touch() executed")
                end)
            end)
            table.insert(methodsAttempted, "delayed pkg:Touch()")
            if ok5 then
                table.insert(methodsSucceeded, "delayed pkg:Touch()")
                fprint("[MCPBridge] reload: delayed pkg:Touch() scheduled")
            end

            local anySucceeded = #methodsSucceeded > 0
            return {
                reloaded = anySucceeded,
                package = params.package_name,
                methods_attempted = table.concat(methodsAttempted, "; "),
                methods_succeeded = table.concat(methodsSucceeded, "; "),
                note = anySucceeded and "已尝试多种刷新方式，请检查编辑器是否有视觉变化"
                        or "所有刷新方式均不可用"
            }
        else
            error("包不存在: " .. params.package_name)
        end
    else
        -- 全量刷新：使用定时器异步执行，避免阻塞主线程和 poll 轮询
        -- 立即返回成功，让调用方知道命令已接收
        CS.FairyGUI.Timers.inst:Add(0.1, 1, function()
            fprint("[MCPBridge] 正在执行 App.RefreshProject...")
            pcall(function()
                App.RefreshProject()
            end)
            fprint("[MCPBridge] App.RefreshProject 完成")
        end)
        return { reloaded = true, async = true, message = "全量刷新已异步触发" }
    end
end

-- 辅助函数：在包中查找组件（支持路径和纯名称）
-- 路径格式：Buttons/Button01 或 Button01
local function findComponentItem(pkg, compName)
    -- 纯名称查找（向后兼容）
    local item = pkg:FindItemByName(compName)
    if item then return item end

    -- 尝试带 .xml 后缀
    item = pkg:GetItemByFileName(pkg.rootItem, compName .. ".xml")
    if item then return item end

    -- 路径格式：递归遍历 rootItem 匹配完整路径
    local pathParts = {}
    for part in compName:gmatch("[^/]+") do
        table.insert(pathParts, part)
    end

    if #pathParts > 1 then
        local fileName = pathParts[#pathParts] .. ".xml"
        local dirParts = {}
        for i = 1, #pathParts - 1 do
            table.insert(dirParts, pathParts[i])
        end

        -- 递归遍历查找
        local function searchInFolder(parentItem, dirIndex)
            if dirIndex > #dirParts then
                -- 已在目标目录，查找文件
                return pkg:GetItemByFileName(parentItem, fileName)
            end
            -- 查找当前层级的目录
            local targetDir = dirParts[dirIndex]
            if parentItem and parentItem.children then
                for i = 0, parentItem.children.Count - 1 do
                    local child = parentItem.children[i]
                    if child and child.type == "folder" and child.name == targetDir then
                        return searchInFolder(child, dirIndex + 1)
                    end
                end
            end
            return nil
        end

        item = searchInFolder(pkg.rootItem, 1)
        if item then return item end
    end

    return nil
end

-- 打开组件
function CommandHandler.handleOpenComponent(params, bridgePath)
    local pkgName = params.package_name
    local compName = params.component_name

    if not pkgName or not compName then
        error("缺少参数: package_name 或 component_name")
    end

    local pkg = App.project:GetPackageByName(pkgName)
    if not pkg then
        error("包不存在: " .. pkgName)
    end

    local item = findComponentItem(pkg, compName)
    if not item then
        error("组件不存在: " .. compName)
    end

    -- 打开文档
    local url = item:GetURL()
    App.docView:OpenDocument(url, true)

    return {
        opened = true,
        url = url,
        name = item.name,
        path = item.path
    }
end

-- 预览组件
function CommandHandler.handlePreview(params, bridgePath)
    local pkgName = params.package_name
    local compName = params.component_name

    if not pkgName or not compName then
        error("缺少参数: package_name 或 component_name")
    end

    local pkg = App.project:GetPackageByName(pkgName)
    if not pkg then
        error("包不存在: " .. pkgName)
    end

    local item = findComponentItem(pkg, compName)
    if not item then
        error("组件不存在: " .. compName)
    end

    -- 显示预览
    App.ShowPreview(item)

    return {
        previewing = true,
        url = item:GetURL(),
        name = item.name
    }
end

-- 截图
function CommandHandler.handleScreenshot(params, bridgePath)
    local target = params.target or "editor"
    local saveName = params.save_name or ("screenshot_" .. os.time())
    local scale = params.scale or 1

    local screenshotPath = bridgePath:gsub("/", "\\") .. "\\screenshots\\" .. saveName .. ".png"

    if target == "preview" then
        -- 使用 GetScreenShot 精确截取当前打开组件的画布渲染
        local doc = App.activeDoc
        if not doc then
            error("没有打开的文档，无法截取组件画布")
        end
        local content = doc.content
        if not content then
            error("文档内容为空")
        end
        local displayObj = content.displayObject
        if not displayObj then
            error("组件 displayObject 为空")
        end

        -- 收集调试信息
        local debugInfo = {}
        table.insert(debugInfo, "content_size=" .. content.width .. "x" .. content.height)
        table.insert(debugInfo, "dobj_size=" .. displayObj.width .. "x" .. displayObj.height)
        pcall(function()
            local b = displayObj:GetBounds(nil)
            table.insert(debugInfo, "dobj_bounds=" .. b.x .. "," .. b.y .. "," .. b.width .. "," .. b.height)
        end)
        pcall(function()
            if displayObj.numChildren then
                table.insert(debugInfo, "dobj_children=" .. displayObj.numChildren)
                for i = 0, math.min(displayObj.numChildren - 1, 5) do
                    local child = displayObj:GetChildAt(i)
                    if child then
                        table.insert(debugInfo, "child" .. i .. "=" .. child.width .. "x" .. child.height)
                    end
                end
            end
        end)

        captureDisplayObject(displayObj, screenshotPath, scale)

        local debugText = table.concat(debugInfo, "|")
        return {
            screenshot = saveName .. ".png",
            path = screenshotPath,
            captured = true,
            debug = debugText
        }
    else
        -- target="editor": 返回标记，由 Python 端使用 Win32 API 截取全屏
        return {
            screenshot = saveName .. ".png",
            path = screenshotPath,
            captured = false,
            use_python_capture = true,
            message = "editor 模式由 Python 端截取"
        }
    end
end

-- 获取选中元素
function CommandHandler.handleGetSelection(params, bridgePath)
    local doc = App.activeDoc
    if not doc then
        return { selection = {}, message = "没有打开的文档" }
    end

    local selection = doc:GetSelection()
    local result = {}

    if selection and selection.Count > 0 then
        for i = 0, selection.Count - 1 do
            local obj = selection[i]
            if obj then
                table.insert(result, {
                    id = obj.id or "",
                    name = obj.name or "",
                    type = obj.objectType or "unknown"
                })
            end
        end
    end

    return { selection = result, count = #result }
end

-- 保存文档
function CommandHandler.handleSave(params, bridgePath)
    local doc = App.activeDoc
    if doc then
        doc:Save()
        return { saved = true }
    end
    return { saved = false, message = "没有打开的文档" }
end

-- 关闭文档
function CommandHandler.handleClose(params, bridgePath)
    local doc = App.activeDoc
    if doc then
        App.docView:CloseDocument(doc)
        return { closed = true }
    end
    return { closed = false, message = "没有打开的文档" }
end

-- 获取组件信息
function CommandHandler.handleGetComponentInfo(params, bridgePath)
    local pkgName = params.package_name
    local compName = params.component_name

    if not pkgName or not compName then
        error("缺少参数: package_name 或 component_name")
    end

    local pkg = App.project:GetPackageByName(pkgName)
    if not pkg then
        error("包不存在: " .. pkgName)
    end

    local item = findComponentItem(pkg, compName)
    if not item then
        error("组件不存在: " .. compName)
    end

    return {
        name = item.name,
        id = item.id,
        type = item.type,
        width = item.width,
        height = item.height,
        path = item.path,
        url = item:GetURL(),
        exported = item.exported
    }
end

-- 列出所有包
function CommandHandler.handleListPackages(params, bridgePath)
    local packages = {}
    local allPackages = App.project.allPackages

    if allPackages then
        for i = 0, allPackages.Count - 1 do
            local pkg = allPackages[i]
            table.insert(packages, {
                name = pkg.name,
                id = pkg.id,
                path = pkg.basePath
            })
        end
    end

    return { packages = packages, count = #packages }
end

-- 列出包内组件
function CommandHandler.handleListComponents(params, bridgePath)
    local pkgName = params.package_name
    if not pkgName then
        error("缺少参数: package_name")
    end

    local pkg = App.project:GetPackageByName(pkgName)
    if not pkg then
        error("包不存在: " .. pkgName)
    end

    local components = {}
    local items = pkg.items

    if items then
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "component" then
                table.insert(components, {
                    name = item.name,
                    id = item.id,
                    path = item.path,
                    width = item.width,
                    height = item.height,
                    exported = item.exported
                })
            end
        end
    end

    return { components = components, count = #components, package = pkgName }
end

-- ========== 预览测试命令 ==========

-- 辅助函数：从适配设置中查找设备信息
-- 返回 { resolutionX, resolutionY, found, scaleMode, screenMatchMode } 或 nil
local function findDeviceInfo(deviceName)
    if not deviceName then return nil end

    local adaptSettings = App.project:GetSettings("Adaptation")
    if not adaptSettings then return nil end

    local function searchInList(devices)
        if not devices then return nil end
        for i = 0, devices.Count - 1 do
            local dev = devices[i]
            if dev.name == deviceName then
                return dev
            end
        end
        return nil
    end

    local dev = searchInList(adaptSettings.defaultDevices) or searchInList(adaptSettings.devices)
    if dev then
        return {
            resolutionX = dev.resolutionX,
            resolutionY = dev.resolutionY,
            found = true,
            scaleMode = adaptSettings.scaleMode,
            screenMatchMode = adaptSettings.screenMatchMode,
            designX = adaptSettings.designResolutionX,
            designY = adaptSettings.designResolutionY
        }
    end
    return nil
end

-- 辅助函数：探查 testView 运行时的内部结构（用于调试设备切换和截图）
local function probeTestViewInternals(testView)
    local info = {}
    table.insert(info, "type=" .. tostring(testView:GetType()))
    table.insert(info, "running=" .. tostring(testView.running))
    table.insert(info, "visible=" .. tostring(testView.visible))
    table.insert(info, "size=" .. testView.width .. "x" .. testView.height)
    table.insert(info, "viewSize=" .. (testView.viewWidth or 0) .. "x" .. (testView.viewHeight or 0))
    table.insert(info, "numChildren=" .. tostring(testView.numChildren))

    -- 遍历子元素
    for i = 0, (testView.numChildren or 0) - 1 do
        local child = testView:GetChildAt(i)
        if child then
            local childInfo = string.format(
                "child%d: name=%s type=%s size=%dx%d visible=%s",
                i, child.name or "", tostring(child:GetType()),
                child.width, child.height, tostring(child.visible)
            )
            table.insert(info, childInfo)
        end
    end

    -- 探查内部属性
    local internalProps = {
        "stage", "Stage", "_stage", "content", "Content", "_content",
        "contentPane", "ContentPane", "_contentPane",
        "mainContainer", "_mainContainer", "viewPanel", "_viewPanel",
        "frame", "Frame",
        "displayObject", "DisplayObject", "_displayObject",
        "viewWidth", "viewHeight", "ViewWidth", "ViewHeight",
    }
    for _, prop in ipairs(internalProps) do
        local ok, val = pcall(function() return testView[prop] end)
        if ok and val ~= nil then
            local vtype = type(val)
            local vstr = tostring(val)
            if vtype == "userdata" then
                pcall(function()
                    vstr = string.format("%s (%.0fx%.0f)", tostring(val:GetType()), val.width, val.height)
                end)
            end
            table.insert(info, prop .. "=" .. vstr .. " [" .. vtype .. "]")
        end
    end

    -- 探查内部方法
    local internalMethods = {
        "SetSize", "setSize", "SetScale", "setScale",
        "Refresh", "refresh", "Repaint", "repaint",
        "UpdateSize", "updateSize", "ApplyDevice", "applyDevice",
    }
    for _, method in ipairs(internalMethods) do
        local ok, val = pcall(function() return testView[method] end)
        if ok and val ~= nil then
            table.insert(info, method .. "=[method:" .. type(val) .. "]")
        end
    end

    return table.concat(info, "\n")
end

-- 辅助函数：保存 testView 原始尺寸/缩放（用于恢复）
local mTestViewState = nil

local function saveTestViewState(testView)
    if mTestViewState then return end  -- 已保存，不重复
    mTestViewState = {
        viewWidth = testView.viewWidth or testView.width or 0,
        viewHeight = testView.viewHeight or testView.height or 0,
        width = testView.width or 0,
        height = testView.height or 0,
    }
    -- 保存 contentPane 状态
    pcall(function()
        local cp = testView.contentPane
        if cp then
            mTestViewState.cpWidth = cp.width
            mTestViewState.cpHeight = cp.height
            mTestViewState.cpScaleX = cp.scaleX or 1
            mTestViewState.cpScaleY = cp.scaleY or 1
        end
    end)
    fprint("[MCPBridge] 已保存 testView 原始状态: " ..
        string.format("view=%dx%d, content=%.0fx%.0f",
            mTestViewState.viewWidth, mTestViewState.viewHeight,
            mTestViewState.cpWidth or 0, mTestViewState.cpHeight or 0))
end

local function restoreTestViewState(testView)
    if not mTestViewState then return end
    pcall(function()
        if mTestViewState.viewWidth > 0 then
            testView.viewWidth = mTestViewState.viewWidth
        end
        if mTestViewState.viewHeight > 0 then
            testView.viewHeight = mTestViewState.viewHeight
        end
    end)
    -- 恢复 contentPane 状态
    pcall(function()
        local cp = testView.contentPane
        if cp and mTestViewState.cpWidth then
            cp:SetSize(mTestViewState.cpWidth, mTestViewState.cpHeight)
            cp:SetScale(mTestViewState.cpScaleX, mTestViewState.cpScaleY)
        end
    end)
    fprint("[MCPBridge] 已恢复 testView 原始状态")
    mTestViewState = nil
end

-- 辅助函数：设置 testView 的预览设备分辨率
-- Bug1 修复：绝对不调用 GRoot.inst:SetContentScaleFactor()，只影响 testView.contentPane
-- 返回 succeeded, methodsList
local function applyTestViewDevice(testView, resX, resY, scaleMode, screenMatchMode)
    local succeeded = false
    local methods = {}

    -- 策略1: 获取 contentPane 并设置其尺寸（最精确，只影响预览内容）
    local contentPane = nil
    pcall(function() contentPane = testView.contentPane end)
    if not contentPane then pcall(function() contentPane = testView.ContentPane end) end

    if contentPane then
        local ok1 = pcall(function()
            contentPane:SetSize(resX, resY)
        end)
        table.insert(methods, "contentPane:SetSize(" .. resX .. "," .. resY .. ")")
        if ok1 then succeeded = true end
    end

    -- 策略2: 设置 contentPane 的缩放比
    if contentPane and not succeeded then
        local adaptSettings = App.project:GetSettings("Adaptation")
        local designX = 0
        local designY = 0
        pcall(function()
            designX = adaptSettings.designResolutionX or 0
            designY = adaptSettings.designResolutionY or 0
        end)
        if designX > 0 and designY > 0 then
            local scaleX = resX / designX
            local scaleY = resY / designY
            local ok2 = pcall(function()
                contentPane:SetScale(scaleX, scaleY)
            end)
            table.insert(methods, "contentPane:SetScale(" .. string.format("%.3f", scaleX) .. "," .. string.format("%.3f", scaleY) .. ")")
            if ok2 then succeeded = true end
        end
    end

    -- 策略3: 设置 testView 的 viewWidth/viewHeight
    if not succeeded then
        local ok3 = pcall(function()
            testView.viewWidth = resX
            testView.viewHeight = resY
        end)
        table.insert(methods, "testView.viewWidth/Height=" .. resX .. "x" .. resY)
        if ok3 then succeeded = true end
    end

    -- 策略4: 设置 testView 的 size（SetSize）
    if not succeeded then
        local ok4 = pcall(function()
            testView:SetSize(resX, resY)
        end)
        table.insert(methods, "testView:SetSize(" .. resX .. "," .. resY .. ")")
        if ok4 then succeeded = true end
    end

    -- 策略5: 通过内部 Stage 设置（testView 独立的 stage，不影响编辑器全局 GRoot）
    if not succeeded then
        local stage = nil
        pcall(function() stage = testView.stage end)
        if not stage then pcall(function() stage = testView.Stage end) end

        if stage then
            local ok5 = pcall(function()
                stage:SetSize(resX, resY)
            end)
            table.insert(methods, "stage:SetSize(" .. resX .. "," .. resY .. ")")
            if ok5 then succeeded = true end
        end
    end

    -- 注意：策略6（GRoot.inst:SetContentScaleFactor）已删除，它会缩放整个编辑器UI

    fprint("[MCPBridge] 设备切换结果: succeeded=" .. tostring(succeeded) .. ", methods=[" .. table.concat(methods, "], [") .. "]")
    return succeeded, table.concat(methods, "; ")
end

-- 辅助函数：获取 testView 的预览内容 displayObject（用于精确截图）
-- 策略优先级：
--   1. testView.child[0]:GetChild("docContainer") - 整个预览容器，配合裁剪到模拟设备区域
--   2. testView 第一个子元素的 displayObject（整个预览面板，含编辑器UI）
--   3. contentPane.displayObject
--   4. testView.displayObject（回退）
-- 返回值：displayObject, source, [cropX, cropY, cropW, cropH] - 裁剪信息（可选）
-- 预览窗默认设成「适配 + 全屏」：默认的 FitSize 会把组件重新改尺寸塞进面板，
-- 与 DisplayObject.GetScreenShot 的取图区域对不上，回图会出现整块空白；
-- 全屏让设备屏铺满面板，截图区域与实际渲染区域一致
-- start_test 打开的预览窗默认没勾「适配」且适应屏幕停在别的档，回图就只剩上半屏。
-- 这里只写这两个开关本身（走控件自己的 setter，会触发编辑器正常重排）；
-- 不要用 SetSize 一类直接改布局对象的做法，那会绕过编辑器布局逻辑把预览卡死（实测需重启才恢复）
local function applyPreviewFit(testView)
    local applied = {}
    pcall(function()
        local inner = testView:GetChildAt(0)
        local adp = inner:GetChild("adaptation")
        if adp and not adp.selected then
            adp.selected = true
            table.insert(applied, "adaptation=on")
        end
        local sm = inner:GetChild("screenMatch")
        if sm and sm.selectedIndex ~= 1 then
            sm.selectedIndex = 1
            table.insert(applied, "screenMatch=1")
        end
    end)
    if #applied > 0 then
        fprint("[MCPBridge] 预览窗补设: " .. table.concat(applied, ", "))
    end
end

local function getTestViewCaptureTarget(testView)
    -- 策略1: docContainer + 裁剪到模拟设备屏幕（实测内容对齐正确；设备屏对象自身取图会整体偏移）
    if testView.numChildren and testView.numChildren > 0 then
        local child0 = nil
        pcall(function() child0 = testView:GetChildAt(0) end)
        if child0 then
            local docContainer = nil
            pcall(function() docContainer = child0:GetChild("docContainer") end)
            if docContainer and docContainer.numChildren > 0 then
                local deviceScreen = nil
                pcall(function() deviceScreen = docContainer:GetChildAt(0) end)
                if deviceScreen then
                    local dobj = nil
                    pcall(function() dobj = docContainer.displayObject end)
                    if dobj then
                        -- deviceScreen 在 docContainer 内的子元素的真实偏移
                        -- 通过 deviceScreen 自身坐标 + 它内部第一个子元素的偏移得到
                        local cropX = deviceScreen.x or 0
                        local cropY = deviceScreen.y or 0
                        if deviceScreen.numChildren and deviceScreen.numChildren > 0 then
                            local inner = nil
                            pcall(function() inner = deviceScreen:GetChildAt(0) end)
                            if inner then
                                cropX = cropX + (inner.x or 0)
                                cropY = cropY + (inner.y or 0)
                            end
                        end
                        local cropW = deviceScreen.width or 0
                        local cropH = deviceScreen.height or 0
                        fprint(string.format("[MCPBridge] 截图目标(docContainer + crop): %sx%s at (%s,%s)",
                            tostring(cropW), tostring(cropH),
                            tostring(cropX), tostring(cropY)))
                        return dobj, "docContainer_cropped", cropX, cropY, cropW, cropH
                    end
                end
            end
        end
    end

    -- 策略2: docContainer 整体（含设备外灰色区域）
    if testView.numChildren and testView.numChildren > 0 then
        local child0 = nil
        pcall(function() child0 = testView:GetChildAt(0) end)
        if child0 then
            local docContainer = nil
            pcall(function() docContainer = child0:GetChild("docContainer") end)
            if docContainer then
                local dobj = nil
                pcall(function() dobj = docContainer.displayObject end)
                if dobj then
                    return dobj, "docContainer"
                end
            end
        end
    end

    -- 策略3（旧逻辑）: testView.child[0].displayObject
    if testView.numChildren and testView.numChildren > 0 then
        local child = nil
        pcall(function() child = testView:GetChildAt(0) end)
        if child then
            local dobj = nil
            pcall(function() dobj = child.displayObject end)
            if dobj then
                return dobj, "child0_" .. tostring(child.name)
            end
        end
    end

    -- 策略4: contentPane.displayObject
    local contentPane = nil
    pcall(function() contentPane = testView.contentPane end)
    if not contentPane then pcall(function() contentPane = testView.ContentPane end) end

    if contentPane then
        local dobj = nil
        pcall(function() dobj = contentPane.displayObject end)
        if dobj then
            return dobj, "contentPane"
        end
    end

    -- 策略5: testView.displayObject（最后回退）
    local dobj = nil
    pcall(function() dobj = testView.displayObject end)
    if dobj then
        return dobj, "testView_displayObject"
    end

    return nil, "none"
end

-- 启动预览测试（F5）
-- FIX-1: device_name 通过延迟定时器 + 多策略设置设备分辨率
-- FIX-2: 组件大于 testView 可视区域时自动调整 testView 尺寸
function CommandHandler.handleStartTest(params, bridgePath)
    local pkgName = params.package_name
    local compName = params.component_name
    local deviceName = params.device_name

    if not pkgName or not compName then
        error("缺少参数: package_name 或 component_name")
    end

    local pkg = App.project:GetPackageByName(pkgName)
    if not pkg then
        error("包不存在: " .. pkgName)
    end

    local item = findComponentItem(pkg, compName)
    if not item then
        error("组件不存在: " .. compName)
    end

    -- 如果 testView 已在运行，先停止再重新启动，避免缓存状态导致显示上一次的组件
    local testView = App.testView
    if testView and testView.running then
        fprint("[MCPBridge] testView 正在运行，先停止再重新启动")
        testView:Stop()
    end

    -- F5 预览运行的是编辑器当前打开的组件（activeDoc），而非 Start 参数指定的组件
    -- 因此必须先打开目标组件文档，确保 activeDoc 指向正确组件
    local url = item:GetURL()
    App.docView:OpenDocument(url, true)

    -- 启动 F5 预览测试
    App.testView:Start(item)
    -- F5 模式会设置 runInBackground = false，导致窗口失焦后定时器停止
    -- 启动后立即重置，确保后续命令能正常轮询
    CS.UnityEngine.Application.runInBackground = true

    -- Start 内部异步，等初始化完成再补设适配档，否则会被 Start 的重排覆盖
    CS.FairyGUI.Timers.inst:Add(0.6, 1, function()
        local tv = App.testView
        if tv and tv.running then
            applyPreviewFit(tv)
        end
    end)

    -- FIX-1: 如果指定了设备，通过延迟定时器切换分辨率
    -- testView:Start 是异步的，立即设置可能不生效
    local currentDevice = "default"
    local resX = 0
    local resY = 0
    local deviceFound = false

    if deviceName then
        local devInfo = findDeviceInfo(deviceName)
        if devInfo and devInfo.found then
            resX = devInfo.resolutionX
            resY = devInfo.resolutionY
            currentDevice = deviceName
            deviceFound = true

            -- FIX-2: 增加延迟到 0.5s，确保 testView:Start 内部初始化完成
            CS.FairyGUI.Timers.inst:Add(0.5, 1, function()
                local tv = App.testView
                if tv and tv.running then
                    -- 保存原始状态以便恢复
                    saveTestViewState(tv)
                    applyTestViewDevice(tv, resX, resY, devInfo.scaleMode, devInfo.screenMatchMode)
                else
                    fprint("[MCPBridge] testView 未运行，无法设置设备分辨率")
                end
            end)
        else
            -- 设备未找到，打印所有可用设备名称用于调试
            fprint("[MCPBridge] 设备未找到: '" .. deviceName .. "'")
            local adaptSettings = App.project:GetSettings("Adaptation")
            if adaptSettings then
                if adaptSettings.defaultDevices then
                    for i = 0, adaptSettings.defaultDevices.Count - 1 do
                        local dev = adaptSettings.defaultDevices[i]
                        fprint("[MCPBridge]  默认设备[" .. i .. "]: '" .. dev.name .. "' (" .. dev.resolutionX .. "x" .. dev.resolutionY .. ")")
                    end
                end
                if adaptSettings.devices then
                    for i = 0, adaptSettings.devices.Count - 1 do
                        local dev = adaptSettings.devices[i]
                        fprint("[MCPBridge]  自定义设备[" .. i .. "]: '" .. dev.name .. "' (" .. dev.resolutionX .. "x" .. dev.resolutionY .. ")")
                    end
                end
            end
        end
    end

    -- FIX-2: 如果组件尺寸大于 testView 可视区域，调整 testView 尺寸确保组件完整可见
    local compW = item.width or 0
    local compH = item.height or 0

    CS.FairyGUI.Timers.inst:Add(0.5, 1, function()
        local tv = App.testView
        if not tv or not tv.running then return end
        if compW <= 0 or compH <= 0 then return end

        local viewW = tv.viewWidth > 0 and tv.viewWidth or tv.width
        local viewH = tv.viewHeight > 0 and tv.viewHeight or tv.height
        if viewW <= 0 or viewH <= 0 then return end

        if compW > viewW or compH > viewH then
            pcall(function()
                tv.viewWidth = compW
                tv.viewHeight = compH
            end)
            fprint("[MCPBridge] 组件(" .. compW .. "x" .. compH .. ")大于预览区域(" .. viewW .. "x" .. viewH .. ")，已调整 testView 尺寸")
        end
    end)

    return {
        started = true,
        item_name = item.name,
        item_id = item.id,
        component = compName,
        package = pkgName,
        component_size = { width = compW, height = compH },
        device = currentDevice,
        device_found = deviceFound,
        resolutionX = resX,
        resolutionY = resY
    }
end

-- 停止预览测试
function CommandHandler.handleStopTest(params, bridgePath)
    local testView = App.testView
    if testView and testView.running then
        -- 恢复设备切换前的原始尺寸/缩放
        restoreTestViewState(testView)
        testView:Stop()
        -- 停止 F5 后重置 runInBackground，确保编辑器恢复正常后台运行
        CS.UnityEngine.Application.runInBackground = true
        return { stopped = true }
    end
    return { stopped = false, message = "预览未运行" }
end

-- 切换设备分辨率
function CommandHandler.handleSwitchDevice(params, bridgePath)
    local deviceName = params.device_name

    if not deviceName then
        error("缺少参数: device_name")
    end

    local testView = App.testView
    if not testView or not testView.running then
        error("预览未运行，请先调用 start_test")
    end

    -- 保存原始状态（如果还未保存）
    saveTestViewState(testView)

    local devInfo = findDeviceInfo(deviceName)
    if not devInfo or not devInfo.found then
        error("设备未找到: " .. deviceName)
    end

    local resX = devInfo.resolutionX
    local resY = devInfo.resolutionY
    local _, methods = applyTestViewDevice(testView, resX, resY, devInfo.scaleMode, devInfo.screenMatchMode)

    return {
        switched = true,
        device = deviceName,
        resolutionX = resX,
        resolutionY = resY,
        methods = methods
    }
end

-- 预览结构旁证：截图"缺内容"时必须能分辨是界面没画出来，还是预览面板装不下，
-- 两者处置相反。结构随图落盘，避免依赖交互式 probe 通道
local function dumpPreviewStructure(testView)
    local lines = {}
    local function typeName(obj)
        local n = "?"
        pcall(function() n = tostring(obj:GetType().Name) end)
        return n
    end
    local function walk(node, depth)
        if not node or depth > 3 or #lines > 60 then return end
        pcall(function()
            table.insert(lines, string.format("%s%s [%s] %sx%s visible=%s",
                string.rep("  ", depth), tostring(node.name or ""), typeName(node),
                tostring(node.width or 0), tostring(node.height or 0), tostring(node.visible)))
        end)
        local count = 0
        pcall(function() count = node.numChildren or 0 end)
        for i = 0, count - 1 do
            local ok, child = pcall(function() return node:GetChildAt(i) end)
            if ok then walk(child, depth + 1) end
        end
    end
    pcall(function()
        table.insert(lines, "GRoot=" .. tostring(GRoot.inst.width) .. "x" .. tostring(GRoot.inst.height))
    end)
    walk(testView, 0)
    pcall(function()
        local tb = App.mainView.toolbar
        if not tb then return end
        table.insert(lines, "toolbar:")
        for i = 0, 60 do
            local ok, c = pcall(function() return tb:GetChildAt(i) end)
            if not ok or not c then break end
            pcall(function()
                table.insert(lines, "  " .. tostring(c.name) .. " [" .. typeName(c) .. "] text=" .. tostring(c.text or ""))
            end)
        end
    end)
    pcall(function()
        local inner = testView:GetChildAt(0)
        local adp = inner:GetChild("adaptation")
        local sm = inner:GetChild("screenMatch")
        local dv = inner:GetChild("device")
        local function tryget(o, p)
            if not o then return "<nil-obj>" end
            local ok, v = pcall(function() return o[p] end)
            if ok then return tostring(v) end
            return "?"
        end
        table.insert(lines, "adaptation.selected=" .. tryget(adp, "selected"))
        table.insert(lines, "screenMatch.value=" .. tryget(sm, "value")
            .. " selectedIndex=" .. tryget(sm, "selectedIndex")
            .. " itemsCount=" .. tryget(sm, "itemsCount"))
        local okItems, items = pcall(function() return sm.items end)
        if okItems and items then
            local n = 0
            pcall(function() n = items.Count end)
            for i = 0, n - 1 do
                pcall(function()
                    local it = items[i]
                    table.insert(lines, "  sm_item[" .. i .. "] text=" .. tostring(it.text)
                        .. " value=" .. tostring(it.value))
                end)
            end
        end
        table.insert(lines, "device.value=" .. tryget(dv, "value")
            .. " selectedIndex=" .. tryget(dv, "selectedIndex"))
    end)
    -- 截图候选链逐个定位：谁包含谁、谁被缩放、谁在舞台的什么位置
    pcall(function()
        local chain = {}
        local node = testView
        table.insert(chain, node)
        local cur = node
        for _ = 1, 6 do
            local nxt = nil
            pcall(function()
                if cur and cur.numChildren and cur.numChildren > 0 then
                    if cur.name == "docContainer" then
                        nxt = cur:GetChildAt(0)
                    else
                        local dc = nil
                        pcall(function() dc = cur:GetChild("docContainer") end)
                        nxt = dc or cur:GetChildAt(0)
                    end
                end
            end)
            if not nxt then break end
            table.insert(chain, nxt)
            cur = nxt
        end
        for _, o in ipairs(chain) do
            local gx, gy = "?", "?"
            pcall(function()
                local p = o:LocalToGlobal(0, 0)
                gx, gy = string.format("%.1f", p.x), string.format("%.1f", p.y)
            end)
            local sx, sy, vw, vh = "?", "?", "?", "?"
            pcall(function() sx = string.format("%.3f", o.scaleX) end)
            pcall(function() sy = string.format("%.3f", o.scaleY) end)
            pcall(function() vw = o.viewWidth end)
            pcall(function() vh = o.viewHeight end)
            table.insert(lines, string.format("chain %s [%s] size=%sx%s scale=%s,%s global=(%s,%s) view=%sx%s",
                tostring((function() local n = "?"; pcall(function() n = o.name end); return n end)()),
                typeName(o),
                tostring((function() local w = "?"; pcall(function() w = o.width end); return w end)()),
                tostring((function() local h = "?"; pcall(function() h = o.height end); return h end)()),
                tostring(sx), tostring(sy), tostring(gx), tostring(gy), tostring(vw), tostring(vh)))
        end
    end)
    return table.concat(lines, "\n")
end

-- 截取预览截图
-- FIX-3: 使用 getTestViewCaptureTarget 精确定位截图目标，而非 GRoot.inst 全屏
function CommandHandler.handleCapturePreview(params, bridgePath)
    local saveName = params.save_name or ("preview_" .. os.time())
    local deviceName = params.device_name
    local scale = params.scale or 1

    local testView = App.testView
    if not testView or not testView.running then
        error("预览未运行，请先调用 start_test")
    end

    -- 如果指定了设备，先切换
    if deviceName then
        CommandHandler.handleSwitchDevice({ device_name = deviceName }, bridgePath)
        saveName = saveName .. "_" .. deviceName:gsub(" ", "_")
    end

    local screenshotPath = bridgePath:gsub("/", "\\") .. "\\screenshots\\" .. saveName .. ".png"

    -- FIX-3: 精确截图 -- 使用 getTestViewCaptureTarget 查找最佳截图目标
    -- 返回值可能包含裁剪信息（cropX, cropY, cropW, cropH）
    local captureObj, captureSource, cropX, cropY, cropW, cropH = getTestViewCaptureTarget(testView)
    if not captureObj then
        error("无法获取预览渲染对象")
    end

    captureDisplayObject(captureObj, screenshotPath, scale, cropX, cropY, cropW, cropH)

    pcall(function()
        CS.System.IO.File.WriteAllText(
            screenshotPath:gsub("%.[Pp][Nn][Gg]$", "") .. ".probe.txt",
            dumpPreviewStructure(testView))
    end)

    return {
        captured = true,
        screenshot = saveName .. ".png",
        path = screenshotPath,
        capture_source = captureSource
    }
end

-- 列出可用设备
function CommandHandler.handleListDevices(params, bridgePath)
    local adaptSettings = App.project:GetSettings("Adaptation")
    if not adaptSettings then
        error("无法获取适配设置")
    end

    local devices = {}

    -- 默认设备
    if adaptSettings.defaultDevices then
        for i = 0, adaptSettings.defaultDevices.Count - 1 do
            local dev = adaptSettings.defaultDevices[i]
            table.insert(devices, {
                name = dev.name,
                resolutionX = dev.resolutionX,
                resolutionY = dev.resolutionY,
                source = "default"
            })
        end
    end

    -- 自定义设备
    if adaptSettings.devices then
        for i = 0, adaptSettings.devices.Count - 1 do
            local dev = adaptSettings.devices[i]
            table.insert(devices, {
                name = dev.name,
                resolutionX = dev.resolutionX,
                resolutionY = dev.resolutionY,
                source = "custom"
            })
        end
    end

    -- 当前适配设置
    local scaleMode = adaptSettings.scaleMode or "unknown"
    local screenMathMode = adaptSettings.screenMatchMode or "unknown"
    local designX = adaptSettings.designResolutionX or 0
    local designY = adaptSettings.designResolutionY or 0

    return {
        devices = devices,
        count = #devices,
        scaleMode = scaleMode,
        screenMatchMode = screenMathMode,
        designResolution = { x = designX, y = designY }
    }
end

-- ========== 控制器操作命令 ==========

-- 切换控制器状态
function CommandHandler.handleSwitchController(params, bridgePath)
    local controllerName = params.controller_name
    local pageIndex = params.page_index
    local pageName = params.page_name

    if not controllerName then
        error("缺少参数: controller_name")
    end

    local doc = App.activeDoc
    if not doc then
        error("没有打开的文档")
    end

    local component = doc.content
    if not component then
        error("无法获取文档组件")
    end

    -- 从 controllers 集合中按名称查找
    local ctrl = nil
    local ctrls = component.controllers
    if ctrls then
        for i = 0, ctrls.Count - 1 do
            local c = ctrls[i]
            if c.name == controllerName then
                ctrl = c
                break
            end
        end
    end

    if not ctrl then
        error("控制器不存在: " .. controllerName)
    end

    local oldIndex = ctrl.selectedIndex
    local totalPages = ctrl.pageCount

    if pageName then
        -- 按页名称切换：从 XML 解析页面名称匹配索引
        local pkgName = ""
        local compName = ""
        pcall(function()
            pkgName = doc.packageItem.owner.name
            compName = doc.packageItem.name:gsub("%.xml$", "")
        end)
        error("page_name 暂不支持在编辑器端使用，请使用 page_index 代替（可先用 list_controllers 获取页面索引）")
    elseif pageIndex ~= nil then
        -- 按页索引切换
        if pageIndex < 0 or pageIndex >= totalPages then
            error("页索引超出范围: " .. pageIndex .. "（总页数: " .. totalPages .. "）")
        end
        ctrl.selectedIndex = pageIndex
    else
        error("缺少参数: page_index 或 page_name")
    end

    return {
        switched = true,
        controller = controllerName,
        oldIndex = oldIndex,
        newIndex = ctrl.selectedIndex,
        totalPages = totalPages
    }
end

-- 列出控制器
function CommandHandler.handleListControllers(params, bridgePath)
    local doc = App.activeDoc
    if not doc then
        error("没有打开的文档")
    end

    local content = doc.content
    if not content then
        error("无法获取文档组件")
    end

    local ctrls = content.controllers
    if not ctrls then
        return { controllers = {}, count = 0, component = doc.displayTitle or "unknown" }
    end

    local controllers = {}

    for i = 0, ctrls.Count - 1 do
        local ctrl = ctrls[i]
        table.insert(controllers, {
            name = ctrl.name,
            selectedIndex = ctrl.selectedIndex,
            pageCount = ctrl.pageCount,
            alias = ctrl.alias or "",
            exported = ctrl.exported or false
        })
    end

    -- 获取包名和组件名，供 Python 侧从 XML 补充页面名称
    local pkgName = ""
    local compName = ""
    local ok, _ = pcall(function()
        pkgName = doc.packageItem.owner.name
        compName = doc.packageItem.name:gsub("%.xml$", "")
    end)

    return {
        controllers = controllers,
        count = #controllers,
        component = doc.displayTitle or "unknown",
        package_name = pkgName,
        component_name = compName
    }
end

-- ========== 选择元素命令 (NEW-1) ==========

-- 在编辑器中选中指定元素
--
-- 已知限制（FairyEditor 私有 API 限制，无解）：
-- 右侧检查器面板的渲染源是 Document.inspectingTarget 字段，该字段是 C# 的
-- { get; private set; } 只读属性，setter 由编辑器 selectionLayer 鼠标事件链路
-- 内部触发，外部 Lua 无法写入。这意味着本工具只能更新 selection（驱动画布选中
-- 框），无法让右侧检查器面板自动切换到所选元素的属性。如需查看属性，必须由人
-- 工在编辑器中手动点击元素。
--
-- 实现采用 UnselectAll + SelectObject 组合：
-- - UnselectAll：清空旧选择，避免 SelectObject 把目标加入而不是替换
-- - SelectObject(obj, scrollToView, allowOpenGroups)：编辑器内部选中元素的标
--   准 API，会同步 selection 集合并触发画布选中框跟随
function CommandHandler.handleSelectElement(params, bridgePath)
    local elementName = params.element_name

    if not elementName then
        error("缺少参数: element_name")
    end

    local doc = App.activeDoc
    if not doc then
        error("没有打开的文档")
    end

    -- doc.content 类型为 FairyEditor.FComponent
    -- FComponent:GetChild(name) 返回 FairyEditor.FObject（不是 FairyGUI.GObject）
    -- Document:SelectObject 期望的也是 FObject，类型完全匹配
    local content = doc.content
    if not content then
        error("无法获取文档组件")
    end

    -- 策略1: 直接按名称查找
    local targetChild = nil
    pcall(function()
        targetChild = content:GetChild(elementName)
    end)

    -- 策略2: 遍历 FComponent.children 列表兜底
    if not targetChild then
        pcall(function()
            local children = content.children
            if children and children.Count then
                for i = 0, children.Count - 1 do
                    local child = children[i]
                    if child and child.name == elementName then
                        targetChild = child
                        break
                    end
                end
            end
        end)
    end

    if not targetChild then
        error("元素不存在: " .. elementName)
    end

    -- UnselectAll + SelectObject 组合：先清空旧选择再添加目标，等价于"独占选中"
    pcall(function() doc:UnselectAll() end)

    local selectOk, selectErr = pcall(function()
        doc:SelectObject(targetChild, true, true)
    end)

    if not selectOk then
        error("选中失败: " .. tostring(selectErr))
    end

    -- 尝试通过 docFactory:InvokeDocumentMethod 调用 Document 内部非 public 方法
    -- 来同步 inspectingTarget。这是 FairyEditor 给 Lua 插件预留的反射调用入口。
    local inspectorSyncMethod = "none"
    local inspectorSynced = false
    pcall(function()
        local methodCandidates = {
            "InspectObject", "Inspect", "SetInspectingTarget",
            "DoInspect", "UpdateInspectingTarget",
        }
        for _, methodName in ipairs(methodCandidates) do
            local args = CS.System.Array.CreateInstance(typeof(CS.System.Object), 1)
            args:SetValue(targetChild, 0)
            local invokeOk = pcall(function()
                App.docFactory:InvokeDocumentMethod(methodName, args)
            end)
            if invokeOk then
                -- 检查 inspectingTarget 是否真的被改了
                if doc.inspectingTarget and doc.inspectingTarget.name == elementName then
                    inspectorSynced = true
                    inspectorSyncMethod = "InvokeDocumentMethod(" .. methodName .. ")"
                    break
                end
            end
        end
    end)

    -- 校验 selection 是否真的更新到目标元素
    local selectionVerified = false
    pcall(function()
        local sel = doc:GetSelection()
        if sel and sel.Count == 1 and sel[0] and sel[0].name == elementName then
            selectionVerified = true
        end
    end)

    return {
        selected = true,
        element_name = elementName,
        selection_verified = selectionVerified,
        inspector_synced = inspectorSynced,
        inspector_sync_method = inspectorSyncMethod,
        note = inspectorSynced
            and "selection 已更新且检查器面板已同步"
            or "selection 已更新（画布选中框已切换）。检查器面板的 inspectingTarget 是 FairyEditor 私有 setter，外部 Lua 无法写入；如需查看元素属性请在编辑器中手动点击。"
    }
end

-- ========== 开发诊断命令 ==========

-- 列出插件管理器已加载的全部插件名
function CommandHandler.handleDevListPlugins(params, bridgePath)
    local names = {}
    pcall(function()
        local mgr = App.pluginManager
        local plugins = mgr.allPlugins
        for i = 0, plugins.Count - 1 do
            local info = plugins[i]
            local n = nil
            pcall(function() n = info.name end)
            table.insert(names, tostring(n))
        end
    end)
    return { count = #names, plugins = names }
end

-- 倒出 fprint 日志环形缓冲(MCPBridge main.lua 拦截收集,含其他插件输出)
function CommandHandler.handleDevLogs(params, bridgePath)
    local buf = _G._mcpLogBuffer or {}
    local total = #buf
    local tailN = tonumber(params.tail) or 200
    local startIdx = math.max(1, total - tailN + 1)
    local logs = {}
    for i = startIdx, total do
        table.insert(logs, buf[i])
    end
    local filter = params.filter
    if filter then
        local filtered = {}
        for _, entry in ipairs(logs) do
            if entry.message and string.find(entry.message, filter, 1, true) then
                table.insert(filtered, entry)
            end
        end
        logs = filtered
    end
    return { total = total, returned = #logs, logs = logs }
end

-- 读取活动文档指定元素的文本特效属性(类型+值)
function CommandHandler.handleDevTextProps(params, bridgePath)
    local doc = App.activeDoc
    if not doc then
        return { error = "没有打开的文档" }
    end
    local comp = nil
    pcall(function() comp = doc.content end)
    if not comp then
        return { error = "文档无内容" }
    end
    local targetName = params.element_name
    local obj = nil
    local children = comp.children
    for i = 0, children.Count - 1 do
        local child = children[i]
        local cname = nil
        pcall(function() cname = child.name end)
        if cname == targetName then
            obj = child
            break
        end
    end
    if not obj then
        return { error = "未找到元素: " .. tostring(targetName) }
    end

    local props = { "objectType", "stroke", "strokeSize", "strokeColor", "shadow", "shadowX", "shadowY", "shadowColor", "customData" }
    local out = {}
    for _, p in ipairs(props) do
        local ok, v = pcall(function() return obj[p] end)
        if ok then
            out[p] = { vtype = type(v), value = tostring(v) }
        else
            out[p] = { error = tostring(v) }
        end
    end
    return { element = targetName, props = out }
end

-- 受控 SetProperty 测试:写入→读回→恢复原值(不保存文档)
-- params: element_name, prop, value(标量)
function CommandHandler.handleDevSetTest(params, bridgePath)
    local doc = App.activeDoc
    if not doc then
        return { error = "没有打开的文档" }
    end
    local comp = nil
    pcall(function() comp = doc.content end)
    if not comp then
        return { error = "文档无内容" }
    end
    local targetName = params.element_name
    local obj = nil
    local children = comp.children
    for i = 0, children.Count - 1 do
        local child = children[i]
        local cname = nil
        pcall(function() cname = child.name end)
        if cname == targetName then
            obj = child
            break
        end
    end
    if not obj then
        return { error = "未找到元素: " .. tostring(targetName) }
    end

    local function readProps()
        local out = {}
        for _, p in ipairs({ "stroke", "strokeSize", "shadow", "customData" }) do
            local ok, v = pcall(function() return obj[p] end)
            out[p] = ok and tostring(v) or ("ERR:" .. tostring(v))
        end
        return out
    end

    local prop = params.prop
    if prop == nil then
        return { error = "缺少参数: prop" }
    end

    local log = { prop = prop, value = tostring(params.value), before = readProps() }
    local orig = nil
    local origOk = pcall(function() orig = obj[prop] end)

    local setOk, setErr = pcall(function()
        obj.docElement:SetProperty(prop, params.value)
    end)
    log.set_ok = setOk
    log.after_set = readProps()
    if not setOk then
        log.set_err = tostring(setErr)
    end

    if origOk and orig ~= nil and not params.no_restore then
        local rOk, rErr = pcall(function()
            obj.docElement:SetProperty(prop, orig)
        end)
        log.restore_ok = rOk
        if not rOk then
            log.restore_err = tostring(rErr)
        end
    else
        log.restore_skipped = "原值为 nil"
    end
    log.after_restore = readProps()
    return log
end

-- 验证 App.onUpdate 分发是否工作:首次调用注册计数器,后续调用返回计数
function CommandHandler.handleDevOnUpdateTest(params, bridgePath)
    if _G.__devOnUpdateCount == nil then
        _G.__devOnUpdateCount = 0
        App.onUpdate("+", function()
            _G.__devOnUpdateCount = _G.__devOnUpdateCount + 1
        end)
        return { registered = true, count = 0 }
    end
    return { registered = true, count = _G.__devOnUpdateCount }
end

-- 复现 CustomShadow 变化检测循环,逐步报告每控制器的读取结果
function CommandHandler.handleDevCtrlProbe(params, bridgePath)
    local doc = App.activeDoc
    if not doc then
        return { error = "没有打开的文档" }
    end
    local comp = nil
    pcall(function() comp = doc.content end)
    if not comp then
        return { error = "文档无内容" }
    end
    local out = {}
    local loopOk, loopErr = pcall(function()
        local ctrlList = comp.controllers
        for i = 0, ctrlList.Count - 1 do
            local ctrl = ctrlList[i]
            local entry = {}
            local ok, v = pcall(function() return ctrl.name end)
            entry.name = ok and tostring(v) or ("ERR:" .. tostring(v))
            ok, v = pcall(function() return tostring(ctrl.selectedPageId) end)
            entry.selectedPageId = ok and tostring(v) or ("ERR:" .. tostring(v))
            ok, v = pcall(function() return tostring(ctrl.selectedIndex) end)
            entry.selectedIndex = ok and tostring(v) or ("ERR:" .. tostring(v))
            table.insert(out, entry)
        end
    end)
    return { loop_ok = loopOk, loop_err = not loopOk and tostring(loopErr) or nil, controllers = out }
end

-- 验证 xLua 包装器身份稳定性:同帧两次读取 + 跨帧暂存比较
function CommandHandler.handleDevIdentityProbe(params, bridgePath)
    local d1 = App.activeDoc
    local d2 = App.activeDoc
    local result = {
        same_frame_doc_equal = tostring(d1 == d2),
        doc_type = type(d1)
    }
    if d1 ~= nil then
        local c1 = nil
        local c2 = nil
        pcall(function() c1 = d1.content end)
        pcall(function() c2 = d1.content end)
        result.same_frame_content_equal = tostring(c1 == c2)
        result.content_type = type(c1)
    end
    if _G.__devIdentityDoc == nil then
        _G.__devIdentityDoc = d1
        result.cross_frame = "first call, stored"
    else
        result.cross_frame_doc_equal = tostring(_G.__devIdentityDoc == d1)
    end
    return result
end

-- ========== 插件管理命令 ==========

-- 探查插件管理 API
function CommandHandler.handleProbePluginApi(params, bridgePath)
    local found = {}
    local target = params.target or "overview"

    if target == "overview" then
        -- 探查 App 上与插件相关的属性
        local appProps = {
            "pluginManager", "PluginManager", "pluginMgr", "PluginMgr",
            "plugins", "Plugins", "pluginSystem", "PluginSystem",
            "luaEnv", "LuaEnv", "luaManager", "LuaManager",
        }

        for _, prop in ipairs(appProps) do
            local ok, val = pcall(function() return App[prop] end)
            if ok and val ~= nil then
                table.insert(found, {
                    path = "App." .. prop,
                    valtype = type(val),
                    value = tostring(val)
                })
            end
        end

    elseif target == "pluginManager" then
        -- 深入探查 App.pluginManager 的属性和方法
        local mgr = App.pluginManager
        local props = {
            "allPlugins", "plugins", "loadedPlugins", "pluginList",
            "count", "Count",
            "ReloadAll", "Reload", "ReloadPlugin",
            "LoadPlugin", "UnloadPlugin",
            "LoadAll", "StopAll", "RestartAll",
            "Dispose",
        }

        for _, prop in ipairs(props) do
            local ok, val = pcall(function() return mgr[prop] end)
            if ok and val ~= nil then
                table.insert(found, {
                    path = "pluginManager." .. prop,
                    valtype = type(val),
                    value = tostring(val)
                })
            end
        end

        -- 探查 allPlugins 中的第一个插件信息
        local ok, plugins = pcall(function() return mgr.allPlugins end)
        if ok and plugins then
            local pcount = plugins.Count
            table.insert(found, { path = "allPlugins.Count", valtype = "number", value = tostring(pcount) })

            if pcount > 0 then
                local p0 = plugins[0]
                local infoProps = {"name", "id", "path", "enabled", "loaded", "running",
                                   "version", "desc", "author",
                                   "Reload", "reload", "Restart", "restart",
                                   "Start", "start", "Stop", "stop",
                                   "Load", "load", "Unload", "unload"}
                for _, ip in ipairs(infoProps) do
                    local ok2, val2 = pcall(function() return p0[ip] end)
                    if ok2 and val2 ~= nil then
                        table.insert(found, {
                            path = "pluginInfo[0]." .. ip,
                            valtype = type(val2),
                            value = tostring(val2)
                        })
                    end
                end
            end
        end

    elseif target == "luaManager" then
        -- 探查 LuaManager
        local ok, lm = pcall(function() return CS.FairyEditor.LuaManager end)
        if ok and lm then
            local props = {
                "inst", "Instance", "instance",
                "Reload", "reload", "ReloadAll", "reloadAll",
                "RestartAll", "restartAll",
                "LoadScript", "loadScript",
                "DoFile", "doFile",
            }
            for _, prop in ipairs(props) do
                local ok2, val = pcall(function() return lm[prop] end)
                if ok2 and val ~= nil then
                    table.insert(found, {
                        path = "LuaManager." .. prop,
                        valtype = type(val),
                        value = tostring(val)
                    })
                end
            end
        end

    elseif target == "console" then
        -- 探查控制台/日志相关 API
        local csTypes = {
            "Console", "OutputPanel", "LogManager", "OutputManager",
            "TraceManager", "MessageManager", "OutputView",
        }
        for _, t in ipairs(csTypes) do
            local ok, val = pcall(function() return CS.FairyEditor[t] end)
            if ok and val ~= nil then
                table.insert(found, { path = "CS.FairyEditor." .. t, valtype = type(val), value = tostring(val) })
            end
        end

    elseif target == "console_deep" then
        -- 深入探查 CS.FairyEditor.Console 实例
        local console = CS.FairyEditor.Console
        if not console then
            table.insert(found, { path = "CS.FairyEditor.Console", valtype = "nil", value = "not found" })
        else
            -- 获取 inst 单例
            local inst = nil
            local ok, v = pcall(function() return console.inst end)
            if ok and v ~= nil then inst = v end

            local obj = inst or console
            local prefix = inst and "Console.inst" or "Console"

            local subProps = {
                "logs", "Logs", "items", "Items", "messages", "Messages",
                "entries", "Entries", "records", "Records", "list", "List",
                "GetLogs", "getLogs", "GetMessages", "getMessages", "GetEntries", "GetItems",
                "Clear", "clear", "ClearAll", "clearAll", "ClearLogs", "Reset",
                "Count", "count", "length", "Length",
                "view", "View", "panel", "Panel", "content", "Content",
            }
            for _, sp in ipairs(subProps) do
                local ok2, val2 = pcall(function() return obj[sp] end)
                if ok2 and val2 ~= nil then
                    local vtype = type(val2)
                    local vstr = tostring(val2)
                    -- 如果是集合，尝试获取 Count
                    if vtype == "table" and val2.Count ~= nil then
                        local ok3, cnt = pcall(function() return val2.Count end)
                        if ok3 then vstr = vstr .. " [Count=" .. tostring(cnt) .. "]" end
                    end
                    table.insert(found, { path = prefix .. "." .. sp, valtype = vtype, value = vstr })
                end
            end
        end

    elseif target == "pluginSystem" then
        -- 探查 PluginSystem
        local ok, ps = pcall(function() return CS.FairyEditor.PluginSystem end)
        if ok and ps then
            local props = {
                "inst", "Instance", "instance",
                "Reload", "reload", "ReloadAll", "reloadAll",
                "Restart", "restart", "RestartAll", "restartAll",
                "LoadAll", "loadAll",
            }
            for _, prop in ipairs(props) do
                local ok2, val = pcall(function() return ps[prop] end)
                if ok2 and val ~= nil then
                    table.insert(found, {
                        path = "PluginSystem." .. prop,
                        valtype = type(val),
                        value = tostring(val)
                    })
                end
            end
        end

    elseif target == "preview_scale" then
        -- 探查预览缩放相关属性
        local tv = App.testView
        if tv and tv.numChildren and tv.numChildren > 0 then
            local child0 = nil
            pcall(function() child0 = tv:GetChildAt(0) end)
            if child0 then
                -- 探查 contentScaler 控件
                local scaler = nil
                pcall(function() scaler = child0:GetChild("contentScaler") end)
                if scaler then
                    local props = {"value", "title", "text", "selected", "selectedIndex"}
                    for _, p in ipairs(props) do
                        local ok, val = pcall(function() return scaler[p] end)
                        if ok and val ~= nil then
                            table.insert(found, { path = "contentScaler." .. p, value = tostring(val) })
                        end
                    end
                end

                -- 探查 docContainer 内部元素的缩放
                local docContainer = nil
                pcall(function() docContainer = child0:GetChild("docContainer") end)
                if docContainer and docContainer.numChildren > 0 then
                    table.insert(found, {
                        path = "docContainer",
                        value = string.format("%sx%s, scaleX=%s, scaleY=%s",
                            tostring(docContainer.width or 0), tostring(docContainer.height or 0),
                            tostring(docContainer.scaleX or 1), tostring(docContainer.scaleY or 1))
                    })
                    local devCont = nil
                    pcall(function() devCont = docContainer:GetChildAt(0) end)
                    if devCont then
                        table.insert(found, {
                            path = "deviceContainer",
                            value = string.format("%sx%s, scaleX=%s, scaleY=%s, x=%s, y=%s",
                                tostring(devCont.width or 0), tostring(devCont.height or 0),
                                tostring(devCont.scaleX or 1), tostring(devCont.scaleY or 1),
                                tostring(devCont.x or 0), tostring(devCont.y or 0))
                        })
                    end
                end
            end
        end

    elseif target == "docContainer_deep" then
        -- 探查 docContainer.child[0] 里面的内容
        local tv = App.testView
        if tv and tv.numChildren and tv.numChildren > 0 then
            local child0 = nil
            pcall(function() child0 = tv:GetChildAt(0) end)
            if child0 then
                local docContainer = nil
                pcall(function() docContainer = child0:GetChild("docContainer") end)
                if docContainer and docContainer.numChildren > 0 then
                    local devCont = nil
                    pcall(function() devCont = docContainer:GetChildAt(0) end)
                    if devCont then
                        local n = devCont.numChildren or 0
                        table.insert(found, {
                            path = "docContainer.child[0]",
                            value = string.format("name=%s, %sx%s, numChildren=%d",
                                tostring(devCont.name or ""),
                                tostring(devCont.width or 0),
                                tostring(devCont.height or 0), n)
                        })
                        for i = 0, math.min(n - 1, 10) do
                            local gc = nil
                            pcall(function() gc = devCont:GetChildAt(i) end)
                            if gc then
                                table.insert(found, {
                                    path = "deviceContainer.child[" .. i .. "]",
                                    value = string.format("name=%s, %sx%s at (%s,%s)",
                                        tostring(gc.name or ""),
                                        tostring(gc.width or 0), tostring(gc.height or 0),
                                        tostring(gc.x or 0), tostring(gc.y or 0))
                                })
                            end
                        end
                    end
                end
            end
        end

    elseif target == "docContainer" then
        -- 探查 docContainer 内部结构（找到模拟设备区域）
        local tv = App.testView
        if tv and tv.numChildren and tv.numChildren > 0 then
            local child0 = nil
            pcall(function() child0 = tv:GetChildAt(0) end)
            if child0 then
                local docContainer = nil
                pcall(function() docContainer = child0:GetChild("docContainer") end)
                if docContainer then
                    local n = docContainer.numChildren or 0
                    table.insert(found, { path = "docContainer.numChildren", value = tostring(n) })
                    for i = 0, math.min(n - 1, 14) do
                        local gc = nil
                        pcall(function() gc = docContainer:GetChildAt(i) end)
                        if gc then
                            table.insert(found, {
                                path = "docContainer.child[" .. i .. "]",
                                value = string.format("name=%s, %sx%s at (%s,%s)",
                                    tostring(gc.name or ""),
                                    tostring(gc.width or 0), tostring(gc.height or 0),
                                    tostring(gc.x or 0), tostring(gc.y or 0))
                            })
                        end
                    end
                end
            end
        end

    elseif target == "testView_grandchild" then
        -- 探查 testView.child[0] 内部的所有子元素
        local tv = App.testView
        if tv and tv.numChildren and tv.numChildren > 0 then
            local child = nil
            pcall(function() child = tv:GetChildAt(0) end)
            if child and child.numChildren then
                local n = child.numChildren
                table.insert(found, { path = "child0.numChildren", value = tostring(n) })
                for i = 0, math.min(n - 1, 14) do
                    local gc = nil
                    pcall(function() gc = child:GetChildAt(i) end)
                    if gc then
                        table.insert(found, {
                            path = "child0.child[" .. i .. "]",
                            value = string.format("name=%s, %dx%d at (%d,%d)",
                                tostring(gc.name or ""), gc.width or 0, gc.height or 0,
                                gc.x or 0, gc.y or 0)
                        })
                    end
                end
            end
        end

    elseif target == "testView_child0" then
        -- 探查 testView 第一个子元素（被预览的组件）
        local tv = App.testView
        if tv and tv.numChildren and tv.numChildren > 0 then
            local child = nil
            pcall(function() child = tv:GetChildAt(0) end)
            if child then
                local props = {
                    "name", "x", "y", "width", "height", "scaleX", "scaleY",
                    "displayObject", "numChildren", "asCom", "AsCom",
                }
                for _, p in ipairs(props) do
                    local ok, val = pcall(function() return child[p] end)
                    if ok and val ~= nil then
                        table.insert(found, { path = "child0." .. p, valtype = type(val), value = tostring(val) })
                    end
                end
                -- 如果 displayObject 存在，深入一层
                local dobj = nil
                pcall(function() dobj = child.displayObject end)
                if dobj then
                    local sp = {"x", "y", "width", "height", "name", "scaleX", "scaleY"}
                    for _, p in ipairs(sp) do
                        local ok, val = pcall(function() return dobj[p] end)
                        if ok and val ~= nil then
                            table.insert(found, { path = "child0.displayObject." .. p, valtype = type(val), value = tostring(val) })
                        end
                    end
                end
            end
        end

    elseif target == "testView_full" then
        -- 深入探查 testView 内部结构
        local tv = App.testView
        if tv then
            local props = {
                "running", "visible", "contentPane", "ContentPane",
                "stage", "Stage", "displayObject", "DisplayObject",
                "view", "View", "panel", "Panel", "container", "Container",
                "viewWidth", "viewHeight", "width", "height",
                "scaleX", "scaleY", "scale",
                "x", "y", "name",
                "numChildren", "GetChildAt", "GetChild",
                "rootView", "RootView", "host", "Host",
                "parent", "Parent",
                "GetScreenShot", "GetBounds",
                "viewport", "Viewport", "frame", "Frame",
                "preview", "Preview", "previewWindow", "PreviewWindow",
                "tester", "Tester",
                "originSize", "size",
                "testView", "innerView",
            }
            for _, p in ipairs(props) do
                local ok, val = pcall(function() return tv[p] end)
                if ok and val ~= nil then
                    table.insert(found, { path = "testView." .. p, valtype = type(val), value = tostring(val) })
                    -- 如果是对象，深入一层
                    if type(val) == "userdata" or type(val) == "table" then
                        local subProps = {"displayObject", "x", "y", "width", "height", "name", "scaleX", "scaleY", "numChildren", "parent"}
                        for _, sp in ipairs(subProps) do
                            local ok2, sv = pcall(function() return val[sp] end)
                            if ok2 and sv ~= nil then
                                table.insert(found, { path = "testView." .. p .. "." .. sp, valtype = type(sv), value = tostring(sv) })
                            end
                        end
                    end
                end
            end
        end

    elseif target == "testView" then
        -- 探查 App.testView 相关 API
        local tvProps = {"testView", "TestView", "previewView", "PreviewView"}
        for _, pName in ipairs(tvProps) do
            local ok, tv = pcall(function() return App[pName] end)
            if ok and tv ~= nil then
                table.insert(found, { path = "App." .. pName, valtype = type(tv), value = tostring(tv) })
                -- 探查 testView 的属性和方法
                local methods = {
                    "running", "Running", "visible", "Visible",
                    "Start", "start", "Run", "run", "Show", "show",
                    "Stop", "stop", "Close", "close", "Hide", "hide",
                    "item", "Item", "content", "Content",
                    "component", "Component",
                }
                for _, m in ipairs(methods) do
                    local ok2, val2 = pcall(function() return tv[m] end)
                    if ok2 and val2 ~= nil then
                        table.insert(found, {
                            path = "App." .. pName .. "." .. m,
                            valtype = type(val2),
                            value = tostring(val2)
                        })
                    end
                end
            end
        end

        -- 探查 App 上的测试/预览相关方法
        local appMethods = {
            "StartTest", "startTest", "RunTest", "runTest",
            "TestPreview", "testPreview",
            "ShowTestView", "showTestView",
            "StartPreview", "startPreview",
        }
        for _, m in ipairs(appMethods) do
            local ok, val = pcall(function() return App[m] end)
            if ok and val ~= nil then
                table.insert(found, { path = "App." .. m, valtype = type(val), value = tostring(val) })
            end
        end
    elseif target == "publishSettings" then
        -- 探查发布设置相关 API
        local pubProps = {
            "publishView", "PublishView", "publishSettings", "PublishSettings",
            "showPublishDialog", "ShowPublishDialog", "openPublishDialog", "OpenPublishDialog",
            "publishDialog", "PublishDialog", "publishPanel", "PublishPanel",
        }
        for _, prop in ipairs(pubProps) do
            local ok, val = pcall(function() return App[prop] end)
            if ok and val ~= nil then
                table.insert(found, { path = "App." .. prop, valtype = type(val), value = tostring(val) })
            end
        end
        -- 尝试调用发布设置相关方法
        local pubMethods = {
            "showPublishView", "ShowPublishView", "openPublishSettings", "OpenPublishSettings",
            "showPublishDialog", "ShowPublishDialog", "openPublishPanel", "OpenPublishPanel",
        }
        for _, method in ipairs(pubMethods) do
            local ok, val = pcall(function() return App[method] end)
            if ok and val ~= nil then
                table.insert(found, { path = "App." .. method, valtype = type(val), value = tostring(val) })
            end
        end
        -- 尝试 CS.FairyEditor 中发布相关的类
        local pubTypes = {
            "PublishView", "PublishDialog", "PublishSettings", "PublishDialogBase",
        }
        for _, t in ipairs(pubTypes) do
            local ok, val = pcall(function() return CS.FairyEditor[t] end)
            if ok and val ~= nil then
                table.insert(found, { path = "CS.FairyEditor." .. t, valtype = type(val), value = tostring(val) })
            end
        end

    end

    return { found_apis = found, count = #found, target = target }
end

-- 重载所有插件
function CommandHandler.handleReloadAllPlugins(params, bridgePath)
    local results = {}

    -- 探查可用的插件管理 API
    local apiFound = {}
    local mgr = nil

    -- 尝试多种路径获取 pluginManager
    local mgrPaths = {
        "App.pluginManager", "App.PluginManager", "App.pluginMgr",
        "CS.FairyEditor.PluginSystem.inst",
        "CS.FairyEditor.PluginSystem.Instance",
    }
    for _, path in ipairs(mgrPaths) do
        local ok, val = pcall(function()
            local parts = {}
            for p in path:gmatch("[^.]+") do
                table.insert(parts, p)
            end
            local obj = _G
            for _, p in ipairs(parts) do
                if obj == _G and p == "App" then
                    obj = App
                elseif obj == _G and p == "CS" then
                    obj = CS
                elseif obj == CS and p == "FairyEditor" then
                    obj = CS.FairyEditor
                else
                    obj = obj[p]
                end
            end
            return obj
        end)
        if ok and val ~= nil then
            mgr = val
            table.insert(apiFound, path)
        end
    end

    -- 获取所有插件信息
    local pluginNames = {}
    if mgr then
        local ok, plugins = pcall(function() return mgr.allPlugins end)
        if ok and plugins then
            for i = 0, plugins.Count - 1 do
                local p = plugins[i]
                table.insert(pluginNames, p.name)
            end
        end
    end
    table.insert(results, "loaded plugins: " .. table.concat(pluginNames, ", "))
    table.insert(results, "api found: " .. table.concat(apiFound, ", "))

    -- 尝试直接调用 ReloadAll（不延迟），让 pcall 捕获错误
    local methodUsed = "none"
    local reloadOk = false

    -- 方式1: PluginSystem.inst:ReloadAll()
    local ok1, err1 = pcall(function()
        local ps = CS.FairyEditor.PluginSystem
        if ps and ps.inst then
            ps.inst:ReloadAll()
            methodUsed = "PluginSystem.inst:ReloadAll()"
            reloadOk = true
        elseif ps and ps.Instance then
            ps.Instance:ReloadAll()
            methodUsed = "PluginSystem.Instance:ReloadAll()"
            reloadOk = true
        end
    end)
    if ok1 and reloadOk then
        fprint("[MCPBridge] ReloadAll via PluginSystem succeeded")
    elseif not ok1 then
        fprint("[MCPBridge] ReloadAll via PluginSystem failed: " .. tostring(err1))
    end

    -- 方式2: App.pluginManager:ReloadAll()
    if not reloadOk then
        local ok2, _ = pcall(function()
            if mgr then
                mgr:ReloadAll()
                methodUsed = "pluginManager:ReloadAll()"
                reloadOk = true
            end
        end)
        if ok2 and reloadOk then
            fprint("[MCPBridge] ReloadAll via pluginManager succeeded")
        end
    end

    -- 方式3: 遍历所有插件逐个 Reload
    if not reloadOk and mgr then
        local ok3, _ = pcall(function()
            local plugins = mgr.allPlugins
            if plugins then
                for i = 0, plugins.Count - 1 do
                    local p = plugins[i]
                    pcall(function() p:Reload() end)
                end
                methodUsed = "per-plugin Reload()"
                reloadOk = true
            end
        end)
        if ok3 and reloadOk then
            fprint("[MCPBridge] ReloadAll via per-plugin Reload succeeded")
        end
    end

    -- 方式4: 定时器延迟执行（作为兜底）
    if not reloadOk then
        CS.FairyGUI.Timers.inst:Add(0.8, 1, function()
            fprint("[MCPBridge] Fallback: scheduled PluginSystem.ReloadAll in timer...")
            pcall(function()
                local ps = CS.FairyEditor.PluginSystem
                if ps and ps.inst then
                    ps.inst:ReloadAll()
                end
            end)
        end)
        methodUsed = "PluginSystem.ReloadAll (delayed 0.8s fallback)"
    end

    table.insert(results, "method: " .. methodUsed)
    table.insert(results, "immediate_result: " .. tostring(reloadOk))

    return {
        reloaded = reloadOk or methodUsed:find("fallback") ~= nil,
        method = methodUsed,
        details = results,
        warning = reloadOk and "all plugins reloaded, MCPBridge will re-initialize"
                  or "direct reload failed, fallback timer scheduled"
    }
end

-- ========== 发布命令 ==========

-- 单包发布产物路径（{pkg}_fui.bytes）；exportPath 缺失时按工程 Publish 设置推导
local function resolveBytesPath(handler, pkgName)
    local okP, p = pcall(function() return handler.exportPath end)
    if okP and p and p ~= "" then
        return p .. "/" .. pkgName .. "_fui.bytes"
    end
    local okS, settings = pcall(function() return App.project:GetSettings("Publish") end)
    if okS and settings and settings.path then
        return App.project.basePath .. "/" .. settings.path .. "/" .. pkgName .. "_fui.bytes"
    end
    return nil
end

-- 内部函数：通过 PublishHandler 单包发布（FairyGUI 编辑器的正确单包发布 API）
-- 关键：xLua 未暴露 PublishHandler.New 静态方法，必须直接构造 CS.FairyEditor.PublishHandler(pkg, branch)
-- branch 取 App.project.activeBranch（无分支时为空字符串）；Run 异步触发，产物秒级写入，
-- isSuccess 字段返回时尚未更新（不可靠），判完成需看 exportPath 下 {pkg}_fui.bytes 的 mtime
--
-- 置位时序契约（2026-10-12 实验定案）：Run() 对图片的依赖收集发生在其后的异步帧；同步块内
-- Run() 后立即恢复 exported 会使置位窗口对图片收集无效——组件清单在 Run 同步开场快照故幸存，
-- 而组件引用的图片全部丢失（Chat 包实测丢 34/35 张）。因此恢复必须延迟到产物真正落盘：
-- Timers 轮询 {pkg}_fui.bytes mtime 相对发布前快照变化，再留 3s 图集尾写窗口后按包恢复。
-- 在途状态挂 _G：同一时间仅允许一个裸 Run 发布，防止下一包 dofile/置位踩掉未兑现的恢复。
local function tryPublishViaHandler(pkgName)
    if _G.__mcpPendingPublish then
        return false, "previous publish still in progress: " .. tostring(_G.__mcpPendingPublish.pkg)
    end

    local pkg = nil
    local getPkgOk = pcall(function() pkg = App.project:GetPackageByName(pkgName) end)
    if not getPkgOk or not pkg then
        return false, "package not found: " .. tostring(pkgName)
    end

    -- 条件加载 MyGenCode：仅当置位/恢复函数缺席时才 dofile，避免每次发布重载整个
    -- code_gen.lua 触发其文件级副作用
    if type(EnsureItemReferencedExported) ~= "function" or type(RestoreItemReferencedExported) ~= "function" then
        local okGen = pcall(function()
            dofile(App.project.basePath .. "/plugins/MyGenCode/code_gen.lua")
        end)
        if not okGen then
            fprint("[MCPBridge] 警告: MyGenCode code_gen.lua 加载失败，item/gearIcon 引用组件可能漏导出")
        end
    end

    local handler
    local branch = App.project.activeBranch or ""
    local newOk = pcall(function()
        handler = CS.FairyEditor.PublishHandler(pkg, branch)
    end)
    if not newOk or not handler then
        return false, "PublishHandler construct failed"
    end

    local bytesPath = resolveBytesPath(handler, pkgName)
    if not bytesPath then
        return false, "cannot resolve output path for " .. tostring(pkgName)
    end

    -- 发布前产物 mtime 快照（文件不存在时 .NET 返回 1601 纪元小值，同样满足"变化"判定）
    local oldTicks = 0
    pcall(function()
        oldTicks = CS.System.IO.File.GetLastWriteTime(bytesPath).Ticks
    end)

    -- 裸 Run() 不触发 PluginManager.OnPublishStart，且 Run() 开场即收集发布资源清单（items），
    -- onPublish 钩子（MyGenCode 的置位）在 items 定型之后才执行、对本轮无效，须 Run 前手动置位
    if type(EnsureItemReferencedExported) == "function" then
        pcall(function() EnsureItemReferencedExported(pkg) end)
    else
        fprint("[MCPBridge] 警告: MyGenCode 置位函数不可用，item/gearIcon 引用组件可能漏导出")
    end

    local runOk = pcall(function() handler:Run() end)
    if not runOk then
        -- Run 触发失败无异步流程在途，置位立即回滚
        if type(RestoreItemReferencedExported) == "function" then
            pcall(function() RestoreItemReferencedExported(pkgName) end)
        end
        return false, "PublishHandler.Run failed"
    end

    _G.__mcpPendingPublish = { pkg = pkgName }
    local state = { afterUpdateWaits = 0 }
    local timerCb
    timerCb = function()
        local settled = false
        local okT, ticks = pcall(function() return CS.System.IO.File.GetLastWriteTime(bytesPath).Ticks end)
        if okT and ticks and ticks ~= oldTicks then
            if state.updatedSeen then
                state.afterUpdateWaits = state.afterUpdateWaits + 1
                -- bytes 落盘后再留 3s（6 次 x 0.5s）图集尾写窗口
                if state.afterUpdateWaits >= 6 then
                    settled = true
                end
            else
                state.updatedSeen = true
                fprint("[publish] 产物已落盘，尾写窗口后恢复置位: " .. pkgName)
            end
        end
        state.checks = (state.checks or 0) + 1
        if settled or state.checks > 200 then
            CS.FairyGUI.Timers.inst:Remove(timerCb)
            if type(RestoreItemReferencedExported) == "function" then
                pcall(function() RestoreItemReferencedExported(pkgName) end)
            end
            _G.__mcpPendingPublish = nil
            if not settled then
                fprint("[MCPBridge] 警告: " .. pkgName .. " 100s 未见产物更新，已超时强制恢复，请人工核对产物")
            end
        end
    end
    CS.FairyGUI.Timers.inst:Add(0.5, 0, timerCb)

    return true, "PublishHandler", handler.exportPath or ""
end

-- 打开发布设置对话框
function CommandHandler.handleOpenPublishSettings(params, bridgePath)
    -- 探查 CS.FairyEditor.PublishSettings 的方法
    local found = {}
    local settingsClass = CS.FairyEditor.PublishSettings
    if settingsClass then
        -- 尝试各种打开方法
        local methods = {
            "Show", "show", "Open", "open", "ShowDialog", "showDialog",
            "ShowSettings", "showSettings", "OpenDialog", "openDialog",
            "ShowWindow", "showWindow", "OpenWindow", "openWindow",
            "ShowPanel", "showPanel", "OpenPanel", "openPanel",
            "ShowSettingsWindow", "OpenSettingsWindow",
            "inst", "Instance", "instance",
        }
        for _, m in ipairs(methods) do
            local ok, val = pcall(function() return settingsClass[m] end)
            if ok and val ~= nil then
                table.insert(found, { path = "PublishSettings." .. m, valtype = type(val), value = tostring(val) })
            end
        end
    end

    -- 尝试点击 tbPublishSettings 按钮
    local toolbar = App.mainView.toolbar
    if toolbar then
        local btn = nil
        pcall(function() btn = toolbar:GetChild("tbPublishSettings") end)
        if btn then
            local clickParams = {{true, true}, {false, false}, {true, false}, {false, true}}
            for _, cp in ipairs(clickParams) do
                local clickOk, _ = pcall(function() btn:FireClick(cp[1], cp[2]) end)
                if clickOk then
                    table.insert(found, { method = "FireClick", params = tostring(cp[1])..","..tostring(cp[2]), button = "tbPublishSettings", success = true })
                    break
                end
            end
        else
            table.insert(found, { error = "tbPublishSettings button not found" })
        end
    end

    return { found = found, count = #found }
end

-- 枚举工具栏全部子控件（发布入口诊断：确认全包发布按钮的真实控件名）
function CommandHandler.handleProbeToolbar(params, bridgePath)
    local toolbar = App.mainView.toolbar
    if not toolbar then return { error = "toolbar not found" } end
    local names = {}
    local i = 0
    while i < 200 do
        local ok, child = pcall(function() return toolbar:GetChildAt(i) end)
        if not ok or child == nil then break end
        names[#names + 1] = child.name
        i = i + 1
    end
    return { children = names, count = #names }
end

-- 探查发布对话框/发布管理器的可访问成员（找原生全包发布入口）
function CommandHandler.handleProbePublishDialog(params, bridgePath)
    local found = {}
    local probes = {
        { prefix = "PublishDialog", cls = CS.FairyEditor.PublishDialog,
          members = { "inst", "Instance", "instance", "Show", "show", "ShowDialog",
                      "showDialog", "Open", "open", "ShowMe", "showMe", "New" } },
        { prefix = "PublishManager", cls = CS.FairyEditor.PublishManager,
          members = { "inst", "Instance", "instance", "PublishAll", "publishAll",
                      "Publish", "New" } },
        { prefix = "PublishView", cls = CS.FairyEditor.PublishView,
          members = { "inst", "Instance", "instance", "Show", "Open", "New" } },
    }
    for _, probe in ipairs(probes) do
        if probe.cls then
            for _, m in ipairs(probe.members) do
                local ok, val = pcall(function() return probe.cls[m] end)
                if ok and val ~= nil then
                    found[#found + 1] = { path = probe.prefix .. "." .. m, valtype = type(val), value = tostring(val):sub(1, 80) }
                end
            end
        else
            found[#found + 1] = { path = probe.prefix, valtype = "nil" }
        end
    end
    return { found = found, count = #found }
end

-- 对话框发布公共流程：轮换候选组合打开发布设置对话框 →（单包场景先点击 pkgs 列表中的
-- 目标包节点选中）→ 目标按钮 FireClick → 编辑器原生发布管线（onPublishStart/End 钩子，
-- MyGenCode 置位自管）→ 产物目录安静后释放 pending。调用前置条件：_G.__mcpPendingPublish
-- 已设置。对话框打开方式不稳定（同一按钮同一参数随编辑器状态时开时不开），故组合轮换重试
local function beginDialogPublish(buttonName, pendingPkg, logLabel, selectPkgId)
    CS.UnityEngine.Application.runInBackground = true
    local openCandidates = {
        { "tbPublishDesc",     false, false },
        { "tbPublishSettings", true,  true  },
        { "tbPublishDesc",     true,  true  },
        { "tbPublishSettings", false, false },
    }
    local tryCount = 0
    local pkgSelected = false
    local clickTimer
    clickTimer = function()
        tryCount = tryCount + 1
        local dlg
        local okDlg = pcall(function()
            dlg = CS.FairyGUI.GRoot.inst:GetChildAt(1):GetChildAt(0)
        end)
        if not okDlg or not dlg then
            goto retryOpen
        end

        -- 单包场景：先选中 pkgs 列表中的目标包节点（对话框的发布对象跟随该选中）
        if selectPkgId and not pkgSelected then
            local pkgNode
            local okNode = pcall(function()
                pkgNode = dlg:GetChild("pkgs"):GetChild(selectPkgId)
            end)
            if okNode and pkgNode then
                pcall(function() pkgNode:FireClick(false, false) end)
                pkgSelected = true
                fprint("[publish] 已选中包节点 " .. selectPkgId .. "，待界面切换后点发布")
            end
            goto retryOpen
        end

        do
            local btn
            local okBtn = pcall(function()
                btn = dlg:GetChild(buttonName)
            end)
            if okBtn and btn then
                pcall(function() btn:FireClick(true, true) end)
                CS.FairyGUI.Timers.inst:Remove(clickTimer)
                fprint("[publish] 已点击" .. logLabel .. "（原生发布管线启动）")
            -- pending 闸门：待产物目录安静（连续 6 次 0.5s 采样无 mtime 变化，起始 10s 保持期）
            -- 后释放，上限 300s 兜底；产物完备性由外层脚本门禁+自检负责
            local checks = 0
            local lastMax = 0
            local quietRounds = 0
            local releaseCb
            releaseCb = function()
                checks = checks + 1
                local okDir, files = pcall(function()
                    return CS.System.IO.Directory.GetFiles(App.project.basePath .. "/../game/Assets/Publish/ui", "*_fui.bytes")
                end)
                if okDir and files then
                    local maxTick = 0
                    for i = 0, files.Length - 1 do
                        local okT, t = pcall(function()
                            return CS.System.IO.File.GetLastWriteTime(files[i]).Ticks
                        end)
                        if okT and t and t > maxTick then
                            maxTick = t
                        end
                    end
                    if maxTick == lastMax and checks >= 20 then
                        quietRounds = quietRounds + 1
                    else
                        quietRounds = 0
                    end
                    lastMax = maxTick
                    if quietRounds >= 6 then
                        CS.FairyGUI.Timers.inst:Remove(releaseCb)
                        if _G.__mcpPendingPublish and _G.__mcpPendingPublish.pkg == pendingPkg then
                            _G.__mcpPendingPublish = nil
                        end
                        fprint("[publish] " .. logLabel .. "产物已安静，pending 释放")
                        return
                    end
                end
                if checks >= 600 then
                    CS.FairyGUI.Timers.inst:Remove(releaseCb)
                    if _G.__mcpPendingPublish and _G.__mcpPendingPublish.pkg == pendingPkg then
                        _G.__mcpPendingPublish = nil
                    end
                    fprint("[publish] " .. logLabel .. "pending 超时释放（300s）")
                end
            end
                CS.FairyGUI.Timers.inst:Add(0.5, 0, releaseCb)
                return
            end
        end
        ::retryOpen::
        if tryCount >= 40 then
            CS.FairyGUI.Timers.inst:Remove(clickTimer)
            _G.__mcpPendingPublish = nil
            fprint("[publish] 错误: 发布设置对话框 12s 内未能完成定位与点击（已轮换全部打开组合），请人工检查")
        else
            -- 对话框/包节点/按钮未就绪：每轮换下一组打开组合重试 FireClick
            local cand = openCandidates[(tryCount % #openCandidates) + 1]
            local tb
            pcall(function()
                tb = App.mainView.toolbar:GetChild(cand[1])
            end)
            if tb then
                pcall(function() tb:FireClick(cand[2], cand[3]) end)
            end
        end
    end
    CS.FairyGUI.Timers.inst:Add(0.3, 0, clickTimer)
end

-- 原生全包发布：发布设置对话框 publishAll 按钮 → 编辑器并发全包管线，全量耗时秒级。
-- 响应 published 仅代表触发，产物完成由外层脚本落盘门禁+自检判定
function CommandHandler.handlePublishAllNative(params, bridgePath)
    if _G.__mcpPendingPublish then
        return { published = false, reason = "previous publish still in progress: " .. tostring(_G.__mcpPendingPublish.pkg) }
    end
    local toolbar = App.mainView.toolbar
    local btnDesc
    pcall(function() btnDesc = toolbar:GetChild("tbPublishDesc") end)
    if not btnDesc then
        return { published = false, reason = "tbPublishDesc not found" }
    end
    _G.__mcpPendingPublish = { pkg = "*", native = true }
    beginDialogPublish("publishAll", "*", "全部发布")
    return { published = true, method = "PublishDialog.publishAll" }
end

-- 打开/枚举发布设置对话框（找"发布/全部发布"按钮的真实控件名）
-- params.mode: "show" 打开对话框；"enum" 仅枚举当前对话框内容
function CommandHandler.handlePublishDialogProbe(params, bridgePath)
    local pd = CS.FairyEditor.PublishDialog
    if not pd then return { error = "PublishDialog type not found" } end
    local inst = pd.inst or pd.Instance or pd.instance
    local out = { has_inst = inst ~= nil, steps = {} }
    if not inst then return out end

    if (params.mode or "show") == "show" then
        local okShow, errShow = pcall(function() inst:Show() end)
        out.steps[#out.steps + 1] = "Show:" .. tostring(okShow) .. (okShow and "" or ("(" .. tostring(errShow):sub(1, 80) .. ")"))
    end

    -- 枚举对话框直属子控件（按钮类控件名列出 title）
    local names = {}
    local i = 0
    while i < 100 do
        local ok, child = pcall(function() return inst:GetChildAt(i) end)
        if not ok or child == nil then break end
        local cname, ctitle = "?", ""
        pcall(function() cname = child.name end)
        pcall(function() ctitle = child.title or "" end)
        names[#names + 1] = cname .. (ctitle and ctitle ~= "" and ("[" .. tostring(ctitle) .. "]") or "")
        i = i + 1
    end
    out.children = names
    out.count = #names
    return out
end

-- 点击工具栏指定按钮并回报（FireClick 不抛异常，真实效果需调用方截图判定）
-- params.button: 按钮名；params.form: fire 形式序号 1-4（对应不同参数组合，默认 1）
function CommandHandler.handleFireToolbar(params, bridgePath)
    local btnName = params.button
    if not btnName then error("缺少参数: button") end
    local toolbar = App.mainView.toolbar
    if not toolbar then return { clicked = false, reason = "toolbar not found" } end
    local btn
    local hasBtn = pcall(function() btn = toolbar:GetChild(btnName) end)
    if not hasBtn or not btn then
        return { clicked = false, reason = "button not found: " .. btnName }
    end
    local forms = {
        { false, false }, { true, true }, { true, false }, { false, true },
    }
    local idx = math.max(1, math.min(4, tonumber(params.form) or 1))
    local cp = forms[idx]
    local ok = pcall(function() btn:FireClick(cp[1], cp[2]) end)
    CS.UnityEngine.Application.runInBackground = true
    return { clicked = ok, button = btnName, form = idx }
end

-- 枚举 mainView 顶层子控件（定位模态对话框宿主）；params.target: "mainView"(默认)/"groot"/指定子控件名
function CommandHandler.handleProbeMainView(params, bridgePath)
    local target
    if params.target == "groot" then
        local ok, root = pcall(function() return CS.FairyGUI.GRoot.inst end)
        if not ok or not root then return { error = "GRoot.inst not found" } end
        target = root
    else
        local mv = App.mainView
        if not mv then return { error = "mainView not found" } end
        target = mv
    end
    if params.child then
        local ok, c = pcall(function() return target:GetChild(params.child) end)
        if not ok or not c then return { error = "child not found: " .. tostring(params.child) } end
        target = c
    end
    local names = {}
    local i = 0
    while i < 200 do
        local ok, child = pcall(function() return target:GetChildAt(i) end)
        if not ok or child == nil then break end
        local cname, ctitle = "?", ""
        pcall(function() cname = child.name end)
        pcall(function() ctitle = child.title or "" end)
        names[#names + 1] = cname .. (ctitle and ctitle ~= "" and ("[" .. tostring(ctitle) .. "]") or "")
        i = i + 1
    end
    return { children = names, count = #names, target = params.target or "mainView" }
end

-- 递归搜索 GRoot 子树，收集控件名/title 文本（定位对话框内按钮的真实路径）
-- params.keyword: 只返回 title/text 含关键字的控件；params.max_depth: 递归深度（默认 5）
function CommandHandler.handleSearchTree(params, bridgePath)
    local okRoot, root = pcall(function() return CS.FairyGUI.GRoot.inst end)
    if not okRoot or not root then return { error = "GRoot.inst not found" } end
    local keyword = params.keyword
    local maxDepth = tonumber(params.max_depth) or 5
    local hits, visited = {}, 0
    local function walk(node, path, depth)
        if depth > maxDepth then return end
        local i = 0
        while i < 300 do
            local ok, child = pcall(function() return node:GetChildAt(i) end)
            if not ok or child == nil then break end
            visited = visited + 1
            local cname, ctitle, ctext = "", "", ""
            pcall(function() cname = child.name or "" end)
            pcall(function() ctitle = child.title or "" end)
            pcall(function() ctext = child.text or "" end)
            local label = cname .. " |t:" .. tostring(ctitle) .. " |x:" .. tostring(ctext)
            if not keyword or string.find(tostring(ctitle) .. tostring(ctext) .. cname, keyword, 1, true) then
                hits[#hits + 1] = path .. "/" .. label
            end
            walk(child, path .. "/" .. (cname ~= "" and cname or ("@" .. i)), depth + 1)
            i = i + 1
        end
    end
    walk(root, "", 1)
    return { hits = hits, count = #hits, visited = visited }
end

-- 点击发布设置对话框内的按钮（对话框须已打开；按钮名：publishAll/publish/publishDesc/close）
-- 按钮宿主路径固定为 GRoot 第二子容器的第一子控件（发布设置对话框窗口）
function CommandHandler.handlePublishDialogButton(params, bridgePath)
    local button = params.button or "publishAll"
    local okRoot, root = pcall(function() return CS.FairyGUI.GRoot.inst end)
    if not okRoot or not root then return { clicked = false, reason = "GRoot not found" } end
    local dlg
    local okNav = pcall(function()
        dlg = root:GetChildAt(1):GetChildAt(0)
    end)
    if not okNav or not dlg then return { clicked = false, reason = "publish dialog not found (open it first)" } end
    local btn
    local okGet = pcall(function() btn = dlg:GetChild(button) end)
    if not okGet or not btn then return { clicked = false, reason = "button not found: " .. button } end
    -- FireClick 不抛异常，真实效果由调用方截图/产物判定；默认 {true,true}（工具栏发布按钮
    -- 实测有效的形态）
    local forms = {
        { true, true }, { false, false }, { true, false }, { false, true },
    }
    local idx = math.max(1, math.min(4, tonumber(params.form) or 1))
    local cp = forms[idx]
    local okFire = pcall(function() btn:FireClick(cp[1], cp[2]) end)
    return { clicked = okFire, button = button, form = idx }
end

-- 探查发布相关 API
function CommandHandler.handleProbePublish(params, bridgePath)
    local found = {}

    -- App 上的发布相关属性和方法
    local appProps = {
        "publishView", "PublishView", "publishSettings", "PublishSettings",
        "showPublishDialog", "ShowPublishDialog", "showPublishView", "ShowPublishView",
        "openPublishDialog", "OpenPublishDialog", "publishDialog", "PublishDialog",
        "publishPanel", "PublishPanel", "openPublishSettings", "OpenPublishSettings",
    }
    for _, prop in ipairs(appProps) do
        local ok, val = pcall(function() return App[prop] end)
        if ok and val ~= nil then
            table.insert(found, { path = "App." .. prop, valtype = type(val), value = tostring(val) })
        end
    end

    -- CS.FairyEditor 中的发布相关类
    local csTypes = {
        "PublishView", "PublishDialog", "PublishSettings", "PublishDialogBase",
        "Publish", "PublishHandler", "PublishManager",
    }
    for _, t in ipairs(csTypes) do
        local ok, val = pcall(function() return CS.FairyEditor[t] end)
        if ok and val ~= nil then
            table.insert(found, { path = "CS.FairyEditor." .. t, valtype = type(val), value = tostring(val) })
        end
    end

    -- 工具栏上所有子元素
    local toolbar = nil
    pcall(function() toolbar = App.mainView.toolbar end)
    if toolbar then
        table.insert(found, { path = "toolbar.childCount", valtype = "number", value = tostring(toolbar.numChildren or 0) })
        if toolbar.numChildren then
            for i = 0, math.min(toolbar.numChildren - 1, 30) do
                local child = toolbar:GetChildAt(i)
                if child then
                    table.insert(found, { path = "toolbar.child[" .. i .. "].name", valtype = "string", value = child.name })
                end
            end
        end
    end

    return { found_apis = found, count = #found }
end

-- 发布指定包
function CommandHandler.handlePublishPackage(params, bridgePath)
    local pkgName = params.package_name
    if not pkgName then error("缺少参数: package_name") end
    -- 兜底剥掉包名尾部的换行/回车控制符（曾因调用链残留 \r 导致查询必然失败）
    pkgName = pkgName:gsub("[%c%s]+$", "")
    if _G.__mcpPendingPublish then
        return { published = false, reason = "previous publish still in progress: " .. tostring(_G.__mcpPendingPublish.pkg) }
    end

    -- 裸 PublishHandler 路径：直接以 pkg 对象发布（天然指定包）。发布设置对话框的"发布"按钮
    -- 发布对象跟随对话框打开时锁定的包，pkgs 列表节点的模拟点击无法改变它（实测发错包），
    -- 故对话框路径仅用于全包（publishAll 无选包问题）。裸 Run 的置位由 MCPBridge 手动前置、
    -- 产物落盘后延迟恢复，产物与人工发布逐字节一致（2026-10-12 实测）
    local handlerOk, handlerMethod, exportPath = tryPublishViaHandler(pkgName)
    if handlerOk then
        return { published = true, package = pkgName, path = exportPath, method = handlerMethod }
    end
    return { published = false, reason = handlerMethod }
end

-- 发布所有包：走发布设置对话框的 publishAll 按钮（编辑器原生并发全包管线，秒级完成；
-- 曾用 FireClick(tbPublish) 只发布当前激活包并虚报 total，已废除）。
-- 响应保留 total/published/failed/packages 字段集（CLI 客户端对 publish all 的解析依赖）
function CommandHandler.handlePublishAll(params, bridgePath)
    local names = {}
    local allPackages = App.project.allPackages
    if allPackages then
        for i = 0, allPackages.Count - 1 do
            names[#names + 1] = allPackages[i].name
        end
    end
    local result = CommandHandler.handlePublishAllNative(params, bridgePath)
    result.total = #names
    if result.published then
        result.published = #names
        result.failed = 0
    else
        result.published = 0
        result.failed = #names
    end
    result.packages = names
    return result
end

-- ========== 日志管理命令 ==========

-- 获取编辑器日志（从 main.lua 的 fprint 拦截缓冲区读取）
function CommandHandler.handleGetLogs(params, bridgePath)
    local buf = _G._mcpLogBuffer
    if not buf then
        return { total = 0, logs = {}, returned = 0, start_index = 0, note = "日志缓冲区未初始化" }
    end

    local maxCount = params.max_count or 100
    local level = params.level or "all"

    local total = #buf
    local logs = {}
    local count = 0
    local startIdx = math.max(1, total - maxCount + 1)

    for i = startIdx, total do
        if count >= maxCount then break end
        local entry = buf[i]
        if entry and (level == "all" or entry.level == level) then
            table.insert(logs, {
                time = entry.time or "",
                level = entry.level or "info",
                message = entry.message or ""
            })
            count = count + 1
        end
    end

    return {
        total = total,
        logs = logs,
        returned = count,
        start_index = startIdx
    }
end

-- 清空编辑器日志（清空 fprint 拦截缓冲区）
function CommandHandler.handleClearLogs(params, bridgePath)
    local buf = _G._mcpLogBuffer
    if not buf then
        return { cleared = false, note = "日志缓冲区未初始化" }
    end

    local oldCount = #buf
    for i = #buf, 1, -1 do
        table.remove(buf, i)
    end

    return {
        cleared = true,
        method = "buffer:clear()",
        cleared_count = oldCount
    }
end

-- 深度探查日志存储位置
function CommandHandler.handleProbeLogs(params, bridgePath)
    local result = {}
    local target = params.target or "consoleview"

    if target == "consoleview" then
        local cv = App.consoleView
        if not cv then
            return { error = "App.consoleView is nil" }
        end
        local props = {
            "items","Items","logs","Logs","messages","Messages","list","List",
            "content","Content","text","Text","data","Data",
            "GetLogs","getLogs","GetItems","getItems","GetMessages",
            "Clear","clear","ClearAll","clearAll","ClearLogs",
            "count","Count","length","Length",
            "view","View","logList","LogList",
        }
        for _, name in ipairs(props) do
            local ok, v = pcall(function() return cv[name] end)
            if ok and v ~= nil then
                local vstr = tostring(v)
                local vtype = type(v)
                if vtype == "table" or vtype == "userdata" then
                    local ok2, c = pcall(function() return v.Count end)
                    if ok2 and c ~= nil then vstr = vstr .. " [Count=" .. tostring(c) .. "]" end
                end
                table.insert(result, { name = "consoleView."..name, valtype = vtype, value = vstr })
            end
        end

    elseif target == "fprint_source" then
        local found = {}

        local ok, fp = pcall(function() return fprint end)
        if ok and fp then
            table.insert(found, { name = "fprint", valtype = type(fp), value = tostring(fp) })
        end

        local appMethods = {"Log","log","Print","print","AddLog","addLog","Write","write","Trace","trace"}
        for _, m in ipairs(appMethods) do
            local ok2, v = pcall(function() return App[m] end)
            if ok2 and v ~= nil then
                table.insert(found, { name = "App."..m, valtype = type(v), value = tostring(v) })
            end
        end

        for _, cls in ipairs({"LogManager","OutputManager","TraceManager"}) do
            local ok2, inst = pcall(function()
                local c = CS.FairyEditor[cls]
                return c and c.inst
            end)
            if ok2 and inst ~= nil then
                for _, col in ipairs({"items","Items","logs","Logs","list","List","messages","Messages"}) do
                    local ok3, v = pcall(function() return inst[col] end)
                    if ok3 and v ~= nil then
                        local cnt = 0
                        pcall(function()
                            local c = v.Count
                            if type(c) == "number" then cnt = c
                            elseif c ~= nil then cnt = tonumber(tostring(c)) or 0 end
                        end)
                        table.insert(found, {
                            name = cls..".inst."..col,
                            valtype = type(v),
                            value = tostring(v),
                            count = cnt
                        })
                    end
                end
            end
        end

        return { found = found }
    end

    return { probes = result }
end

return CommandHandler
