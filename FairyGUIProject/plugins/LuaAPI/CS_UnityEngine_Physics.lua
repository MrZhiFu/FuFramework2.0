---@class CS.UnityEngine.Physics
---@field public IgnoreRaycastLayer number
---@field public DefaultRaycastLayers number
---@field public AllLayers number
---@field public gravity CS.UnityEngine.Vector3
---@field public defaultContactOffset number
---@field public sleepThreshold number
---@field public queriesHitTriggers boolean
---@field public queriesHitBackfaces boolean
---@field public bounceThreshold number
---@field public defaultMaxDepenetrationVelocity number
---@field public defaultSolverIterations number
---@field public defaultSolverVelocityIterations number
---@field public simulationMode number
---@field public defaultMaxAngularSpeed number
---@field public improvedPatchFriction boolean
---@field public invokeCollisionCallbacks boolean
---@field public defaultPhysicsScene CS.UnityEngine.PhysicsScene
---@field public autoSyncTransforms boolean
---@field public reuseCollisionCallbacks boolean
---@field public interCollisionDistance number
---@field public interCollisionStiffness number
---@field public interCollisionSettingsToggle boolean
---@field public clothGravity CS.UnityEngine.Vector3

---@type CS.UnityEngine.Physics
CS.UnityEngine.Physics = { }
---@return CS.UnityEngine.Physics
function CS.UnityEngine.Physics.New() end
---@overload fun(collider1:CS.UnityEngine.Collider, collider2:CS.UnityEngine.Collider): void
---@param collider1 CS.UnityEngine.Collider
---@param collider2 CS.UnityEngine.Collider
---@param optional ignore boolean
function CS.UnityEngine.Physics.IgnoreCollision(collider1, collider2, ignore) end
---@param op string
---@param value (fun(arg1:CS.UnityEngine.PhysicsScene, arg2:CS.Unity.Collections.NativeArray_CS.UnityEngine.ModifiableContactPair):void)
function CS.UnityEngine.Physics.ContactModifyEvent(op, value) end
---@param op string
---@param value (fun(arg1:CS.UnityEngine.PhysicsScene, arg2:CS.Unity.Collections.NativeArray_CS.UnityEngine.ModifiableContactPair):void)
function CS.UnityEngine.Physics.ContactModifyEventCCD(op, value) end
---@overload fun(layer1:number, layer2:number): void
---@param layer1 number
---@param layer2 number
---@param optional ignore boolean
function CS.UnityEngine.Physics.IgnoreLayerCollision(layer1, layer2, ignore) end
---@return boolean
---@param layer1 number
---@param layer2 number
function CS.UnityEngine.Physics.GetIgnoreLayerCollision(layer1, layer2) end
---@return boolean
---@param collider1 CS.UnityEngine.Collider
---@param collider2 CS.UnityEngine.Collider
function CS.UnityEngine.Physics.GetIgnoreCollision(collider1, collider2) end
---@overload fun(ray:CS.UnityEngine.Ray): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3): boolean
---@overload fun(ray:CS.UnityEngine.Ray, maxDistance:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, maxDistance:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(ray:CS.UnityEngine.Ray, maxDistance:number, layerMask:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@return boolean
---@param origin CS.UnityEngine.Vector3
---@param optional direction CS.UnityEngine.Vector3
---@param optional hitInfo CS.UnityEngine.RaycastHit
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.Raycast(origin, direction, hitInfo, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3): boolean
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3, layerMask:number): boolean
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3, layerMask:number, queryTriggerInteraction:number): boolean
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, layerMask:number): boolean
---@return boolean
---@param start CS.UnityEngine.Vector3
---@param ed CS.UnityEngine.Vector3
---@param optional hitInfo CS.UnityEngine.RaycastHit
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.Linecast(start, ed, hitInfo, layerMask, queryTriggerInteraction) end
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3): boolean
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number): boolean
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number): boolean
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number): boolean
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number): boolean
---@return boolean
---@param point1 CS.UnityEngine.Vector3
---@param point2 CS.UnityEngine.Vector3
---@param radius number
---@param direction CS.UnityEngine.Vector3
---@param optional hitInfo CS.UnityEngine.RaycastHit
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.CapsuleCast(point1, point2, radius, direction, hitInfo, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(ray:CS.UnityEngine.Ray, radius:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, maxDistance:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, maxDistance:number, layerMask:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number): boolean
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number): boolean
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, hitInfo:CS.UnityEngine.RaycastHit, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@return boolean
---@param origin CS.UnityEngine.Vector3
---@param radius number
---@param optional direction CS.UnityEngine.Vector3
---@param optional hitInfo CS.UnityEngine.RaycastHit
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.SphereCast(origin, radius, direction, hitInfo, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, maxDistance:number): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, orientation:CS.UnityEngine.Quaternion): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, maxDistance:number, layerMask:number): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, orientation:CS.UnityEngine.Quaternion, maxDistance:number): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, maxDistance:number, layerMask:number, queryTriggerInteraction:number): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, hitInfo:CS.UnityEngine.RaycastHit, orientation:CS.UnityEngine.Quaternion, maxDistance:number, layerMask:number): boolean
---@return boolean
---@param center CS.UnityEngine.Vector3
---@param halfExtents CS.UnityEngine.Vector3
---@param direction CS.UnityEngine.Vector3
---@param optional hitInfo CS.UnityEngine.RaycastHit
---@param optional orientation CS.UnityEngine.Quaternion
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.BoxCast(center, halfExtents, direction, hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(ray:CS.UnityEngine.Ray): RaycastHit[]
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3): RaycastHit[]
---@overload fun(ray:CS.UnityEngine.Ray, maxDistance:number): RaycastHit[]
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, maxDistance:number): RaycastHit[]
---@overload fun(ray:CS.UnityEngine.Ray, maxDistance:number, layerMask:number): RaycastHit[]
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number): RaycastHit[]
---@overload fun(ray:CS.UnityEngine.Ray, maxDistance:number, layerMask:number, queryTriggerInteraction:number): RaycastHit[]
---@return RaycastHit[]
---@param origin CS.UnityEngine.Vector3
---@param optional direction CS.UnityEngine.Vector3
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(ray:CS.UnityEngine.Ray, results:RaycastHit[]): number
---@overload fun(ray:CS.UnityEngine.Ray, results:RaycastHit[], maxDistance:number): number
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[]): number
---@overload fun(ray:CS.UnityEngine.Ray, results:RaycastHit[], maxDistance:number, layerMask:number): number
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[], maxDistance:number): number
---@overload fun(ray:CS.UnityEngine.Ray, results:RaycastHit[], maxDistance:number, layerMask:number, queryTriggerInteraction:number): number
---@overload fun(origin:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[], maxDistance:number, layerMask:number): number
---@return number
---@param origin CS.UnityEngine.Vector3
---@param direction CS.UnityEngine.Vector3
---@param optional results RaycastHit[]
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3): RaycastHit[]
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number): RaycastHit[]
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number): RaycastHit[]
---@return RaycastHit[]
---@param point1 CS.UnityEngine.Vector3
---@param point2 CS.UnityEngine.Vector3
---@param radius number
---@param direction CS.UnityEngine.Vector3
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(ray:CS.UnityEngine.Ray, radius:number): RaycastHit[]
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3): RaycastHit[]
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, maxDistance:number): RaycastHit[]
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number): RaycastHit[]
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, maxDistance:number, layerMask:number): RaycastHit[]
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, maxDistance:number, layerMask:number): RaycastHit[]
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, maxDistance:number, layerMask:number, queryTriggerInteraction:number): RaycastHit[]
---@return RaycastHit[]
---@param origin CS.UnityEngine.Vector3
---@param radius number
---@param optional direction CS.UnityEngine.Vector3
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.SphereCastAll(origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(point0:CS.UnityEngine.Vector3, point1:CS.UnityEngine.Vector3, radius:number): Collider[]
---@overload fun(point0:CS.UnityEngine.Vector3, point1:CS.UnityEngine.Vector3, radius:number, layerMask:number): Collider[]
---@return Collider[]
---@param point0 CS.UnityEngine.Vector3
---@param point1 CS.UnityEngine.Vector3
---@param radius number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.OverlapCapsule(point0, point1, radius, layerMask, queryTriggerInteraction) end
---@overload fun(position:CS.UnityEngine.Vector3, radius:number): Collider[]
---@overload fun(position:CS.UnityEngine.Vector3, radius:number, layerMask:number): Collider[]
---@return Collider[]
---@param position CS.UnityEngine.Vector3
---@param radius number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction) end
---@param step number
function CS.UnityEngine.Physics.Simulate(step) end
function CS.UnityEngine.Physics.SyncTransforms() end
---@return boolean
---@param colliderA CS.UnityEngine.Collider
---@param positionA CS.UnityEngine.Vector3
---@param rotationA CS.UnityEngine.Quaternion
---@param colliderB CS.UnityEngine.Collider
---@param positionB CS.UnityEngine.Vector3
---@param rotationB CS.UnityEngine.Quaternion
---@param direction CS.UnityEngine.Vector3
---@param distance CS.System.Single
function CS.UnityEngine.Physics.ComputePenetration(colliderA, positionA, rotationA, colliderB, positionB, rotationB, direction, distance) end
---@return CS.UnityEngine.Vector3
---@param point CS.UnityEngine.Vector3
---@param collider CS.UnityEngine.Collider
---@param position CS.UnityEngine.Vector3
---@param rotation CS.UnityEngine.Quaternion
function CS.UnityEngine.Physics.ClosestPoint(point, collider, position, rotation) end
---@overload fun(position:CS.UnityEngine.Vector3, radius:number, results:Collider[]): number
---@overload fun(position:CS.UnityEngine.Vector3, radius:number, results:Collider[], layerMask:number): number
---@return number
---@param position CS.UnityEngine.Vector3
---@param radius number
---@param results Collider[]
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.OverlapSphereNonAlloc(position, radius, results, layerMask, queryTriggerInteraction) end
---@overload fun(position:CS.UnityEngine.Vector3, radius:number): boolean
---@overload fun(position:CS.UnityEngine.Vector3, radius:number, layerMask:number): boolean
---@return boolean
---@param position CS.UnityEngine.Vector3
---@param radius number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.CheckSphere(position, radius, layerMask, queryTriggerInteraction) end
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, results:RaycastHit[]): number
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, results:RaycastHit[], maxDistance:number): number
---@overload fun(point1:CS.UnityEngine.Vector3, point2:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, results:RaycastHit[], maxDistance:number, layerMask:number): number
---@return number
---@param point1 CS.UnityEngine.Vector3
---@param point2 CS.UnityEngine.Vector3
---@param radius number
---@param direction CS.UnityEngine.Vector3
---@param results RaycastHit[]
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, results:RaycastHit[]): number
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, results:RaycastHit[]): number
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, results:RaycastHit[], maxDistance:number): number
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, results:RaycastHit[], maxDistance:number): number
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, results:RaycastHit[], maxDistance:number, layerMask:number): number
---@overload fun(origin:CS.UnityEngine.Vector3, radius:number, direction:CS.UnityEngine.Vector3, results:RaycastHit[], maxDistance:number, layerMask:number): number
---@overload fun(ray:CS.UnityEngine.Ray, radius:number, results:RaycastHit[], maxDistance:number, layerMask:number, queryTriggerInteraction:number): number
---@return number
---@param origin CS.UnityEngine.Vector3
---@param radius number
---@param direction CS.UnityEngine.Vector3
---@param optional results RaycastHit[]
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3, radius:number): boolean
---@overload fun(start:CS.UnityEngine.Vector3, ed:CS.UnityEngine.Vector3, radius:number, layerMask:number): boolean
---@return boolean
---@param start CS.UnityEngine.Vector3
---@param ed CS.UnityEngine.Vector3
---@param radius number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.CheckCapsule(start, ed, radius, layerMask, queryTriggerInteraction) end
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion): boolean
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, layerMask:number): boolean
---@return boolean
---@param center CS.UnityEngine.Vector3
---@param halfExtents CS.UnityEngine.Vector3
---@param optional orientation CS.UnityEngine.Quaternion
---@param optional layermask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.CheckBox(center, halfExtents, orientation, layermask, queryTriggerInteraction) end
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3): Collider[]
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion): Collider[]
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, layerMask:number): Collider[]
---@return Collider[]
---@param center CS.UnityEngine.Vector3
---@param halfExtents CS.UnityEngine.Vector3
---@param optional orientation CS.UnityEngine.Quaternion
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction) end
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, results:Collider[]): number
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, results:Collider[], orientation:CS.UnityEngine.Quaternion): number
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, results:Collider[], orientation:CS.UnityEngine.Quaternion, mask:number): number
---@return number
---@param center CS.UnityEngine.Vector3
---@param halfExtents CS.UnityEngine.Vector3
---@param results Collider[]
---@param optional orientation CS.UnityEngine.Quaternion
---@param optional mask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, mask, queryTriggerInteraction) end
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[]): number
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[], orientation:CS.UnityEngine.Quaternion): number
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[], orientation:CS.UnityEngine.Quaternion, maxDistance:number): number
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, results:RaycastHit[], orientation:CS.UnityEngine.Quaternion, maxDistance:number, layerMask:number): number
---@return number
---@param center CS.UnityEngine.Vector3
---@param halfExtents CS.UnityEngine.Vector3
---@param direction CS.UnityEngine.Vector3
---@param results RaycastHit[]
---@param optional orientation CS.UnityEngine.Quaternion
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3): RaycastHit[]
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion): RaycastHit[]
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, maxDistance:number): RaycastHit[]
---@overload fun(center:CS.UnityEngine.Vector3, halfExtents:CS.UnityEngine.Vector3, direction:CS.UnityEngine.Vector3, orientation:CS.UnityEngine.Quaternion, maxDistance:number, layerMask:number): RaycastHit[]
---@return RaycastHit[]
---@param center CS.UnityEngine.Vector3
---@param halfExtents CS.UnityEngine.Vector3
---@param direction CS.UnityEngine.Vector3
---@param optional orientation CS.UnityEngine.Quaternion
---@param optional maxDistance number
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction) end
---@overload fun(point0:CS.UnityEngine.Vector3, point1:CS.UnityEngine.Vector3, radius:number, results:Collider[]): number
---@overload fun(point0:CS.UnityEngine.Vector3, point1:CS.UnityEngine.Vector3, radius:number, results:Collider[], layerMask:number): number
---@return number
---@param point0 CS.UnityEngine.Vector3
---@param point1 CS.UnityEngine.Vector3
---@param radius number
---@param results Collider[]
---@param optional layerMask number
---@param optional queryTriggerInteraction number
function CS.UnityEngine.Physics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, queryTriggerInteraction) end
---@param worldBounds CS.UnityEngine.Bounds
---@param subdivisions number
function CS.UnityEngine.Physics.RebuildBroadphaseRegions(worldBounds, subdivisions) end
---@overload fun(meshID:number, convex:boolean): void
---@param meshID number
---@param convex boolean
---@param optional cookingOptions number
function CS.UnityEngine.Physics.BakeMesh(meshID, convex, cookingOptions) end
---@param op string
---@param value (fun(scene:CS.UnityEngine.PhysicsScene, headerArray:CS.Unity.Collections.NativeArray_CS.UnityEngine.ContactPairHeader.ReadOnly):void)
function CS.UnityEngine.Physics.ContactEvent(op, value) end
return CS.UnityEngine.Physics
