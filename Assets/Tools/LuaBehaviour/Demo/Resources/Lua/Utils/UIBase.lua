--@Author: wangyun
--@CreateTime: 2022-03-29 00:27:58 207
--@LastEditor: wangyun
--@EditTime: 2022-03-29 00:27:58 207

---@class CSLike.UIType : number

---@class CSLike.UIType.Class : CSLike.LuaEnum
---@field SCENE CSLike.UIType	@场景界面
---@field SCENE_PART CSLike.UIType	@从场景界面里拆出来的一部分，视觉上属于场景界面，为了复用和便于修改而拆出来
---@field WINDOW CSLike.UIType	@普通窗口界面
---@field DIALOG CSLike.UIType	@阻塞流程，强制用户操作，如模态对话框
---@field TEMPORARY CSLike.UIType	@不可穿透，阻塞背后界面的操作，但能自动消失，如加载中、网络连接中
---@field TRANSPARENT CSLike.UIType	@可穿透，不需要主动关闭，不阻塞背后界面的操作，如Toast、Banner广告等
local UIType = CSLike.LuaEnum.Create("CSLike", "UIType",{
	SCENE = 1,	-- 场景界面
	SCENE_PART = 2,	-- 从场景界面里拆出来的一部分，视觉上属于场景界面，为了复用和便于修改而拆出来
	WINDOW = 3,	-- 普通窗口界面
	DIALOG = 4,	-- 阻塞流程，强制用户操作，如模态对话框
	TEMPORARY = 5,	-- 不可穿透，阻塞背后界面的操作，但能自动消失，如加载中、网络连接中
	TRANSPARENT = 6,	-- 可穿透，不需要主动关闭，不阻塞背后界面的操作，如Toast、Banner广告等
});
CSLike.UIType = UIType;

---@class CSLike.UIBase : CSLike.LuaBehaviour
---@field UIType CSLike.UIType @static
---@field ResPath string @static
---@field SortingOrder number @static
---@field WillShowLoading boolean @static
---@field WillClearOnDestroy boolean @static
---@field View UnityEngine.RectTransform @property @readonly
local m = CSLike.Class("CSLike", "UIBase", CSLike.LuaBehaviour, {
	UIType = CSLike.UIType.SCENE,
	ResPath = "",
	SortingOrder = 0,
	WillShowLoading = true,
	WillClearOnDestroy = true,
});
CSLike.UIBase = m;

---@private
function m:ctor()
	---@type table<any, PropertyHandle>
	local propertyHandles = {
		View = {
			Get = function() 
				return self.m_CSBehaviour.transform;
			end;
		}
	};
	CSLike.SetProperty(self, propertyHandles);
end

---@protected
function m:Close()
	CSLike.UIManager.Instance:Close(self);
end

---@protected
function m:OnDestroy()
	LogVerbose("OnDestroy:", self.className);
	
	m.super.OnDestroy(self);

	if self.WillClearOnDestroy then
		if CSLike.UIManager.Instance then
			CSLike.UIManager.Instance:ReleaseByClass(self.class);
		end
	end
end

return m;