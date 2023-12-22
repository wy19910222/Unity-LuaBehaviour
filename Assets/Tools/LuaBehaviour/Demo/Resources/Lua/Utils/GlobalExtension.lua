--@Author: wangyun
--@CreateTime: 2022-02-26 22:03:52 081
--@LastEditor: wangyun
--@EditTime: 2022-02-27 03:03:16 582

local builtInPairs = pairs;
---@param data System.Collections.IEnumerable | table
---@return (fun(t:System.Collections.IEnumerable, index:number):number, any), System.Collections.IEnumerable, number
function pairs(data)
	if type(data) == "table" then
		return builtInPairs(data);
	end
	local enumerator = data:GetEnumerator();
	-----@param t System.Collections.IEnumerable
	-----@param index number
	-----@return any, any
	local function next(t, index)
		if enumerator:MoveNext() then
			local current = enumerator.Current;
			if type(current) == "userdata" and current.Key then
				return current.Key, current.Value;
			else
				return index + 1, current;
			end
		end
	end
	return next, data, -1;
end

local builtInIPairs = ipairs;
---@param data System.Collections.IEnumerable | table
---@return (fun(t:System.Collections.IEnumerable, index:number):number, any), System.Collections.IEnumerable, number
function ipairs(data)
	if type(data) == "table" then
		return builtInIPairs(data);
	end
	local enumerator = data:GetEnumerator();
	-----@param t System.Collections.IEnumerable
	-----@param index number
	-----@return number, any
	local function next(t, index)
		if enumerator:MoveNext() then
			return index + 1, enumerator.Current;
		end
	end
	return next, data, -1;
end

LogVerbose = CS.Log.Verbose;
LogDebug = CS.Log.Debug;
LogInfo = CS.Log.Info;
LogWarn = CS.Log.Warn;
LogError = function(...)
	if CS.Log.LogLevel:GetHashCode() >= CS.LogLevel.ERROR:GetHashCode() then
		local args = table.pack(...);
		local argCount = args.n + 1;
		args[argCount] = debug.traceback(nil, 2);
		CS.Log.Error(table.unpack(args, 1, argCount));
	end
end;

---@type UnityEngine.TextAsset
local utilAsset = CS.UnityEngine.Resources.Load("xlua/util.lua", typeof(CS.UnityEngine.TextAsset));
xpcall(function() util = load(utilAsset.text)(); end, function(error) LogError(error); end);