---@class CS.UnityEngine.AsyncOperation : CS.UnityEngine.YieldInstruction
---@field public isDone boolean
---@field public progress number
---@field public priority number
---@field public allowSceneActivation boolean

---@type CS.UnityEngine.AsyncOperation
CS.UnityEngine.AsyncOperation = { }
---@return CS.UnityEngine.AsyncOperation
function CS.UnityEngine.AsyncOperation.New() end
---@param op string
---@param value (fun(obj:CS.UnityEngine.AsyncOperation):void)
function CS.UnityEngine.AsyncOperation:completed(op, value) end
return CS.UnityEngine.AsyncOperation
