--@Author: wangyun
--@CreateTime: 2022-12-16 20:42:01 447
--@LastEditor: wangyun
--@EditTime: 2022-01-06 14:31:54 450

require("UI.UIToast");

---@class CSLike.UIUserInfo : CSLike.UIFade
---@field public btnClose UnityEngine.UI.Button
---@field public btnEnergyAdd UnityEngine.UI.Button
---@field public btnCoinAdd UnityEngine.UI.Button
---@field public btnQuit UnityEngine.UI.Button
local m = CSLike.Class("CSLike", "UIUserInfo", CSLike.UIFade);
m.ResPath = "UI/UIUserInfo/UIUserInfo.prefab";
m.UIType = CSLike.UIType.WINDOW;
CSLike.UIUserInfo = m;

---@private
function m:Awake()
	self.btnEnergyAdd.onClick:AddListener(function()
		CSLike.UIToast.Show("仅用于展示，无任何功能")
	end);
	self.btnCoinAdd.onClick:AddListener(function()
		CSLike.UIToast.Show("仅用于展示，无任何功能")
	end);
	self.btnClose.onClick:AddListener(function()
		self:Exit();
	end);
	self.btnQuit.onClick:AddListener(function()
		CSLike.UIManager.Instance:GotoScene(CSLike.UIWelcome, true);
	end);
end

return m;