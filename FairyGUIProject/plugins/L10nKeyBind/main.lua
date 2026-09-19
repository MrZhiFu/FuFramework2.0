-- ============================================================================
-- L10nKeyBind 多语言绑定编辑器插件 (main.lua)
-- ============================================================================
-- 为 FairyGUI 编辑器提供多语言 key 的可视化绑定功能。
--
-- 【Inspector 架构】
--   ConnectInspector 的 forObjectType 是**选中对象的 objectType 过滤**，
--   不同基础类型需分别连接（GButton/GLabel 的 objectType="component"，
--   GTextField 的 objectType="text"），故注册两个 Inspector：
--
--   1. L10nKey_set_com（objectType: component）
--      覆盖 GButton/GLabel（经 extentionId 二级判别）与其他扩展组件（排除）
--   2. L10nKey_set_text（objectType: text）
--      覆盖 GTextField（GRichTextField 若 objectType 同为 text 亦覆盖）
--
-- 【customData 存储】
--   多语言 key 以 "L10n:<key>" 格式存储在对象的 customData 字段中，
--   由 common.lua 提供读写支持（"|" 分段与其他插件段共存）。
--   运行时由 FairyGUI GObject.L10n 分部解析并自动应用对应语言文本。
--
-- 【一期范围】
--   仅支持简单模式（单 key）。控制器模式（逐页 key）由运行时解析兼容
--   （手填 "L10n:ctrl,0=key1,1=key2" 生效），编辑器交互后续按需扩展。
-- ============================================================================

fprint('[L10nKeyBind] 正在加载插件...')

-- 加载公共方法库（读写 customData 中的多语言 key 段）
local common = dofile(PluginPath .. '/common.lua')

-- ============================================================================
-- 配置常量
-- ============================================================================

-- 基础 objectType 的可绑定集合（小写比较）
-- component：扩展组件（GButton/GLabel 等），需经 extentionId 二级判别
-- text：GTextField/GRichTextField
local BINDABLE_OBJECT_TYPES = {
    component = true,
    text      = true,
}

-- 扩展组件中可绑定的 extentionId（小写比较；未收录值会诊断输出）
local BINDABLE_EXTENTION_IDS = {
    button = true,
    label  = true,
}

-- ============================================================================
-- 辅助函数
-- ============================================================================

-- ---------------------------------------------------------------------------
-- 判断选中对象是否可绑定多语言 key（objectType + extentionId 二级判别）
-- 未收录的类型/扩展会输出诊断（fprint），便于校准集合
-- @param obj: FObject - 编辑器对象
-- @return boolean     - 可绑定返回 true
-- @return string      - 判别依据（诊断用）
-- ---------------------------------------------------------------------------
local function isBindableType(obj)
    local ot = string.lower(tostring(obj.objectType or ''))

    if ot == 'text' or ot == 'richtext' then
        return true, ot
    end

    if ot == 'component' then
        local extId = string.lower(tostring(obj.extentionId or ''))
        if extId == '' then
            return false, ot
        end
        if BINDABLE_EXTENTION_IDS[extId] then
            return true, ot .. '/' .. extId
        end
        fprint('[L10n] 未收录的 extentionId: "' .. extId .. '"')
        return false, ot .. '/' .. extId
    end

    fprint('[L10n] 未收录的 objectType: "' .. ot .. '"')
    return false, ot
end

-- ============================================================================
-- 面板创建与更新（两个 Inspector 共用实现，各自持有面板实例）
-- ============================================================================

-- ---------------------------------------------------------------------------
-- 创建面板：从 L10nKey 包创建 SetL10nKey 组件，绑定输入框失焦写入
-- @return GComponent - 面板根组件
-- ---------------------------------------------------------------------------
local function createPanel()
    local panel = CS.FairyGUI.UIPackage.CreateObject("L10nKey", "SetL10nKey")
    fprint('[L10n] create: panel=' .. tostring(panel ~= nil))
    local input = panel and panel:GetChild("l10n_key")
    fprint('[L10n] create: l10n_key=' .. tostring(input ~= nil))

    if input then
        -- 失焦写入：类型守卫通过后把输入框内容写入 customData 的 L10n: 段
        input.onFocusOut:Add(function()
            local doc = App.activeDoc
            if not doc then return end
            local obj = doc.inspectingTarget
            if not obj then return end

            local bindable = isBindableType(obj)
            if not bindable then return end

            local key = input.text or ""
            -- 空值 → 清除绑定；非空 → 写入（key 为字符串，不做格式强校验）
            if key == "" then
                common.removeL10nData(obj)
                fprint('[L10n] 已清除多语言绑定')
                return
            end
            common.writeL10nData(obj, key)
            fprint('[L10n] 已绑定多语言 key: ' .. key)
        end)
    end

    return panel, input
end

-- ---------------------------------------------------------------------------
-- 更新面板：回显选中对象的已绑定 key
-- @param panel:  GComponent     - 面板实例
-- @param input:  GTextInput    - 输入框实例
-- @param obj:    FObject|nil   - 选中对象
-- @return boolean - 是否显示此 Inspector
-- ---------------------------------------------------------------------------
local function updatePanel(panel, input, obj)
    if not obj or obj.isDisposed then return false end

    local bindable = isBindableType(obj)
    if not bindable then return false end

    if input then
        input.text = common.readL10nData(obj) or ""
    end

    return true
end

-- ============================================================================
-- 加载 UI 包
-- 必须在 AddInspector 之前执行，否则 inspector.create() 中
-- 的 UIPackage.CreateObject() 会因找不到包而返回 nil
-- ============================================================================

App.pluginManager:LoadUIPackage(PluginPath .. '/L10nKey')

-- ============================================================================
-- Inspector 1: L10nKey_set_com — objectType=component（GButton/GLabel）
-- ============================================================================

local setComInspector = {}

function setComInspector.create()
    setComInspector.panel, setComInspector.l10n_key = createPanel()
    return setComInspector.panel
end

function setComInspector.updateUI()
    local doc = App.activeDoc
    if not doc then return false end
    local obj = doc.inspectingTarget
    if not obj or obj.isDisposed then return false end

    -- 仅 component 基础类型处理（text 类由 text 版 Inspector 负责）
    local ot = string.lower(tostring(obj.objectType or ''))
    if ot ~= 'component' then return false end

    return updatePanel(setComInspector.panel, setComInspector.l10n_key, obj)
end

-- ============================================================================
-- Inspector 2: L10nKey_set_text — objectType=text（GTextField）
-- ============================================================================

local setTextInspector = {}

function setTextInspector.create()
    setTextInspector.panel, setTextInspector.l10n_key = createPanel()
    return setTextInspector.panel
end

function setTextInspector.updateUI()
    local doc = App.activeDoc
    if not doc then return false end
    local obj = doc.inspectingTarget
    if not obj or obj.isDisposed then return false end

    -- 仅 text/richtext 基础类型处理（component 类由 com 版 Inspector 负责）
    local ot = string.lower(tostring(obj.objectType or ''))
    if ot ~= 'text' and ot ~= 'richtext' then return false end

    return updatePanel(setTextInspector.panel, setTextInspector.l10n_key, obj)
end

-- ============================================================================
-- 注册插件
-- ============================================================================

-- 向编辑器注册 Inspector（text 与 richtext 共用 setTextInspector 实例）
App.inspectorView:AddInspector(setComInspector,  "L10nKey_set_com",  "多语言绑定")
App.inspectorView:AddInspector(setTextInspector, "L10nKey_set_text", "多语言绑定")
App.inspectorView:AddInspector(setTextInspector, "L10nKey_set_richtext", "多语言绑定")

-- 将 Inspector 连接到编辑器文档（forObjectType = 选中对象的 objectType 过滤）
-- ConnectInspector(name, forObjectType, forEmptySelection, forTimelineMode)
App.docFactory:ConnectInspector("L10nKey_set_com",       "component", false, false)
App.docFactory:ConnectInspector("L10nKey_set_text",      "text",      false, false)
App.docFactory:ConnectInspector("L10nKey_set_richtext",  "richtext",  false, false)

-- ---------------------------------------------------------------------------
-- 插件销毁回调：编辑器卸载插件时调用
-- ---------------------------------------------------------------------------
function onDestroy()
    fprint('[L10nKeyBind] 插件已销毁')
end

fprint('[L10nKeyBind] 插件加载成功')
