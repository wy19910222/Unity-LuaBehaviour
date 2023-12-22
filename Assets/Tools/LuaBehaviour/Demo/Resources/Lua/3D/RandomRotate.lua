--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.RandomRotate : CSLike.LuaBehaviour
---@field public rotateSpeed number
---@field public changeInterval number
---
---@field private m_PrevAxis UnityEngine.Vector3
---@field private m_NextAxis UnityEngine.Vector3
---@field private m_Time number
local m = CSLike.Class("CSLike", "RandomRotate", CSLike.LuaBehaviour);
CSLike.RandomRotate = m;

---@private
function m:Awake()
	self.m_PrevAxis = CS.UnityEngine.Random.onUnitSphere;
	self.m_NextAxis = CS.UnityEngine.Random.onUnitSphere;
	self.m_Time = 0;
end

---@private
function m:Update()
	local axis = CS.UnityEngine.Vector3.Slerp(self.m_PrevAxis, self.m_NextAxis, self.m_Time / self.changeInterval);
	self.m_CSBehaviour.transform:Rotate(axis, self.rotateSpeed * CS.UnityEngine.Time.deltaTime);
	self.m_Time = self.m_Time + CS.UnityEngine.Time.deltaTime;
	if self.m_Time > self.changeInterval then
		self.m_PrevAxis = self.m_NextAxis;
		self.m_NextAxis = CS.UnityEngine.Random.onUnitSphere;
		self.m_Time = 0;
	end
end

return m;