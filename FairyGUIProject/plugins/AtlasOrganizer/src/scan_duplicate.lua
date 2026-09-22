-- 功能2: 扫描重复图片
-- 基于 MD5 检测跨包重复图片，输出重复分布报告（人工审计用）
-- 清理工作流已由功能7「一键清理重复图片」承担（同包合并、跨包收拢 Common）

local utils = require("utils")

local M = {}

--- 扫描所有包，基于文件 MD5 检测重复图片，输出分布报告
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
                        hashMap[hash][#hashMap[hash] + 1] = { pkg = pkg, item = item, path = filePath }
                    end
                end
            end
        end
    end

    md5Provider:Dispose()

    local rows = {}
    for _, entries in pairs(hashMap) do
        if #entries > 1 then
            local locations = {}
            for _, entry in ipairs(entries) do
                locations[#locations + 1] = string.format("%s:%s%s", entry.pkg.name, entry.item.path, entry.item.fileName)
            end
            rows[#rows + 1] = {
                sortKey = entries[1].pkg.name,
                text = string.format("[重复x%d] → %s", #entries, table.concat(locations, " || "))
            }
        end
    end

    utils.showResult("扫描重复图片", rows)
    if #rows == 0 then
        fprint("[AtlasOrganizer] 未发现重复图片")
    else
        fprint(string.format("[AtlasOrganizer] 共 %d 组重复图片，可使用「一键清理重复图片」自动归一", #rows))
    end
end

return M
