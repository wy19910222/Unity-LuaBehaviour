/*
 * @Author: wangyun
 * @CreateTime: 2023-02-25 13:18:58 316
 * @LastEditor: wangyun
 * @EditTime: 2023-02-25 13:18:58 321
 */

using UnityEngine;
using XLua;

namespace LuaApp {
	public class LuaBehaviourDynamicAdd : LuaBehaviour {
		public LuaInjectionData injectionData;

		public void InitLuaByTable(LuaTable luaClassTable, params object[] args) {
			if (m_State != State.UNINITIALIZED) {
				Debug.LogError("This LuaBehaviour is already initialized!");
				return;
			}
			if (luaClassTable == null) {
				Debug.LogError("LuaClassTable is null!");
				return;
			}
			LuaTable luaInstanceTable = LuaMain.Instance.FuncInvoke<LuaTable>(luaClassTable, args);
			if (luaInstanceTable == null) {
				Debug.LogError("LuaObject has not instanced!");
				Dispose();
				return;
			}
			Init(luaInstanceTable);
		}

		[ContextMenu("Inject Data")]
		protected override void InjectData() {
			base.InjectData();
			if (injectionData) {
				injectionData.ToDictTable(m_LuaTable);
			}
		}
	}
}
