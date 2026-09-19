-- ============================================================================
-- L10nKeyBind 插件公共方法库 (common.lua)
-- ============================================================================
-- 提供对 FObject.customData 中多语言 key 段的读写操作。
--
-- 【customData 数据格式】
-- customData 是一个竖线 "|" 分隔的键值对字符串，每段格式为 "前缀:值"。
-- 例如: "L10n:common_continue_game|red_dot:1001"
-- 多语言 key 使用前缀 "L10n:" 标识，值为多语言 key 字符串
-- （与运行时 L10nKey 常量类 / TbLocalization 表的字符串主键一致）。
-- 运行时（FairyGUI GObject.L10n 分部）按相同前缀解析并自动应用对应语言文本。
--
-- 【导出接口】
--   readL10nData(obj)        - 读取对象上存储的多语言 key
--   writeL10nData(obj, key)  - 写入多语言 key 到对象
--   removeL10nData(obj)      - 清除对象上的多语言 key
-- ============================================================================

-- 多语言 key 在 customData 中的前缀标识（与运行时 GObject.L10n 解析约定一致）
local L10N_PREFIX = "L10n:"

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
-- @param prefix: string - 段前缀（如 "L10n:"）
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
-- @param prefix: string - 段前缀（如 "L10n:"）
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
-- 读取对象上存储的多语言 key
-- @param obj: FObject - FairyGUI 编辑器对象
-- @return string|nil - 多语言 key；未设置时返回 nil
-- ---------------------------------------------------------------------------
local function readL10nData(obj)
    local data = sanitize(obj.customData)
    return getSegment(data, L10N_PREFIX)
end

-- ---------------------------------------------------------------------------
-- 将多语言 key 写入对象的 customData 中
-- 注意：编辑器中必须通过 obj.docElement:SetProperty() 来修改属性，
-- 这样才能被编辑器的撤销/重做机制追踪。
-- @param obj: FObject - FairyGUI 编辑器对象
-- @param key: string  - 要设置的多语言 key
-- ---------------------------------------------------------------------------
local function writeL10nData(obj, key)
    local data = sanitize(obj.customData)
    local newData = sanitize(setSegment(data, L10N_PREFIX, key))
    -- 编辑器写入：使用 SetProperty 以支持撤销/重做 (Ctrl+Z)
    local ok, err = pcall(function()
        obj.docElement:SetProperty("customData", newData)
        obj.customData = newData
    end)
    if not ok then
        fprint('[L10n] 写入失败: ' .. tostring(err))
    end
end

-- ---------------------------------------------------------------------------
-- 清除对象上的多语言 key（将 L10n 段置空即删除）
-- @param obj: FObject - FairyGUI 编辑器对象
-- ---------------------------------------------------------------------------
local function removeL10nData(obj)
    writeL10nData(obj, "")
end

-- ============================================================================
-- 模块导出
-- ============================================================================
return {
    L10N_PREFIX     = L10N_PREFIX,
    setSegment      = setSegment,
    getSegment      = getSegment,
    readL10nData    = readL10nData,
    writeL10nData   = writeL10nData,
    removeL10nData  = removeL10nData
}
