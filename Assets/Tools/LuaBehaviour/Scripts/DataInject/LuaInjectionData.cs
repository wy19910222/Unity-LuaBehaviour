/*
 * @Author: wangyun
 * @CreateTime: 2022-02-20 17:14:27 764
 * @LastEditor: wangyun
 * @EditTime: 2022-07-02 19:17:35 154
 */

using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace CSLike {
	public class LuaInjectionData : MonoBehaviour {
#if UNITY_EDITOR
		// ReSharper disable once NotAccessedField.Local
		[SerializeField] private string m_LuaClassName;
#endif
		
		[SerializeField] private List<Injection7> m_InjectionList = new List<Injection7>();

		public void Clear() => m_InjectionList.Clear();

		public LuaTable Data => ToDictTable();
		
		public LuaTable ToDictTable(LuaTable table = null) {
			return InjectionConverter.ToDictTable(m_InjectionList, table);
		}
	}
}
