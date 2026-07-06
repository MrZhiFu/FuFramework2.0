-- 功能4: 扫描大图
-- 检测宽或高 >= 512px 且未放入 /Images/Single/ 的图片
-- 支持一键移动到 Single 文件夹并设置 alone_npot 单独发布

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

-- 大图列表
M.largeImageList = {}
-- 当前定位索引
M.largeImageIndex = 0
-- 大图阈值（像素）
M.LARGE_THRESHOLD = 512

--- 定位下一个大图
function M.locateNext()
    if #M.largeImageList == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描大图")
        return
    end
    M.largeImageIndex = M.largeImageIndex + 1
    if M.largeImageIndex > #M.largeImageList then M.largeImageIndex = 1 end
    local info = M.largeImageList[M.largeImageIndex]
    App.libView:Highlight(info.pi, true)
    fprint(string.format("[AtlasOrganizer] 大图 %d/%d: [%s] %s%s  %dx%d",
        M.largeImageIndex, #M.largeImageList, info.pkgName, info.path, info.fileName, info.w, info.h))
end

--- 定位上一个大图
function M.locatePrev()
    if #M.largeImageList == 0 then
        fprint("[AtlasOrganizer] 请先执行扫描大图")
        return
    end
    M.largeImageIndex = M.largeImageIndex - 1
    if M.largeImageIndex < 1 then M.largeImageIndex = #M.largeImageList end
    local info = M.largeImageList[M.largeImageIndex]
    App.libView:Highlight(info.pi, true)
    fprint(string.format("[AtlasOrganizer] 大图 %d/%d: [%s] %s%s  %dx%d",
        M.largeImageIndex, #M.largeImageList, info.pkgName, info.path, info.fileName, info.w, info.h))
end

--- 扫描所有包中未放入 Single 的大图
function M.scan()
    fprint("[AtlasOrganizer] 开始扫描大图 (宽或高 >= 512px)...")
    local rows = {}
    M.largeImageList = {}
    M.largeImageIndex = 0

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
                    and item.path ~= "/Images/Single/"
                    and folderAtlas ~= "alone_npot" then
                    rows[#rows + 1] = {
                        sortKey = pkg.name,
                        text = string.format("[%s] %s  %dx%d  path=%s", pkg.name, item.fileName, w, h, item.path)
                    }
                    M.largeImageList[#M.largeImageList + 1] = {
                        pi = item, pkgName = pkg.name,
                        path = item.path, fileName = item.fileName, w = w, h = h
                    }
                end
            end
        end
    end

    utils.showResult("扫描大图-需整理", rows)
    if #rows > 0 then
        fprint(string.format("[AtlasOrganizer] 共 %d 张大图需要整理，使用「定位下一个/上一个大图」浏览，「整理大图 → Single」执行", #rows))
    else
        fprint("[AtlasOrganizer] 所有大图均已在 /Images/Single/，无需整理")
    end
end

--- 将扫描到的大图批量移动到 /Images/Single/ 并设为 alone_npot
--- Icon 包特殊处理：不移动，仅将源文件夹设为 alone_npot
function M.organize()
    if #M.largeImageList == 0 then
        fprint("[AtlasOrganizer] 请先执行「扫描大图」")
        return
    end

    fprint("[AtlasOrganizer] 开始整理大图 → 移动到 /Images/Single/ 并设为 alone_npot...")
    local movedCount = 0
    local failCount = 0

    local pkgItems = {}
    for _, info in ipairs(M.largeImageList) do
        local pkgName = info.pkgName
        if not pkgItems[pkgName] then
            pkgItems[pkgName] = { pkg = info.pi.owner, items = {}, srcPaths = {} }
        end
        pkgItems[pkgName].items[#pkgItems[pkgName].items + 1] = info.pi
        pkgItems[pkgName].srcPaths[info.path] = true
    end

    for pkgName, data in pairs(pkgItems) do
        local pkg = data.pkg

        -- Icon 包特殊：图标按功能分文件夹，不移动到 Single，仅设置 alone_npot
        if pkgName == "Icon" then
            for srcPath, _ in pairs(data.srcPaths) do
                local folderItem = pkg:EnsurePathExists(srcPath, false)
                if folderItem then
                    folderItem.folderAtlas = "alone_npot"
                    fprint(string.format("[AtlasOrganizer] [Icon] 已设置文件夹 %s 为 alone_npot", srcPath))
                end
            end
            movedCount = movedCount + #data.items
            pkg:Save()
        else
            -- 其他包：创建 /Images/Single/ 文件夹并移动大图到其中
            local singleFolder = pkg:EnsurePathExists("/Images/Single/", true)
            if singleFolder then
                singleFolder.folderAtlas = "alone_npot"
            end

            pkg:FreeUnusedResources(true)
            pkg:BeginBatch()
            for _, item in ipairs(data.items) do
                local ok, err = pcall(function()
                    pkg:MoveItem(item, "/Images/Single/")
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
                if srcPath ~= "/" and srcPath ~= "/Images/Single/" then
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
    end

    fprint(string.format("[AtlasOrganizer] 整理完成: 成功 %d 张，失败 %d 张", movedCount, failCount))

    -- 清理无大图的冗余 Single 文件夹
    for _, pkg in ipairs(utils.getAllPackages()) do
        local singleFolder = pkg:GetItemByPath("/Images/Single/")
        if singleFolder then
            local hasLarge = false
            local items = pkg.items
            for i = 0, items.Count - 1 do
                local item = items[i]
                if item.type == "image" and item.path == "/Images/Single/" then
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

    M.largeImageList = {}
    M.largeImageIndex = 0
end

--- 重置状态
function M.reset()
    M.largeImageList = {}
    M.largeImageIndex = 0
end

return M