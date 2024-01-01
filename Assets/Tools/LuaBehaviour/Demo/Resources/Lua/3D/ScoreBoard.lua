--@Author: wangyun
--@CreateTime: 2023-12-20 20:02:56 608
--@LastEditor: wangyun
--@EditTime: 2023-12-20 20:02:56 609

---@class CSLike.ScoreBoard : CSLike.LuaBehaviour
---@field public viewportPos UnityEngine.Vector3
---@field public txtScore UnityEngine.TextMesh
---
---@field private m_Score number
local m = CSLike.Class("CSLike", "ScoreBoard", CSLike.LuaBehaviour);
CSLike.ScoreBoard = m;

---@private
function m:Awake()
	CSLike.EventManager.Instance:On("Hit", self, self.OnHit);
	self.m_Score = 0;
	self.txtScore.text = "000";
end

---@private
function m:Update()
	self.m_CSBehaviour.transform.position = CS.UnityEngine.Camera.main:ViewportToWorldPoint(self.viewportPos);
end

---@private
function m:OnHit()
	self.m_Score = self.m_Score + 1;
	local scoreStr = tostring(self.m_Score);
	local length = CS.LuaApp.LuaHelp.StringLength(scoreStr);
	for _ = length, 2 do
		scoreStr = "0" .. scoreStr;
		length = length + 1;
	end
	self.txtScore.text = CS.LuaApp.LuaHelp.StringSub(scoreStr, length - 3);
end

return m;