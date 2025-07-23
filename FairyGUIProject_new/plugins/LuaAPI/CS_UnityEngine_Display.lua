---@class CS.UnityEngine.Display
---@field public displays Display[]
---@field public renderingWidth number
---@field public renderingHeight number
---@field public systemWidth number
---@field public systemHeight number
---@field public colorBuffer CS.UnityEngine.RenderBuffer
---@field public depthBuffer CS.UnityEngine.RenderBuffer
---@field public active boolean
---@field public requiresBlitToBackbuffer boolean
---@field public requiresSrgbBlitToBackbuffer boolean
---@field public main CS.UnityEngine.Display
---@field public activeEditorGameViewTarget number

---@type CS.UnityEngine.Display
CS.UnityEngine.Display = { }
---@overload fun(): void
---@param optional width number
---@param optional height number
---@param optional refreshRate CS.UnityEngine.RefreshRate
function CS.UnityEngine.Display:Activate(width, height, refreshRate) end
---@param width number
---@param height number
---@param x number
---@param y number
function CS.UnityEngine.Display:SetParams(width, height, x, y) end
---@param w number
---@param h number
function CS.UnityEngine.Display:SetRenderingResolution(w, h) end
---@return CS.UnityEngine.Vector3
---@param inputMouseCoordinates CS.UnityEngine.Vector3
function CS.UnityEngine.Display.RelativeMouseAt(inputMouseCoordinates) end
---@param op string
---@param value (fun():void)
function CS.UnityEngine.Display.onDisplaysUpdated(op, value) end
return CS.UnityEngine.Display
