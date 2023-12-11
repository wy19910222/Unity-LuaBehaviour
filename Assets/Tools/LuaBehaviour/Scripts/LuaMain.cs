using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace CSLike {
	public class LuaMain : MonoBehaviour {
		public static LuaMain m_Instance;
		public static LuaMain Instance {
			get {
				if (!m_Instance) {
					GameObject go = new GameObject(nameof(LuaMain));
					DontDestroyOnLoad(go);
					m_Instance = go.AddComponent<LuaMain>();
				}
				return m_Instance;
			}
		}
		
		public LuaEnv LuaEnv { get; private set; }
		
		private Func<string, LuaTable> m_Require;
		private Func<string, int, string> m_Traceback;
		private Func<object, object[], object> m_FuncInvoke;

		private HashSet<string> m_LoadedBuiltInSet, m_PreloadBuiltInSet;
		private HashSet<string> m_GlobalVarBuiltInSet;

		private void Awake() {
			m_Instance = this;
			Init();
		}

		private void OnDestroy() {
			Release();
		}

		public void Init() {
			m_LoadedBuiltInSet = new HashSet<string>();
			m_PreloadBuiltInSet = new HashSet<string>();
			m_GlobalVarBuiltInSet = new HashSet<string>();

			LuaEnv = new LuaEnv();
			LuaEnv.AddLoader((ref string luaPath) => Resources.Load<TextAsset>(luaPath.Replace('.', '/')).bytes);

			LuaTable package = LuaEnv.Global.Get<LuaTable>("package");
			package.Get<LuaTable>("loaded").ForEach<string, object>((key, _) => m_LoadedBuiltInSet.Add(key));
			package.Get<LuaTable>("preload").ForEach<string, object>((key, _) => m_PreloadBuiltInSet.Add(key));
			
			LuaEnv.Global.ForEach<string, object>((key, _) => m_GlobalVarBuiltInSet.Add(key));
			
			m_Require = LuaEnv.Global.Get<Func<string, LuaTable>>("require");
			m_Traceback = LuaEnv.Global.GetInPath<Func<string, int, string>>("debug.traceback");
			LuaTable csHelp = Require("CSLike.CSHelp");
			LuaEnv.Global.Set("CSHelp", csHelp);
			m_FuncInvoke = csHelp.Get<Func<object, object[], object>>("FuncInvoke");
			Require("CSLike.GlobalExtension");
			Require("CSLike.LuaClass");
			Require("CSLike.LuaEnum");
		}

		private void Release() {
			LuaTable package = LuaEnv.Global.Get<LuaTable>("package");
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

			List<string> globalVarRemoveList = new List<string>();
			LuaEnv.Global.ForEach<string, object>((key, _) => globalVarRemoveList.Add(key));
			foreach (string varName in globalVarRemoveList) {
				if (m_GlobalVarBuiltInSet.Contains(varName)) continue;
				LuaEnv.Global.Set<string, object>(varName, null);
			}

			m_Require = null;
			m_Traceback = null;
			m_FuncInvoke = null;

			try {
				LuaEnv.Dispose();
				LuaEnv = null;
			} catch {
				Debug.LogError("LuaEnv dispose failed!");
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
		
		public string Traceback(string message, int level = 2) {
			return m_Traceback(message, level);
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
