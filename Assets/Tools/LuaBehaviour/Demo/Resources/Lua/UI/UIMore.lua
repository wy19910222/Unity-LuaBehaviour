--@Author: wangyun
--@CreateTime: 2022-12-16 20:42:01 447
--@LastEditor: wangyun
--@EditTime: 2022-01-06 14:31:54 450

require("UI.UIToast");

---@class CSLike.UIMore : CSLike.UIBase
---@field public btnMove UnityEngine.UI.Button
---@field public btnFold UnityEngine.UI.Button
---@field public tweenMoveIn DG.Tweening.DOTweenAnimation
---@field public tweenMoveOut DG.Tweening.DOTweenAnimation
---@field public btnContents UnityEngine.UI.Button[]
---
---@field private m_Showing boolean
local m = CSLike.Class("CSLike", "UIMore", CSLike.UIBase);
m.ResPath = "UI/UIMore/UIMore.prefab";
m.UIType = CSLike.UIType.SCENE_PART;
CSLike.UIMore = m;

---@private
function m:Awake()
	self.btnMove.onClick:AddListener(function()
		---@type DG.Tweening.Tween
		local tweenPrev;
		---@type DG.Tweening.Tween
		local tweenNext;
		if self.m_Showing then
			tweenPrev = self.tweenMoveIn.tween;
			tweenNext = self.tweenMoveOut.tween;
		else
			tweenPrev = self.tweenMoveOut.tween;
			tweenNext = self.tweenMoveIn.tween;
		end
		if tweenPrev and tweenPrev:IsPlaying() then
			return;
		end
		if tweenNext then
			tweenNext:Rewind(true);
			tweenNext:Play();
			self.m_Showing = not self.m_Showing;
			if self.m_Showing then
				self.btnFold.gameObject:SetActive(true);
			end
		end
	end);
	self.btnFold.onClick:AddListener(function()
		if not self.m_Showing then
			return;
		end
		---@type DG.Tweening.Tween
		local tweenPrev = self.tweenMoveIn.tween;
		if tweenPrev and tweenPrev:IsPlaying() then
			return;
		end
		---@type DG.Tweening.Tween
		local tweenNext = self.tweenMoveOut.tween;
		if tweenNext then
			tweenNext:Rewind(true);
			tweenNext:Play();
			self.m_Showing = false;
			self.btnFold.gameObject:SetActive(false);
		end
	end);
	for _, btnContent in ipairs(self.btnContents) do
		btnContent.onClick:AddListener(function()
			CSLike.UIToast.Show("仅用于展示，无任何功能")
		end);
	end
end

return m;