-- 功能3: 扫描未引用资源
-- 全局跨包检测未被任何组件引用的图片/Sprite，支持逐个定位和删除

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

-- 未引用资源列表
M.unusedList = {}
-- 当前定位索引
M.unusedIndex = 0

--- 定位下一个未引用资源
function M.locateNext()
    if #M.unusedList == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描未引用资源")
        return
    end
    M.unusedIndex = M.unusedIndex + 1
    if M.unusedIndex > #M.unusedList then M.unusedIndex = 1 end
    local info = M.unusedList[M.unusedIndex]
    App.libView:Highlight(info.pi, true)
    fprint(string.format("[AtlasOrganizer] 未引用 %d/%d: [%s] %s%s",
        M.unusedIndex, #M.unusedList, info.pkgName, info.path, info.fileName))
end

--- 定位上一个未引用资源
function M.locatePrev()
    if #M.unusedList == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描未引用资源")
        return
    end
    M.unusedIndex = M.unusedIndex - 1
    if M.unusedIndex < 1 then M.unusedIndex = #M.unusedList end
    local info = M.unusedList[M.unusedIndex]
    App.libView:Highlight(info.pi, true)
    fprint(string.format("[AtlasOrganizer] 未引用 %d/%d: [%s] %s%s",
        M.unusedIndex, #M.unusedList, info.pkgName, info.path, info.fileName))
end

--- 删除当前定位的未引用资源
function M.deleteCurrent()
    if #M.unusedList == 0 or M.unusedIndex == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描未引用资源并定位")
        return
    end
    local info = M.unusedList[M.unusedIndex]
    local pkg = info.pi.owner
    if pkg then
        pkg:DeleteItem(info.pi)
        fprint(string.format("[AtlasOrganizer] 已删除: [%s] %s%s", info.pkgName, info.path, info.fileName))
    end
    table.remove(M.unusedList, M.unusedIndex)
    if #M.unusedList == 0 then
        fprint("[AtlasOrganizer] 所有未引用资源已清理完毕！")
        M.unusedIndex = 0
        return
    end
    if M.unusedIndex > #M.unusedList then M.unusedIndex = #M.unusedList end
    local nextInfo = M.unusedList[M.unusedIndex]
    App.libView:Highlight(nextInfo.pi, true)
    fprint(string.format("[AtlasOrganizer] 未引用 %d/%d: [%s] %s%s",
        M.unusedIndex, #M.unusedList, nextInfo.pkgName, nextInfo.path, nextInfo.fileName))
end

--- 扫描全局未引用的图片/Sprite资源
--- 检测方式：遍历所有组件 XML，收集 src="" 和 ui:// 引用的 id，未被引用且未导出的图片视为未引用
function M.scan()
    fprint("[AtlasOrganizer] 开始扫描未引用资源 (全局跨包)...")

    local allImages = {}
    local allPackages = utils.getAllPackages()

    for _, pkg in ipairs(allPackages) do
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "image" or item.type == "sprite" then
                allImages[item.id] = { pkg = pkg, item = item }
            end
        end
    end

    local referencedIds = {}
    for _, pkg in ipairs(allPackages) do
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "component" then
                local xmlPath = App.project.basePath .. "/assets/" .. pkg.name .. item.path .. item.fileName
                local xmlContent = utils.readFileText(xmlPath)
                if xmlContent then
                    for id in string.gmatch(xmlContent, 'src="([^"]+)"') do
                        referencedIds[id] = true
                    end
                    for id in string.gmatch(xmlContent, 'ui://........([%w]+)') do
                        referencedIds[id] = true
                    end
                end
            end
        end
    end

    local rows = {}
    M.unusedList = {}
    M.unusedIndex = 0
    for id, info in pairs(allImages) do
        if not referencedIds[id] and not info.item.exported then
            local item = info.item
            local pkg = info.pkg
            if not utils.isExcludedPath(item.path) then
                rows[#rows + 1] = {
                    sortKey = pkg.name,
                    text = string.format("[%s] %s", pkg.name, item.fileName)
                }
                M.unusedList[#M.unusedList + 1] = {
                    pi = item, pkgName = pkg.name,
                    path = item.path, fileName = item.fileName
                }
            end
        end
    end

    utils.showResult("扫描未引用资源", rows)
    if #M.unusedList > 0 then
        fprint(string.format("[AtlasOrganizer] 共 %d 个未引用资源，使用「定位下一个/上一个未引用」浏览，「删除当前未引用」清理", #M.unusedList))
    end
end

--- 重置状态
function M.reset()
    M.unusedList = {}
    M.unusedIndex = 0
end

return M