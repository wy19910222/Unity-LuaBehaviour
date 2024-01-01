--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.TestDict : CSLike.LuaBehaviour
---@field public a number
---@field public b UnityEngine.Vector2

---@class CSLike.Test : CSLike.LuaBehaviour
---@field public complexValue {a:number, b:{c:UnityEngine.GameObject[], d:string}[]}
---@field public listValue UnityEngine.GameObject[]
---@field public dictValue1 CSLike.TestDict
---@field public dictValue2 {a:number, b:UnityEngine.Vector2}
---@field public luaTableValue {a:number, b:UnityEngine.Vector2} @injection
---@field public valueIgnored1 number @ignore
---@field public valueIgnored2 number @property
---@field public valueIgnored3 number @static
---@field public enumValue UnityEngine.ScreenOrientation
---@field public constraintValue UnityEngine.Collider




---@class CSLike.Aiming : CSLike.LuaBehaviour
---@field public distance number
---@field public transH UnityEngine.Transform
---@field public transV UnityEngine.Transform
local m = CSLike.Class("CSLike", "Aiming", CSLike.LuaBehaviour);
CSLike.Aiming = m;

---@private
function m:Update()
	local mousePos = CS.UnityEngine.Input.mousePosition;
	local ray = CS.UnityEngine.Camera.main:ScreenPointToRay(mousePos);
	local forwardValue = CS.UnityEngine.Vector3.Dot(ray.direction, CS.UnityEngine.Camera.main.transform.forward);
	local directionDistance = (self.distance - CS.UnityEngine.Camera.main.nearClipPlane) / forwardValue;
	local targetPos = ray:GetPoint(directionDistance);
	local targetPosH = CS.UnityEngine.Vector3(targetPos.x, self.transH.position.y, targetPos.z);
	self.transH:LookAt(targetPosH);
	self.transV:LookAt(targetPos);
end

return m;