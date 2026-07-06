-- 功能2: 扫描重复图片
-- 基于 MD5 检测跨包重复图片，支持逐组浏览、一键替换引用并删除

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

-- 重复图片组列表（每组包含多个相同内容的图片信息）
M.duplicateGroups = {}
-- 当前浏览的组索引
M.groupIndex = 0
-- 当前组内的图片索引
M.itemInGroupIndex = 0

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

--- 显示当前组的对比信息（各图片路径、引用数、是否可安全删除）
local function showGroupComparison()
    if M.groupIndex == 0 or M.groupIndex > #M.duplicateGroups then return end
    local group = M.duplicateGroups[M.groupIndex]
    fprint(string.format("[AtlasOrganizer] ======== 第%d组/共%d组 (重复x%d) ========",
        M.groupIndex, #M.duplicateGroups, #group))
    for i, info in ipairs(group) do
        local refCount = getRefCount(info.pi)
        info.refCount = refCount
        local mark = refCount == 0 and " ⚠️可安全删除" or ""
        fprint(string.format("  [%d] [%s] %s%s  引用数=%d%s",
            i, info.pkgName, info.path, info.fileName, refCount, mark))
    end
    M.itemInGroupIndex = 1
    App.libView:Highlight(group[1].pi, true)
    fprint("[AtlasOrganizer] 操作: 「定位下一个」切换组内查看 |「删除当前并替换引用」执行清理")
end

--- 定位下一个重复图片（组内循环，超出则切换到下一组）
function M.locateNext()
    if #M.duplicateGroups == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描重复图片")
        return
    end
    if M.groupIndex == 0 then
        M.groupIndex = 1
        showGroupComparison()
        return
    end
    M.itemInGroupIndex = M.itemInGroupIndex + 1
    if M.itemInGroupIndex > #M.duplicateGroups[M.groupIndex] then
        M.groupIndex = M.groupIndex + 1
        if M.groupIndex > #M.duplicateGroups then M.groupIndex = 1 end
        showGroupComparison()
    else
        local info = M.duplicateGroups[M.groupIndex][M.itemInGroupIndex]
        App.libView:Highlight(info.pi, true)
        fprint(string.format("[AtlasOrganizer] 查看组内 [%d]: [%s] %s%s",
            M.itemInGroupIndex, info.pkgName, info.path, info.fileName))
    end
end

--- 定位上一个重复图片
function M.locatePrev()
    if #M.duplicateGroups == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描重复图片")
        return
    end
    if M.groupIndex == 0 then
        M.groupIndex = #M.duplicateGroups
        showGroupComparison()
        return
    end
    M.itemInGroupIndex = M.itemInGroupIndex - 1
    if M.itemInGroupIndex < 1 then
        M.groupIndex = M.groupIndex - 1
        if M.groupIndex < 1 then M.groupIndex = #M.duplicateGroups end
        showGroupComparison()
    else
        local info = M.duplicateGroups[M.groupIndex][M.itemInGroupIndex]
        App.libView:Highlight(info.pi, true)
        fprint(string.format("[AtlasOrganizer] 查看组内 [%d]: [%s] %s%s",
            M.itemInGroupIndex, info.pkgName, info.path, info.fileName))
    end
end

--- 删除当前定位的重复图片，并将其引用替换到组内引用数最多的图片
function M.deleteCurrentAndReplace()
    if #M.duplicateGroups == 0 or M.groupIndex == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描重复图片并定位到某组")
        return
    end
    local group = M.duplicateGroups[M.groupIndex]
    if #group < 2 then
        fprint("[AtlasOrganizer] 此组已处理完毕，跳到下一组")
        M.groupIndex = M.groupIndex + 1
        if M.groupIndex > #M.duplicateGroups then M.groupIndex = 1 end
        showGroupComparison()
        return
    end

    local current = group[M.itemInGroupIndex]

    -- 找到组内引用数最多的图片作为保留目标
    local keepIndex = nil
    local maxRef = -1
    for i, info in ipairs(group) do
        if i ~= M.itemInGroupIndex then
            local ref = info.refCount or getRefCount(info.pi)
            if ref > maxRef then
                maxRef = ref
                keepIndex = i
            end
        end
    end

    if not keepIndex then
        fprint("[AtlasOrganizer] 无法找到保留目标")
        return
    end

    local keepItem = group[keepIndex]

    -- 将当前图片的所有引用替换到保留目标
    local query = CS.FairyEditor.DependencyQuery()
    query:QueryReferences(App.project, current.pi:GetURL())
    local refCount = query.references and query.references.Count or 0
    if refCount > 0 then
        query:ReplaceReferences(keepItem.pi)
        fprint(string.format("[AtlasOrganizer] 已将 %d 处引用从 [%s]%s 替换到 [%s]%s",
            refCount, current.pkgName, current.fileName, keepItem.pkgName, keepItem.fileName))
    end

    local pkg = current.pi.owner
    if pkg then
        pkg:DeleteItem(current.pi)
        fprint(string.format("[AtlasOrganizer] 已删除: [%s] %s%s", current.pkgName, current.path, current.fileName))
    end

    table.remove(group, M.itemInGroupIndex)
    if M.itemInGroupIndex > #group then M.itemInGroupIndex = #group end

    if #group < 2 then
        fprint("[AtlasOrganizer] 此组已清理完毕")
        table.remove(M.duplicateGroups, M.groupIndex)
        if #M.duplicateGroups == 0 then
            fprint("[AtlasOrganizer] 所有重复图片已清理完毕！")
            M.groupIndex = 0
            return
        end
        if M.groupIndex > #M.duplicateGroups then M.groupIndex = 1 end
        showGroupComparison()
    else
        showGroupComparison()
    end
end

--- 扫描所有包，基于文件 MD5 检测重复图片
function M.scan()
    fprint("[AtlasOrganizer] 开始扫描重复图片 (MD5 检测)...")

    local md5Provider = CS.System.Security.Cryptography.MD5.Create()
    local hashMap = {}

    for _, pkg in ipairs(utils.getAllPackages()) do
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "image" and not utils.isExcludedPath(item.path) then
                local filePath = utils.getImageFilePath(pkg, item)
                if CS.System.IO.File.Exists(filePath) then
                    local ok, hash = pcall(function()
                        local bytes = CS.System.IO.File.ReadAllBytes(filePath)
                        local hashBytes = md5Provider:ComputeHash(bytes)
                        return CS.System.BitConverter.ToString(hashBytes):gsub("-", "")
                    end)
                    if ok and hash then
                        if not hashMap[hash] then hashMap[hash] = {} end
                        hashMap[hash][#hashMap[hash] + 1] = {
                            pkg = pkg, item = item, path = filePath
                        }
                    end
                end
            end
        end
    end

    local rows = {}
    M.duplicateGroups = {}
    M.groupIndex = 0
    M.itemInGroupIndex = 0
    for _, entries in pairs(hashMap) do
        if #entries > 1 then
            local locations = {}
            local group = {}
            for _, entry in ipairs(entries) do
                locations[#locations + 1] = string.format("%s:%s%s", entry.pkg.name, entry.item.path, entry.item.fileName)
                group[#group + 1] = {
                    pi = entry.item,
                    pkgName = entry.pkg.name,
                    path = entry.item.path,
                    fileName = entry.item.fileName
                }
            end
            M.duplicateGroups[#M.duplicateGroups + 1] = group
            rows[#rows + 1] = {
                sortKey = entries[1].pkg.name,
                text = string.format("[重复x%d] → %s", #entries, table.concat(locations, " || "))
            }
        end
    end

    md5Provider:Dispose()
    utils.showResult("扫描重复图片", rows)
    if #M.duplicateGroups > 0 then
        fprint(string.format("[AtlasOrganizer] 共 %d 组重复图片，使用「定位下一个/上一个」按组浏览，「删除当前并替换引用」执行清理", #M.duplicateGroups))
        M.groupIndex = 1
        showGroupComparison()
    end
end

--- 重置状态
function M.reset()
    M.duplicateGroups = {}
    M.groupIndex = 0
    M.itemInGroupIndex = 0
end

return M