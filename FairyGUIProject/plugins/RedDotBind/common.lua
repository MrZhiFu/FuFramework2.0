-- ============================================================================
-- RedDot 插件公共方法库 (common.lua)
-- ============================================================================
-- 提供对 FObject.customData 中红点数据段的读写操作。
--
-- 【customData 数据格式】
-- customData 是一个竖线 "|" 分隔的键值对字符串，每段格式为 "前缀:值"。
-- 例如: "red_dot:Shop_Main|other_key:other_value"
-- 红点数据使用前缀 "red_dot:" 标识，值为红点 ID 字符串。
--
-- 【导出接口】
--   readRedDotData(obj)        - 读取对象上存储的红点 ID
--   writeRedDotData(obj, id)   - 写入红点 ID 到对象
--   removeRedDotData(obj)      - 清除对象上的红点数据
-- ============================================================================

-- 红点数据在 customData 中的前缀标识
local RED_DOT_PREFIX = "red_dot:"

-- ---------------------------------------------------------------------------
-- 清理字符串中的换行符，防止 customData 格式被破坏
-- @param s: string - 待清理的字符串
-- @return string - 清理后的字符串
-- ---------------------------------------------------------------------------
local function sanitize(s)
    return (s or ""):gsub("[\r\n]", "")
end

-- ---------------------------------------------------------------------------
-- 在 customData 字符串中设置指定前缀的段值（增/改/删）
-- @param data:  string - 原始 customData 字符串
-- @param prefix: string - 段前缀（如 "red_dot:"）
-- @param value:  string - 要设置的值；传 nil 或 "" 表示删除该段
-- @return string - 更新后的 customData 字符串
-- ---------------------------------------------------------------------------
local function setSegment(data, prefix, value)
    data = data or ""
    -- 构建匹配该前缀段的正则模式（转义特殊字符后追加 "[^|]*" 匹配到下一个 | 或字符串末尾）
    local pattern = prefix:gsub("[%^%$%(%)%%%.%[%]%*%+%-%?]", "%%%1") .. "[^|]*"
    -- 先移除旧段
    local newData = data:gsub(pattern, "")
    -- 清理可能残留的连续分隔符和首尾分隔符
    newData = newData:gsub("||+", "|"):gsub("^|", ""):gsub("|$", "")
    -- 如果新值非空，追加到末尾
    if value ~= nil and value ~= "" then
        if newData ~= "" then
            newData = newData .. "|" .. prefix .. value
        else
            newData = prefix .. value
        end
    end
    return newData
end

-- ---------------------------------------------------------------------------
-- 从 customData 字符串中读取指定前缀的段值
-- @param data:  string - customData 字符串
-- @param prefix: string - 段前缀（如 "red_dot:"）
-- @return string|nil - 段值；若未找到则返回 nil
-- ---------------------------------------------------------------------------
local function getSegment(data, prefix)
    data = data or ""
    -- 转义前缀中的正则特殊字符后构建搜索模式
    local escapedPrefix = prefix:gsub("[%^%$%(%)%%%.%[%]%*%+%-%?]", "%%%1")
    local s, e = data:find(escapedPrefix)
    if not s then return nil end
    -- 从前缀结束位置取到下一个 "|" 或字符串末尾
    local rest = data:sub(e + 1)
    local pipe = rest:find("|")
    return pipe and rest:sub(1, pipe - 1) or rest
end

-- ---------------------------------------------------------------------------
-- 读取对象上存储的红点 ID
-- @param obj: FObject - FairyGUI 编辑器对象
-- @return string|nil - 红点 ID；未设置时返回 nil
-- ---------------------------------------------------------------------------
local function readRedDotData(obj)
    local data = sanitize(obj.customData)
    return getSegment(data, RED_DOT_PREFIX)
end

-- ---------------------------------------------------------------------------
-- 将红点 ID 写入对象的 customData 中
-- 注意：编辑器中必须通过 obj.docElement:SetProperty() 来修改属性，
-- 这样才能被编辑器的撤销/重做机制追踪。
-- @param obj:   FObject - FairyGUI 编辑器对象
-- @param redId: string  - 要设置的红点 ID
-- ---------------------------------------------------------------------------
local function writeRedDotData(obj, redId)
    local data = sanitize(obj.customData)
    local newData = sanitize(setSegment(data, RED_DOT_PREFIX, redId))
    -- 编辑器写入：使用 SetProperty 以支持撤销/重做 (Ctrl+Z)
    local ok, err = pcall(function()
        obj.docElement:SetProperty("customData", newData)
        obj.customData = newData
    end)
    if not ok then
        fprint('[RedDot] 写入失败: ' .. tostring(err))
    end
end

-- ---------------------------------------------------------------------------
-- 清除对象上的红点数据（将红点段置空即删除）
-- @param obj: FObject - FairyGUI 编辑器对象
-- ---------------------------------------------------------------------------
local function removeRedDotData(obj)
    writeRedDotData(obj, "")
end

-- ============================================================================
-- 模块导出
-- ============================================================================
return {
    RED_DOT_PREFIX   = RED_DOT_PREFIX,
    setSegment       = setSegment,
    getSegment       = getSegment,
    readRedDotData   = readRedDotData,
    writeRedDotData  = writeRedDotData,
    removeRedDotData = removeRedDotData
}
