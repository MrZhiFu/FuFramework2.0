-- 功能3: 扫描未引用资源
-- 全局跨包检测未被任何组件/动画引用的图片/Sprite，输出分布报告
-- 「一键清理未引用资源」自动删除（导出资源豁免，示例类路径排除）

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

--- 收集全局未被引用的图片/Sprite 条目
--- 检测方式：读取所有组件/动画 XML，收集 src="" 与 ui:// 引用的资源 id，
--- 未被引用且未导出的图片视为未引用
---@return table 未引用条目列表 {pi, pkg, pkgName, path, fileName}
local function collectUnused()
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
            -- 组件与动画(MovieClip)的 XML 都会引用图片
            if item.type == "component" or item.type == "movieclip" then
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

    local unused = {}
    for id, info in pairs(allImages) do
        if not referencedIds[id] and not info.item.exported then
            local item = info.item
            if not utils.isExcludedPath(item.path) then
                unused[#unused + 1] = {
                    pi = item, pkg = info.pkg, pkgName = info.pkg.name,
                    path = item.path, fileName = item.fileName
                }
            end
        end
    end
    return unused
end

--- 扫描全局未引用的图片/Sprite资源，输出分布报告
function M.scan()
    fprint("[AtlasOrganizer] 开始扫描未引用资源 (全局跨包)...")

    local unused = collectUnused()

    local rows = {}
    for _, info in ipairs(unused) do
        rows[#rows + 1] = {
            sortKey = info.pkgName,
            text = string.format("[%s] %s%s", info.pkgName, info.path, info.fileName)
        }
    end

    utils.showResult("扫描未引用资源", rows)
    if #unused == 0 then
        fprint("[AtlasOrganizer] 未发现未引用资源")
    else
        fprint(string.format("[AtlasOrganizer] 共 %d 个未引用资源，可使用「一键清理未引用资源」自动删除", #unused))
    end
end

--- 一键删除全部未引用的图片/Sprite资源
function M.clean()
    fprint("[AtlasOrganizer] ======== 一键清理未引用资源 ========")
    fprint("[AtlasOrganizer] 正在扫描 (全局跨包)...")

    local unused = collectUnused()
    if #unused == 0 then
        fprint("[AtlasOrganizer] 未发现未引用资源，无需清理")
        return
    end

    fprint(string.format("[AtlasOrganizer] 发现 %d 个未引用资源，开始清理...", #unused))

    local deletedCount = 0
    local touchedPkgs = {}
    for _, info in ipairs(unused) do
        -- 快照写回式删除: 同文件多ID时保护仍在包内引用该文件的条目
        local filePath = utils.getImageFilePath(info.pkg, info.pi)
        if utils.deleteItemKeepFile(info.pi, filePath) then
            deletedCount = deletedCount + 1
            touchedPkgs[info.pkg] = true
            fprint(string.format("[AtlasOrganizer] 已删除: [%s] %s%s", info.pkgName, info.path, info.fileName))
        end
    end

    local pkgList = {}
    for pkg in pairs(touchedPkgs) do pkgList[#pkgList + 1] = pkg end
    for _, pkg in ipairs(pkgList) do
        pcall(function() pkg:Save() end)
    end

    fprint(string.format("[AtlasOrganizer] ======== 完成: 删除 %d 个 / 保存 %d 个包 ========",
        deletedCount, #pkgList))
end

return M
