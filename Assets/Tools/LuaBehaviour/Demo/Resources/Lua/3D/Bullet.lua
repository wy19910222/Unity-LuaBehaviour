--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.Bullet : CSLike.LuaBehaviour
---@field public initialSpeed number
---@field public lifetime number
---
---@field private m_Velocity UnityEngine.Vector3
local m = CSLike.Class("CSLike", "Bullet", CSLike.LuaBehaviour);
CSLike.Bullet = m;

---@private
function m:Awake()
	self.m_Velocity = self.m_CSBehaviour.transform.forward * self.initialSpeed;
	CSLike.CoroutineManager.Instance:Once(self.lifetime, function()
		CS.UnityEngine.Object.Destroy(self.m_CSBehaviour.gameObject);
	end, self.m_CSBehaviour);
end

---@private
function m:Update()
	local pos = self.m_CSBehaviour.transform.position;
	pos = pos + self.m_Velocity * CS.UnityEngine.Time.deltaTime;
	self.m_CSBehaviour.transform:LookAt(pos);
	self.m_CSBehaviour.transform.position = pos;
	self.m_Velocity = self.m_Velocity + CS.UnityEngine.Physics.gravity * CS.UnityEngine.Time.deltaTime;
end

---@private
---@param other UnityEngine.Collider
function m:OnTriggerEnter(other)
	-- 这里只会碰到目标，所以就不做判断了
	CS.UnityEngine.Object.Destroy(self.m_CSBehaviour.gameObject);
end

return m;