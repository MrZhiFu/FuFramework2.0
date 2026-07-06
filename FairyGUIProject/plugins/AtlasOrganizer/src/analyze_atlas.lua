-- 功能5: 图集优化分析
-- 诊断三类问题：
--   混用：图片被其他包的组件引用，修改时会导致不相关图集重编（Common/Icon 包除外，它们本就是公用包）
--   分散：单个组件引用了 3+ 个不同 atlas 编号的图集，造成额外 DrawCall
--   未分配：文件夹未显式指定 atlas 编号，使用默认 auto，不利于控制发布粒度

---@type CS.FairyEditor.App
local App = App
local utils = require("utils")

local M = {}

--- 执行图集优化分析，输出诊断报告到控制台
function M.analyze()
    fprint("[AtlasOrganizer] 开始图集优化分析...")

    local allPackages = utils.getAllPackages()

    -- 收集每张图片的引用关系
    -- imageRefs[pkgName][imageId] = { compsByPkg = {refPkgName={compName=true,...},...} }
    -- compsByPkg 按引用方的包名分组，记录哪些包的哪些组件引用了这张图片
    local imageRefs = {}
    for _, pkg in ipairs(allPackages) do
        imageRefs[pkg.name] = {}
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "image" and not utils.isExcludedPath(item.path) then
                imageRefs[pkg.name][item.id] = { compsByPkg = {} }
            end
        end
    end

    -- 扫描所有组件 XML，建立图片的引用关系
    -- 同包引用：src="imageId" 形式
    -- 跨包引用：ui://pkgId+resId 形式（前8位为包ID，后续为资源ID）
    for _, pkg in ipairs(allPackages) do
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "component" then
                local xmlPath = App.project.basePath .. "/assets/" .. pkg.name .. item.path .. item.fileName
                local xmlContent = utils.readFileText(xmlPath)
                if xmlContent then
                    -- 同包引用：src="imageId"
                    for srcId in string.gmatch(xmlContent, 'src="([^"]+)"') do
                        local ref = imageRefs[pkg.name] and imageRefs[pkg.name][srcId]
                        if ref then
                            if not ref.compsByPkg[pkg.name] then ref.compsByPkg[pkg.name] = {} end
                            ref.compsByPkg[pkg.name][item.name] = true
                        end
                    end

                    -- 跨包引用：ui://pkgId(8位)+resId
                    for fullId in string.gmatch(xmlContent, 'ui://([%w]+)') do
                        if #fullId > 8 then
                            local resPkgId = string.sub(fullId, 1, 8)
                            local resId = string.sub(fullId, 9)
                            for _, p2 in ipairs(allPackages) do
                                if p2.id == resPkgId and imageRefs[p2.name] and imageRefs[p2.name][resId] then
                                    local ref = imageRefs[p2.name][resId]
                                    if not ref.compsByPkg[pkg.name] then ref.compsByPkg[pkg.name] = {} end
                                    ref.compsByPkg[pkg.name][item.name] = true
                                end
                            end
                        end
                    end
                end
            end
        end
    end

    -- 分析三类问题
    local mixedFolders = {}   -- 混用：被其他包引用的图片
    local unassigned = {}     -- 未分配：使用默认 auto 的文件夹
    local scatteredComps = {} -- 分散：引用 3+ 图集的组件

    for _, pkg in ipairs(allPackages) do
        -- 解析 package.xml 中文件夹的 atlas 配置
        local xmlContent = utils.readFileText(utils.getPackageXmlPath(pkg))
        local folderAtlasMap = {} -- folderPath → atlas编号
        if xmlContent then
            for folderPath, atlas in string.gmatch(xmlContent, '<folder[^>]*id="([^"]*)"[^>]*atlas="([^"]*)"') do
                folderAtlasMap[folderPath] = atlas
            end
        end

        -- 按文件夹分组图片
        local folderImages = {}
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "image" and not utils.isExcludedPath(item.path) then
                if not folderImages[item.path] then folderImages[item.path] = {} end
                folderImages[item.path][#folderImages[item.path] + 1] = item
            end
        end

        for path, imgs in pairs(folderImages) do
            local atlas = folderAtlasMap[path]

            -- 未分配检查：文件夹无 atlas 配置且不是 Single 文件夹
            if not atlas and path ~= "/Images/Single/" then
                unassigned[#unassigned + 1] = { pkgName = pkg.name, path = path, count = #imgs }
            end

            -- 混用检查：排除 alone_npot（已单独发布）和 Common/Icon 公用包
            -- 判定逻辑：图片被自身包以外的包引用 → 混用
            if atlas ~= "alone_npot" and pkg.name ~= "Common" and pkg.name ~= "Icon" then
                for _, img in ipairs(imgs) do
                    local ref = imageRefs[pkg.name] and imageRefs[pkg.name][img.id]
                    if ref then
                        local otherPkgs = {}
                        local otherComps = {}
                        for refPkg, comps in pairs(ref.compsByPkg) do
                            if refPkg ~= pkg.name then
                                otherPkgs[refPkg] = true
                                for compName, _ in pairs(comps) do
                                    otherComps[compName] = true
                                end
                            end
                        end
                        if next(otherPkgs) then
                            mixedFolders[#mixedFolders + 1] = {
                                pkgName = pkg.name, path = path,
                                imageName = img.fileName,
                                otherPkgs = otherPkgs, otherComps = otherComps
                            }
                        end
                    end
                end
            end
        end

        -- 分散检查：统计每个组件引用的图片分布在多少个不同 atlas
        -- 引用 3+ 个不同图集的组件会产生额外 DrawCall
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item.type == "component" then
                local xmlPath2 = App.project.basePath .. "/assets/" .. pkg.name .. item.path .. item.fileName
                local xmlContent2 = utils.readFileText(xmlPath2)
                if xmlContent2 then
                    local atlasSet = {}
                    for srcId in string.gmatch(xmlContent2, 'src="([^"]+)"') do
                        for j = 0, items.Count - 1 do
                            local img = items[j]
                            if img.id == srcId and img.type == "image" then
                                local imgAtlas = folderAtlasMap[img.path] or "auto"
                                if imgAtlas ~= "alone_npot" then
                                    atlasSet[imgAtlas] = true
                                end
                                break
                            end
                        end
                    end
                    local atlasCount = 0
                    for _ in pairs(atlasSet) do atlasCount = atlasCount + 1 end
                    if atlasCount >= 3 then
                        local atlasList = {}
                        for a, _ in pairs(atlasSet) do atlasList[#atlasList + 1] = a end
                        scatteredComps[#scatteredComps + 1] = {
                            pkgName = pkg.name, compName = item.name,
                            compPath = item.path, atlasCount = atlasCount,
                            atlases = table.concat(atlasList, ", ")
                        }
                    end
                end
            end
        end
    end

    -- 输出诊断报告
    fprint("========== 图集优化诊断报告 ==========")

    fprint("")
    fprint(string.format("── 混用问题 (%d 张图片被其他包引用) ──", #mixedFolders))
    if #mixedFolders == 0 then
        fprint("  (无)")
    else
        for _, info in ipairs(mixedFolders) do
            local pkgs = {}
            for p, _ in pairs(info.otherPkgs) do pkgs[#pkgs + 1] = p end
            local comps = {}
            for c, _ in pairs(info.otherComps) do comps[#comps + 1] = c end
            local compSample = {}
            for k = 1, math.min(3, #comps) do compSample[#compSample + 1] = comps[k] end
            local more = #comps > 3 and string.format(" ...共%d个", #comps) or ""
            fprint(string.format("  [%s] %s%s → 包[%s] 组件: %s%s",
                info.pkgName, info.path, info.imageName, table.concat(pkgs, ","), table.concat(compSample, ", "), more))
        end
    end

    fprint("")
    fprint(string.format("── 分散问题 (%d 个组件引用了3+个不同图集) ──", #scatteredComps))
    if #scatteredComps == 0 then
        fprint("  (无)")
    else
        table.sort(scatteredComps, function(a, b) return a.atlasCount > b.atlasCount end)
        for k = 1, math.min(30, #scatteredComps) do
            local info = scatteredComps[k]
            fprint(string.format("  [%s] %s%s  引用%d个图集: %s",
                info.pkgName, info.compPath, info.compName, info.atlasCount, info.atlases))
        end
        if #scatteredComps > 30 then
            fprint(string.format("  ... 共 %d 个，仅显示前30", #scatteredComps))
        end
    end

    fprint("")
    fprint(string.format("── 未分配图集编号 (%d 个文件夹使用默认 auto) ──", #unassigned))
    if #unassigned == 0 then
        fprint("  (无)")
    else
        table.sort(unassigned, function(a, b)
            if a.pkgName ~= b.pkgName then return a.pkgName < b.pkgName end
            return a.path < b.path
        end)
        for _, info in ipairs(unassigned) do
            fprint(string.format("  [%s] %s  图片数=%d", info.pkgName, info.path, info.count))
        end
    end

    fprint("")
    fprint("========== 诊断结束 ==========")
end

return M