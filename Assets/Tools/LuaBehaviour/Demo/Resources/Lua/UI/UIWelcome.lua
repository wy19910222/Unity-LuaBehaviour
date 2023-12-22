--@Author: wangyun
--@CreateTime: 2022-12-16 20:42:01 447
--@LastEditor: wangyun
--@EditTime: 2022-01-06 14:31:54 450

---@class CSLike.UIWelcome : CSLike.UIBase
---@field public btnEnter UnityEngine.UI.Button
local m = CSLike.Class("CSLike", "UIWelcome", CSLike.UIBase);
m.ResPath = "UI/UIWelcome/UIWelcome.prefab";
m.UIType = CSLike.UIType.SCENE;
CSLike.UIWelcome = m;

---@private
function m:Awake()
	self.btnEnter.onClick:AddListener(function()
		require("UI.UIMain");
		CSLike.UIManager.Instance:GotoScene(CSLike.UIMain, true);
	end);
end

return m;