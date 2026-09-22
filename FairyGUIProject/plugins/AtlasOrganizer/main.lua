-- AtlasOrganizer: 图集整理工具
-- 功能：清理空文件夹、扫描重复图片、一键合并同文件重复ID、一键清理重复图片、扫描清理未引用资源、扫描大图、图集优化分析
-- 版本：1.6
-- 菜单路径：工具 → 图集整理
--
-- 文件结构：
--   main.lua           ← 入口文件（菜单注册、模块加载、生命周期）
--   src/utils.lua      ← 公共工具函数
--   src/clean_empty_folders.lua  ← 功能1: 清理空文件夹
--   src/scan_duplicate.lua       ← 功能2: 扫描重复图片
--   src/scan_merge.lua           ← 功能6: 一键合并同文件重复ID
--   src/clean_duplicate.lua      ← 功能7: 一键清理重复图片
--   src/scan_unused.lua          ← 功能3: 扫描/一键清理未引用资源
--   src/scan_large.lua           ← 功能4: 扫描/一键整理大图
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
local scanMerge         = dofile(srcDir .. "scan_merge.lua")
local cleanDuplicate    = dofile(srcDir .. "clean_duplicate.lua")
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

atlasMenu:AddItem("一键合并同文件重复ID", "atlas_merge_same_file", -1, false, function()
    local ok, err = pcall(scanMerge.merge)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("一键清理重复图片", "atlas_clean_duplicate", -1, false, function()
    local ok, err = pcall(cleanDuplicate.clean)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddSeperator()

-- 功能3: 扫描未引用资源
atlasMenu:AddItem("扫描未引用资源", "atlas_scan_unused", -1, false, function()
    local ok, err = pcall(scanUnused.scan)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("一键清理未引用资源", "atlas_clean_unused", -1, false, function()
    local ok, err = pcall(scanUnused.clean)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddSeperator()

-- 功能4: 扫描大图
atlasMenu:AddItem("扫描大图 (>=512px)", "atlas_scan_large", -1, false, function()
    local ok, err = pcall(scanLarge.scan)
    if not ok then fprint("[AtlasOrganizer] 错误: " .. tostring(err)) end
end)

atlasMenu:AddItem("一键整理大图", "atlas_organize_large", -1, false, function()
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
end