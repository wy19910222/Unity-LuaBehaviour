--@Author: wangyun
--@CreateTime: 2022-03-29 01:49:59 530
--@LastEditor: wangyun
--@EditTime: 2022-03-29 01:49:59 532

---@class CSLike.AsyncTask : CSLike.Object
---@field public Tag any
---
---@field public IsDone boolean
---@field public IsCanceled boolean
---@field public IsSucceed boolean
---
---@field public Result any
---@field public Reason any
---
---@field private m_IsDone boolean
---@field private m_IsCanceled boolean
---@field private m_IsSucceed boolean
---@field private m_Result any
---@field private m_Reason any
---@field private m_ThenCalls fun(result:any)[]
---@field private m_CatchCalls fun(reason:any)[]
---@field private m_FinallyCalls fun(result:any, reason:any)[]
local m = CSLike.Class("CSLike", "AsyncTask");
CSLike.AsyncTask = m;

---@private
---@param executor fun(success:fun(result:any), fail:fun(reason:any))
---@param tag any
function m:ctor(executor, tag)
	---@type table<any, PropertyHandle>
	local propertyHandles = {
		Tag = tag and {
			Get = function() return tag; end;
		},
		IsDone = {
			Get = function() return self.m_IsDone; end;
		},
		IsCanceled = {
			Get = function() return self.m_IsCanceled; end;
		},
		IsSucceed = {
			Get = function() return self.m_IsSucceed; end;
		},
		Result = {
			Get = function() return self.m_Result; end;
		},
		Reason = {
			Get = function() return self.m_Reason; end;
		}
	};
	CSLike.SetProperty(self, propertyHandles);
	self.m_ThenCalls = {};
	self.m_CatchCalls = {};
	self.m_FinallyCalls = {};
	
	executor(
		function(result)
			if not self.m_IsDone then
				self.m_Result = result;
				self:Done(true);
			end
		end,
		function(reason)
			if not self.m_IsDone then
				self.m_Reason = reason;
				self:Done(false);
			end
		end
	);
end

function m:Cancel()
	if not self.m_IsDone then
		self.m_IsCanceled = true;
		self:Done(false);
	end
end

---@protected
---@param isSucceed boolean
function m:Done(isSucceed)
	self.m_IsDone = true;
	self.m_IsSucceed = isSucceed;
	if isSucceed then
		for _, thenCall in ipairs(self.m_ThenCalls) do
			xpcall(thenCall, function(error) LogError(error); end, self.m_Result);
		end
	else
		for _, catchCall in ipairs(self.m_CatchCalls) do
			xpcall(catchCall, function(error) LogError(error); end, self.m_Reason);
		end
	end
	for _, finallyCall in ipairs(self.m_FinallyCalls) do
		xpcall(finallyCall, function(error) LogError(error); end, self.m_Result, self.m_Reason);
	end
	self.m_ThenCalls = nil;
	self.m_CatchCalls = nil;
	self.m_FinallyCalls = nil;
end

---@param thenCall fun(result:any)
function m:Then(thenCall)
	if thenCall then
		if not self.m_IsDone then
			table.insert(self.m_ThenCalls, thenCall);
		elseif self.m_IsSucceed then
			xpcall(thenCall, function(error) LogError(error); end, self.m_Result);
		end
	end
end

---@param catchCall fun(reason:any)
function m:Catch(catchCall)
	if catchCall then
		if not self.m_IsDone then
			table.insert(self.m_CatchCalls, catchCall);
		elseif not self.m_IsSucceed then
			xpcall(catchCall, function(error) LogError(error); end, self.m_Reason);
		end
	end
end

---@param finallyCall fun(result:any, reason:any)
function m:Finally(finallyCall)
	if finallyCall then
		if not self.m_IsDone then
			table.insert(self.m_FinallyCalls, finallyCall);
		else
			xpcall(finallyCall, function(error) LogError(error); end, self.m_Result, self.m_Reason);
		end
	end
end

return m;