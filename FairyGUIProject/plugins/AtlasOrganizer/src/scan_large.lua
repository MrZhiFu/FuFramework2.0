-- 功能4: 扫描大图
-- 检测宽或高 >= 512px 且未放入 /Res/Single/ 的图片，输出分布报告
-- 「一键整理大图」批量移动到 /Res/Single/ 并设置 alone_npot

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

-- 大图阈值（像素）
M.LARGE_THRESHOLD = 512

-- 大图整理目标路径
local SINGLE_PATH = "/Res/Single/"

--- 收集所有未放入 Single 的大图
--- 同时解析各包 package.xml 的文件夹 atlas 配置，排除已设 alone_npot 的文件夹
---@return table 大图列表 {pi, pkg, pkgName, path, fileName, w, h}
local function collectLargeImages()
    -- 解析所有包的 package.xml，获取文件夹 atlas 配置
    local folderAtlasMap = {}  -- [pkgName][folderPath] → atlas编号
    for _, pkg in ipairs(utils.getAllPackages()) do
        folderAtlasMap[pkg.name] = {}
        local xmlContent = utils.readFileText(utils.getPackageXmlPath(pkg))
        if xmlContent then
            for folderPath, atlas in string.gmatch(xmlContent, '<folder[^>]*id="([^"]*)"[^>]*atlas="([^"]*)"') do
                folderAtlasMap[pkg.name][folderPath] = atlas
            end
        end
    end

    local list = {}
    for _, pkg in ipairs(utils.getAllPackages()) do
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "image" and not utils.isExcludedPath(item.path) then
                local w = item.width or 0
                local h = item.height or 0
                -- 检查文件夹是否已设置为 alone_npot，若已设置则忽略
                local folderAtlas = folderAtlasMap[pkg.name] and folderAtlasMap[pkg.name][item.path]
                if (w >= M.LARGE_THRESHOLD or h >= M.LARGE_THRESHOLD)
                    and item.path ~= SINGLE_PATH
                    and folderAtlas ~= "alone_npot" then
                    list[#list + 1] = {
                        pi = item, pkg = pkg, pkgName = pkg.name,
                        path = item.path, fileName = item.fileName, w = w, h = h
                    }
                end
            end
        end
    end
    return list
end

--- 扫描所有包中未放入 Single 的大图，输出分布报告
function M.scan()
    fprint(string.format("[AtlasOrganizer] 开始扫描大图 (宽或高 >= %dpx)...", M.LARGE_THRESHOLD))

    local list = collectLargeImages()

    local rows = {}
    for _, info in ipairs(list) do
        rows[#rows + 1] = {
            sortKey = info.pkgName,
            text = string.format("[%s] %s%s  %dx%d", info.pkgName, info.path, info.fileName, info.w, info.h)
        }
    end

    utils.showResult("扫描大图-需整理", rows)
    if #list == 0 then
        fprint("[AtlasOrganizer] 所有大图均已在 " .. SINGLE_PATH .. "，无需整理")
    else
        fprint(string.format("[AtlasOrganizer] 共 %d 张大图需要整理，可使用「一键整理大图」处理", #list))
    end
end

--- 一键将所有未放入 /Res/Single/ 的大图批量移动到 /Res/Single/ 并设为 alone_npot
function M.organize()
    local list = collectLargeImages()
    if #list == 0 then
        fprint("[AtlasOrganizer] 所有大图均已在 " .. SINGLE_PATH .. "，无需整理")
        return
    end

    fprint(string.format("[AtlasOrganizer] 开始整理大图 → 移动到 %s 并设为 alone_npot (共 %d 张)...", SINGLE_PATH, #list))
    local movedCount = 0
    local failCount = 0

    -- 按包分组
    local pkgItems = {}
    for _, info in ipairs(list) do
        if not pkgItems[info.pkgName] then
            pkgItems[info.pkgName] = { pkg = info.pkg, items = {}, srcPaths = {} }
        end
        pkgItems[info.pkgName].items[#pkgItems[info.pkgName].items + 1] = info.pi
        pkgItems[info.pkgName].srcPaths[info.path] = true
    end

    for pkgName, data in pairs(pkgItems) do
        local pkg = data.pkg

        -- 创建目标文件夹并移动大图到其中
        local singleFolder = pkg:EnsurePathExists(SINGLE_PATH, true)
        if singleFolder then
            singleFolder.folderAtlas = "alone_npot"
        end

        pkg:FreeUnusedResources(true)
        pkg:BeginBatch()
        for _, item in ipairs(data.items) do
            local ok, err = pcall(function()
                pkg:MoveItem(item, SINGLE_PATH)
            end)
            if ok then
                movedCount = movedCount + 1
            else
                failCount = failCount + 1
                fprint(string.format("[AtlasOrganizer] 移动失败: [%s] %s%s - %s",
                    pkgName, item.path, item.fileName, tostring(err)))
            end
        end
        pkg:EndBatch()

        -- 移动后清理空源文件夹
        for srcPath, _ in pairs(data.srcPaths) do
            if srcPath ~= "/" and srcPath ~= SINGLE_PATH then
                local folderItem = pkg:EnsurePathExists(srcPath, false)
                if folderItem then
                    local isEmpty = (folderItem.children == nil) or (folderItem.children.Count == 0)
                    if isEmpty then
                        pcall(function() pkg:DeleteItem(folderItem) end)
                    end
                end
            end
        end

        pkg:Save()
    end

    fprint(string.format("[AtlasOrganizer] 整理完成: 成功 %d 张，失败 %d 张", movedCount, failCount))

    -- 清理无大图的冗余 Single 文件夹
    for _, pkg in ipairs(utils.getAllPackages()) do
        local singleFolder = pkg:GetItemByPath(SINGLE_PATH)
        if singleFolder then
            local hasLarge = false
            local items = pkg.items
            for i = 0, items.Count - 1 do
                local item = items[i]
                if item.type == "image" and item.path == SINGLE_PATH then
                    hasLarge = true
                    break
                end
            end
            if not hasLarge then
                pcall(function()
                    pkg:DeleteItem(singleFolder)
                    pkg:Save()
                    fprint(string.format("[AtlasOrganizer] 已删除冗余 Single 文件夹: [%s]", pkg.name))
                end)
            end
        end
    end
end

return M
