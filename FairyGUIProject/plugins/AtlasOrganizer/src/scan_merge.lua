-- 功能6: 一键合并同文件重复ID
-- 同一张图片在 package.xml 中可能出现多个条目(不同 ID 指向同一文件),
-- 导致删除并替换引用时, DeleteItem 把共享的物理文件一并删掉, 保留条目无图可用。
-- 本功能按物理文件路径分组, 组内保留引用最多/已导出/最早创建的条目,
-- 将其余条目的引用统一改写并删除条目, 物理图片文件始终保留。

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

--- 查询资源被引用次数
---@param item FPackageItem
---@return number
local function getRefCount(item)
    local query = CS.FairyEditor.DependencyQuery()
    query:QueryReferences(App.project, item:GetURL())
    local refs = query.references
    if refs then return refs.Count end
    return 0
end

--- 候选条目是否优于当前保留项(保留规则: 引用多 > 已导出 > 序号早)
---@param candidate table {refCount=number, exported=boolean}
---@param currentKeep table 同结构
---@return boolean
local function isBetterKeep(candidate, currentKeep)
    if candidate.refCount ~= currentKeep.refCount then
        return candidate.refCount > currentKeep.refCount
    end
    if candidate.exported ~= currentKeep.exported then
        return candidate.exported
    end
    return false
end

--- 删除条目但保留物理文件
--- DeleteItem 会把图片文件一并删掉, 而本组保留条目仍引用同一文件,
--- 故先快照文件字节, 删除后文件消失则写回。
---@param pi FPackageItem
---@param filePath string 该条目的物理文件路径
---@return boolean
local function deleteItemKeepFile(pi, filePath)
    local pkg = pi.owner
    if not pkg then return false end

    local bytes = nil
    if CS.System.IO.File.Exists(filePath) then
        local ok, data = pcall(function()
            return CS.System.IO.File.ReadAllBytes(filePath)
        end)
        if ok then bytes = data end
    end

    local ok, err = pcall(function() pkg:DeleteItem(pi) end)
    if not ok then
        fprint("[AtlasOrganizer] 删除条目失败: " .. tostring(err))
        return false
    end

    if bytes and not CS.System.IO.File.Exists(filePath) then
        pcall(function() CS.System.IO.File.WriteAllBytes(filePath, bytes) end)
    end
    return true
end

--- 一键合并全部包中的同文件重复ID
function M.merge()
    local pathMap = {}
    for _, pkg in ipairs(utils.getAllPackages()) do
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "image" and not utils.isExcludedPath(item.path) then
                local filePath = utils.getImageFilePath(pkg, item)
                if not pathMap[filePath] then pathMap[filePath] = {} end
                pathMap[filePath][#pathMap[filePath] + 1] = { pkg = pkg, item = item }
            end
        end
    end

    local groups = {}
    for filePath, entries in pairs(pathMap) do
        if #entries > 1 then
            groups[#groups + 1] = { filePath = filePath, entries = entries }
        end
    end

    if #groups == 0 then
        fprint("[AtlasOrganizer] 未发现同文件重复ID的图片，无需合并")
        return
    end

    fprint("[AtlasOrganizer] ======== 一键合并同文件重复ID ========")
    fprint(string.format("[AtlasOrganizer] 发现 %d 组同文件重复ID，开始合并...", #groups))

    local mergedGroups = 0
    local deletedItems = 0
    local rewrittenRefs = 0
    local touchedPkgs = {}

    for _, group in ipairs(groups) do
        local entries = group.entries
        for _, e in ipairs(entries) do
            e.refCount = getRefCount(e.item)
            e.exported = e.item.exported
        end

        local keep = entries[1]
        for i = 2, #entries do
            if isBetterKeep(entries[i], keep) then keep = entries[i] end
        end

        local details = {}
        for _, e in ipairs(entries) do
            if e ~= keep then
                -- 单项失败不中断整批, 报告后继续
                local ok, err = pcall(function()
                    local query = CS.FairyEditor.DependencyQuery()
                    query:QueryReferences(App.project, e.item:GetURL())
                    local refCount = query.references and query.references.Count or 0
                    if refCount > 0 then
                        query:ReplaceReferences(keep.item)
                        rewrittenRefs = rewrittenRefs + refCount
                    end
                    if deleteItemKeepFile(e.item, group.filePath) then
                        deletedItems = deletedItems + 1
                        touchedPkgs[e.pkg] = true
                    end
                end)
                if ok then
                    details[#details + 1] = string.format("%s→%s", e.item.id, keep.item.id)
                else
                    fprint("[AtlasOrganizer] 合并条目失败: " .. tostring(err))
                    details[#details + 1] = string.format("%s→失败", e.item.id)
                end
            end
        end

        mergedGroups = mergedGroups + 1
        local fileName = group.filePath:match("[^/\\]+$") or group.filePath
        fprint(string.format("[AtlasOrganizer] [%s] %s 保留 %s，合并: %s",
            keep.pkg.name, fileName, keep.item.id, table.concat(details, "、")))
    end

    local pkgList = {}
    for pkg in pairs(touchedPkgs) do pkgList[#pkgList + 1] = pkg end
    for _, pkg in ipairs(pkgList) do
        pcall(function() pkg:Save() end)
    end

    fprint(string.format(
        "[AtlasOrganizer] ======== 完成: 合并 %d 组 / 删除 %d 个重复ID / 改写 %d 处引用 / 保存 %d 个包 ========",
        mergedGroups, deletedItems, rewrittenRefs, #pkgList))
end

return M