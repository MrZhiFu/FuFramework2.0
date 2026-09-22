-- AtlasOrganizer 工具函数模块
-- 提供所有功能共用的基础工具方法

---@type CS.FairyEditor.App
local App = App

local M = {}

--- 获取所有包（排除 Launcher）
---@return table FPackage[]
function M.getAllPackages()
    local pkgs = {}
    local allPackages = App.project.allPackages
    for i = 0, allPackages.Count - 1 do
        local pkg = allPackages[i]
        if pkg.name ~= "Launcher" then
            pkgs[#pkgs + 1] = pkg
        end
    end
    return pkgs
end

--- 获取包的 package.xml 文件路径
---@param pkg FPackage
---@return string
function M.getPackageXmlPath(pkg)
    return App.project.basePath .. "/assets/" .. pkg.name .. "/package.xml"
end

--- 读取文件内容，不存在返回 nil
---@param path string
---@return string|nil
function M.readFileText(path)
    if CS.System.IO.File.Exists(path) then
        return CS.System.IO.File.ReadAllText(path)
    end
    return nil
end

--- 获取图片资源的磁盘文件路径
---@param pkg FPackage
---@param item FPackageItem
---@return string
function M.getImageFilePath(pkg, item)
    return App.project.basePath .. "/assets/" .. pkg.name .. item.path .. item.fileName
end

--- 判断路径是否应排除扫描（示例、效果图等非正式资源）
---@param path string
---@return boolean
function M.isExcludedPath(path)
    if string.find(path, "示例") then return true end
    if string.find(path, "效果图") then return true end
    if string.find(path, "示意图") then return true end
    if string.find(path, "设计图") then return true end
    return false
end

--- 查询资源被引用次数
---@param item FPackageItem
---@return number
function M.getRefCount(item)
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
function M.isBetterKeep(candidate, currentKeep)
    if candidate.refCount ~= currentKeep.refCount then
        return candidate.refCount > currentKeep.refCount
    end
    if candidate.exported ~= currentKeep.exported then
        return candidate.exported
    end
    return false
end

--- 删除条目, 包内仍有其他条目引用同一物理文件时保留该文件
--- DeleteItem 会把图片文件一并删掉: 同文件多ID的组删除一条后, 保留条目仍引用
--- 同一文件, 需快照写回; 若无任何条目引用, 文件必须随条目删除,
--- 否则残留文件会在编辑器刷新时被重新导入, 已删条目复活。
---@param pi FPackageItem
---@param filePath string 该条目的物理文件路径
---@return boolean
function M.deleteItemKeepFile(pi, filePath)
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
        -- 仅当包内仍有其他条目引用同一物理文件时才写回快照
        local items = pkg.items
        for i = 0, items.Count - 1 do
            local item = items[i]
            if item ~= pi and item.type == "image" and M.getImageFilePath(pkg, item) == filePath then
                pcall(function() CS.System.IO.File.WriteAllBytes(filePath, bytes) end)
                break
            end
        end
    end
    return true
end

--- 按 sortKey 排序后输出结果列表
---@param title string 标题
---@param rows table[] {sortKey, text}
function M.showResult(title, rows)
    table.sort(rows, function(a, b) return a.sortKey < b.sortKey end)
    fprint("========== " .. title .. " (共 " .. #rows .. " 条) ==========")
    for _, row in ipairs(rows) do
        fprint(row.text)
    end
    fprint("========== 结束 ==========")
end

return M