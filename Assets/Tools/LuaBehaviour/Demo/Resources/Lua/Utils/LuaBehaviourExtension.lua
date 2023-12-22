--@Author: wangyun
--@CreateTime: 2022-02-26 22:03:52 081
--@LastEditor: wangyun
--@EditTime: 2022-02-27 03:03:16 582

local m = CSLike.LuaBehaviour;

local OnDestroy = m.OnDestroy;

---@protected
function m:OnDestroy()
	if CSLike.EventManager.Instance then
		CSLike.EventManager.Instance:OffAll(self);
	end
	self.m_CSBehaviour:StopAllCoroutines();
	OnDestroy(self);
end