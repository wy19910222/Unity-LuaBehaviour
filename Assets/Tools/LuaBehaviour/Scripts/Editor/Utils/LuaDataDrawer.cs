/*
 * @Author: wangyun
 * @CreateTime: 2022-04-29 22:01:44 907
 * @LastEditor: wangyun
 * @EditTime: 2022-12-18 01:21:43 418
 */

#if UNITY_EDITOR

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using XLua;
using LuaApp;

using UObject = UnityEngine.Object;

public static class LuaDataDrawer {
	private const string KEY_FIRST_IN_TABLE = "m_CSBehaviour";
	private const string KEY_CLASS = "class";
	private const string KEY_SUPER_CLASS = "super";
	private const string KEY_CONSTRUCTOR = "ctor";
	private const string KEY_CLASS_NAME = "className";
	private static readonly HashSet<string> KEYS_VECTOR = new HashSet<string> { "x", "y", "z", "w" };
	private static readonly HashSet<string> KEYS_COLOR = new HashSet<string> { "r", "g", "b", "a" };
	
	private static readonly GUILayoutOption WIDTH_OPTION_BTN = GUILayout.Width(80F);
	private static readonly GUILayoutOption WIDTH_OPTION_SIZE_LABEL = GUILayout.Width(28F);
	private const float WIDTH_KEY_MIN = 60F;
	// private const float WIDTH_KEY_CHAR_RATE = 6.7F;
	private const float WIDTH_KEY_VIEW_RATE_MAX = 0.4F;
	
	private const int DEPTH_MAX = 10;
	private const string HINT_DEPTH_MAX_TABLE = "层数超过上限，不再显示内部字段";
	private const string HINT_DEPTH_MAX_ARRAY = "层数超过上限，不再显示内部元素";
	private const string HINT_DURATION_MAX_TABLE = "耗时过久，不再显示内部字段";
	private const string HINT_DURATION_MAX_ARRAY = "耗时过久，不再显示内部元素";
	
	private static HashSet<LuaTable> s_TableCache;
	private static Dictionary<LuaFunction, string> s_FuncArgsMap;
	private static int s_Depth;
	
	private const long DURATION_MAX = 100 * 10000;	// Ticks / MilliSeconds
	private static long s_DrawStartTime;
	
	private static readonly GUIStyle s_Style = new GUIStyle();


	public static object DrawData([NotNull] object value, [NotNull] HashSet<LuaTable> tableCache, [NotNull] Dictionary<LuaFunction, string> funcArgsMap) {
		s_DrawStartTime = DateTime.Now.Ticks;
		s_TableCache = tableCache;
		s_FuncArgsMap = funcArgsMap;
		s_Depth = 0;
		GUILayout.BeginHorizontal();
		s_TableCache = tableCache;
		value = DrawValue(value);
		s_TableCache = null;
		GUILayout.EndHorizontal();
		s_TableCache = null;
		s_FuncArgsMap = null;
		return value;
	}

	public static void DrawTable([NotNull] LuaTable table, [NotNull] HashSet<LuaTable> tableCache, [NotNull] Dictionary<LuaFunction, string> funcArgsMap) {
		s_DrawStartTime = DateTime.Now.Ticks;
		
		s_TableCache = tableCache;
		s_FuncArgsMap = funcArgsMap;
		s_Depth = 1;
		GUILayout.BeginVertical();
		if (IsStruct(table, KEYS_VECTOR)) {
			DrawVector(table);
		} else if (IsStruct(table, KEYS_COLOR)) {
			DrawColor(table);
		} else if (IsArray(table)) {
			DrawArray(table);
		} else {
			DrawObject(table);
		}
		GUILayout.EndVertical();
		s_TableCache = null;
		s_FuncArgsMap = null;
	}
	
	private static object DrawValue(object value) {
		switch (value) {
			case int iValue:
				value = EditorGUILayout.IntField(iValue);
				break;
			case long lValue:
				value = EditorGUILayout.LongField(lValue);
				break;
			case float fValue:
				value = EditorGUILayout.FloatField(fValue);
				break;
			case double dValue:
				value = EditorGUILayout.DoubleField(dValue);
				break;
			case string sValue:
				value = EditorGUILayout.TextArea(sValue);
				break;
			case bool bValue:
				value = EditorGUILayout.Toggle(bValue);
				break;
			case Color color:
				value = EditorGUILayout.ColorField(string.Empty, color);
				break;
			case Vector2 vector2:
				value = EditorGUILayout.Vector2Field(string.Empty, vector2);
				break;
			case Vector3 vector3:
				value = EditorGUILayout.Vector3Field(string.Empty, vector3);
				break;
			case Vector4 vector4:
				value = EditorGUILayout.Vector4Field(string.Empty, vector4);
				break;
			case AnimationCurve curve:
				value = EditorGUILayout.CurveField(string.Empty, curve);
				break;
			case UObject uObj:
				value = EditorGUILayout.ObjectField(uObj, uObj.GetType(), true);
				break;
			case null:
				value = EditorGUILayout.ObjectField(null, typeof(UObject), true);
				break;
			case LuaTable tbl:
				s_TableCache.Add(tbl);
				if (IsStruct(tbl, KEYS_VECTOR)) {
					value = DrawVector(tbl);
				} else if (IsStruct(tbl, KEYS_COLOR)) {
					value = DrawColor(tbl);
				} else if (IsArray(tbl)) {
					GUILayout.Label("Size", WIDTH_OPTION_SIZE_LABEL);
					bool prevEnabled = GUI.enabled;
					GUI.enabled = false;
					EditorGUILayout.IntField(tbl.GetKeys<int>().Count());
					GUI.enabled = prevEnabled;
					
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Space(20F);
					GUILayout.BeginVertical();
					if (s_Depth < DEPTH_MAX && DateTime.Now.Ticks - s_DrawStartTime < DURATION_MAX) {
						++s_Depth;
						DrawArray(tbl);
						--s_Depth;
					} else {
						Color prevColor = GUI.contentColor;
						GUI.contentColor = Color.red;
						GUILayout.Label(s_Depth < DEPTH_MAX ? HINT_DURATION_MAX_ARRAY : HINT_DEPTH_MAX_ARRAY, EditorStyles.boldLabel);
						GUI.contentColor = prevColor;
					}
					GUILayout.EndVertical();
				} else {
					GUILayout.Label("Size", WIDTH_OPTION_SIZE_LABEL);
					bool prevEnabled = GUI.enabled;
					GUI.enabled = false;
					EditorGUILayout.IntField(tbl.GetKeys<object>().Count());
					GUI.enabled = prevEnabled;
					
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Space(20F);
					GUILayout.BeginVertical();
					if (s_Depth < DEPTH_MAX && DateTime.Now.Ticks - s_DrawStartTime < DURATION_MAX) {
						++s_Depth;
						DrawObject(tbl);
						--s_Depth;
					} else {
						Color prevColor = GUI.contentColor;
						GUI.contentColor = Color.red;
						GUILayout.Label(s_Depth < DEPTH_MAX ? HINT_DURATION_MAX_TABLE : HINT_DEPTH_MAX_TABLE, EditorStyles.boldLabel);
						GUI.contentColor = prevColor;
					}
					GUILayout.EndVertical();
				}
				break;
			case LuaFunction _:
				break;
			default:
				GUILayout.Label("[" + value.GetType() + "]");
				break;
		}
		return value;
	}

	private static void DrawObject([NotNull] LuaTable table) {
		// 自己的key
		List<object> keys = table.GetKeys<object>().ToList();
		if (keys.Count > 99) {
			return;
		}
		// 类和父类的key
		if (table.GetInPath<object>(KEY_CLASS) is LuaTable classTable) {
			int i = 0;
			while (classTable != null && i < 10) {
				foreach (object key in classTable.GetKeys<object>()) {
					if (!keys.Contains(key)) {
						keys.Add(key);
					}
				}
				classTable = classTable.GetInPath<LuaTable>(KEY_SUPER_CLASS);
				i++;
			}
		}
		// 排除元方法和构造函数
		for (int index = keys.Count - 1; index >= 0; --index) {
			switch (keys[index]) {
				case null:
					keys.RemoveAt(index);
					break;
				case string keyStr: {
					if (keyStr.StartsWith("__") || keyStr == KEY_CONSTRUCTOR) {
						keys.RemoveAt(index);
					}
					break;
				}
			}
		}
		// 排序
		keys.Sort(KeyCompare);
		
		// 计算key的最大宽度
		// float keyLength = 0;
		// ASCIIEncoding ascii = new ASCIIEncoding();
		// foreach (object key in keys) {
		// 	float _keyLength = 0;
		// 	byte[] bytes = ascii.GetBytes(key + ":");
		// 	for (int i = 0, length = bytes.Length; i < length; ++i) {
		// 		_keyLength += bytes[i] == 63 ? 2.5F : 1F;
		// 	}
		// 	keyLength = Mathf.Max(keyLength,  _keyLength);
		// }
		// float widthMax = EditorGUIUtility.currentViewWidth * WIDTH_KEY_VIEW_RATE_MAX;
		// GUILayoutOption keyWidthOption = GUILayout.Width(Mathf.Clamp(keyLength * WIDTH_KEY_CHAR_RATE, WIDTH_KEY_MIN, widthMax));
		float keyWidthMax = 50F;
		foreach (object key in keys) {
			string text;
			if (key is LuaTable _table) {
				string className = _table.Get<string>(KEY_CLASS_NAME);
				text = string.IsNullOrEmpty(className) ? "table:" : "table:" + className + ":";
			} else {
				text = key + ":";
			}
			float keyWidth = s_Style.CalcSize(new GUIContent(text)).x + 5;
			if (keyWidth > keyWidthMax) {
				keyWidthMax = keyWidth;
			}
		}
		float widthMax = EditorGUIUtility.currentViewWidth * WIDTH_KEY_VIEW_RATE_MAX;
		GUILayoutOption keyWidthOption = GUILayout.Width(Mathf.Clamp(keyWidthMax, WIDTH_KEY_MIN, widthMax));

		// 绘制
		foreach (object key in keys) {
			GUILayout.BeginHorizontal();
			
			string text;
			if (key is LuaTable _table) {
				string className = _table.Get<string>(KEY_CLASS_NAME);
				text = string.IsNullOrEmpty(className) ? "table:" : "table:" + className + ":";
			} else {
				text = key + ":";
			}
			GUILayout.Label(text, keyWidthOption);
			object value = table.Get<object, object>(key);
			if (Equals(value, table)) {
				Color prevColor = GUI.contentColor;
				GUI.contentColor = Color.red;
				GUILayout.Label("self", EditorStyles.boldLabel);
				GUI.contentColor = prevColor;
			} else if (value is LuaFunction func) {
				s_FuncArgsMap.TryGetValue(func, out string argsStr);
				if (GUILayout.Button(".Call", WIDTH_OPTION_BTN)) {
					FuncCall(func, argsStr);
				}
				if (GUILayout.Button(":Call", WIDTH_OPTION_BTN)) {
					FuncCall(func, argsStr, table);
				}
				string newArgsStr = EditorGUILayout.TextField(argsStr);
				if (newArgsStr != argsStr) {
					s_FuncArgsMap[func] = newArgsStr;
				}
			} else {
				object newValue = DrawValue(value);
				if (newValue != null && value != null ? newValue.GetHashCode() != value.GetHashCode() : newValue != value) {
					table.Set(key, newValue);
				}
			}
			
			GUILayout.EndHorizontal();
		}
	}

	private static void DrawArray(LuaTable table) {
		// 自己的key
		List<int> indexes = new List<int>(table.GetKeys<int>());
		if (indexes.Count <= 0 || indexes.Count > 99) {
			return;
		}
		
		// 排序
		indexes.Sort();
		
		// 计算index的最大宽度
		// GUILayoutOption indexWidthOption = GUILayout.Width((indexes[indexes.Count - 1] + ":").Length * WIDTH_KEY_CHAR_RATE);
		int maxIndexLength = indexes[indexes.Count - 1].ToString().Length;
		float numberWidth = s_Style.CalcSize(new GUIContent("5")).x;
		GUILayoutOption indexWidthOption = GUILayout.Width(numberWidth * maxIndexLength + 8);
		
		// 绘制
		foreach (int index in indexes) {
			GUILayout.BeginHorizontal();
			
			GUILayout.Label(index + ":", indexWidthOption);
			object value = table.Get<int, object>(index);
			if (Equals(value, table)) {
				Color prevColor = GUI.contentColor;
				GUI.contentColor = Color.red;
				GUILayout.Label("self", EditorStyles.boldLabel);
				GUI.contentColor = prevColor;
			} else if (value is LuaFunction func) {
				s_FuncArgsMap.TryGetValue(func, out string argsStr);
				if (GUILayout.Button(".Call", WIDTH_OPTION_BTN)) {
					FuncCall(func, argsStr);
				}
				if (GUILayout.Button(":Call", WIDTH_OPTION_BTN)) {
					FuncCall(func, argsStr, table);
				}
				string newArgsStr = EditorGUILayout.TextField(argsStr);
				if (newArgsStr != argsStr) {
					s_FuncArgsMap[func] = newArgsStr;
				}
			} else {
				object newValue = DrawValue(value);
				if (newValue != null && value != null ? newValue.GetHashCode() != value.GetHashCode() : newValue != value) {
					table.Set(index, newValue);
				}
			}
			
			GUILayout.EndHorizontal();
		}
	}

	private static object DrawVector(LuaTable table) {
		(bool hasW, bool hasZ) = (false, false);
		foreach (string k in table.GetKeys<string>()) {
			if (k == "z") {
				hasZ = true;
			}
			if (k == "w") {
				hasW = true;
				hasZ = true;
				break;
			}
		}
		Vector4 vector4 = new Vector4();
		TryToSingle(table.Get<object>("x"), out vector4.x, 0);
		TryToSingle(table.Get<object>("y"), out vector4.y, 0);
		if (!hasZ) {
			return EditorGUILayout.Vector2Field(string.Empty, vector4);
		}
		TryToSingle(table.Get<object>("z"), out vector4.z, 0);
		if (!hasW) {
			return EditorGUILayout.Vector3Field(string.Empty, vector4);
		}
		TryToSingle(table.Get<object>("w"), out vector4.w, 0);
		return EditorGUILayout.Vector4Field(string.Empty, vector4);
	}

	private static object DrawColor(LuaTable table) {
		Color color = new Color();
		TryToSingle(table.Get<object>("r"), out color.r, 0);
		TryToSingle(table.Get<object>("g"), out color.g, 0);
		TryToSingle(table.Get<object>("b"), out color.b, 0);
		TryToSingle(table.Get<object>("a"), out color.a, 1);
		return EditorGUILayout.ColorField(string.Empty, color);
	}
	
	private static bool IsArray(LuaTable table) {
		foreach (object k in table.GetKeys<object>()) {
			if (!(k is long)) {
				return false;
			}
		}
		return true;
	}

	private static bool IsStruct(LuaTable table, ICollection<string> structKeys) {
		bool ret = false;
		foreach (object k in table.GetKeys<object>()) {
			if (!(k is string) || !structKeys.Contains(k.ToString())) {
				return false;
			}
			ret = true;
		}
		return ret;
	}
	
	private static void TryToSingle(object obj, out float value, float defaultValue) {
		try {
			value = Convert.ToSingle(obj);
		} catch (Exception) {
			value = defaultValue;
		}
	}

	private static void FuncCall(LuaFunction func, string argsStr, LuaTable self = null) {
		List<object> argList = self is null ? new List<object>() : new List<object>{self};
		object[] args = LuaMain.Instance.LuaEnv.DoString("return " + argsStr, "LuaDataDrawer");
		if (args != null) {
			argList.AddRange(args);
		}
		func.Call(argList.ToArray());
	}
	
	private static int KeyCompare(object keyObj1, object keyObj2) {
		string key1 = keyObj1.ToString();
		string key2 = keyObj2.ToString();
		if (key1 == KEY_FIRST_IN_TABLE) {
			return -1;
		}
		if (key2 == KEY_FIRST_IN_TABLE) {
			return 1;
		}
		bool isUInt1 = Regex.IsMatch(key1, @"^\d+$");
		bool isUInt2 = Regex.IsMatch(key2, @"^\d+$");
		if (isUInt1) {
			if (isUInt2) {
				return int.Parse(key1) - int.Parse(key2);
			}
			return -1;
		} else {
			if (isUInt2) {
				return -1;
			}
			return string.CompareOrdinal(key1, key2);
		}
	}
}

#endif