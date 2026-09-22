-- 功能7: 一键清理重复图片
-- 基于 MD5 检测内容重复的图片(物理文件不同但内容相同), 一键自动合并:
-- 同包组: 组内保留引用最多/已导出/最早创建的条目, 其余改写引用后删除;
-- 跨包组: 图片收拢到 Common 包 /Res/ 并设置导出, 各包引用统一改指 Common。
-- 与功能6(一键合并同文件重复ID)互补: 功能6处理不同ID指向同一物理文件,
-- 本功能处理MD5相同但物理文件不同的重复。

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

-- 收拢目标公共包名与资源路径
local COMMON_PKG_NAME = "Common"
local COMMON_RES_PATH = "/Res/"

--- 扫描全部包, 构建 MD5 → 条目列表 映射
---@return table hashMap
local function buildMD5HashMap()
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
                        hashMap[hash][#hashMap[hash] + 1] = { pkg = pkg, item = item, filePath = filePath }
                    end
                end
            end
        end
    end
    md5Provider:Dispose()
    return hashMap
end

--- 将条目的引用改写到目标条目并删除该条目
---@param e table {pkg, item, filePath}
---@param targetItem FPackageItem
---@param stats table 汇总统计
---@return boolean 是否成功
local function replaceAndDelete(e, targetItem, stats)
    local ok, result = pcall(function()
        local query = CS.FairyEditor.DependencyQuery()
        query:QueryReferences(App.project, e.item:GetURL())
        local refCount = query.references and query.references.Count or 0
        if refCount > 0 then
            query:ReplaceReferences(targetItem)
            stats.rewrittenRefs = stats.rewrittenRefs + refCount
        end
        -- 快照写回删除: 防止组内混有同文件多ID时误删共享物理文件
        return utils.deleteItemKeepFile(e.item, e.filePath)
    end)
    if not ok then
        fprint("[AtlasOrganizer] 合并条目失败: " .. tostring(result))
        return false
    end
    if result then
        stats.deletedItems = stats.deletedItems + 1
        stats.touchedPkgs[e.pkg] = true
        return true
    end
    return false
end

--- 同包组: 组内保留最优条目, 其余改写引用后删除
---@param group table {entries}
---@param stats table
local function cleanSamePackageGroup(group, stats)
    local entries = group.entries
    local keep = entries[1]
    for i = 2, #entries do
        if utils.isBetterKeep(entries[i], keep) then keep = entries[i] end
    end

    local details = {}
    for _, e in ipairs(entries) do
        if e.item ~= keep.item then
            if replaceAndDelete(e, keep.item, stats) then
                details[#details + 1] = string.format("%s→%s", e.item.id, keep.item.id)
            else
                details[#details + 1] = string.format("%s→失败", e.item.id)
            end
        end
    end

    stats.mergedGroups = stats.mergedGroups + 1
    fprint(string.format("[AtlasOrganizer] [%s] %s 保留 %s，合并: %s",
        keep.pkg.name, keep.item.fileName, keep.item.id, table.concat(details, "、")))
end

--- 跨包组: 收拢到 Common 包 /Res/ 并设置导出
---@param group table {entries}
---@param commonPkg FPackage|nil
---@param stats table
local function cleanCrossPackageGroup(group, commonPkg, stats)
    local entries = group.entries

    if not commonPkg then
        stats.skippedGroups = stats.skippedGroups + 1
        local locations = {}
        for _, e in ipairs(entries) do
            locations[#locations + 1] = e.pkg.name .. ":" .. e.item.fileName
        end
        fprint("[AtlasOrganizer] [跳过] 未找到 Common 包，无法收拢: " .. table.concat(locations, " || "))
        return
    end

    -- 收拢目标: 组内已有 Common 条目则用之(B1), 否则从源文件导入新条目(B2)
    local target = nil
    for _, e in ipairs(entries) do
        if e.pkg.name == COMMON_PKG_NAME then
            target = e
            break
        end
    end

    if target then
        -- B1: 归位到 /Res/ 并设为导出(MoveItem 改 path 不改 id, 既有引用自动跟随)
        local ok, err = pcall(function()
            if target.item.path ~= COMMON_RES_PATH then
                commonPkg:EnsurePathExists(COMMON_RES_PATH, true)
                commonPkg:MoveItem(target.item, COMMON_RES_PATH)
                stats.movedItems = stats.movedItems + 1
            end
            if not target.item.exported then
                target.item.exported = true
                stats.exportedItems = stats.exportedItems + 1
            end
            stats.touchedPkgs[commonPkg] = true
        end)
        if not ok then
            stats.skippedGroups = stats.skippedGroups + 1
            fprint("[AtlasOrganizer] [跳过] Common 条目归位失败: " .. tostring(err))
            return
        end
    else
        -- B2: 从引用最多的条目导入 Common
        local src = entries[1]
        for i = 2, #entries do
            if utils.isBetterKeep(entries[i], src) then src = entries[i] end
        end
        local ok, err = pcall(function()
            local resFolder = commonPkg:EnsurePathExists(COMMON_RES_PATH, true)
            -- 物理文件名经 GetUniqueName 去重, 资源名取去扩展名部分
            local uniqueFileName = commonPkg:GetUniqueName(resFolder, src.item.fileName)
            local resName = uniqueFileName:match("^(.*)%.[^%.]+$") or uniqueFileName
            local task = commonPkg:ImportResource(src.filePath, COMMON_RES_PATH, resName)
            local newItem = task:GetAwaiter():GetResult()
            newItem.exported = true
            stats.exportedItems = stats.exportedItems + 1
            stats.importedItems = stats.importedItems + 1
            stats.touchedPkgs[commonPkg] = true
            target = { pkg = commonPkg, item = newItem }
        end)
        if not ok or not target then
            stats.skippedGroups = stats.skippedGroups + 1
            fprint("[AtlasOrganizer] [跳过] 导入 Common 失败: " .. tostring(err))
            return
        end
    end

    -- 组内其余条目改写引用到收拢目标并删除
    local details = {}
    for _, e in ipairs(entries) do
        if e.item ~= target.item then
            if replaceAndDelete(e, target.item, stats) then
                details[#details + 1] = string.format("[%s]%s→%s", e.pkg.name, e.item.id, COMMON_PKG_NAME)
            else
                details[#details + 1] = string.format("[%s]%s→失败", e.pkg.name, e.item.id)
            end
        end
    end

    stats.mergedGroups = stats.mergedGroups + 1
    fprint(string.format("[AtlasOrganizer] 跨包组收拢 [%s]%s%s 保留 %s，合并: %s",
        COMMON_PKG_NAME, target.item.path, target.item.fileName, target.item.id, table.concat(details, "、")))
end

--- 一键清理全部包中的 MD5 重复图片
function M.clean()
    fprint("[AtlasOrganizer] ======== 一键清理重复图片 ========")
    fprint("[AtlasOrganizer] 正在扫描 (MD5 检测)...")

    local hashMap = buildMD5HashMap()

    local groups = {}
    for _, entries in pairs(hashMap) do
        if #entries > 1 then
            groups[#groups + 1] = { entries = entries }
        end
    end

    if #groups == 0 then
        fprint("[AtlasOrganizer] 未发现重复图片，无需清理")
        return
    end

    fprint(string.format("[AtlasOrganizer] 发现 %d 组重复图片，开始清理...", #groups))

    -- 查找 Common 包(跨包收拢目标)
    local commonPkg = nil
    for _, pkg in ipairs(utils.getAllPackages()) do
        if pkg.name == COMMON_PKG_NAME then
            commonPkg = pkg
            break
        end
    end

    local stats = {
        mergedGroups = 0,
        deletedItems = 0,
        rewrittenRefs = 0,
        movedItems = 0,
        importedItems = 0,
        exportedItems = 0,
        skippedGroups = 0,
        touchedPkgs = {},
    }

    for _, group in ipairs(groups) do
        -- 预取引用数与导出状态(保留规则依赖)
        for _, e in ipairs(group.entries) do
            e.refCount = utils.getRefCount(e.item)
            e.exported = e.item.exported
        end

        -- 分流: 组内条目是否跨包
        local crossPackage = false
        for i = 2, #group.entries do
            if group.entries[i].pkg ~= group.entries[1].pkg then
                crossPackage = true
                break
            end
        end

        if crossPackage then
            cleanCrossPackageGroup(group, commonPkg, stats)
        else
            cleanSamePackageGroup(group, stats)
        end
    end

    local pkgList = {}
    for pkg in pairs(stats.touchedPkgs) do pkgList[#pkgList + 1] = pkg end
    for _, pkg in ipairs(pkgList) do
        pcall(function() pkg:Save() end)
    end

    fprint(string.format(
        "[AtlasOrganizer] ======== 完成: 清理 %d 组 / 删除 %d 张 / 改写 %d 处引用 / 移动 %d 张 / 导入 %d 张 / 设导出 %d 张 / 跳过 %d 组 / 保存 %d 个包 ========",
        stats.mergedGroups, stats.deletedItems, stats.rewrittenRefs,
        stats.movedItems, stats.importedItems, stats.exportedItems,
        stats.skippedGroups, #pkgList))
end

return M
