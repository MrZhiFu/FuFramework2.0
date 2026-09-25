-- ============================================================================
-- L10nKeyBind 多语言绑定编辑器插件 (main.lua)
-- ============================================================================
-- 为 FairyGUI 编辑器提供多语言 key 的可视化绑定功能。
--
-- 【Inspector 架构】
--   ConnectInspector 的 forObjectType 是**选中对象的 objectType 过滤**，
--   不同基础类型需分别连接（GButton/GLabel 的 objectType="component"，
--   GTextField 的 objectType="text"），故注册多个 Inspector：
--
--   1. L10nKey_set_com（objectType: component）
--      覆盖 GButton/GLabel（经 extentionId 二级判别）与其他扩展组件（排除）
--   2. L10nKey_set_text（objectType: text）
--      覆盖 GTextField（GRichTextField 若 objectType 同为 text 亦覆盖）
--
-- 【面板与绑定模式】
--   面板（L10nKey 包 SetL10nKey 组件）提供两种绑定模式，同一时刻只保留一种：
--   1. 简单模式：l10n_key 输入框填写单 key → customData 写 "L10n:<key>"
--   2. 控制器模式：ctrl_list 下拉选择所在组件的控制器，page_keys 逐页填写
--      key → customData 写 "L10n:<ctrlName>,<pageId>=<key>,..."
--      任一页输入失焦即组装提交；两区提交互相覆盖对方显示与存储。
--
-- 【customData 存储】
--   多语言绑定以 "L10n:..." 格式存储在对象的 customData 字段中，
--   由 common.lua 提供读写支持（"|" 分段与其他插件段共存）。
--   运行时由 FairyGUI GObject.L10n 分部解析并自动应用对应语言文本。
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

-- ---------------------------------------------------------------------------
-- 向上查找选中对象所在组件（含自身）：返回最近一个带 controllers 的组件
-- @param obj: FObject - 编辑器选中对象
-- @return FComponent|nil - 所在组件（无控制器宿主时返回 nil）
-- ---------------------------------------------------------------------------
local function getOwnerComponent(obj)
    local cur     = obj
    local found   = nil
    while cur ~= nil do
        if cur.controllers ~= nil then
            found = cur
        end
        cur = cur.parent
    end
    return found
end

-- ============================================================================
-- 面板创建与更新（两个 Inspector 共用实现，各自持有面板实例）
-- 面板子件：l10n_key(简单模式输入) / ctrl_list(控制器下拉) / page_keys(逐页key列表)
-- ============================================================================

-- ---------------------------------------------------------------------------
-- 控制器模式提交：遍历 page_keys 各行组装 "ctrlName,0=k1,1=k2" 写入 customData。
-- 绑定为提交函数闭包共享的 panel 状态（写入时从 doc 重新取 obj）。
-- @param state: table - 面板共享状态 { currentCtrl=FController|nil }
-- ---------------------------------------------------------------------------
local function submitCtrlMode(state)
    local doc = App.activeDoc
    if not doc then return end
    local obj = doc.inspectingTarget
    if not obj or obj.isDisposed then return end
    if not state.currentCtrl then return end

    local parts = { tostring(state.currentCtrl.name) }
    local list  = state.page_keys
    local n     = list.numChildren
    for i = 0, n - 1 do
        local item = list:GetChildAt(i)
        local key  = item:GetChild("key_input").text or ""
        if key ~= "" then
            parts[#parts + 1] = i .. "=" .. key
        end
    end

    if #parts == 1 then
        -- 所有页都为空：视为清除绑定
        common.removeL10nData(obj)
        state.l10n_key.text = ""
        fprint('[L10n] 控制器绑定已清除（所有页为空）')
        return
    end

    common.writeL10nData(obj, table.concat(parts, ","))
    state.l10n_key.text = "" -- 两模式互斥：控制器提交后清掉简单模式显示
    fprint('[L10n] 已绑定控制器多语言: ' .. table.concat(parts, ","))
end

-- ---------------------------------------------------------------------------
-- 重建 page_keys 行：按控制器页数生成（page_name + key_input），回显已有绑定
-- @param state:      table           - 面板共享状态
-- @param ctrl:       FController     - 目标控制器
-- @param boundPages: table|nil       - 已绑定页 key 映射 {[pageIndex]=key}
-- ---------------------------------------------------------------------------
local function rebuildPageList(state, ctrl, boundPages)
    local list = state.page_keys
    list:RemoveChildren() -- 直接销毁旧行，避免池化 item 上的旧事件闭包残留

    local names = ctrl:GetPageNames()
    local count = names.Count
    for i = 0, count - 1 do
        local item = list:AddItemFromPool()
        if item == nil then
            fprint('[L10n] AddItemFromPool 返回 nil：请确认 L10nKey 包已包含 PageKeyItem 组件并重新发布')
            return
        end
        local nameLabel = item:GetChild("page_name")
        nameLabel.text = tostring(names[i]) .. " [" .. i .. "]"

        local input = item:GetChild("key_input")
        input.text = (boundPages ~= nil and boundPages[i]) or ""
        input.onFocusOut:Add(function()
            submitCtrlMode(state)
        end)
    end
end

-- ---------------------------------------------------------------------------
-- 控制器下拉填充：枚举所在组件的控制器名写入下拉
-- @param state: table         - 面板共享状态
-- @param comp:  FComponent|nil - 所在组件
-- ---------------------------------------------------------------------------
local function fillCtrlCombo(state, comp)
    local combo = state.ctrl_list
    local names = {}
    state.controllers = nil

    if comp ~= nil and comp.controllers ~= nil then
        state.controllers = comp.controllers
        for i = 0, comp.controllers.Count - 1 do
            names[#names + 1] = tostring(comp.controllers[i].name)
        end
    end

    -- 面板 HasCtrl 控制器切页：控制控制器区的显隐与面板高度（gearDisplay + height_ref gearSize）
    local hasCtrl = state.controllers ~= nil and state.controllers.Count > 0
    local hasCtrlGear = state.panel and state.panel:GetController("HasCtrl")
    if hasCtrlGear ~= nil then
        hasCtrlGear:SetSelectedIndex(hasCtrl and 1 or 0)
    end

    local ok, err = pcall(function()
        combo.items = names
        combo:ApplyListChange()
        combo.selectedIndex = -1
    end)
    if not ok then
        fprint('[L10n] 控制器下拉填充失败: ' .. tostring(err))
    end
end

-- ---------------------------------------------------------------------------
-- 创建面板：从 L10nKey 包创建 SetL10nKey 组件，绑定输入框失焦写入
-- @return GComponent - 面板根组件
-- ---------------------------------------------------------------------------
local function createPanel()
    local panel = CS.FairyGUI.UIPackage.CreateObject("L10nKey", "SetL10nKey")
    fprint('[L10n] create: panel=' .. tostring(panel ~= nil))

    local state = {
        panel       = panel,
        l10n_key    = panel and panel:GetChild("l10n_key"),
        ctrl_list   = panel and panel:GetChild("ctrl_list"),
        page_keys   = panel and panel:GetChild("page_keys"),
        currentCtrl = nil,
        controllers = nil,
    }
    fprint('[L10n] create: l10n_key=' .. tostring(state.l10n_key ~= nil)
           .. ' ctrl_list=' .. tostring(state.ctrl_list ~= nil)
           .. ' page_keys=' .. tostring(state.page_keys ~= nil))

    if state.l10n_key then
        -- 简单模式失焦写入：类型守卫通过后把输入框内容写入 customData 的 L10n: 段
        state.l10n_key.onFocusOut:Add(function()
            local doc = App.activeDoc
            if not doc then return end
            local obj = doc.inspectingTarget
            if not obj then return end

            local bindable = isBindableType(obj)
            if not bindable then return end

            local key = state.l10n_key.text or ""
            -- 空值 → 清除绑定；非空 → 写入（key 为字符串，不做格式强校验）
            if key == "" then
                common.removeL10nData(obj)
                fprint('[L10n] 已清除多语言绑定')
                return
            end
            common.writeL10nData(obj, key)
            -- 两模式互斥：简单提交后清掉控制器区显示
            state.currentCtrl = nil
            state.page_keys:RemoveChildren()
            fprint('[L10n] 已绑定多语言 key: ' .. key)
        end)
    end

    if state.ctrl_list then
        -- 控制器下拉切换：按选中控制器重建逐页 key 行（回显已有绑定）
        state.ctrl_list.onChanged:Add(function()
            local doc = App.activeDoc
            if not doc then return end
            local obj = doc.inspectingTarget
            if not obj or not state.controllers then return end

            local idx = state.ctrl_list.selectedIndex
            if idx < 0 or idx >= state.controllers.Count then
                state.currentCtrl = nil
                state.page_keys:RemoveChildren()
                return
            end

            state.currentCtrl = state.controllers[idx]

            -- 回显该控制器的已绑定页 key（若当前绑定正好指向此控制器）
            local boundPages = nil
            local parsed = common.parseL10nValue(common.readL10nData(obj))
            if parsed.mode == "ctrl" and parsed.ctrl == tostring(state.currentCtrl.name) then
                boundPages = parsed.pages
            end

            rebuildPageList(state, state.currentCtrl, boundPages)
            state.l10n_key.text = "" -- 两模式互斥：切到控制器模式清简单显示
        end)
    end

    return panel, state
end

-- ---------------------------------------------------------------------------
-- 更新面板：回显选中对象的已绑定 key（简单/控制器两模式互斥回显）
-- @param panel: GComponent   - 面板实例
-- @param state: table        - 面板共享状态（各子件引用）
-- @param obj:   FObject|nil  - 选中对象
-- @return boolean - 是否显示此 Inspector
-- ---------------------------------------------------------------------------
local function updatePanel(panel, state, obj)
    if not obj or obj.isDisposed then return false end

    local bindable = isBindableType(obj)
    if not bindable then return false end

    -- 控制器下拉填充（所在组件的控制器列表）
    fillCtrlCombo(state, getOwnerComponent(obj))

    -- 按已绑定模式回显
    local parsed = common.parseL10nValue(common.readL10nData(obj))
    if parsed.mode == "ctrl" then
        -- 控制器模式：选中对应控制器并重建逐页行
        state.l10n_key.text = ""
        local ctrlIndex = -1
        if state.controllers ~= nil then
            for i = 0, state.controllers.Count - 1 do
                if tostring(state.controllers[i].name) == parsed.ctrl then
                    ctrlIndex = i
                    break
                end
            end
        end
        if ctrlIndex >= 0 then
            state.ctrl_list.selectedIndex = ctrlIndex -- 触发 onChanged 重建页行并回显
        else
            -- 控制器已被删除/重命名：清空控制器区显示
            state.currentCtrl = nil
            state.page_keys:RemoveChildren()
            fprint('[L10n] 绑定指向的控制器不存在: "' .. tostring(parsed.ctrl) .. '"')
        end
    else
        -- 简单模式或无绑定：填简单输入框，清控制器区
        state.l10n_key.text = parsed.mode == "simple" and (parsed.key or "") or ""
        state.currentCtrl   = nil
        state.ctrl_list.selectedIndex = -1
        state.page_keys:RemoveChildren()
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
    setComInspector.panel, setComInspector.state = createPanel()
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

    return updatePanel(setComInspector.panel, setComInspector.state, obj)
end

-- ============================================================================
-- Inspector 2: L10nKey_set_text — objectType=text（GTextField）
-- ============================================================================

local setTextInspector = {}

function setTextInspector.create()
    setTextInspector.panel, setTextInspector.state = createPanel()
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

    return updatePanel(setTextInspector.panel, setTextInspector.state, obj)
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
