--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.TargetGenerator : CSLike.LuaBehaviour
---@field public targetPrefab UnityEngine.Transform
---@field public rangeX number
---@field public rangeY number
---@field public rangeZ number
---
---@field private m_Parent UnityEngine.Transform
local m = CSLike.Class("CSLike", "RandomRotate", CSLike.LuaBehaviour);
CSLike.RandomRotate = m;

---@private
function m:Awake()
	self.m_Parent = self.m_CSBehaviour.transform;
	if not self.rangeX then
		self.rangeX = self.rangeY * CS.UnityEngine.Screen.width / CS.UnityEngine.Screen.height;
	end
	if not self.rangeY then
		self.rangeY = self.rangeX * CS.UnityEngine.Screen.height / CS.UnityEngine.Screen.width;
	end
	if not self.rangeZ then
		self.rangeZ = 0;
	end
	self:Generate();
end

---@private
---@return UnityEngine.Transform
function m:Generate()
	local randomX = CS.UnityEngine.Random.Range(-self.rangeX, self.rangeX);
	local randomY = CS.UnityEngine.Random.Range(-self.rangeY, self.rangeY);
	local randomZ = CS.UnityEngine.Random.Range(-self.rangeZ, self.rangeZ);
	local randomPos = self.m_Parent:TransformPoint(randomX, randomY, randomZ);
	local target = CS.UnityEngine.Object.Instantiate(self.targetPrefab, randomPos, self.m_Parent.rotation, self.m_Parent);
	local collider = target:GetComponentInChildren(typeof(CS.UnityEngine.Collider));
	CSLike.BehaviourUtility.AddListener(collider.gameObject, {
		---@param other UnityEngine.Collider
		OnTriggerEnter = function(_, other)
			CS.UnityEngine.Object.Destroy(target.gameObject);
			self:Generate();
		end
	});
	return target;
end

return m;