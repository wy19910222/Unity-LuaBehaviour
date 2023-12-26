/*
 * @Author: wangyun
 * @CreateTime: 2023-11-28 03:42:41 138
 * @LastEditor: wangyun
 * @EditTime: 2023-11-28 03:42:41 143
 */

using System.Collections.Generic;
using XLua;
using UObject = UnityEngine.Object;

namespace LuaApp {
	public static class InjectionConverter {
		public static LuaTable ToDictTable<T>(List<T> array, LuaTable table = null) where T : Injection {
			if (table == null) {
				table = LuaMain.Instance.LuaEnv.NewTable();
			}
			foreach (var injection in array) {
				// 正常情况下Type为Space时，Name必定为空，所以先判断Name可以短路后面的判断
				if (!string.IsNullOrEmpty(injection.Name) && injection.Type != InjectionType.Space) {
					table.Set(injection.Name, ToLuaValue(injection));
				}
			}
			return table;
		}

		private static LuaTable ToDictTable(object array) {
			switch (array) {
				case List<Injection6> list6:
					return ToDictTable(list6);
				case List<Injection5> list5:
					return ToDictTable(list5);
				case List<Injection4> list4:
					return ToDictTable(list4);
				case List<Injection3> list3:
					return ToDictTable(list3);
				case List<Injection2> list2:
					return ToDictTable(list2);
				case List<Injection1> list1:
					return ToDictTable(list1);
				case List<Injection> list:
					return ToDictTable(list);
			}
			return null;
		}

		public static LuaTable ToListTable<T>(List<T> array, LuaTable table = null) where T : Injection {
			if (table == null) {
				table = LuaMain.Instance.LuaEnv.NewTable();
			}
			for (int index = 0, realIndex = 0, length = array.Count; index < length; ++index) {
				Injection injection = array[index];
				if (injection.Type != InjectionType.Space) {
					table.Set(++realIndex, ToLuaValue(injection));
				}
			}
			return table;
		}

		private static LuaTable ToListTable(object array) {
			switch (array) {
				case List<Injection6> list6:
					return ToListTable(list6);
				case List<Injection5> list5:
					return ToListTable(list5);
				case List<Injection4> list4:
					return ToListTable(list4);
				case List<Injection3> list3:
					return ToListTable(list3);
				case List<Injection2> list2:
					return ToListTable(list2);
				case List<Injection1> list1:
					return ToListTable(list1);
				case List<Injection> list:
					return ToListTable(list);
			}
			return null;
		}

		public static object ToLuaValue(Injection injection) {
			object luaValue = injection.Value;
			InjectionType itemType = injection.Type;
			switch (itemType) {
				case InjectionType.Dict:
					luaValue = ToDictTable(luaValue);
					break;
				case InjectionType.List:
					luaValue = ToListTable(luaValue);
					break;
				default: {
					if (luaValue is UObject obj) {
						if (!obj) {
							luaValue = null;
						} else if (itemType == InjectionType.LuaTable) {
							switch (luaValue) {
								case LuaInjectionData injectionData:
									luaValue = injectionData.Data;
									break;
								case LuaScriptableData scriptableData:
									luaValue = scriptableData.Data;
									break;
								case LuaBehaviour behaviour:
									luaValue = behaviour.LuaTable;
									break;
							}
						}
					}
					break;
				}
			}
			return luaValue;
		}
	}
}
