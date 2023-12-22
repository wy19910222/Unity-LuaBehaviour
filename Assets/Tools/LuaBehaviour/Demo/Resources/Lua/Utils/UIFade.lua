--@Author: wangyun
--@CreateTime: 2022-03-30 02:20:31 373
--@LastEditor: wangyun
--@EditTime: 2022-03-30 02:20:31 373

---@class CSLike.UIFade : CSLike.UIBase
---@field public fadeIns DG.Tweening.DOTweenAnimation[]
---@field public fadeOuts DG.Tweening.DOTweenAnimation[]
---
---@field public IsExiting boolean @property @readonly
---@field public IsFadedIn boolean @property @readonly
---@field public OnExitCallback fun() @property
---@field public OnDestroyCallback fun() @property
---
---@field private m_IsFadedIn boolean
---@field private m_IsExiting boolean
---@field protected m_FadingInCo UnityEngine.Coroutine
local m = CSLike.Class("CSLike", "UIFade", CSLike.UIBase);
m.UIType = CSLike.UIType.WINDOW;
CSLike.UIFade = m;

---@private
function m:ctor()
	CSLike.SetProperty(self, {
		IsExiting = {
			Get = function(_)
				return self.m_IsExiting;
			end,
		},
		IsFadedIn = {
			Get = function(_)
				return self.m_IsFadedIn;
			end,
		},
		OnExitCallback = {
			Get = function(values)
				return values.m_OnExitCallback;
			end,
			Set = function(values, value)
				values.m_OnExitCallback = value;
			end,
		},
		OnDestroyCallback = {
			Get = function(values)
				return values.m_OnDestroyCallback;
			end,
			Set = function(values, value)
				values.m_OnDestroyCallback = value;
			end,
		}
	});
end

---@protected
function m:Start()
	self:FadeIn();
end

---@protected
function m:FadeIn()
	local tweenCount = 0;
	if self.fadeIns then
		for _, tween in ipairs(self.fadeIns) do
			if tween.tween then
				tween.tween:PlayForward();
				tweenCount = tweenCount + 1;
				tween.tween.onComplete = function()
					tweenCount = tweenCount - 1;
				end
			end
		end
	end
	self.m_FadingInCo = CSLike.CoroutineManager.Instance:Wait(function() return tweenCount <= 0 end, function()
		self.m_FadingInCo = nil;
		self.m_IsFadedIn = true;
		self:OnFadedIn();
	end, self.m_CSBehaviour);
end

---@protected
function m:OnFadedIn()
end

function m:Exit()
	if not self.m_IsExiting then
		self.m_IsExiting = true;
		self:OnExit();
		if self.OnExitCallback then
			self.OnExitCallback();
		end
		self:FadeOut(function() self:Close(); end);
	end
end

---@protected
function m:OnExit()
end

---@overload fun()
---@protected
---@param callback fun()
function m:FadeOut(callback)
	if self.m_FadingInCo then
		CSLike.CoroutineManager.Instance:StopCo(self.m_FadingInCo, self.m_CSBehaviour);
		self.m_FadingInCo = nil;
		if self.fadeIns then
			for _, tween in ipairs(self.fadeIns) do
				if tween.tween then
					tween.tween:Kill();
				end
			end
		end
		self:OnFadedIn();
	end
	local tweenCount = 0;
	if self.fadeOuts then
		for _, tween in ipairs(self.fadeOuts) do
			if tween.tween then
				tween.tween:PlayForward();
				tweenCount = tweenCount + 1;
				tween.tween.onComplete = function()
					tweenCount = tweenCount - 1;
				end
			end
		end
	end
	CSLike.CoroutineManager.Instance:Wait(function() return tweenCount <= 0 end, function()
		self:OnFadedOut();
		if callback then
			callback();
		end
	end, self.m_CSBehaviour);
end

---@protected
function m:OnFadedOut()
end

---@protected
function m:OnDestroy()
	if not self.m_IsExiting then
		self:OnExit();
		if self.OnExitCallback then
			self.OnExitCallback();
		end
	end
	if self.OnDestroyCallback then
		self.OnDestroyCallback();
	end
	m.super.OnDestroy(self);
end

return m;