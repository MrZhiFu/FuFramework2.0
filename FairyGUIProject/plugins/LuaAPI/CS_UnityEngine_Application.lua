---@class CS.UnityEngine.Application
---@field public isPlaying boolean
---@field public isFocused boolean
---@field public buildGUID string
---@field public runInBackground boolean
---@field public isBatchMode boolean
---@field public dataPath string
---@field public streamingAssetsPath string
---@field public persistentDataPath string
---@field public temporaryCachePath string
---@field public absoluteURL string
---@field public unityVersion string
---@field public version string
---@field public installerName string
---@field public identifier string
---@field public installMode number
---@field public sandboxType number
---@field public productName string
---@field public companyName string
---@field public cloudProjectId string
---@field public targetFrameRate number
---@field public consoleLogPath string
---@field public backgroundLoadingPriority number
---@field public genuine boolean
---@field public genuineCheckAvailable boolean
---@field public platform number
---@field public isMobilePlatform boolean
---@field public isConsolePlatform boolean
---@field public systemLanguage number
---@field public internetReachability number
---@field public exitCancellationToken CS.System.Threading.CancellationToken
---@field public isEditor boolean

---@type CS.UnityEngine.Application
CS.UnityEngine.Application = { }
---@return CS.UnityEngine.Application
function CS.UnityEngine.Application.New() end
---@overload fun(): void
---@param optional exitCode number
function CS.UnityEngine.Application.Quit(exitCode) end
function CS.UnityEngine.Application.Unload() end
---@overload fun(levelIndex:number): boolean
---@return boolean
---@param levelName string
function CS.UnityEngine.Application.CanStreamedLevelBeLoaded(levelName) end
---@return boolean
---@param obj CS.UnityEngine.Object
function CS.UnityEngine.Application.IsPlaying(obj) end
---@return boolean
function CS.UnityEngine.Application.HasProLicense() end
---@return boolean
---@param delegateMethod (fun(advertisingId:string, trackingEnabled:boolean, errorMsg:string):void)
function CS.UnityEngine.Application.RequestAdvertisingIdentifierAsync(delegateMethod) end
---@param url string
function CS.UnityEngine.Application.OpenURL(url) end
---@return number
---@param logType number
function CS.UnityEngine.Application.GetStackTraceLogType(logType) end
---@param logType number
---@param stackTraceType number
function CS.UnityEngine.Application.SetStackTraceLogType(logType, stackTraceType) end
---@return CS.UnityEngine.AsyncOperation
---@param mode number
function CS.UnityEngine.Application.RequestUserAuthorization(mode) end
---@return boolean
---@param mode number
function CS.UnityEngine.Application.HasUserAuthorization(mode) end
---@param op string
---@param value (fun():void)
function CS.UnityEngine.Application.lowMemory(op, value) end
---@param op string
---@param value (fun(usage:CS.UnityEngine.ApplicationMemoryUsageChange):void)
function CS.UnityEngine.Application.memoryUsageChanged(op, value) end
---@param op string
---@param value (fun(condition:string, stackTrace:string, type:number):void)
function CS.UnityEngine.Application.logMessageReceived(op, value) end
---@param op string
---@param value (fun(condition:string, stackTrace:string, type:number):void)
function CS.UnityEngine.Application.logMessageReceivedThreaded(op, value) end
---@param op string
---@param value (fun():void)
function CS.UnityEngine.Application.onBeforeRender(op, value) end
---@param op string
---@param value (fun(obj:boolean):void)
function CS.UnityEngine.Application.focusChanged(op, value) end
---@param op string
---@param value (fun(obj:string):void)
function CS.UnityEngine.Application.deepLinkActivated(op, value) end
---@param op string
---@param value (fun():boolean)
function CS.UnityEngine.Application.wantsToQuit(op, value) end
---@param op string
---@param value (fun():void)
function CS.UnityEngine.Application.quitting(op, value) end
---@param op string
---@param value (fun():void)
function CS.UnityEngine.Application.unloading(op, value) end
return CS.UnityEngine.Application
