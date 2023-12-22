--@Author: wangyun
--@CreateTime: 2022-12-16 20:42:01 447
--@LastEditor: wangyun
--@EditTime: 2022-01-06 14:31:54 450

require("UI.UIToast");
require("UI.UIMore");

---@class CSLike.UIMain : CSLike.UIBase
---@field public btnAvatar UnityEngine.UI.Button
---@field public btnEnergyAdd UnityEngine.UI.Button
---@field public btnCoinAdd UnityEngine.UI.Button
local m = CSLike.Class("CSLike", "UIMain", CSLike.UIBase);
m.ResPath = "UI/UIMain/UIMain.prefab";
m.UIType = CSLike.UIType.SCENE;
CSLike.UIMain = m;

---@private
function m:Awake()
	self.btnEnergyAdd.onClick:AddListener(function()
		CSLike.UIToast.Show("仅用于展示，无任何功能")
	end);
	self.btnCoinAdd.onClick:AddListener(function()
		CSLike.UIToast.Show("仅用于展示，无任何功能")
	end);
	self.btnAvatar.onClick:AddListener(function()
		require("UI.UIUserInfo");
		CSLike.UIManager.Instance:Open(CSLike.UIUserInfo);
	end);
	CSLike.UIManager.Instance:Open(CSLike.UIMore);
end

return m;