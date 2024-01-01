--@Author: wangyun
--@CreateTime: 2022-03-15 12:28:08 119
--@LastEditor: wangyun
--@EditTime: 2022-03-15 12:28:08 120

---@class CSLike.BehaviourUtility : CSLike.Object
local m = CSLike.Class("CSLike", "BehaviourUtility");
CSLike.BehaviourUtility = m;

--- static
---@overload fun(nameSpace:string, className:string):CSLike.LuaBehaviour
---@param nameSpace string
---@param className string
---@param dontDestroyOnLoad boolean @Default is false
---@return CSLike.LuaBehaviour
function m.Singleton(nameSpace, className, dontDestroyOnLoad)
	---@type CSLike.LuaBehaviour
	local singletonClass = CSLike.Class(nameSpace, className, CSLike.LuaBehaviour);

	---@param propertyValues {s_Instance:CSLike.LuaBehaviour}
	---@return CSLike.LuaBehaviour
	local function Get(propertyValues)
		if not CS.LuaApp.LuaMain.IsApplicationQuit then
			if not propertyValues.s_Instance then
				local instanceName = "[" .. (nameSpace and nameSpace .. "." .. className or className) .. "]";
				local go = CS.UnityEngine.GameObject(instanceName);
				--go.transform:SetParent(CS.LuaApp.LuaMain.Instance.transform);
				---@type LuaApp.LuaBehaviourDynamicAdd
				local csBehaviour = go:AddComponent(typeof(CS.LuaApp.LuaBehaviourDynamicAdd));
				csBehaviour:InitLuaByTable(singletonClass);
				propertyValues.s_Instance = csBehaviour.LuaTable;
				if dontDestroyOnLoad then
					CS.UnityEngine.GameObject.DontDestroyOnLoad(csBehaviour);
				end
			end
		end
		return propertyValues.s_Instance;
	end
	---@type table<any, PropertyHandle>
	local propertyHandles = {
		Instance = {
			Get = Get;
		}
	};
	CSLike.SetProperty(singletonClass, propertyHandles);

	return singletonClass;
end

---可以对某个GameObject或Component监听以下事件
---Awake、Start、Update、FixedUpdate、LateUpdate、OnEnable、OnDisable、OnDestroy、OnGUI
---OnBecameVisible、OnBecameInvisible、OnApplicationFocus、OnApplicationPause、OnApplicationQuit
---OnTriggerEnter/Stay/Exit、OnCollisionEnter/Stay/Exit、OnTriggerEnter2D/Stay2D/Exit2D、OnCollisionEnter2D/Stay2D/Exit2D
---OnDrag、OnDrop、OnInitializePotentialDrag、OnBeginDrag、OnEndDrag
---OnPointerClick、OnPointerDown、OnPointerUp、OnPointerEnter、OnPointerExit、OnScroll
---OnSelect、OnDeselect、OnUpdateSelected、OnMove、OnSubmit、OnCancel
---@public
---@param target UnityEngine.GameObject
---@param listener table<string, fun(...)>
---@return table<string, fun(...)>
function m.AddListener(target, listener)
	if target and listener then
		---@type LuaApp.LuaBehaviourDynamicAdd
		local csBehaviour = target:AddComponent(typeof(CS.LuaApp.LuaBehaviourDynamicAdd));
		csBehaviour:InitLuaByTable(setmetatable({}, {
			__index = listener;
			__call = function(clsTable)
				return setmetatable({}, {__index = clsTable});
			end
		}));
	end
end

return m;