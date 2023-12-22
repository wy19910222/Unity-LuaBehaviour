using System;
using Unity.VisualScripting;
using UnityEngine;

namespace LuaApp {
	public partial class LuaMain {
		public string initializeLuaPath = "Init";
		public string entranceLuaPath;
		
		public static bool IsApplicationQuit { get; private set; }

		private void Awake() {
			if (m_Instance && m_Instance != this) {
				if (m_Instance.m_Initialized) {
					m_Initialized = true;
					LuaEnv = m_Instance.LuaEnv;
					m_LoadedBuiltInSet.Clear();
					m_LoadedBuiltInSet.AddRange(m_Instance.m_LoadedBuiltInSet);
					m_PreloadBuiltInSet.Clear();
					m_PreloadBuiltInSet.AddRange(m_Instance.m_PreloadBuiltInSet);
					m_GlobalVarBuiltInSet.Clear();
					m_GlobalVarBuiltInSet.AddRange(m_Instance.m_GlobalVarBuiltInSet);
					m_Require = m_Instance.m_Require;
					m_FuncInvoke = m_Instance.m_FuncInvoke;
					
					Destroy(m_Instance.gameObject);
					m_Instance = this;
					DontDestroyOnLoad(gameObject);
				} else {
					Destroy(m_Instance.gameObject);
					m_Instance = this;
					DontDestroyOnLoad(gameObject);
					InitLua();
				}
			} else {
				m_Instance = this;
				DontDestroyOnLoad(gameObject);
				InitLua();
			}
		}

		private void Start() {
			LaunchLua();
		}

		private void OnDestroy() {
			if (!IsApplicationQuit) {
				ReleaseLua();
			}
		}

		[ContextMenu("InitLua")]
		private void InitLua() {
			Init();
			LuaEnv.AddLoader((ref string luaPath) => Resources.Load<TextAsset>("Lua/" + luaPath.Replace('.', '/'))?.bytes);
			if (!string.IsNullOrEmpty(initializeLuaPath)) {
				Require(initializeLuaPath);
			}
		}

		[ContextMenu("LaunchLua")]
		private void LaunchLua() {
			if (!string.IsNullOrEmpty(entranceLuaPath)) {
				Require(entranceLuaPath);
			}
		}

		[ContextMenu("ReleaseLua")]
		private void ReleaseLua() {
			Release();
		}

		private void OnApplicationQuit() {
			IsApplicationQuit = true;
		}
	}
}