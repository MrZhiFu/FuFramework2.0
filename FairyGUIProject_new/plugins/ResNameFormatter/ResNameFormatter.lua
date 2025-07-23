---@class ResNameFormatter
---资源名称格式化器：用于格式化资源名称，可批量修改资源名称，以便于在编辑器中更统一的管理资源。
---
---使用方法：打开编辑器，在菜单栏中点击“工具”->“自定义-资源名称格式化器”；
---
---注意：目前暂未有具体规范，后续再根据实际情况进行调整。
local ResNameFormatter = {}

---初始化
---@param pluginPath_ string 插件路径
function ResNameFormatter:Init(pluginPath_)
    Tool = require(pluginPath_ .. '/Tool')
end

--- 执行重命名
function ResNameFormatter:Run()
    self:DoRename()
    App.RefreshProject()
end

--- 根据类型与原名，获得新名字
---@param itemType table 类型
---@param oriName string 原名
---@return string 新名
local function getNewName(itemType, oriName)
    local switch = {
        ["image"] = function(name)
            -- todo
            return name
        end,
        ["component"] = function(name)
            -- todo
            return name
        end,
        ["font"] = function(name)
            -- todo
            return name
        end,
    }

    local defaultFunc = function(name)
        return name
    end

    local func = switch[itemType] or defaultFunc
    return func(oriName)
end

--- 执行重命名
function ResNameFormatter:DoRename()
    local project = App.project
    if not project.opened then
        return
    end

    local allPackages = project.allPackages

    ---遍历所有包
    for i = 0, allPackages.Count - 1 do
        ---@type CS.FairyEditor.FPackage
        local pkg = allPackages[i]
        local items = pkg.items

        ---遍历包下的所有Item，进行重命名
        for j = 0, items.Count - 1 do
            ---@type CS.FairyEditor.FPackageItem
            local item = items[j]
            local newName = getNewName(item.type, item.name)
            if newName ~= item.name then
                Tool:Log("资源被重命名：OldName-%s, NewName-%s", item.name, newName)
                pkg:RenameItem(item, newName)
            end
        end

        collectgarbage('collect')
    end
end

return ResNameFormatter