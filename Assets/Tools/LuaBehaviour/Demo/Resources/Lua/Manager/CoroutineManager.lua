--@Author: wangyun
--@CreateTime: 2022-03-20 01:26:50 242
--@LastEditor: wangyun
--@EditTime: 2022-03-20 01:26:50 245

---@class CSLike.CoroutineManager : CSLike.LuaBehaviour
---@field public Instance CSLike.CoroutineManager @static
---@field private m_CoroutineMap table<UnityEngine.Coroutine, System.Collections.IEnumerator>
local m = CSLike.BehaviourUtility.Singleton("CSLike", "CoroutineManager", true);
CSLike.CoroutineManager = m;

local WAIT_FOR_END_OF_FRAME = CS.UnityEngine.WaitForEndOfFrame();

---@private
function m:Awake()
	self.m_CoroutineMap = setmetatable({}, { __mode = "kv"});
end

local typeofCoroutine = typeof(CS.UnityEngine.Coroutine);
---@public
---@overload fun(routine:fun()|System.Collections.IEnumerator|UnityEngine.Coroutine):UnityEngine.Coroutine
---@param routine fun()|System.Collections.IEnumerator|UnityEngine.Coroutine
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:StartCo(routine, owner)
	if type(routine) == "function" then
		routine = util.cs_generator(routine);
	elseif type(routine) == "userdata" and routine:GetType() == typeofCoroutine then
		routine = self.m_CoroutineMap and self.m_CoroutineMap[routine];
	end
	if not routine then
		return nil;
	end
	if not owner or owner:Equals(nil) or not owner.enabled or not owner.gameObject.activeInHierarchy then
		owner = self.Instance.m_CSBehaviour;
	end
	if not owner then
		return nil;
	end
	owner:StopCoroutine(routine);
	local co = owner:StartCoroutine(routine);
	if co and self.m_CoroutineMap then
		self.m_CoroutineMap[co] = routine;
	end
	return co;
end

---@public
---@overload fun(routine:System.Collections.IEnumerator|UnityEngine.Coroutine)
---@param routine System.Collections.IEnumerator|UnityEngine.Coroutine
---@param owner UnityEngine.MonoBehaviour
function m:StopCo(routine, owner)
	if not routine then
		return;
	end
	if not owner or owner:Equals(nil) then
		owner = self.Instance.m_CSBehaviour;
	end
	if not owner then
		return;
	end
	owner:StopCoroutine(routine);
end

---@public
---@overload fun()
---@param owner UnityEngine.MonoBehaviour
function m:StopAllCos(owner)
	if not owner or owner:Equals(nil) then
		owner = self.Instance.m_CSBehaviour;
	end
	if not owner then
		return;
	end
	owner:StopAllCoroutines();
end

---@public
---@overload fun(routine:System.Collections.IEnumerator|UnityEngine.Coroutine):UnityEngine.Coroutine
---@param routine System.Collections.IEnumerator|UnityEngine.Coroutine
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:MoveNext(routine, owner)
	self:StopCo(owner, routine);
	return self:StartCo(owner, routine);
end

---@public
---@overload fun(routine:System.Collections.IEnumerator|UnityEngine.Coroutine):UnityEngine.Coroutine
---@param routine System.Collections.IEnumerator|UnityEngine.Coroutine
---@param maxSteps number @Default is 999
function m:Flush(routine, maxSteps)
	if type(routine) == "userdata" and routine:GetType() == typeofCoroutine then
		routine = self.m_CoroutineMap and self.m_CoroutineMap[routine];
	end
	if not routine then
		return;
	end
	if not maxSteps then
		maxSteps = 999;
	end
	
	local hasNext = true;
	local steps = 0;
	while hasNext and steps < maxSteps do
		hasNext = routine:MoveNext();
		steps = steps + 1;
	end
	if steps >= maxSteps then
		LogError("Flush " .. steps .. " steps!");
	end
end

---@public
---@overload fun(callback:fun()):UnityEngine.Coroutine
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:Late(callback, owner)
	return self:StartCo(self:IELate(callback), owner);
end
---@private
---@param callback fun()
---@return System.Collections.IEnumerator
function m:IELate(callback)
	return util.cs_generator(function()
		if callback then
			coroutine.yield(WAIT_FOR_END_OF_FRAME);
			callback();
		end
	end);
end

---@public
---@overload fun(delay:number, callback:fun()):UnityEngine.Coroutine
---@param delay number
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:Once(delay, callback, owner)
	return self:StartCo(self:IEOnce(delay, callback, false), owner);
end
---@public
---@overload fun(delay:number, callback:fun()):UnityEngine.Coroutine
---@param delay number
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:LateOnce(delay, callback, owner)
	return self:StartCo(self:IEOnce(delay, callback, true), owner);
end
---@private
---@param delay number
---@param callback fun()
---@param late boolean
---@return System.Collections.IEnumerator
function m:IEOnce(delay, callback, late)
	return util.cs_generator(function()
		if callback then
			if delay > 0 then
				coroutine.yield(CS.UnityEngine.WaitForSeconds(delay));
			end
			if late then
				coroutine.yield(WAIT_FOR_END_OF_FRAME);
			end
			callback();
		end
	end);
end

---@public
---@overload fun(delay:number, callback:fun()):UnityEngine.Coroutine
---@param delay number
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:FrameOnce(delay, callback, owner)
	return self:StartCo(self:IEFrameOnce(delay, callback, false), owner);
end
---@public
---@overload fun(delay:number, callback:fun()):UnityEngine.Coroutine
---@param delay number
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:LateFrameOnce(delay, callback, owner)
	return self:StartCo(self:IEFrameOnce(delay, callback, true), owner);
end
---@private
---@param delay number
---@param callback fun()
---@param late boolean
---@return System.Collections.IEnumerator
function m:IEFrameOnce(delay, callback, late)
	return util.cs_generator(function()
		if callback then
			for _ = 1, delay do
				coroutine.yield(nil);
			end
			if late then
				coroutine.yield(WAIT_FOR_END_OF_FRAME);
			end
			callback();
		end
	end);
end

---@public
---@overload fun(loopUntilOrInstruction:(fun():boolean)|any, callback:fun()):UnityEngine.Coroutine
---@param loopUntilOrInstruction (fun():boolean)|any 
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:Wait(loopUntilOrInstruction, callback, owner)
	return self:StartCo(self:IEWait(loopUntilOrInstruction, callback, false), owner);
end
---@public
---@overload fun(loopUntilOrInstruction:(fun():boolean)|any, callback:fun()):UnityEngine.Coroutine
---@param loopUntilOrInstruction (fun():boolean)|any
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:LateWait(loopUntilOrInstruction, callback, owner)
	return self:StartCo(self:IEWait(loopUntilOrInstruction, callback, true), owner);
end
---@private
---@param loopUntilOrInstruction (fun():boolean)|any
---@param callback fun()
---@param late boolean
---@return System.Collections.IEnumerator
function m:IEWait(loopUntilOrInstruction, callback, late)
	return util.cs_generator(function()
		if loopUntilOrInstruction or callback then
			if type(loopUntilOrInstruction) == "function" then
				while not loopUntilOrInstruction() do
					coroutine.yield(nil);
				end
			else
				coroutine.yield(loopUntilOrInstruction);
			end
			if late then
				coroutine.yield(WAIT_FOR_END_OF_FRAME);
			end
			if callback then
				callback();
			end
		end
	end);
end

---@public
---@overload fun(interval:number, loopUntil:fun():boolean):UnityEngine.Coroutine
---@param interval number
---@param loopUntil fun():boolean
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:Loop(interval, loopUntil, owner)
	return self:StartCo(self:IELoop(interval, loopUntil, false), owner);
end
---@public
---@overload fun(interval:number, loopUntil:fun():boolean):UnityEngine.Coroutine
---@param interval number
---@param loopUntil fun():boolean
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:LateLoop(interval, loopUntil, owner)
	return self:StartCo(self:IELoop(interval, loopUntil, true), owner);
end
---@private
---@param interval number
---@param loopUntil fun():boolean
---@param late boolean
---@return System.Collections.IEnumerator
function m:IELoop(interval, loopUntil, late)
	return util.cs_generator(function()
		if loopUntil then
			local instruction = CS.UnityEngine.WaitForSeconds(interval);
			if late then
				coroutine.yield(WAIT_FOR_END_OF_FRAME);
			end
			while not loopUntil() do
				coroutine.yield(instruction);
				if late then
					coroutine.yield(WAIT_FOR_END_OF_FRAME);
				end
			end
		end
	end);
end

---@public
---@overload fun(interval:number, loopUntil:fun():boolean):UnityEngine.Coroutine
---@param interval number
---@param loopUntil fun():boolean
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:FrameLoop(interval, loopUntil, owner)
	return self:StartCo(self:IEFrameLoop(interval, loopUntil, false), owner);
end
---@public
---@overload fun(interval:number, loopUntil:fun():boolean):UnityEngine.Coroutine
---@param interval number
---@param loopUntil fun():boolean
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:LateFrameLoop(interval, loopUntil, owner)
	return self:StartCo(self:IEFrameLoop(interval, loopUntil, true), owner);
end
---@private
---@param interval number
---@param loopUntil fun():boolean
---@param late boolean
---@return System.Collections.IEnumerator
function m:IEFrameLoop(interval, loopUntil, late)
	return util.cs_generator(function()
		if loopUntil then
			if late then
				coroutine.yield(WAIT_FOR_END_OF_FRAME);
				while not loopUntil() do
					for _ = 1, interval do
						coroutine.yield(WAIT_FOR_END_OF_FRAME);
					end
				end
			else
				while not loopUntil() do
					for _ = 1, interval do
						coroutine.yield(nil);
					end
				end
			end
		end
	end);
end

---@public
---@overload fun():UnityEngine.Coroutine
---@overload fun(callback:fun()):UnityEngine.Coroutine
---@param callback fun()
---@param owner UnityEngine.MonoBehaviour
---@return UnityEngine.Coroutine
function m:EndOfLag(callback, owner)
	return self:StartCo(self:IEEndOfLag(callback), owner);
end
local LAG_FRAME_COUNT_MAX = 20;
local LAG_CHECK_FRAME_COUNT = 3;
local LAG_CHECK_THRESHOLD = 0.00001;
---@private
---@param callback fun()
---@return System.Collections.IEnumerator
function m:IEEndOfLag(callback)
	return util.cs_generator(function()
		local maxFrame = CS.UnityEngine.Time.frameCount + math.max(LAG_FRAME_COUNT_MAX, LAG_CHECK_FRAME_COUNT);
		---@type number[]
		local deltaTimeQueue = {};
		table.insert(deltaTimeQueue, CS.UnityEngine.Time.deltaTime);
		coroutine.yield(nil);
		table.insert(deltaTimeQueue, CS.UnityEngine.Time.deltaTime);
		while CS.UnityEngine.Time.frameCount < maxFrame do
			coroutine.yield(nil);
			table.insert(deltaTimeQueue, CS.UnityEngine.Time.deltaTime);
			if #deltaTimeQueue > LAG_CHECK_FRAME_COUNT then
				table.remove(deltaTimeQueue, 1);
			end
			local variance = self:GetVariance(deltaTimeQueue);
			if variance < LAG_CHECK_THRESHOLD then
				break;
			end
		end
		if callback then
			callback();
		end
	end);
end
---@private
---@param values number[]
---@return number
function m:GetVariance(values)
	local count = #values;
	local sum = 0;
	for _, value in ipairs(values) do
		sum = sum + value;
	end
	local average = sum / count;
	sum = 0;
	for _, value in ipairs(values) do
		sum = sum + (value - average) ^ 2;
	end
	return sum / count;
end

return m;