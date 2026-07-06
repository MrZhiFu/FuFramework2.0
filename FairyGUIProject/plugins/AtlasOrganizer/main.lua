-- AtlasOrganizer: 图集整理工具
-- 功能：清理空文件夹、扫描重复图片、扫描未引用资源、扫描大图、图集优化分析
-- 版本：1.0
-- 菜单路径：工具 → 图集整理
--
-- 文件结构：
--   main.lua           ← 入口文件（菜单注册、模块加载、生命周期）
--   src/utils.lua      ← 公共工具函数
--   src/clean_empty_folders.lua  ← 功能1: 清理空文件夹
--   src/scan_duplicate.lua       ← 功能2: 扫描重复图片
--   src/scan_unused.lua          ← 功能3: 扫描未引用资源
--   src/scan_large.lua           ← 功能4: 扫描大图
--   src/analyze_atlas.lua        ← 功能5: 图集优化分析

---@type CS.FairyEditor.App
local App = App

-- ========== 模块加载 ==========
-- FairyGUI 插件环境使用 dofile 加载子模块（无标准 require 搜索路径）

local srcDir = PluginPath .. "/src/"

-- 注入 require 搜索路径，使子模块间可以互相 require
package.path = srcDir .. "?.lua;" .. package.path

local cleanEmptyFolders = dofile(srcDir .. "clean_empty_folders.lua")
local scanDuplicate     = dofile(srcDir .. "scan_duplicate.lua")
local scanUnused        = dofile(srcDir .. "scan_unused.lua")
local scanLarge         = dofile(srcDir .. "scan_large.lua")
local analyzeAtlas      = dofile(srcDir .. "analyze_atlas.lua")

-- ========== 菜单注册 ==========
-- 菜单路径：工具 → 图集整理 → 各功能项

local toolMenu = App.menu:GetSubMenu("tool")

-- 先移除旧菜单（插件热重载时避免重复注册）
pcall(function() toolMenu:RemoveItem("atlas_organizer") end)

toolMenu:AddItem("图集整理", "atlas_organizer", -1, true, nil)
local atlasMenu = toolMenu:GetSubMenu("atlas_organizer")

-- 功能1: 清理空文件夹
atlasMenu:AddItem("清理空文件夹", "atlas_clean_empty_folders", -1, false, function()
    local ok, err = pcall(cleanEmptyFolders.cleanEmptyFolders)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddSeperator()

-- 功能2: 扫描重复图片
atlasMenu:AddItem("扫描重复图片", "atlas_scan_duplicate", -1, false, function()
    local ok, err = pcall(scanDuplicate.scan)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("定位下一个重复图片", "atlas_locate_next", -1, false, function()
    local ok, err = pcall(scanDuplicate.locateNext)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("定位上一个重复图片", "atlas_locate_prev", -1, false, function()
    local ok, err = pcall(scanDuplicate.locatePrev)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("删除当前并替换引用", "atlas_delete_replace", -1, false, function()
    local ok, err = pcall(scanDuplicate.deleteCurrentAndReplace)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddSeperator()

-- 功能3: 扫描未引用资源
atlasMenu:AddItem("扫描未引用资源", "atlas_scan_unused", -1, false, function()
    local ok, err = pcall(scanUnused.scan)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("定位下一个未引用", "atlas_unused_next", -1, false, function()
    local ok, err = pcall(scanUnused.locateNext)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("定位上一个未引用", "atlas_unused_prev", -1, false, function()
    local ok, err = pcall(scanUnused.locatePrev)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("删除当前未引用", "atlas_unused_delete", -1, false, function()
    local ok, err = pcall(scanUnused.deleteCurrent)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddSeperator()

-- 功能4: 扫描大图
atlasMenu:AddItem("扫描大图 (>=512px)", "atlas_scan_large", -1, false, function()
    local ok, err = pcall(scanLarge.scan)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("定位下一个大图", "atlas_large_next", -1, false, function()
    local ok, err = pcall(scanLarge.locateNext)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("定位上一个大图", "atlas_large_prev", -1, false, function()
    local ok, err = pcall(scanLarge.locatePrev)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("整理大图 → Single", "atlas_organize_large", -1, false, function()
    local ok, err = pcall(scanLarge.organize)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddSeperator()

-- 功能5: 图集优化分析
atlasMenu:AddItem("图集优化分析", "atlas_analyze", -1, false, function()
    local ok, err = pcall(analyzeAtlas.analyze)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

fprint("[AtlasOrganizer] 图集整理工具已加载")

-- ========== 清理 ==========

--- 插件卸载时清理菜单和状态
function onDestroy()
    pcall(function() toolMenu:RemoveItem("atlas_organizer") end)
    toolMenu = nil
    scanDuplicate.reset()
    scanUnused.reset()
    scanLarge.reset()
end