/*
 * @Author: wangyun
 * @CreateTime: 2022-12-16 13:07:10 798
 * @LastEditor: wangyun
 * @EditTime: 2022-12-16 13:07:10 803
 */

#if UNITY_EDITOR

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using CSLike;

using UObject = UnityEngine.Object;

public static class LuaInjectionDataDeduce {
	private const string LUA_SRC_PATH = "Assets/Basic/Scripts/Lua/";
	private const string LUA_FILE_EXT = ".lua";
	private const string BASE_CLASS_NAME = "CSLike.Object";
	
	private const string KEYWORD_STATIC = "@static";
	private const string KEYWORD_PROPERTY = "@property";
	private const string KEYWORD_IGNORE = "@ignore";
	private const string KEYWORD_COMPONENT = "@component";
	
	// [a-zA-Z_]\w*
	private const string PATTERN_VAR_NAME = @"[a-zA-Z_]\w*";
	// return [a-zA-Z_]\w*;
	private const string PATTERN_RETURN = @"(?<=^return\s+)" + PATTERN_VAR_NAME + @"(?=\s*;*$)";
	// table<number, T>
	private const string PATTERN_NUMBER_DICT = @"(?<=^table\s*<\s*number\s*,\s*).+(?=\s*>$)";
	// table<string, T>
	private const string PATTERN_STRING_DICT = @"(?<=^table\s*<\s*string\s*,\s*).+(?=\s*>$)";

	private static readonly List<string[]> m_AllLuaLinesCache = new List<string[]>();

	/// <summary>
	/// 基于EmmyLua的注解格式，分析推断成员变量并生成结构
	/// </summary>
	/// <param name="luaClassName">lua require路径</param>
	/// <param name="injections">结构生成在这里</param>
	public static void DeduceFieldsByClassName(List<Injection7> injections, string luaClassName) {
		string[] luaClassDeclare = GetGlobalLuaClassDeclare(luaClassName);
		if (luaClassDeclare != null) {
			ParseClass(injections, luaClassDeclare);
		}
		m_AllLuaLinesCache.Clear();
		Debug.Log("Deducing complete!");
	}

	/// <summary>
	/// 基于EmmyLua的注解格式，分析推断成员变量并生成结构
	/// </summary>
	/// <param name="luaPath">lua require路径</param>
	/// <param name="injections">结构生成在这里</param>
	public static void DeduceFields(List<Injection7> injections, string luaPath) {
		string luaFilePath = LUA_SRC_PATH + (luaPath?.Replace(".", "/") ?? string.Empty) + LUA_FILE_EXT;
		string luaStr = GetTextContent(luaFilePath);
		string[] pathParts = luaFilePath.Split('.');
		if (pathParts.Length > 1) {
			string[] luaStrLines = luaStr.Replace("\r\n", "\n").Trim().Split('\n');
			int lineCount = luaStrLines.Length;
			string lastLineStr = luaStrLines[lineCount - 1].Trim();
			// 找到最后return的变量名称
			string classLocalName = Regex.Match(lastLineStr, PATTERN_RETURN).Value;
			if (!string.IsNullOrEmpty(classLocalName)) {
				// 找到声明它的位置
				Regex declareLocalNameRegex = new Regex(@"^local\s+" + classLocalName);
				int declareLocalNameIndex = Array.FindIndex(luaStrLines, lineStr => declareLocalNameRegex.IsMatch(lineStr));
				if (declareLocalNameIndex != -1) {
					// 找到---@class的位置
					Regex classDeclareRegex = new Regex(@"^---@class\s+");
					int declareClassIndex = declareLocalNameIndex - 1;
					while (declareClassIndex >= 0) {
						string lineStr = luaStrLines[declareClassIndex];
						if (classDeclareRegex.IsMatch(lineStr)) {
							break;
						}
						if (!lineStr.StartsWith("---")) {
							declareClassIndex = 0;
						}
						--declareClassIndex;
					}
					if (declareClassIndex != -1) {
						// 解析注解
						ParseClass(injections, luaStrLines.Skip(declareClassIndex).Take(declareLocalNameIndex - declareClassIndex).ToArray());
					}
				}
			}
		}
		m_AllLuaLinesCache.Clear();
		Debug.Log("Deducing complete!");
	}

	/// <summary>
	/// 返回函数名列表
	/// </summary>
	/// <param name="luaPath">lua require路径</param>
	public static string[] GetMethodNames(string luaPath) {
		string luaFilePath = LUA_SRC_PATH + (luaPath?.Replace(".", "/") ?? string.Empty) + LUA_FILE_EXT;
		string luaStr = GetTextContent(luaFilePath);
		string[] pathParts = luaFilePath.Split('.');
		if (pathParts.Length > 1) {
			string[] luaStrLines = luaStr.Replace("\r\n", "\n").Trim().Split('\n');
			int lineCount = luaStrLines.Length;
			string lastLineStr = luaStrLines[lineCount - 1].Trim();
			// 找到最后return的变量名称
			string classLocalName = Regex.Match(lastLineStr, PATTERN_RETURN).Value;
			if (!string.IsNullOrEmpty(classLocalName)) {
				List<string> methodNames = new List<string>();
				string pattern = @"(?<=(\s|^)function\s+" + classLocalName + @"\s*:\s*)" + PATTERN_VAR_NAME + @"(?=\s*\()";
				Regex funcNameRegex = new Regex(pattern);
				foreach (var luaStrLine in luaStrLines) {
					string methodName = funcNameRegex.Match(luaStrLine).Value;
					if (!string.IsNullOrEmpty(methodName)) {
						methodNames.Add(methodName);
					}
				}
				return methodNames.ToArray();
			}
		}
		return Array.Empty<string>();
	}

	/// <summary>
	/// 返回成员变量名列表
	/// </summary>
	/// <param name="luaPath">lua require路径</param>
	/// <param name="includePropertyAndIgnore">是否包含被{KEYWORD_PROPERTY}或{KEYWORD_IGNORE}修饰的字段</param>
	public static string[] GetFieldNames(string luaPath, bool includePropertyAndIgnore = false) {
		string luaFilePath = LUA_SRC_PATH + (luaPath?.Replace(".", "/") ?? string.Empty) + LUA_FILE_EXT;
		string luaStr = GetTextContent(luaFilePath);
		string[] pathParts = luaFilePath.Split('.');
		if (pathParts.Length > 1) {
			string[] luaStrLines = luaStr.Replace("\r\n", "\n").Trim().Split('\n');
			int lineCount = luaStrLines.Length;
			string lastLineStr = luaStrLines[lineCount - 1].Trim();
			// 找到最后return的变量名称
			string classLocalName = Regex.Match(lastLineStr, PATTERN_RETURN).Value;
			if (!string.IsNullOrEmpty(classLocalName)) {
				// 找到声明它的位置
				Regex declareLocalNameRegex = new Regex(@"^local\s+" + classLocalName);
				int declareLocalNameIndex = Array.FindIndex(luaStrLines, lineStr => declareLocalNameRegex.IsMatch(lineStr));
				if (declareLocalNameIndex != -1) {
					// 找到---@class的位置
					Regex classDeclareRegex = new Regex(@"^---@class\s+");
					int declareClassIndex = declareLocalNameIndex - 1;
					while (declareClassIndex >= 0) {
						string lineStr = luaStrLines[declareClassIndex];
						if (classDeclareRegex.IsMatch(lineStr)) {
							break;
						}
						if (!lineStr.StartsWith("---")) {
							declareClassIndex = 0;
						}
						--declareClassIndex;
					}
					if (declareClassIndex != -1) {
						List<string> fieldNames = new List<string>();
						Regex separatorRegex = new Regex(@"\s+");
						for (int index = declareClassIndex + 1; index < declareLocalNameIndex; ++index) {
							string[] lineStrParts = separatorRegex.Split(luaStrLines[index]);
							if (lineStrParts[0] == "---@field" && !lineStrParts.Contains(KEYWORD_STATIC) &&
									(!includePropertyAndIgnore || !lineStrParts.Contains(KEYWORD_PROPERTY) && !lineStrParts.Contains(KEYWORD_IGNORE))) {
								if (lineStrParts[1] != "private" && lineStrParts[1] != "protected") {
									int indexOffset = lineStrParts[1] == "public" ? 1 : 0;
									string fieldName = lineStrParts[indexOffset + 1];
									fieldNames.Add(fieldName);
								}
							}
						}
						return fieldNames.ToArray();
					}
				}
			}
		}
		return Array.Empty<string>();
	}
	
	private static void ParseClass(object array, IReadOnlyList<string> lines) {
		Regex separatorRegex = new Regex(@"\s+");
		string[] classDeclareParts = separatorRegex.Split(lines[0]);
		if (classDeclareParts.Length >= 4 && classDeclareParts[2] == ":") {
			// 有父类，就先把父类的成员变量加进来
			string superClassName = classDeclareParts[3];
			if (superClassName != BASE_CLASS_NAME) {
				string[] luaClassDeclare = GetGlobalLuaClassDeclare(classDeclareParts[3]);
				if (luaClassDeclare != null) {
					ParseClass(array, luaClassDeclare);
				}
			}
		}
		// 解析成员变量
		string className = classDeclareParts[1];
		for (int index = 1, length = lines.Count; index < length; ++index) {
			string[] lineStrParts = separatorRegex.Split(lines[index]);
			if (lineStrParts[0] == "---@field" && !lineStrParts.Contains(KEYWORD_STATIC) && !lineStrParts.Contains(KEYWORD_PROPERTY) && !lineStrParts.Contains(KEYWORD_IGNORE)) {
				if (lineStrParts[1] != "private" && lineStrParts[1] != "protected") {
					int indexOffset = lineStrParts[1] == "public" ? 1 : 0;
					string fieldName = lineStrParts[indexOffset + 1];
					string typeStr = GetTypeStr(lineStrParts.Skip(indexOffset + 2).ToArray(), " ");
					// 过滤掉类型是自己的成员变量
					if (typeStr != className) {
						SetInjection(array, fieldName, typeStr, lineStrParts.Contains(KEYWORD_COMPONENT));
					}
				}
			}
		}
	}

	private static string[] GetGlobalLuaClassDeclare(string className) {
		Regex superClassDeclareRegex = new Regex(@"---@class\s+" + className + @"([\s:]|$)");
		foreach (var luaStrLines in GetAllLuaLinesCache()) {
			int lineCount = luaStrLines.Length;
			int declareClassBegin = -1;
			for (int lineIndex = 0; lineIndex < lineCount; ++lineIndex) {
				if (superClassDeclareRegex.IsMatch(luaStrLines[lineIndex])) {
					// Debug.Log("Find class: " + luaStrLines[lineIndex]);
					declareClassBegin = lineIndex;
					break;
				}
			}
			if (declareClassBegin != -1) {
				int declareClassEnd = lineCount;
				for (int lineIndex = declareClassBegin + 1; lineIndex < lineCount; ++lineIndex) {
					if (!luaStrLines[lineIndex].StartsWith("---")) {
						declareClassEnd = lineIndex;
						break;
					}
				}
				return luaStrLines.Skip(declareClassBegin).Take(declareClassEnd - declareClassBegin).ToArray();
			}
		}
		return null;
	}

	private static void SetInjection<T>([NotNull] List<T> array, string fieldName, string typeStr, bool isLuaTable) where T : Injection, new() {
		if (typeStr.StartsWith("(") && typeStr.EndsWith(")")) {
			typeStr = typeStr.Substring(1, typeStr.Length - 2);
		}
		typeStr = GetTypeStr(typeStr.Split('|'), "|").Trim();

		List<T> injections;
		if (fieldName == null) {
			// 数组元素
			injections = array;
			if (injections.Count <= 0) {
				injections.Add(new T());
			}
		} else {
			// 字典元素
			injections = array.FindAll(_t => _t.Name == fieldName);
			if (injections.Count <= 0) {
				T t1 = new T { Name = fieldName };
				injections.Add(t1);
				array.Add(t1);
			}
		}
		
		foreach (var injection in injections) {
			injection.Constraint = null;
			switch (typeStr) {
				case "string":
					injection.Type = InjectionType.String;
					continue;
				case "number":
					injection.Type = InjectionType.Float;
					continue;
				// 需要在lua端先声明---@alias int number，再用int去声明字段类型
				case "int":
					injection.Type = InjectionType.Int;
					continue;
				case "boolean":
					injection.Type = InjectionType.Boolean;
					continue;
				case "UnityEngine.Color":
					injection.Type = InjectionType.Color;
					continue;
				case "UnityEngine.Vector2":
					injection.Type = InjectionType.Vector2;
					continue;
				case "UnityEngine.Vector3":
					injection.Type = InjectionType.Vector3;
					continue;
				case "UnityEngine.Vector4":
					injection.Type = InjectionType.Vector4;
					continue;
				case "UnityEngine.AnimationCurve":
					injection.Type = InjectionType.Curve;
					continue;
				case "UnityEngine.GameObject":
					injection.Type = InjectionType.GameObject;
					continue;
				case "UnityEngine.Transform":
					injection.Type = InjectionType.Transform;
					continue;
				case "table":
					injection.Type = isLuaTable ? InjectionType.LuaTable : InjectionType.Dict;
					continue;
				default: {
					// typeStr[]
					if (typeStr.EndsWith("[]")) {
						injection.Type = InjectionType.List;
						if (injection.Value == null) {
							CreateList(injection);
						}
						SetInjection(injection.Value, null, typeStr.Substring(0, typeStr.Length - 2).Trim(), isLuaTable);
						continue;
					}
					// table<number, listItemType>
					string listItemTypeStr = Regex.Match(typeStr, PATTERN_NUMBER_DICT).Value;
					if (!string.IsNullOrEmpty(listItemTypeStr)) {
						injection.Type = InjectionType.List;
						if (injection.Value == null) {
							CreateList(injection);
						}
						SetInjection(injection.Value, null, listItemTypeStr, isLuaTable);
						continue;
					}
					// table<string, dictValueType>
					string tableValueTypeStr = Regex.Match(typeStr, PATTERN_STRING_DICT).Value;
					if (!string.IsNullOrEmpty(tableValueTypeStr)) {
						if (isLuaTable) {
							injection.Type = InjectionType.LuaTable;
						} else {
							injection.Type = InjectionType.Dict;
							if (injection.Value == null) {
								CreateList(injection);
							}
							SetInjection(injection.Value, null, tableValueTypeStr);
						}
						continue;
					}
					// 外面套{}的只可能是这种形式：{value:type1, value2:type2, value3:type3}
					if (typeStr.StartsWith("{") && typeStr.EndsWith("}")) {
						if (isLuaTable) {
							injection.Type = InjectionType.LuaTable;
						} else {
							injection.Type = InjectionType.Dict;
							if (injection.Value == null) {
								CreateList(injection);
							}
							List<string> pairs = GetTypeStrs(typeStr.Substring(1, typeStr.Length - 2).Split(','), ",");
							foreach (string pair in pairs) {
								string[] parts = pair.Trim().Split(':');
								SetInjection(injection.Value, parts[0].Trim(), parts[1].Trim());
							}
						}
						continue;
					}
					// 可能是C#类，反射生成Type
					Type type = GetTypeByName(typeStr);
					if (type != null) {
						if (type.IsEnum) {
							injection.Type = InjectionType.Int;
							injection.Constraint = typeStr;
						} else if (typeof(Behaviour).IsAssignableFrom(type)) {
							injection.Type = InjectionType.Behaviour;
							injection.Constraint = typeStr;
						} else if (typeof(Component).IsAssignableFrom(type)) {
							injection.Type = InjectionType.OtherComp;
							injection.Constraint = typeStr;
						} else if (typeof(UObject).IsAssignableFrom(type)) {
							injection.Type = InjectionType.Object;
							injection.Constraint = typeStr;
						} else {
							injection.Type = InjectionType.String;
						}
						continue;
					}
					// 可能是lua类
					string[] luaClassDeclare = GetGlobalLuaClassDeclare(typeStr);
					if (luaClassDeclare != null) {
						if (isLuaTable) {
							injection.Type = InjectionType.LuaTable;
						} else {
							injection.Type = InjectionType.Dict;
							if (injection.Value == null) {
								CreateList(injection);
							}
							ParseClass(injection.Value, luaClassDeclare);
						}
						continue;
					}
					// 未知类型
					injection.Type = InjectionType.String;
					injection.Value = typeStr;
					injection.Constraint = null;
					continue;
				}
			}
		}
	}
	private static Type GetTypeByName(string typeName) {
		if (!string.IsNullOrEmpty(typeName)) {
			// 遍历所有程序集寻找类
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies) {
				Type type = assembly.GetType(typeName);
				if (type != null) {
					return type;
				}
			}
			// 可能是内部类，那么外部类和内部类之间的点需要改成加号
			int dotIndex = typeName.LastIndexOf('.');
			if (dotIndex != -1) {
				string nestedClassName = typeName.Substring(0, dotIndex) + "+" + typeName.Substring(dotIndex + 1);
				foreach (Assembly assembly in assemblies) {
					Type type = assembly.GetType(nestedClassName);
					if (type != null) {
						return type;
					}
				}
			}
		}
		return null;
	}

	private static void SetInjection(object array, string fieldName, string typeStr, bool isLuaTable = false) {
		switch (array) {
			case List<Injection7> list7:
				SetInjection(list7, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection6> list6:
				SetInjection(list6, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection5> list5:
				SetInjection(list5, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection4> list4:
				SetInjection(list4, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection3> list3:
				SetInjection(list3, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection2> list2:
				SetInjection(list2, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection1> list1:
				SetInjection(list1, fieldName, typeStr, isLuaTable);
				break;
			case List<Injection> list:
				SetInjection(list, fieldName, typeStr, isLuaTable);
				break;
		}
	}

	private static void CreateList<T>([NotNull] T t) where T : Injection {
		switch (t) {
			case Injection<Injection6> _:
				t.Value = new List<Injection6>();
				break;
			case Injection<Injection5> _:
				t.Value = new List<Injection5>();
				break;
			case Injection<Injection4> _:
				t.Value = new List<Injection4>();
				break;
			case Injection<Injection3> _:
				t.Value = new List<Injection3>();
				break;
			case Injection<Injection2> _:
				t.Value = new List<Injection2>();
				break;
			case Injection<Injection1> _:
				t.Value = new List<Injection1>();
				break;
			case Injection<Injection> _:
				t.Value = new List<Injection>();
				break;
		}
	}

	private static string GetTypeStr(IReadOnlyList<string> multiTypeStrParts, string joinSeparator) {
		string typeStr = multiTypeStrParts[0];
		// 判断typeStr是不是完整，如果不完整，则往后拼
		int curlyBracketsCount = GetDeltaCharCount(typeStr, '{', '}');
		int angleBracketsCount = GetDeltaCharCount(typeStr, '<', '>');
		int squareBracketsCount = GetDeltaCharCount(typeStr, '[', ']');
		int roundBracketsCount = GetDeltaCharCount(typeStr, '(', ')');
		int partCount = multiTypeStrParts.Count;
		for (int i = 1; (curlyBracketsCount != 0 || angleBracketsCount != 0 || squareBracketsCount != 0 || roundBracketsCount != 0) && i < partCount; i++) {
			string nextPart = multiTypeStrParts[i];
			curlyBracketsCount += GetDeltaCharCount(nextPart, '{', '}');
			angleBracketsCount += GetDeltaCharCount(nextPart, '<', '>');
			squareBracketsCount += GetDeltaCharCount(nextPart, '[', ']');
			roundBracketsCount += GetDeltaCharCount(nextPart, '(', ')');
			typeStr += joinSeparator + nextPart;
		}
		return typeStr;
	}

	private static List<string> GetTypeStrs(IReadOnlyList<string> multiTypeStrParts, string joinSeparator) {
		// 判断typeStr是不是完整，如果不完整，则往后拼
		List<string> typeStrs = new List<string>();
		string tempStr = null;
		int curlyBracketsCount = 0;
		int angleBracketsCount = 0;
		int squareBracketsCount = 0;
		int roundBracketsCount = 0;
		int partCount = multiTypeStrParts.Count;
		for (int i = 0; i < partCount; i++) {
			string nextPart = multiTypeStrParts[i];
			curlyBracketsCount += GetDeltaCharCount(nextPart, '{', '}');
			angleBracketsCount += GetDeltaCharCount(nextPart, '<', '>');
			squareBracketsCount += GetDeltaCharCount(nextPart, '[', ']');
			roundBracketsCount += GetDeltaCharCount(nextPart, '(', ')');
			tempStr = tempStr == null ? nextPart : tempStr + joinSeparator + nextPart;
			if (curlyBracketsCount == 0 && angleBracketsCount == 0 && squareBracketsCount == 0 && roundBracketsCount == 0) {
				typeStrs.Add(tempStr);
				tempStr = null;
				curlyBracketsCount = 0;
				angleBracketsCount = 0;
				squareBracketsCount = 0;
				roundBracketsCount = 0;
			}
		}
		return typeStrs;
	}
	
	private static int GetDeltaCharCount(string str, char char1, char char2) {
		int charCount1 = 0;
		int charCount2 = 0;
		char[] chars = str.ToCharArray();
		for (int i = 0, length = chars.Length; i < length; ++i) {
			char c = chars[i];
			if (c == char1) {
				charCount1++;
			} else if (c == char2) {
				charCount2++;
			}
		}
		return charCount1 - charCount2;
	}

	private static List<string[]> GetAllLuaLinesCache() {
		if (m_AllLuaLinesCache.Count == 0) {
			string[] filePaths = Directory.GetFiles(LUA_SRC_PATH, "*" + LUA_FILE_EXT, SearchOption.AllDirectories);
			foreach (var filePath in filePaths) {
				string luaStr = GetTextContent(filePath);
				string[] luaStrLines = luaStr.Replace("\r\n", "\n").Trim().Split('\n');
				m_AllLuaLinesCache.Add(luaStrLines);
			}
		}
		return m_AllLuaLinesCache;
	}

	private static string GetTextContent(string path) {
		string text;
		FileInfo file = new FileInfo(path);
		using (FileStream fs = file.OpenRead()) {
			using (MemoryStream ms = new MemoryStream()) {
				var bytesTemp = new byte[4096];
				int readLength;
				while ((readLength = fs.Read(bytesTemp, 0, 4096)) > 0) {
					ms.Write(bytesTemp, 0, readLength);
				}
				ms.Flush();
				text = Encoding.UTF8.GetString(ms.ToArray());
			}
		}
		return text;
	}
}

#endif