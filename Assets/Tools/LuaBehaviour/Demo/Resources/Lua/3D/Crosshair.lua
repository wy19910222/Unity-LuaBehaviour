--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.Crosshair : CSLike.LuaBehaviour
local m = CSLike.Class("CSLike", "Crosshair", CSLike.LuaBehaviour);
CSLike.Crosshair = m;

---@private
function m:Update()
	local mousePos = CS.UnityEngine.Input.mousePosition;
	local crosshairPos = CS.UnityEngine.Camera.main:WorldToScreenPoint(self.m_CSBehaviour.transform.position);
	crosshairPos.x = mousePos.x;
	crosshairPos.y = mousePos.y;
	self.m_CSBehaviour.transform.position = CS.UnityEngine.Camera.main:ScreenToWorldPoint(crosshairPos);
end

return m;