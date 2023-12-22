--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.Shooter.State : number

---@class CSLike.Shooter.State.Class : CSLike.LuaEnum
---@field NONE CSLike.Shooter.State @无
---@field READY CSLike.Shooter.State @准备发射
---@field SHOT CSLike.Shooter.State @发射完毕
local State = CSLike.LuaEnum.Create("CSLike.Shooter", "State",{
	NONE = 0,	-- 无
	READY = 1,	-- 准备发射
	SHOT = 2,	-- 发射完毕
});

---@class CSLike.Shooter : CSLike.LuaBehaviour
---@field public transPipeBody UnityEngine.Transform
---@field public durationReady number
---@field public durationShooting number
---@field public scaleDefault UnityEngine.Vector3
---@field public scaleReady UnityEngine.Vector3
---@field public curveReadyScale UnityEngine.AnimationCurve
---@field public curveShootingScale UnityEngine.AnimationCurve
---@field public delayShooting number
---@field public prefabBullet UnityEngine.Transform
---
---@field private m_State CSLike.Shooter.State
local m = CSLike.Class("CSLike", "Shooter", CSLike.LuaBehaviour);
CSLike.Shooter = m;

---@private
function m:Awake()
	self.m_State = State.NONE;
end

---@private
function m:Update()
	if self.m_State <= State.READY then
		-- 蓄力阶段
		local value = self.curveReadyScale:Evaluate(self.m_State - State.NONE);
		self.transPipeBody.localScale = CS.UnityEngine.Vector3.Lerp(self.scaleDefault, self.scaleReady, value);
	else
		-- 发射阶段
		local value = self.curveShootingScale:Evaluate(self.m_State - State.READY);
		self.transPipeBody.localScale = CS.UnityEngine.Vector3.Lerp(self.scaleReady, self.scaleDefault, value);
	end
	
	if self.m_State < State.READY then
		-- 蓄力阶段
		if CS.UnityEngine.Input.GetButton("Fire1") then
			-- 蓄力
			self.m_State = self.m_State + CS.UnityEngine.Time.deltaTime / self.durationReady;
			if self.m_State > State.READY then
				-- 蓄力完成
				self.m_State = State.READY;
			end
		else
			-- 释放
			self.m_State = self.m_State - CS.UnityEngine.Time.deltaTime / self.durationReady;
			self.m_State = CS.UnityEngine.Mathf.Max(self.m_State, State.NONE);
		end
	elseif self.m_State == State.READY then
		-- 蓄力完成
		if not CS.UnityEngine.Input.GetButton("Fire1") then
			-- 释放
			self.m_State = self.m_State + CS.UnityEngine.Time.deltaTime / self.durationShooting;
			CSLike.CoroutineManager.Instance:Once(self.delayShooting, function()
				CS.UnityEngine.Object.Instantiate(self.prefabBullet, self.transPipeBody.position, self.transPipeBody.rotation);
			end, self.m_CSBehaviour);
		end
	else
		-- 发射阶段
		self.m_State = self.m_State + CS.UnityEngine.Time.deltaTime / self.durationShooting;
		if self.m_State >= State.SHOT then
			self.m_State = State.NONE;
		end
	end
end

return m;