/*
 * @Author: wangyun
 * @CreateTime: 2022-07-02 19:47:40 076
 * @LastEditor: wangyun
 * @EditTime: 2022-07-02 19:47:40 081
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace LuaApp {
	public partial class LuaMain : MonoBehaviour {

		public static LuaMain m_Instance;

		public static LuaMain Instance {
			get {
				if (!m_Instance) {
					GameObject go = new GameObject(nameof(LuaMain));
					DontDestroyOnLoad(go);
					m_Instance = go.AddComponent<LuaMain>();
					m_Instance.Init();
				}
				return m_Instance;
			}
		}

		private bool m_Initialized;

		public LuaEnv LuaEnv { get; private set; }

		private readonly HashSet<string> m_LoadedBuiltInSet = new HashSet<string>();
		private readonly HashSet<string> m_PreloadBuiltInSet = new HashSet<string>();
		private readonly HashSet<string> m_GlobalVarBuiltInSet = new HashSet<string>();

		private Func<string, LuaTable> m_Require;
		private Func<object, object[], object> m_FuncInvoke;

		public void Init() {
			if (m_Initialized) {
				return;
			}
			m_Initialized = true;
			
			m_LoadedBuiltInSet.Clear();
			m_PreloadBuiltInSet.Clear();
			m_GlobalVarBuiltInSet.Clear();

			LuaEnv = new LuaEnv();
			LuaEnv.AddLoader((ref string luaPath) => Resources.Load<TextAsset>(luaPath.Replace('.', '/'))?.bytes);

			LuaTable package = LuaEnv.Global.Get<LuaTable>("package");
			package.Get<LuaTable>("loaded").ForEach<string, object>((key, _) => m_LoadedBuiltInSet.Add(key));
			package.Get<LuaTable>("preload").ForEach<string, object>((key, _) => m_PreloadBuiltInSet.Add(key));

			LuaEnv.Global.ForEach<string, object>((key, _) => m_GlobalVarBuiltInSet.Add(key));

			m_Require = LuaEnv.Global.Get<Func<string, LuaTable>>("require");
			LuaTable csHelp = Require("CSLike.CSHelp");
			LuaEnv.Global.Set("CSHelp", csHelp);
			m_FuncInvoke = csHelp.Get<Func<object, object[], object>>("FuncInvoke");
			Require("CSLike.LuaClass");
			Require("CSLike.LuaEnum");
			Require("CSLike.LuaBehaviour");
		}

		public void Release() {
			if (!m_Initialized) {
				return;
			}
			m_Initialized = false;
			
			LuaTable package = LuaEnv?.Global.Get<LuaTable>("package");
			if (package != null) {
				List<string> loadedRemoveList = new List<string>();
				var loadedTable = package.Get<LuaTable>("loaded");
				loadedTable.ForEach<string, object>((luaName, _) => loadedRemoveList.Add(luaName));
				foreach (string luaName in loadedRemoveList) {
					if (m_LoadedBuiltInSet.Contains(luaName)) continue;
					loadedTable.Set<string, object>(luaName, null);
				}
				List<string> preloadRemoveList = new List<string>();
				var preloadTable = package.Get<LuaTable>("preload");
				preloadTable.ForEach<string, object>((luaName, _) => preloadRemoveList.Add(luaName));
				foreach (string luaName in preloadRemoveList) {
					if (m_PreloadBuiltInSet.Contains(luaName)) continue;
					preloadTable.Set<string, object>(luaName, null);
				}
			}

			List<string> globalVarRemoveList = new List<string>();
			LuaEnv?.Global.ForEach<string, object>((key, _) => globalVarRemoveList.Add(key));
			foreach (string varName in globalVarRemoveList) {
				if (m_GlobalVarBuiltInSet.Contains(varName)) continue;
				LuaEnv?.Global.Set<string, object>(varName, null);
			}

			m_Require = null;
			m_FuncInvoke = null;

			try {
				LuaEnv?.Dispose();
				LuaEnv = null;
			} catch (Exception e) {
				Debug.LogError($"LuaEnv dispose failed: {e}");
				LuaEnv?.DoString(@"
					CS.UnityEngine.Debug.Log(""registry:"");
					local registry = debug.getregistry()
					for k, v in pairs(registry) do
						if type(k) == 'number' and type(v) == 'function' and registry[v] == k then
							local info = debug.getinfo(v)
							CS.UnityEngine.Debug.Log(string.format('%s:%d', info.short_src, info.linedefined))
						end
					end
				");
			}
		}

		public LuaTable Require(string luaPath) {
			try {
				Debug.Log("Require: " + luaPath);
				return m_Require(luaPath);
			} catch (LuaException le) {
				Debug.LogError(le);
				return null;
			}
		}
		
		public T FuncInvoke<T>(string path, params object[] args) {
			return FuncInvoke<T>(LuaEnv.Global, path, args);
		}
		public T FuncInvoke<T>(LuaTable table, params object[] args) {
			return FuncInvoke<T>(table as object, args);
		}
		public T FuncInvoke<T>(LuaTable table, string path, params object[] args) {
			object func = table?.GetInPath<object>(path);
			if (func != null) {
				return FuncInvoke<T>(func, args);
			}
			Debug.LogError("FuncInvoke: function is not exist!");
			return default;
		}
		private T FuncInvoke<T>(object func, params object[] args) {
			try {
				return (T) m_FuncInvoke(func, args);
			} catch (LuaException le) {
				Debug.LogError(le);
				return default;
			}
		}
	}
}
