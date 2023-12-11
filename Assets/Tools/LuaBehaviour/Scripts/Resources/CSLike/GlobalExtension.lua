--@Author: wangyun
--@CreateTime: 2022-02-26 22:03:52 081
--@LastEditor: wangyun
--@EditTime: 2022-02-27 03:03:16 582

---@param data System.Collections.IEnumerable | table
---@return (fun(t:System.Collections.IEnumerable, index:number):number, any), System.Collections.IEnumerable, number
function cspairs(data)
	if type(data) == "table" then
		return pairs(data);
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

---@param data System.Collections.IEnumerable | table
---@return (fun(t:System.Collections.IEnumerable, index:number):number, any), System.Collections.IEnumerable, number
function csipairs(data)
	if type(data) == "table" then
		return ipairs(data);
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

---@type table
WeakTable = setmetatable({}, {__mode = "kv"});

---@type UnityEngine.TextAsset
local utilAsset = CS.UnityEngine.Resources.Load("xlua/util.lua", typeof(CS.UnityEngine.TextAsset));
xpcall(function() util = load(utilAsset.text)(); end, function(error) LogError(error); end);

---@overload fun(list:table):number
---@generic V
---@param list table<number, V> | V[]
---@param comp fun(a:V, b:V):boolean @返回true不交换顺序，返回false交换顺序
---@return number
function table.bubbleSort(list, comp)
	local count = #list;
	if count > 1 then
		local sortBorder = count;
		for _ = 1, count - 1 do
			local isSortComplete = true;
			local lastSwapIndex = 1;
			for j = 1, sortBorder - 1 do
				if not comp(list[j], list[j + 1]) then
					list[j], list[j + 1] = list[j + 1], list[j];
					isSortComplete = false;
					lastSwapIndex = j;
				end
			end
			sortBorder = lastSwapIndex;
			if isSortComplete then
				break;
			end
		end
	end
end