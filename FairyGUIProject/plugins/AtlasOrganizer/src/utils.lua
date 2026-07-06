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