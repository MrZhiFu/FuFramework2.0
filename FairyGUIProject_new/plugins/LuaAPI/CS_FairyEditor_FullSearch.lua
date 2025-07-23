---@class CS.FairyEditor.FullSearch
---@field public result CS.System.Collections.Generic.List_CS.FairyEditor.FPackageItem

---@type CS.FairyEditor.FullSearch
CS.FairyEditor.FullSearch = { }
---@return CS.FairyEditor.FullSearch
---@param maxResultCount number
function CS.FairyEditor.FullSearch.New(maxResultCount) end
---@param strName string
---@param strTypes string
---@param includeBrances boolean
function CS.FairyEditor.FullSearch:Start(strName, strTypes, includeBrances) end
return CS.FairyEditor.FullSearch
