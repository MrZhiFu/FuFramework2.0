-- ============================================================================
-- RedDot 红点绑定编辑器插件 (main.lua)
-- ============================================================================
-- 为 FairyGUI 编辑器提供红点系统的可视化绑定功能。
--
-- 【Inspector 架构】
--   本插件注册两个 Inspector，分别在不同选中场景下显示：
--
--   1. RedDot_insert（FGUI 组件: Create）
--      触发时机：空选 / 选中根组件本身
--      功能：在根组件上创建红点子节点（通用红点组件）
--
--   2. RedDot_setId（FGUI 组件: SetId）
--      触发时机：选中红点组件自身 或 选中包含红点子节点的父组件
--      功能：编辑红点 ID，数据存储在对应对象的 customData 中
--
-- 【customData 存储】
--   红点 ID 以 "red_dot:<id>" 格式存储在对象的 customData 字段中，
--   由 common.lua 提供读写支持。
--   编辑时使用 obj.docElement:SetProperty() 以确保撤销/重做可用。
--
-- 【关键设计决策】
--   - forEmptySelection=true（insert）：确保未选中任何对象时面板也能显示
--   - forEmptySelection=false（setId）：确保选中子组件时 updateUI 能被触发
--   - InsertObject 替代 CreateObjectFromURL：后者在编辑器中返回 nil
--   - 名称识别优先于 URL 识别：InsertObject 创建的实例 _res:GetURL() 可能不可靠
-- ============================================================================

fprint('[RedDot] 正在加载插件...')

-- 加载公共方法库（读写 customData 中的红点数据）
local common = dofile(PluginPath .. '/common.lua')

-- ============================================================================
-- 配置常量
-- ============================================================================

-- 通用红点组件的资源 URL（在 FairyGUI 编辑器资源库中的唯一标识）
local RED_DOT_URL = "ui://ats3vms3ubwa2u"

-- 红点子节点在父组件中的固定名称
-- 创建时统一命名，便于后续通过 GetChild(name) 和名称判断来查找和识别
local RED_DOT_CHILD_NAME = "_compRedDot"

-- ============================================================================
-- 辅助函数
-- ============================================================================

-- ---------------------------------------------------------------------------
-- 获取编辑器对象对应的资源 URL
-- 说明：编辑器中的 FObject 通过内部 _res 字段持有资源引用，
-- 不能使用 GComponent.resourceURL（那是运行时属性，编辑器无效）。
-- @param obj: FObject - 编辑器对象
-- @return string - 资源 URL；获取失败返回空字符串
-- ---------------------------------------------------------------------------
local function getObjectURL(obj)
    if not obj then return "" end
    local url = ""
    pcall(function()
        if obj._res then url = obj._res:GetURL() or "" end
    end)
    return url or ""
end

-- ---------------------------------------------------------------------------
-- 判断对象是否为红点组件
-- 采用双重识别策略：
--   1. 优先按名称判断（InsertObject 动态创建的实例 name 固定为 _compRedDot）
--   2. 回退按 URL 判断（覆盖从资源库手动拖入等场景）
-- @param obj: FObject - 待判断的编辑器对象
-- @return boolean - 是红点组件返回 true
-- ---------------------------------------------------------------------------
local function isRedDotComponent(obj)
    if not obj then return false end
    -- 优先：名称识别（InsertObject 创建的实例 _res:GetURL() 可能不可靠）
    local name = ""
    pcall(function() name = obj.name end)
    if name == RED_DOT_CHILD_NAME then return true end
    -- 回退：URL 识别
    local url = getObjectURL(obj)
    return url == RED_DOT_URL
end

-- ---------------------------------------------------------------------------
-- 判断对象是否为顶层组件（当前编辑文档的根组件）
-- 依据：_parent 为 nil 表示没有父级，即为根
-- @param obj: FObject - 待判断的编辑器对象
-- @return boolean - 是根组件返回 true
-- ---------------------------------------------------------------------------
local function isRootComponent(obj)
    if not obj then return false end
    local parent = nil
    pcall(function() parent = obj._parent end)
    return parent == nil
end

-- ---------------------------------------------------------------------------
-- 在对象的子节点中查找红点组件（按固定名称匹配）
-- @param obj: FObject - 父组件
-- @return FObject|nil - 找到的红点子节点；未找到返回 nil
-- ---------------------------------------------------------------------------
local function findRedDotChild(obj)
    if not obj then return nil end
    local child = nil
    pcall(function() child = obj:GetChild(RED_DOT_CHILD_NAME) end)
    return child
end

-- ---------------------------------------------------------------------------
-- 判断对象是否已包含红点子节点
-- @param obj: FObject - 待检查的父组件
-- @return boolean - 包含红点子节点返回 true
-- ---------------------------------------------------------------------------
local function hasRedDotChild(obj)
    return findRedDotChild(obj) ~= nil
end

-- ---------------------------------------------------------------------------
-- 在目标对象下创建红点子节点
-- 流程：清理已有红点 → 创建新实例 → 命名 → 定位到右上角
-- 注意：编辑器中必须用 Document:InsertObject() 而非运行时 API
-- @param obj: FObject - 目标父组件（通常是根组件）
-- @return boolean - 创建成功返回 true
-- ---------------------------------------------------------------------------
local function createRedDot(obj)
    if not obj then return false end

    -- [1/4] 清理：如果目标下已有红点，先移除（保证只有一个红点子节点）
    local old = findRedDotChild(obj)
    if old then
        pcall(function() obj:RemoveChild(old, true) end)
    end

    -- [2/4] 获取当前编辑文档
    local doc = App.activeDoc
    if not doc then
        fprint('[RedDot] 无活动文档')
        return false
    end

    -- [3/4] 计算目标尺寸，用于定位红点到右上角
    local targetW, targetH = 0, 0
    pcall(function() targetW = obj.width end)
    pcall(function() targetH = obj.height end)

    -- [4/4] 通过编辑器文档 API 插入红点组件实例
    -- 注意：不能用 FairyGUI.UIPackage.CreateObject()（运行时 API），
    -- 编辑器场景下该 API 返回 nil；必须使用 Document:InsertObject()
    local insertedObj = nil
    local ok, err = pcall(function()
        insertedObj = doc:InsertObject(RED_DOT_URL, nil, -1)  -- pos=nil 表示画布中心
    end)
    if not ok or not insertedObj then
        fprint('[RedDot] 创建红点失败: ' .. tostring(err))
        return false
    end

    -- 重命名为固定名称，用于后续查找和识别
    pcall(function() insertedObj.name = RED_DOT_CHILD_NAME end)

    -- 定位到目标右上角：x = 目标宽度 - 红点半宽, y = -红点半高
    local dotW, dotH = 0, 0
    pcall(function() dotW = insertedObj._rawWidth end)
    pcall(function() dotH = insertedObj._rawHeight end)
    pcall(function()
        insertedObj.docElement:SetProperty("xy",
            CS.UnityEngine.Vector2(targetW - dotW * 0.5, -dotH * 0.5))
    end)

    fprint('[RedDot] 红点创建成功')
    return true
end

-- ============================================================================
-- 加载 UI 包
-- 必须在 AddInspector 之前执行，否则 inspector.create() 中
-- 的 UIPackage.CreateObject() 会因找不到包而返回 nil
-- ============================================================================
App.pluginManager:LoadUIPackage(PluginPath .. '/RedDot')

-- ============================================================================
-- Inspector 1: RedDot_insert — 创建红点
-- ============================================================================
-- FGUI 组件: Create
-- ConnectInspector: forEmptySelection=true → 空选/根组件时框架调用 updateUI
-- 功能：在根组件下创建红点子节点
-- ============================================================================

local insertInspector = {}

-- ---------------------------------------------------------------------------
-- Inspector 生命周期：create()
-- 框架首次需要显示此 Inspector 时调用，创建并返回面板 UI
-- @return GComponent - 面板根组件
-- ---------------------------------------------------------------------------
function insertInspector.create()
    -- 从已加载的 "RedDot" UI 包中创建 "Create" 组件实例
    insertInspector.panel = CS.FairyGUI.UIPackage.CreateObject("RedDot", "Create")
    insertInspector.create_btn = insertInspector.panel:GetChild("create_btn")

    if insertInspector.create_btn then
        -- 绑定按钮点击：在根组件下创建红点子节点
        insertInspector.create_btn.onClick:Add(function()
            local doc = App.activeDoc
            if not doc then return end
            -- 创建目标：优先取 doc.content（文档根组件），回退到当前选中
            local target = doc.content
            if not target then target = doc.inspectingTarget end
            if not target then return end

            createRedDot(target)
        end)
    end

    return insertInspector.panel
end

-- ---------------------------------------------------------------------------
-- Inspector 生命周期：updateUI()
-- 框架在选中状态变化时调用，返回 false 则隐藏此 Inspector
-- 由于 forEmptySelection=true，空选和根组件选中时都会触发
-- @return boolean - 是否显示此 Inspector
-- ---------------------------------------------------------------------------
function insertInspector.updateUI()
    local doc = App.activeDoc
    if not doc then return false end

    -- 仅在"无选中"或"选中根组件本身"时显示
    -- 原因：红点应创建在根组件下，选中子组件时不显示创建按钮
    local target = doc.inspectingTarget
    local shouldShow = (target == nil) or isRootComponent(target)
    if not shouldShow then return false end

    return true
end

-- ============================================================================
-- Inspector 2: RedDot_setId — 编辑红点 ID
-- ============================================================================
-- FGUI 组件: SetId
-- ConnectInspector: forEmptySelection=false → 选中对象时框架调用 updateUI
-- 功能：读取/编辑红点组件的 ID，存储在 customData 中
-- ============================================================================

local setIdInspector = {}

-- ---------------------------------------------------------------------------
-- Inspector 生命周期：create()
-- @return GComponent - 面板根组件
-- ---------------------------------------------------------------------------
function setIdInspector.create()
    setIdInspector.panel = CS.FairyGUI.UIPackage.CreateObject("RedDot", "SetId")
    setIdInspector.red_id = setIdInspector.panel:GetChild("red_id")

    if setIdInspector.red_id then
        -- 失焦校验：验证输入为有效整数后写入 customData
        setIdInspector.red_id.onFocusOut:Add(function()
            local doc = App.activeDoc
            if not doc then return end
            local obj = doc.inspectingTarget
            if not obj then return end

            -- 安全检查：仅在选中对象为红点组件或含红点子节点时才写入
            -- 自身模式：选中红点组件本身 → 写自身 customData
            -- 子组件模式：选中含红点子的父组件 → 写父组件 customData
            if not isRedDotComponent(obj) and not hasRedDotChild(obj) then return end

            local redId = setIdInspector.red_id.text or ""
            -- 校验：空值或非整数时清除红点数据并提示
            if redId == "" or not tonumber(redId) then
                fprint('[RedDot] 红点 ID 无效（需为整数），已清除: ' .. redId)
                setIdInspector.red_id.text = ""
                common.removeRedDotData(obj)
                return
            end
            common.writeRedDotData(obj, redId)
        end)
    end

    return setIdInspector.panel
end

-- ---------------------------------------------------------------------------
-- Inspector 生命周期：updateUI()
-- @return boolean - 是否显示此 Inspector
-- ---------------------------------------------------------------------------
function setIdInspector.updateUI()
    local doc = App.activeDoc
    if not doc then return false end
    local obj = doc.inspectingTarget

    -- 对象不存在或已被销毁时隐藏
    if not obj or obj.isDisposed then return false end

    -- 自身模式：选中红点组件自身 → 显示面板编辑 ID
    -- 子组件模式：选中含红点子的父组件 → 显示面板编辑 ID
    -- 其他情况（普通子组件等）→ 隐藏
    if not isRedDotComponent(obj) and not hasRedDotChild(obj) then return false end

    -- 回显已有的红点 ID（自身模式读自身，子组件模式读父组件）
    if setIdInspector.red_id then
        local redId = common.readRedDotData(obj) or ""
        setIdInspector.red_id.text = redId
    end

    return true
end

-- ============================================================================
-- 注册插件
-- ============================================================================

-- 向编辑器注册两个 Inspector
-- AddInspector(table, name, title): table 需提供 create() 和 updateUI()
App.inspectorView:AddInspector(insertInspector, "RedDot_insert", "红点设置")
App.inspectorView:AddInspector(setIdInspector,  "RedDot_setId",  "红点 ID")

-- 将 Inspector 连接到编辑器文档
-- ConnectInspector(name, objectType, forEmptySelection, forTimelineMode):
--   objectType="component" → 编辑组件文档时生效
--   forEmptySelection=true  → 空选时也调用 updateUI（用于 insert）
--   forEmptySelection=false → 仅选中对象时调用 updateUI（用于 setId）
--   forTimelineMode=false   → 时间轴模式下不显示
App.docFactory:ConnectInspector("RedDot_insert", "component", true, false)
App.docFactory:ConnectInspector("RedDot_setId",  "component", false, false)

-- ---------------------------------------------------------------------------
-- 插件销毁回调：编辑器卸载插件时调用
-- ---------------------------------------------------------------------------
function onDestroy()
    fprint('[RedDot] 插件已销毁')
end

fprint('[RedDot] 插件加载成功')
