/*
 * @Author: wangyun
 * @CreateTime: 2022-03-09 18:52:05 140
 * @LastEditor: wangyun
 * @EditTime: 2023-01-25 23:55:44 063
 */

using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace LuaApp {
	[CreateAssetMenu(menuName="LuaScriptableData", fileName="LuaData")]
	public class LuaScriptableData : ScriptableObject {
#if UNITY_EDITOR
		// ReSharper disable once NotAccessedField.Local
		[SerializeField] private string m_LuaClassName;
#endif
		
		[SerializeField] private List<Injection7> m_InjectionList = new List<Injection7>();

		public LuaTable Data => ToDictTable();
		
		public LuaTable ToDictTable(LuaTable table = null) {
			return InjectionConverter.ToDictTable(m_InjectionList, table);
		}
	}
}
