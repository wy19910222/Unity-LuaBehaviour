--@Author: wangyun
--@CreateTime: 2022-02-25 17:27:34 161
--@LastEditor: wangyun
--@EditTime: 2022-02-27 03:03:07 216

---@alias CSLike.EventListener fun(caller:any, ...)

---@class CSLike.EventHandler : CSLike.Object
---@field public listener CSLike.EventListener
---@field public argArray any[]
---@field public isOnce boolean
---@field public enabled boolean
local EventHandler = CSLike.Class("CSLike", "EventHandler");

---@param listener CSLike.EventListener
---@param argArray any[]
---@param isOnce boolean
function EventHandler:ctor(listener, argArray, isOnce)
	self.enabled = true;
	self.listener = listener;
	self.argArray = argArray;
	self.isOnce = isOnce;
end


---@class CSLike.EventManager : CSLike.LuaBehaviour
---@field public Instance CSLike.EventManager @static
---@field private m_TypeCallerMap table<string, table<any, CSLike.EventHandler[]>>
local m = CSLike.BehaviourUtility.Singleton("CSLike", "EventManager", true);
CSLike.EventManager = m;

---@private
function m:Awake()
	self.m_TypeCallerMap = {};
end

---@public
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@return boolean
function m:Has(type, caller, listener)
	return self:IsValidParams(type, caller, listener) and self:Find(type, caller, listener) ~= nil;
end

---@public
---@param type string
---@return boolean
function m:NoLogEmit(type, ...)
	return self:InternalEmit(type, false, ...);
end

---@public
---@param type string
---@return boolean
function m:Emit(type, ...)
	return self:InternalEmit(type, true, ...);
end

---@private
---@param type string
---@param logEvent boolean
---@return boolean
function m:InternalEmit(type, logEvent, ...)
	if not type then
		LogError("Event type is null!");
		return false;
	end
	
	local argArray = table.pack(...);
	if logEvent then
		LogVerbose("Emit " .. type .. (argArray.n > 0 and ": " or "."), ...);
	end
	
	---@type CSLike.EventHandler[]
	local handlerList = {};
	---@type any[]
	local callerList = {};
	local callerHandlersMap = self.m_TypeCallerMap[type];
	if callerHandlersMap then
		local count = 0;
		for caller, handlers in pairs(callerHandlersMap) do
			for _, handler in ipairs(handlers) do
				table.insert(handlerList, handler);
				table.insert(callerList, caller);
			end
			for index = #handlers, 1, -1 do
				if handlers[index].isOnce then
					table.remove(handlers, index);
				end
			end
			if #handlers <= 0 then
				callerHandlersMap[caller] = nil;
			else
				count = count + 1;
			end
		end
		if count <= 0 then
			self.m_TypeCallerMap[type] = nil;
		end
	end
	
	local length = #handlerList;
	for index, handler in ipairs(handlerList) do
		if handler.enabled then
			local n1, n2 = handler.argArray.n, argArray.n;
			local n = n1 + n2;
			local args = table.move(argArray, 1, n2, n1 + 1, table.move(handler.argArray, 1, n1, 1, {n = n}));
			xpcall(handler.listener, function(error) LogError(error); end, callerList[index], table.unpack(args, 1, n));
		end
	end
	return length > 0;
end

---@public
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@return CSLike.EventHandler
function m:On(type, caller, listener, ...)
	return self:AddListener(type, caller, listener, false, table.pack(...), false);
end

---@public
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@return CSLike.EventHandler
function m:Once(type, caller, listener, ...)
	return self:AddListener(type, caller, listener, true, table.pack(...), false);
end

---@public
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@param onceOnly boolean
---@param caller any
function m:Off(type, caller, listener, onceOnly)
	self:RemoveBy(function(eventType, listenerCaller, handler)
		if type and type ~= eventType then
			return false;
		end
		if caller and caller ~= listenerCaller then
			return false;
		end
		if listener and listener ~= handler.listener then
			return false;
		end
		if onceOnly and not handler.isOnce then
			return false;
		end
		return true;
	end);
end

---@public
---@param caller any
function m:OffAll(caller)
	for type, callerHandlersMap in pairs(self.m_TypeCallerMap) do
		callerHandlersMap[caller] = nil;
		local count = 0;
		for _, _ in pairs(callerHandlersMap) do
			count = count + 1;
		end
		if count <= 0 then
			self.m_TypeCallerMap[type] = nil;
		end
	end
end

---@private
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@param isOnce boolean
---@param argArray any[]
---@param insertFirst boolean
---@return CSLike.EventHandler
function m:AddListener(type, caller, listener, isOnce, argArray, insertFirst)
	if not self:IsValidParams(type, caller, listener) then
		return nil;
	end
	local handler = self:Find(type, caller, listener);
	if handler then
		LogError("Listener is already exist!\t" .. type);
	else
		handler = EventHandler(listener, argArray, isOnce);
		local handlers = self.m_TypeCallerMap[type][caller];
		if insertFirst then
			table.insert(handlers, 1, handler);
		else
			table.insert(handlers, handler);
		end
	end
	return handler;
end

---@private
---@param predicate fun(type:string, caller:any, handler:CSLike.EventHandler): boolean
function m:RemoveBy(predicate)
	if not predicate then
		return;
	end
	for type, callerHandlersMap in pairs(self.m_TypeCallerMap) do
		local count = 0;
		for caller, handlers in pairs(callerHandlersMap) do
			for index = #handlers, 1, -1 do
				if predicate(type, caller, handlers[index]) then
					table.remove(handlers, index);
				end
			end
			if #handlers <= 0 then
				callerHandlersMap[caller] = nil;
			else
				count = count + 1;
			end
		end
		if count <= 0 then
			self.m_TypeCallerMap[type] = nil;
		end
	end
end

---@private
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@return CSLike.EventHandler
function m:Find(type, caller, listener)
	local callerHandlersMap = self.m_TypeCallerMap[type];
	if not callerHandlersMap then
		callerHandlersMap = {};
		self.m_TypeCallerMap[type] = callerHandlersMap;
	end
	local handlers = callerHandlersMap[caller];
	if not handlers then
		handlers = {};
		callerHandlersMap[caller] = handlers;
	end
	for _, handler in ipairs(handlers) do
		if handler.listener == listener then
			return handler;
		end
	end
	return nil;
end

---@private
---@param type string
---@param caller any
---@param listener CSLike.EventListener
---@return boolean
function m:IsValidParams(type, caller, listener)
	if not type then
		LogError("Event type is nil!");
		return false;
	end
	if not caller then
		LogError("Caller is nil!", type);
		return false;
	end
	if not listener then
		LogError("Listener is nil!", type);
		return false;
	end
	return true;
end

---@protected
function m:OnDestroy()
	m.super.OnDestroy(self);
	self.m_TypeCallerMap = {};
end

return m;