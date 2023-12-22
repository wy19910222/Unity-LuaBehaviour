--@Author: wangyun
--@CreateTime: 2023-03-26 07:07:13 158
--@LastEditor: wangyun
--@EditTime: 2023-03-26 07:07:13 160

local TypeObj = typeof(CS.UnityEngine.Object);
local TypeGO = typeof(CS.UnityEngine.GameObject);
local TypeComp = typeof(CS.UnityEngine.Component);

local CACHING_LENGTH_MAX = 50;

---@class CSLike.AssetManager : CSLike.SingletonBehaviour
---@field public Instance CSLike.AssetManager @static
---@field private m_AssetPathList string[]
---@field private m_AssetTaskCachingDict table<string, CSLike.AsyncTask>
local m = CSLike.BehaviourUtility.Singleton("CSLike", "AssetManager", true);
CSLike.AssetManager = m;

---@private
function m:Awake()
	self.m_AssetPathList = {};
	self.m_AssetTaskCachingDict = {};
end

---@private
function m:OnDestroy()
	for key, task in pairs(self.m_AssetTaskCachingDict) do
		task:Cancel();
		self.m_AssetTaskCachingDict[key] = nil;
	end
	m.super.OnDestroy(self);
end

---@return number
function m:GetCapacity()
	return CACHING_LENGTH_MAX;
end

function m:SetCapacity(capacity)
	CACHING_LENGTH_MAX = capacity;
	self:Cap();
end

---@param assetPath string
---@param type System.Type
---@param callback fun(asset:UnityEngine.Object, error:string)
---@return {isDone:(fun():boolean), asset:UnityEngine.Object, error:string}
function m:LoadAssetAsync(assetPath, type, callback)
	LogDebug("LoadAssetAsync: ", assetPath, type);
	if assetPath and assetPath ~= "" then
		local task = self:GetAssetLoaderTask(assetPath, type);
		---@type {isDone:(fun():boolean), asset:UnityEngine.Object, error:string}
		local ret = {isDone = function() return task.IsDone; end};
		task:Finally(function(result, reason)
			ret.asset = result;
			ret.error = reason;
			if callback then
				callback(result, reason);
			end
		end);
		return ret;
	else
		if callback then
			callback(nil, "Empty path!");
		end
		return {isDone = function() return true; end, error = "Empty path!"};
	end
end

---@param assetPath string
function m:ReleaseAsset(assetPath)
	local task = self.m_AssetTaskCachingDict[assetPath];
	if task then
		task:Cancel();
		for i = 1, #self.m_AssetPathList do
			if self.m_AssetPathList[i] == assetPath then
				table.remove(self.m_AssetPathList, i);
				break;
			end
		end
	end
end

---@private
---@param assetPath string
---@param type System.Type
---@return CSLike.AsyncTask
function m:GetAssetLoaderTask(assetPath, type)
	local task = self.m_AssetTaskCachingDict[assetPath];
	if task then
		-- 找到目标，标记为最后加载
		for i = 1, #self.m_AssetPathList do
			if self.m_AssetPathList[i] == assetPath then
				table.remove(self.m_AssetPathList, i);
				table.insert(self.m_AssetPathList, assetPath);
				break;
			end
		end
	else
		-- 没找到目标，需要新建加载任务
		task = CSLike.AsyncTask(function(success, fail)
			CSLike.CoroutineManager.Instance:StartCo(function()
				local pointIndex = CS.LuaHelp.StringLastIndexOf(assetPath, ".");
				if pointIndex ~= -1 then
					assetPath = CS.LuaHelp.StringSub(assetPath, 0, pointIndex);
				end
				LogDebug("Load asset:", assetPath, type);
				---@type UnityEngine.Object
				local asset;
				if type and TypeComp:IsAssignableFrom(type) then
					-- 如果是组件，则先加载出Prefab，再获取对应组件
					local request = CS.UnityEngine.Resources.LoadAsync(assetPath, TypeGO);
					coroutine.yield(request);
					---@type UnityEngine.GameObject
					local goAsset = request.asset;
					asset = goAsset and goAsset:GetComponent(type);
				else
					local request = CS.UnityEngine.Resources.LoadAsync(assetPath, type or TypeObj);
					coroutine.yield(request);
					asset = request.asset;
				end
				if asset then
					LogDebug("Load asset succeeded:", assetPath, type);
					success(asset)
				else
					LogError("Load asset failed:", assetPath, type);
					fail("Load Asset Failed!")
				end
			end, self.m_CSBehaviour);
		end, assetPath);
		self.m_AssetTaskCachingDict[assetPath] = task;
		table.insert(self.m_AssetPathList, assetPath);
		-- 如果加载失败，则从列表中移除
		task:Catch(function(_)
			self.m_AssetTaskCachingDict[assetPath] = nil;
			for i = #self.m_AssetPathList, 1, -1 do
				if self.m_AssetPathList[i] == assetPath then
					table.remove(self.m_AssetPathList, i);
					break;
				end
			end
		end);
		-- 如果缓存列表超过指定长度，则进行清理
		if #self.m_AssetPathList > CACHING_LENGTH_MAX then
			self:Cap();
		end
	end
	return task;
end

---@private
function m:Cap()
	local removeIndex = 1;
	for _ = 1, #self.m_AssetPathList - CACHING_LENGTH_MAX do
		local path = self.m_AssetPathList[removeIndex];
		local task = self.m_AssetTaskCachingDict[path];
		-- 加载中说明外部还等着用，不能清理
		if task.IsDone then
			table.remove(self.m_AssetPathList, removeIndex);
			self.m_AssetTaskCachingDict[path] = nil;
		else
			-- 未清理，下标后移一位
			removeIndex = removeIndex + 1;
		end
	end
end

return m;