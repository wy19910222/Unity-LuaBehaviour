--@Author: wangyun
--@CreateTime: 2022-08-10 20:42:01 447
--@LastEditor: wangyun
--@EditTime: 2022-08-10 20:42:01 450

---@class CSLike.UIToast : CSLike.UIFade
---@field public txtContent UnityEngine.UI.Text
---@field public duration number
---
---@field private m_Co UnityEngine.Coroutine
local m = CSLike.Class("CSLike", "UIToast", CSLike.UIFade);
m.ResPath = "UI/UIToast/UIToast.prefab";
m.UIType = CSLike.UIType.TRANSPARENT;
CSLike.UIToast = m;

---@param content string
function m:Init(content)
	self.txtContent.text = content;
	if self.m_Co then
		CSLike.CoroutineManager.Instance:StopCo(self.m_Co, self.m_CSBehaviour);
	end
	self.m_Co = CSLike.CoroutineManager.Instance:Once(self.duration, function()
		self:Exit();
		self.m_Co = nil;
	end, self.m_CSBehaviour);
end

--- static
---@overload fun(content: string)
---@param content string
---@param variates table<string, any>
function m.Show(content, variates)
	---@param ui CSLike.UIToast
	CSLike.UIManager.Instance:Open(m):Then(function(ui)
		ui:Init(content, variates)
	end);
end

return m;