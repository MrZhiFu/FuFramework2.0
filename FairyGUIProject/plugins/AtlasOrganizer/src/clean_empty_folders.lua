-- 功能1: 清理空文件夹
-- 扫描所有包中的空文件夹并删除

---@type CS.FairyEditor.App
local App = App

local M = {}

--- 扫描所有包中的空文件夹并删除
function M.cleanEmptyFolders()
    fprint("[AtlasOrganizer] 开始扫描并清理空文件夹...")

    local emptyFolderList = {}
    local allPackages = App.project.allPackages
    for pi = 0, allPackages.Count - 1 do
        local pkg = allPackages[pi]
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "folder" then
                local isEmpty = (item.children == nil) or (item.children.Count == 0)
                if isEmpty then
                    emptyFolderList[#emptyFolderList + 1] = {
                        pi = item,
                        pkgName = pkg.name,
                        folderPath = item.path .. item.fileName .. "/"
                    }
                end
            end
        end
    end

    if #emptyFolderList == 0 then
        fprint("[AtlasOrganizer] 没有空文件夹")
        return
    end

    fprint(string.format("[AtlasOrganizer] 找到 %d 个空文件夹，开始清理...", #emptyFolderList))

    local pkgFolders = {}
    for _, info in ipairs(emptyFolderList) do
        local pkgName = info.pkgName
        if not pkgFolders[pkgName] then
            pkgFolders[pkgName] = { pkg = info.pi.owner, items = {} }
        end
        pkgFolders[pkgName].items[#pkgFolders[pkgName].items + 1] = info
    end

    local deletedCount = 0
    local failCount = 0
    for pkgName, data in pairs(pkgFolders) do
        local pkg = data.pkg
        for _, info in ipairs(data.items) do
            local ok, err = pcall(function()
                pkg:DeleteItem(info.pi)
            end)
            if ok then
                deletedCount = deletedCount + 1
            else
                failCount = failCount + 1
                fprint(string.format("[AtlasOrganizer] 删除失败: [%s] %s - %s",
                    pkgName, info.folderPath, tostring(err)))
            end
        end
        pkg:Save()
    end

    fprint(string.format("[AtlasOrganizer] 清理完成: 成功 %d 个，失败 %d 个", deletedCount, failCount))
end

return M