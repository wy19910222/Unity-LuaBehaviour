/*
 * @Author: wangyun
 * @CreateTime: 2023-02-25 13:18:58 316
 * @LastEditor: wangyun
 * @EditTime: 2023-02-25 13:18:58 321
 */

using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace LuaApp {
	public class LuaBehaviourWithPath : LuaBehaviour {
		public string luaPath;
		
		[SerializeField]
		protected List<Injection7> m_InjectionList = new List<Injection7>();

		public override LuaTable LuaTable {
			get {
				if (m_State == State.UNINITIALIZED) {
					InitLuaByPath();
				}
				return m_LuaTable;
			}
		}
		
		protected override void Awake() {
			InitLuaByPath();
		}

		private void InitLuaByPath() {
			if (m_State != State.UNINITIALIZED) {
				Debug.LogError("This LuaBehaviour is already initialized!");
				return;
			}
			if (string.IsNullOrEmpty(luaPath)) {
				Debug.LogError("LuaPath is empty!");
				return;
			}
			LuaTable luaClassTable = LuaMain.Instance.Require(luaPath);
			if (luaClassTable == null) {
				Debug.LogError("LuaTable not returned: " + luaPath);
				Dispose();
				return;
			}
			LuaTable luaInstanceTable = LuaMain.Instance.FuncInvoke<LuaTable>(luaClassTable);
			if (luaInstanceTable == null) {
				Debug.LogError("LuaObject has not instanced: " + luaPath);
				Dispose();
				return;
			}
			
			Init(luaInstanceTable);
		}

		[ContextMenu("Inject Data")]
		protected override void InjectData() {
			base.InjectData();
			InjectionConverter.ToDictTable(m_InjectionList, m_LuaTable);
		}

#if UNITY_EDITOR
		[ContextMenu("Copy data from LuaInjectionData")]
		protected void CopyDataFromLuaInjectionData() {
			LuaInjectionData injectionData = GetComponent<LuaInjectionData>();
			if (injectionData) {
				const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
				var injectionListField = typeof(LuaInjectionData).GetField("m_InjectionList", flags);
				if (injectionListField?.GetValue(injectionData) is List<Injection7> injections) {
					// JsonUtility不能序列化最外层是数组的结构，所以这里带上个bool一起序列化
					m_InjectionList = JsonUtility.FromJson<(List<Injection7>, bool)>(JsonUtility.ToJson((injections, false))).Item1;
				}
			}
		}
#endif
	}
}
