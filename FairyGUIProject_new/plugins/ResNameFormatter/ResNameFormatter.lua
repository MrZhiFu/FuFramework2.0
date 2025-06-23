local ResNameFormatter = {}

--- CS.FairyEditor_FPackageItemType
--- "image", "movieclip", "swf", "sound", "component", "font", "misc", "atlas", "spine", "dragonbones"

local dicHasType = {}

fprintf = function(fmt, ...)
    local msg = string.format(fmt, ...)
    fprint(msg)
    
    return msg
end

function ResNameFormatter:Run()

    self:DoChange()

    App.RefreshProject()
end

--- 获得新名字
---@param itemType 类型
---@param oriName 原名
---@return 新名
local function getNameName(itemType, oriName)
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

function ResNameFormatter:DoChange()
    local project = App.project

    -- logF('project[%s] opened: %s', project.name, project.opened)
    if not project.opened then
        return
    end
    
    local allPackages = project.allPackages
    local numPkg = allPackages.Count
    for i = 0, numPkg - 1 do
        ---@type CS.FairyEditor.FPackage
        local pkg = allPackages[i]

        -- logF('pkg: %s', pkg.name)
        local items = pkg.items
        local numItem = items.Count
        for j = 0, numItem - 1 do
            ---@type CS.FairyEditor.FPackageItem
            local item = items[j]

            if dicHasType[item.type] == nil then
                dicHasType[item.type] = 1
            end

            local newName = getNameName(item.type, item.name)
            if newName ~= item.name then
                pkg:RenameItem(item, newName)
            end
        end

        collectgarbage('collect')
    end

    fprintf("Has type:")
    for i, v in pairs(dicHasType) do
        fprintf(string.format("Type:%s", i))
    end
end

return ResNameFormatter