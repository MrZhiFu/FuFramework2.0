---@type CS.FairyEditor.App
App = App

---@type GenReady
GenReady = require(PluginPath..'/Src/GenReady')

---@type GenWin
GenWin = require(PluginPath..'/Src/GenWin')

---@type GenComp
GenComp = require(PluginPath..'/Src/GenComp')

---@type GenBinder
GenBinder = require(PluginPath..'/Src/GenBinder')

---@param handler CS.FairyEditor.PublishHandler
function onPublish(handler)

    --- 初始化
    --- 1.加载Tool工具对象，并为Tool指定FGUI发布处理器对象与该插件路径
    --- 2.加载GenCommon生成时的通用功能对象
    GenReady:Init(handler, PluginPath)

    --- 导出路径是否有效
    if not GenReady:IsExportPathOK() then
        return
    end

    ---- 获得Unity工程路径 “xxx/Assets”
    local unityDataPath = GenReady:GetUnityDataPath(handler)

    if not unityDataPath then
        return
    end

    fprintf("开始生成......")
    
    --- 获得所有界面数组，组件数组，所有界面与组件的Map--key-资源名称--value-资源对应的界面或组件
    local winClsArray, compClsArray, AllClsMap = GenReady:GetClsArray(handler)

    --- 生成界面代码
    GenWin:Gen(handler.pkg.name, winClsArray, AllClsMap, unityDataPath)

    --- 生成组件代码
    GenComp:Gen(handler.pkg.name, compClsArray, AllClsMap, unityDataPath)
    
    --- 生成Binder代码
    GenBinder:Gen(handler.pkg.name, compClsArray, unityDataPath)
end

-------do cleanup here-------
function onDestroy()
end