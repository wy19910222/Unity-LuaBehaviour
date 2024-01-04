--@Author: wangyun
--@CreateTime: 2022-03-29 01:42:44 836
--@LastEditor: wangyun
--@EditTime: 2022-03-29 01:42:44 836

local DESIGN_WIDTH = 1920;
local DESIGN_HEIGHT = 1080;

---@type table<CSLike.UIBase, number>
local sortingOrderMap = setmetatable({}, {__mode = "k"});

---@class CSLike.UIManager : CSLike.LuaBehaviour
---@field public Instance CSLike.UIManager @static
---
---@field private m_Root UnityEngine.RectTransform	@所有UI的根节点
---@field private m_Camera UnityEngine.Camera	@UI摄像机
---
---@field private m_LayerList UnityEngine.RectTransform[]	@不同UIType之间分层
---@field private m_UITaskList CSLike.AsyncTask[]	@界面实例列表，元素类型为壳子（AsyncTask），调用open时，先把壳子加到列表，再去加载界面对象，如果加载失败或取消，再移除壳子。
---
---@field private m_WaitLoading {View:UnityEngine.RectTransform, FadeIn:fun(), FadeOut:fun()}	@用于加载界面时临时遮挡整个屏幕
---@field private m_TopCollider UnityEngine.UI.Graphic	@用于主动遮挡整个屏幕，支持状态叠加，只要存在一个key都遮挡，所有key都移除才不遮挡
---@field private m_TopColliderKeyDict table<any, table<string, boolean>>	@用于记录主动遮挡整个屏幕的状态
---
---@field private m_WeakMap table<string, CSLike.UIBase>	@用弱表来检测界面对象有没有被回收
local m = CSLike.BehaviourUtility.Singleton("CSLike", "UIManager", true);
CSLike.UIManager = m;

---@public
function m:Awake()
	self.m_LayerList = {};
	self.m_UITaskList = {};
	self.m_WeakMap = setmetatable({}, { __mode = "v"});
	
	local position = self.m_CSBehaviour.transform.localPosition;
	position.y = 100;
	self.m_CSBehaviour.transform.localPosition = position;
	
	self:InitCamera();
	self:InitRoot();
	self:InitEventSystem();
	self:InitWaitLoading();
	self:InitTopCollider();
end

---@private
function m:InitCamera()
	local cameraGo = CS.UnityEngine.GameObject("UICamera");
	local cameraTrans = cameraGo.transform;
	cameraTrans:SetParent(self.m_CSBehaviour.transform);
	cameraTrans.localPosition = CS.UnityEngine.Vector3.zero;
	cameraTrans.localRotation = CS.UnityEngine.Quaternion.identity;
	cameraTrans.localScale = CS.UnityEngine.Vector3.one;
	
	---@type UnityEngine.Camera
	self.m_Camera = cameraGo:AddComponent(typeof(CS.UnityEngine.Camera));
	self.m_Camera.orthographic = true;
	self.m_Camera.orthographicSize = 5.4;
	self.m_Camera.nearClipPlane = -10;
	self.m_Camera.farClipPlane = 10;
	self.m_Camera.cullingMask = 1 << CS.UnityEngine.LayerMask.NameToLayer("UI");

	if self.m_Camera.GetUniversalAdditionalCameraData then
		local cameraData = self.m_Camera:GetUniversalAdditionalCameraData();
		cameraData.renderType = CS.UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
		---@param camera UnityEngine.Camera
		for _, camera in ipairs(CS.UnityEngine.Camera.allCameras) do
			local _cameraData = camera:GetUniversalAdditionalCameraData();
			if _cameraData.renderType == CS.UnityEngine.Rendering.Universal.CameraRenderType.Base then
				_cameraData.cameraStack:Add(self.m_Camera);
				break;
			end
		end
	else
		self.m_Camera.clearFlags = CS.UnityEngine.CameraClearFlags.Depth;
		local depthMax = 0;
		---@param camera UnityEngine.Camera
		for _, camera in ipairs(CS.UnityEngine.Camera.allCameras) do
			if camera.depth > depthMax then
				depthMax = camera.depth;
			end
		end
		self.m_Camera.depth = depthMax + 1;
	end
end

---@private
function m:InitRoot()
	local rootGo = CS.UnityEngine.GameObject("UIRoot",
			typeof(CS.UnityEngine.Canvas),
			typeof(CS.UnityEngine.UI.CanvasScaler),
			typeof(CS.UnityEngine.UI.GraphicRaycaster)
	);
	rootGo.layer = CS.UnityEngine.LayerMask.NameToLayer("UI");
	self.m_Root = rootGo.transform;
	self.m_Root:SetParent(self.m_CSBehaviour.transform);
	self.m_Root.localPosition = CS.UnityEngine.Vector3.zero;
	self.m_Root.localRotation = CS.UnityEngine.Quaternion.identity;
	self.m_Root.localScale = CS.UnityEngine.Vector3.one;
	
	---@type UnityEngine.Canvas
	local canvas = rootGo:GetComponent(typeof(CS.UnityEngine.Canvas));
	canvas.renderMode = CS.UnityEngine.RenderMode.ScreenSpaceCamera;
	canvas.worldCamera = self.m_Camera;
	canvas.planeDistance = 0;
	canvas.sortingOrder = 1;
	
	---@type UnityEngine.UI.CanvasScaler
	local canvasScaler = rootGo:GetComponent(typeof(CS.UnityEngine.UI.CanvasScaler));
	canvasScaler.uiScaleMode = CS.UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
	canvasScaler.screenMatchMode = CS.UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
	canvasScaler.referenceResolution = {x = DESIGN_WIDTH, y = DESIGN_HEIGHT};

	---@param value CSLike.UIType
	for _, value in ipairs(CSLike.UIType) do
		local layerGo = CS.UnityEngine.GameObject(value,
				typeof(CS.UnityEngine.RectTransform),
				typeof(CS.UnityEngine.Canvas),
				typeof(CS.UnityEngine.UI.GraphicRaycaster)
		);
		layerGo.layer = self.m_Root.gameObject.layer;
		---@type UnityEngine.RectTransform
		local trans = layerGo:GetComponent(typeof(CS.UnityEngine.RectTransform));
		trans:SetParent(self.m_Root);
		trans.anchorMin = CS.UnityEngine.Vector2.zero;
		trans.anchorMax = CS.UnityEngine.Vector2.one;
		trans.sizeDelta = CS.UnityEngine.Vector2.zero;
		trans.localPosition = CS.UnityEngine.Vector3.zero;
		trans.localRotation = CS.UnityEngine.Quaternion.identity;
		trans.localScale = CS.UnityEngine.Vector3.one;
		table.insert(self.m_LayerList, trans);
	end
end

---@private
function m:InitEventSystem()
	if not CS.UnityEngine.EventSystems.EventSystem.current then
		CS.UnityEngine.GameObject("EventSystem",
				typeof(CS.UnityEngine.EventSystems.EventSystem),
				typeof(CS.UnityEngine.EventSystems.StandaloneInputModule)
		).transform:SetParent(self.m_CSBehaviour.transform);
	end
end

---@return UnityEngine.RectTransform
function m:GetRoot()
	return self.m_Root;
end

---@return UnityEngine.Camera
function m:GetCamera()
	return self.m_Camera;
end

---@public
---@overload fun()
---@param uiClass CSLike.UIBase
function m:InitWaitLoading(uiClass)
	if self.m_WaitLoading ~= false then
		if self.m_WaitLoading then
			CS.UnityEngine.Object.Destroy(self.m_WaitLoading.View.gameObject);
		end
		if uiClass then
			-- 创建一个加载中界面当做加载中遮罩
			self.m_WaitLoading = false;	-- 借赋值为false来标记加载中状态
			---@param ui CSLike.UIBase
			self:CreateUI(uiClass):Then(function(ui)
				local viewGo = ui.View.gameObject;
				local buttonType = typeof(CS.UnityEngine.UI.Button);
				local button = viewGo:GetComponent(buttonType) or viewGo:AddComponent(buttonType);
				button.onClick:AddListener(function() LogWarn("WaitLoading clicked!") end);
				if not ui.FadeIn then
					ui.FadeIn = function(_)
						---@type UnityEngine.CanvasGroup
						local canvasGroup = viewGo:GetComponent(typeof(CS.UnityEngine.CanvasGroup));
						if canvasGroup then
							canvasGroup.blocksRaycasts = true;
						else
							viewGo:SetActive(true);
						end
					end
				end
				if not ui.FadeOut then
					ui.FadeOut = function(_)
						---@type UnityEngine.CanvasGroup
						local canvasGroup = viewGo:GetComponent(typeof(CS.UnityEngine.CanvasGroup));
						if canvasGroup then
							canvasGroup.blocksRaycasts = false;
						else
							viewGo:SetActive(false);
						end
					end
				end
				ui:FadeOut();
				self.m_WaitLoading = ui;
			end);
		else
			-- 创建一个空的对象当做加载中遮罩
			local go = CS.UnityEngine.GameObject("WaitLoading");
			go.layer = self.m_Root.gameObject.layer;

			---@type UnityEngine.UI.Image
			local graphic = go:AddComponent(typeof(CS.UnityEngine.UI.Image));
			graphic.color = CS.UnityEngine.Color(0, 0, 0, 0);
			graphic.raycastTarget = false;

			---@type UnityEngine.EventSystems.EventTrigger.Entry
			local entry = CS.UnityEngine.EventSystems.EventTrigger.Entry();
			---@type UnityEngine.EventSystems.EventTriggerType
			entry.eventID = CS.UnityEngine.EventSystems.EventTriggerType.PointerClick;
			entry.callback:AddListener(function() LogWarn("WaitLoading clicked!") end);
			---@type UnityEngine.EventSystems.EventTrigger
			local eventTrigger = go:AddComponent(typeof(CS.UnityEngine.EventSystems.EventTrigger));
			eventTrigger.triggers:Add(entry);
			
			---@type UnityEngine.RectTransform
			local trans = go.transform;
			trans:SetParent(self.m_Root);
			trans.anchorMin = CS.UnityEngine.Vector2.zero;
			trans.anchorMax = CS.UnityEngine.Vector2.one;
			trans.sizeDelta = CS.UnityEngine.Vector2.zero;
			trans.localPosition = CS.UnityEngine.Vector3.zero;
			trans.localRotation = CS.UnityEngine.Quaternion.identity;
			trans.localScale = CS.UnityEngine.Vector3.one;
			
			self.m_WaitLoading = {
				View = trans,
				FadeIn = function(_) graphic.raycastTarget = true; end,
				FadeOut = function(_) graphic.raycastTarget = false; end
			};
		end
	end
end

---@private
function m:InitTopCollider()
	-- 创建一个空的对象当做点击遮罩
	self.m_TopColliderKeyDict = {};
	
	local go = CS.UnityEngine.GameObject("TopCollider");
	go.layer = self.m_Root.gameObject.layer;

	---@type UnityEngine.UI.Image
	local graphic = go:AddComponent(typeof(CS.UnityEngine.UI.Image));
	graphic.color = CS.UnityEngine.Color(0, 0, 0, 0);
	graphic.raycastTarget = false;
	
	---@type UnityEngine.EventSystems.EventTrigger.Entry
	local entry = CS.UnityEngine.EventSystems.EventTrigger.Entry();
	---@type UnityEngine.EventSystems.EventTriggerType
	entry.eventID = CS.UnityEngine.EventSystems.EventTriggerType.PointerClick;
	entry.callback:AddListener(function() LogWarn("TopCollider clicked!") end);
	---@type UnityEngine.EventSystems.EventTrigger
	local eventTrigger = go:AddComponent(typeof(CS.UnityEngine.EventSystems.EventTrigger));
	eventTrigger.triggers:Add(entry);

	---@type UnityEngine.RectTransform
	local trans = go.transform;
	trans:SetParent(self.m_Root);
	trans.anchorMin = CS.UnityEngine.Vector2.zero;
	trans.anchorMax = CS.UnityEngine.Vector2.one;
	trans.sizeDelta = CS.UnityEngine.Vector2.zero;
	trans.localPosition = CS.UnityEngine.Vector3.zero;
	trans.localRotation = CS.UnityEngine.Quaternion.identity;
	trans.localScale = CS.UnityEngine.Vector3.one;

	self.m_TopCollider = graphic;
end

--- 设置了两层key，外层是owner，方便整体移除
---@public
---@overload fun(owner: string)
---@param owner any
---@param key string
function m:AddTopCollider(owner, key)
	if owner then
		local ownerCount = 0;
		for _, _ in pairs(self.m_TopColliderKeyDict) do
			ownerCount = ownerCount + 1;
		end
		if ownerCount <= 0 then
			if not self.m_TopCollider.raycastTarget then
				LogInfo("TopCollider open");
				self.m_TopCollider.raycastTarget = true;
			end
		end
		
		---@type table<string, boolean>
		local dict = self.m_TopColliderKeyDict[owner];
		if not dict then
			dict = {};
			self.m_TopColliderKeyDict[owner] = dict;
		end
		if not key then
			key = "Default";
		end
		if not dict[key] then
			dict[key] = true;
			LogInfo("AddTopCollider", owner.class and "[" .. owner.fullClassName .. "]" or owner, key);
		end
	end
end

--- 当只传入owner时，会将owner下的状态整体移除
---@public
---@overload fun(owner: string)
---@param owner any
---@param key string
function m:RemoveTopCollider(owner, key)
	if owner then
		---@type table<string, boolean>
		local dict = self.m_TopColliderKeyDict[owner];
		if dict then
			if key then
				dict[key] = nil;
				local keyCount = 0;
				for _, _ in pairs(dict) do
					keyCount = keyCount + 1;
				end
				if keyCount > 0 then
					return;
				end
			end
			self.m_TopColliderKeyDict[owner] = nil;
			LogInfo("RemoveTopCollider", owner.class and "[" .. owner.fullClassName .. "]" or owner, key);
		end

		local ownerCount = 0;
		for _, _ in pairs(self.m_TopColliderKeyDict) do
			ownerCount = ownerCount + 1;
		end
		if ownerCount <= 0 then
			if self.m_TopCollider.raycastTarget then
				LogInfo("TopCollider close");
				self.m_TopCollider.raycastTarget = false;
			end
		end
	end
end

---@public
---@param sceneUIClass CSLike.UIBase
---@param closeAllWindow boolean
---@return CSLike.AsyncTask
function m:GotoScene(sceneUIClass, closeAllWindow)
	if not sceneUIClass then
		LogError("UIClass is nil, but try to open!");
		return nil;
	end
	if sceneUIClass.UIType ~= CSLike.UIType.SCENE then
		LogError("UIClass is not a scene UI, but try to goto scene!");
		return nil;
	end
	---@type CSLike.AsyncTask[] @找出所有要销毁的界面
	local closeList = {};
	if closeAllWindow then
		-- 所有窗口界面
		local windowTaskList = self:List({CSLike.UIType.WINDOW});
		table.move(windowTaskList, 1, #windowTaskList, #closeList + 1, closeList);
	end
	-- 所有场景附属界面
	local scenePartTaskList = self:List({CSLike.UIType.SCENE_PART});
	table.move(scenePartTaskList, 1, #scenePartTaskList, #closeList + 1, closeList);
	-- 所有场景界面
	local sceneTaskList = self:List({CSLike.UIType.SCENE});
	table.move(sceneTaskList, 1, #sceneTaskList, #closeList + 1, closeList);
	-- 尚未加载成功的界面直接取消
	for index = #closeList, 1, -1 do
		local taskWillClose = closeList[index];
		if not taskWillClose.IsDone then
			taskWillClose:Cancel();
			table.remove(closeList.table, index);
		end
	end
	local task = self:DoOpen(sceneUIClass);
	if task then
		task:Then(function(_)
			for _, taskWillClose in ipairs(closeList) do
				if not taskWillClose.IsExiting then
					self:DoClose(taskWillClose);
				end
			end
		end);
	end
	return task;
end

---@public
---@param uiClass CSLike.UIBase
---@return CSLike.AsyncTask
function m:Open(uiClass, ...)
	if not uiClass then
		LogError("UIClass is nil!");
		return nil;
	end
	if uiClass.UIType == CSLike.UIType.SCENE then
		LogError("UIClass is a scene UI, but try to open!");
		return nil;
	end
	return self:DoOpen(uiClass, ...);
end

---@private
---@param uiClass CSLike.UIBase
---@return CSLike.AsyncTask
function m:DoOpen(uiClass, ...)
	if uiClass then
		local uiName = uiClass.fullClassName;
		LogInfo("UIManager.open [" .. uiName .. "]");
		local task = self:CreateUI(uiClass, ...);
		table.insert(self.m_UITaskList, task);
		task:Then(function(_)
			LogInfo("UIManager.open [" .. uiName .. "] succeed");
		end);
		task:Catch(function(reason)
			if task.IsCanceled then
				LogInfo("UIManager.open [" .. uiName .. "] canceled");
			else
				LogError("UIManager.open [" .. uiName .. "] failed:", reason);
			end
			for index, uiTask in ipairs(self.m_UITaskList) do
				if uiTask == task then
					table.remove(self.m_UITaskList, index);
					break;
				end
			end
		end);

		if uiClass.WillShowLoading and self.m_WaitLoading then
			self.m_WaitLoading:FadeIn();
			task:Finally(function(_, _)
				self.m_WaitLoading:FadeOut();
			end);
		end
		return task;
	end
	return nil;
end

--- 关闭指定类型的所有界面对象
---@public
---@overload fun()
---@overload fun(types:CSLike.UIType[])
---@param types CSLike.UIType[] @需要关闭的界面类型，如果为nil，则关闭全类型。
---@param exceptUIClass CSLike.UIBase @例外的界面
function m:CloseAll(types, exceptUIClass)
	if types then
		LogInfo("CloseAllUI:[", table.concat(types, ","), "]", exceptUIClass and "except:" .. exceptUIClass.fullClassName or "");
	else
		LogInfo("CloseAllUI", exceptUIClass and "except:" .. exceptUIClass.fullClassName or "");
	end
	for index = #self.m_UITaskList, 1, -1 do
		local uiTask = self.m_UITaskList[index];
		---@type CSLike.UIBase
		local uiClass = uiTask.Tag;
		if uiClass ~= exceptUIClass then
			local willClose;
			if types then
				for _, type in ipairs(types) do
					if uiClass.UIType == type then
						willClose = true;
						break;
					end
				end
			else
				willClose = true;
			end
			if willClose then
				table.remove(self.m_UITaskList, index);
				if uiTask.IsDone then
					---@type CSLike.UIBase
					local uiInst = uiTask.Result;
					if uiInst then
						CS.UnityEngine.Object.Destroy(uiInst.View.gameObject);
					end
				else
					uiTask:Cancel();
				end
			end
		end
	end
end

---@public
---@param uiClassOrInstanceOrTask CSLike.UIBase | CSLike.AsyncTask
function m:Close(uiClassOrInstanceOrTask)
	if uiClassOrInstanceOrTask then
		local uiType;
		if uiClassOrInstanceOrTask.class == CSLike.AsyncTask then
			uiType = uiClassOrInstanceOrTask.Tag.UIType;
		elseif uiClassOrInstanceOrTask.isClass then
			uiType = uiClassOrInstanceOrTask.UIType;
		else
			uiType = uiClassOrInstanceOrTask.class.UIType;
		end
		if uiType ~= CSLike.UIType.SCENE then
			self:DoClose(uiClassOrInstanceOrTask);
		end
	end
end

---@private
---@param uiClassOrInstanceOrTask CSLike.UIBase | CSLike.AsyncTask
function m:DoClose(uiClassOrInstanceOrTask)
	if uiClassOrInstanceOrTask then
		if uiClassOrInstanceOrTask.class == CSLike.AsyncTask then
			LogInfo("UIManager.close [" .. uiClassOrInstanceOrTask.Tag.fullClassName .. "]");
			self:DestroyUITask(uiClassOrInstanceOrTask);
		else
			LogInfo("UIManager.close [" .. uiClassOrInstanceOrTask.fullClassName .. "]");
			self:DestroyUI(uiClassOrInstanceOrTask);
		end
	end
end

---@public
---@param uiClass CSLike.UIBase
---@return boolean
function m:IsExist(uiClass)
	return self:GetIndex(uiClass) ~= -1;
end

---@public
---@overload fun(uiClass:CSLike.UIBase)
---@param uiClass CSLike.UIBase
---@param notFindLog boolean
---@return CSLike.AsyncTask
function m:Get(uiClass, notFindLog)
	local index = self:GetIndex(uiClass);
	if index == -1 and notFindLog then
		LogWarn("Find ui error:" .. uiClass.fullClassName .. " is not found!");
	end
	return self.m_UITaskList[index];
end

---@public
---@return CSLike.AsyncTask
function m:GetScene()
	for index = #self.m_UITaskList, 1, -1 do
		local uiTask = self.m_UITaskList[index];
		---@type CSLike.UIBase
		local uiClass = uiTask.Tag;
		if uiClass.UIType == CSLike.UIType.SCENE then
			return uiTask;
		end
	end
	return nil;
end

---@public
---@overload fun():CSLike.AsyncTask[]
---@param types CSLike.UIType[] @需要获取的界面类型，如果为nil，则关闭全类型。
---@return CSLike.AsyncTask[]
function m:List(types)
	local list = {};
	for index = #self.m_UITaskList, 1, -1 do
		local uiTask = self.m_UITaskList[index];
		---@type CSLike.UIBase
		local uiClass = uiTask.Tag;
		if types then
			for _, type in ipairs(types) do
				if uiClass.UIType == type then
					table.insert(list, uiTask);
					break;
				end
			end
		else
			table.insert(list, uiTask);
		end
	end
	return list;
end

---@private
---@param uiClass CSLike.UIBase
---@return CSLike.AsyncTask @fun(success:fun(result:CSLike.UIBase), fail:fun(reason:any))
function m:CreateUI(uiClass, ...)
	local args = table.pack(...);
	---@type CSLike.AsyncTask
	local task;
	task = CSLike.AsyncTask(function(success, fail)
		local resPath = uiClass.ResPath;
		if resPath and resPath ~= "" then
			CSLike.CoroutineManager.Instance:StartCo(function()
				local request = CSLike.AssetManager.Instance:LoadAssetAsync(resPath, typeof(CS.UnityEngine.GameObject));
				while not request.isDone() do
					coroutine.yield(nil)
				end
				local prefab = request.asset;
				if prefab then
					-- task不存在说明是同步加载了，那么就没机会取消
					if not task or not task.IsDone then
						---@type UnityEngine.GameObject
						local go = CS.UnityEngine.Object.Instantiate(prefab);
						if not go then
							fail("Instantiate prefab failed!");
							return;
						end
						---@type UnityEngine.RectTransform
						local trans = go:GetComponent(typeof(CS.UnityEngine.RectTransform));
						if not trans then
							trans = go:AddComponent(typeof(CS.UnityEngine.RectTransform));
						end
						local parent = self.m_LayerList[uiClass.UIType];
						trans:SetParent(parent);
						trans.anchorMin = CS.UnityEngine.Vector2.zero;
						trans.anchorMax = CS.UnityEngine.Vector2.one;
						trans.sizeDelta = CS.UnityEngine.Vector2.zero;
						trans.localPosition = CS.UnityEngine.Vector3.zero;
						trans.localRotation = CS.UnityEngine.Quaternion.identity;
						trans.localScale = CS.UnityEngine.Vector3.one;

						sortingOrderMap[trans] = uiClass.SortingOrder;
						for index = 0, parent.childCount - 1 do
							local child = parent:GetChild(index);
							local sortingOrder = sortingOrderMap[child] or 0;
							if uiClass.SortingOrder < sortingOrder then
								trans:SetSiblingIndex(index);
								break;
							end
						end

						---@type CSLike.UIBase
						local instance;
						---@type LuaApp.LuaBehaviour[]
						local csBehaviours = go:GetComponents(typeof(CS.LuaApp.LuaBehaviour));
						---@param csBehaviour LuaApp.LuaBehaviour
						for _, csBehaviour in ipairs(csBehaviours) do
							---@type CSLike.LuaBehaviour
							local luaBehaviour = csBehaviour.LuaTable;
							if CSLike.IsInstanceOf(luaBehaviour, uiClass) then
								instance = luaBehaviour;
								break;
							end
						end
						if not instance then
							---@type LuaApp.LuaBehaviourDynamicAdd
							local csBehaviour = go:AddComponent(typeof(CS.LuaApp.LuaBehaviourDynamicAdd));
							csBehaviour.injectionData = go:GetComponent(typeof(CS.LuaApp.LuaInjectionData));
							csBehaviour:InitLuaByTable(uiClass, table.unpack(args));
							instance = csBehaviour.LuaTable;
						end
						if not instance then
							fail("Add lua component failed!");
							CS.UnityEngine.Object.Destroy(go);
							return;
						end

						local timestamp = CS.System.DateTime.Now.Ticks;
						local info = instance.fullClassName .. "[" .. timestamp .. "]";
						self.m_WeakMap[info] = instance;

						success(instance);
					end
				else
					fail("Load " .. resPath .. " failed: " .. request.error);
				end
			end, self.m_CSBehaviour);
		else
			fail("ResPath is nil or empty!");
		end
	end, uiClass);
	return task;
end

---@private
---@param uiClassOrInstance CSLike.UIBase
function m:DestroyUI(uiClassOrInstance)
	local index = self:GetIndex(uiClassOrInstance);
	if index ~= -1 then
		local uiTask = table.remove(self.m_UITaskList, index);
		if uiTask.IsDone then
			---@type CSLike.UIBase
			local uiInst = uiTask.Result;
			if uiInst then
				CS.UnityEngine.Object.Destroy(uiInst.View.gameObject);
			end
		else
			uiTask:Cancel();
		end
		return;
	end
	LogError("Destroy ui error: " .. uiClassOrInstance.fullClassName .. " is not found!");
end

---@private
---@param uiTask CSLike.AsyncTask
function m:DestroyUITask(uiTask)
	for index = #self.m_UITaskList, 1, -1 do
		if self.m_UITaskList[index] == uiTask then
			table.remove(self.m_UITaskList, index);
			if uiTask.IsDone then
				---@type CSLike.UIBase
				local uiInst = uiTask.Result;
				if uiInst then
					CS.UnityEngine.Object.Destroy(uiInst.View.gameObject);
				end
			else
				uiTask:Cancel();
			end
			return;
		end
	end
	LogError("Destroy ui error: " .. uiTask.Tag.fullClassName .. " is not found!");
end

---@private
---@param uiClassOrInstance CSLike.UIBase
function m:GetIndex(uiClassOrInstance)
	if uiClassOrInstance.isClass then
		for index = #self.m_UITaskList, 1, -1 do
			if self.m_UITaskList[index].Tag == uiClassOrInstance then
				return index;
			end
		end
	else
		for index = #self.m_UITaskList, 1, -1 do
			if self.m_UITaskList[index].Result == uiClassOrInstance then
				return index;
			end
		end
	end
	return -1;
end

---@param uiClass CSLike.UIBase
function m:ReleaseByClass(uiClass)
	local usingResPathDict = {};
	for _, task in ipairs(self.m_UITaskList) do
		usingResPathDict[task.Tag.ResPath] = true;
	end
	local resPath = uiClass.ResPath;
	if not usingResPathDict[resPath] then
		CSLike.AssetManager.Instance:ReleaseAsset(resPath);
		collectgarbage();
	end
end

---@protected
function m:OnDestroy()
	self:CloseAll();
	m.super.OnDestroy(self);
end

return m;