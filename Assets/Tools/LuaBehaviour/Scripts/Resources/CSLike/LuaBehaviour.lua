--@Author: wangyun
--@CreateTime: 2022-02-25 19:04:37 259
--@LastEditor: wangyun
--@EditTime: 2022-02-27 05:58:06 997

---@class CSLike.LuaBehaviour : CSLike.Object
---@field protected m_CSBehaviour LuaApp.LuaBehaviour
local m = CSLike.Class("CSLike", "LuaBehaviour", nil);
CSLike.LuaBehaviour = m;

---@type table<string, CSLike.LuaBehaviour>	@用弱表来检测对象有没有被回收
local s_WeakMap = setmetatable({}, {__mode = "v"});
CSLike.LuaBehaviourMap = s_WeakMap;

---@private
function m:ctor()
	-- 用于监控对象回收情况，如果不需要，可以删除
	local timestamp = CS.System.DateTime.Now.Ticks;
	local nameSpace = self.nameSpace or "nil";
	local className = self.className;
	local key = nameSpace .. "." .. className .. "[" .. timestamp .. "]";
	s_WeakMap[key] = self;
end

---@protected
function m:OnDestroy()
	-- 移除所有对C#对象的引用，避免两边相互引用，然后等待Lua和C#分别垃圾回收
	for key, value in pairs(self) do
		if type(value) == "userdata" then
			self[key] = nil;
		elseif type(value) == "table" then
			self[key] = {};
		end
	end
end

return m;