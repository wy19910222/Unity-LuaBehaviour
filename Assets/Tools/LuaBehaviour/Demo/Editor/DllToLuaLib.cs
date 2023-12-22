using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;

public class DllToLuaLib : Editor {
	private static readonly string[] LUA_KEYWORDS = { "function", "end", "local", "nil", "and", "or", "not", "then", "elseif", "repeat", "until" };
	private static readonly Assembly[] EXT_ASSEMBLIES = { typeof(AssetDatabase).Assembly };
	private static readonly string OUTPUT_PATH = Application.dataPath + "/../LuaLib";

	[MenuItem("XLua/EmmyLua/DllToLuaLib/IDEA", false, -100)]
	private static void GenLuaLibZip() {
		GenLuaLib(true);
	}

	[MenuItem("XLua/EmmyLua/DllToLuaLib/Rider | VSCode | Other", false, -100)]
	private static void GenLuaLibFolder() {
		GenLuaLib(false);
	}

	private static void GenLuaLib(bool compressToZip) {
		Dictionary<string, List<Type>> dllNamesDict = new Dictionary<string, List<Type>>();
		List<MethodInfo> allExtensionMethodList = new List<MethodInfo>();

		// 通过LuaCallCSharp里的所有类找到对应的程序集
		// 不直接获取类是因为，程序集中的其他类，虽然不会被lua调用到，但是偶尔需要二次跳转查看。r
		CSObjectWrapEditor.Generator.GetGenConfig(XLua.Utils.GetAllTypes());
		foreach (Type type in CSObjectWrapEditor.Generator.LuaCallCSharp) {
			string assemblyName = type.Assembly.GetName().Name;
			if (!dllNamesDict.TryGetValue(assemblyName, out List<Type> _)) {
				dllNamesDict[assemblyName] = new List<Type>();
			}
		}
		foreach (string assemblyName in dllNamesDict.Keys) {
			Assembly assembly = Assembly.Load(assemblyName);
			Type[] assemblyTypes = assembly.GetTypes();
			List<Type> types = dllNamesDict[assemblyName];
			foreach (Type type in assemblyTypes) {
				if (assemblyName == "Assembly-CSharp"
						&& type.Namespace != null && type.FullName != null
						&& (type.Namespace.Contains("XLua.CSObjectWrap") || type.FullName.Contains("XLua.ObjectTranslator"))) {
					continue;
				}
				types.Add(type);
				if (type.IsDefined(typeof(ExtensionAttribute), false)) {
					MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
					foreach (MethodInfo method in methods) {
						if (method.IsDefined(typeof(ExtensionAttribute), false)) {
							allExtensionMethodList.Add(method);
						}
					}
				}
			}
		}

		// ScriptAssemblies目录下的dll
		List<string> dllNames = new List<string> { "mscorlib" };
		string[] files = Directory.GetFiles(Environment.CurrentDirectory + "\\Library\\ScriptAssemblies", "*.dll");
		foreach (string filePath in files) {
			if (filePath.EndsWith(".dll") && !filePath.Contains("Editor") && !filePath.Contains("editor")) {
				int startIndex = filePath.LastIndexOf("\\") + 1;
				int length = filePath.Length - startIndex - 4; // ".dll".Length
				dllNames.Add(filePath.Substring(startIndex, length));
			}
		}
		foreach (string dllName in dllNames) {
			if (!dllNamesDict.ContainsKey(dllName)) {
				Assembly assembly = null;
				try {
					assembly = Assembly.Load(dllName);
				} catch (FileNotFoundException) {
				}
				if (assembly != null) {
					Type[] types = assembly.GetTypes();
					dllNamesDict[dllName] = new List<Type>(types);
					foreach (Type type in types) {
						if (type.IsDefined(typeof(ExtensionAttribute), false)) {
							MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
							foreach (MethodInfo method in methods) {
								if (method.IsDefined(typeof(ExtensionAttribute), false)) {
									allExtensionMethodList.Add(method);
								}
							}
						}
					}
				} else {
					Debug.LogError(dllName + " assembly is not exist!");
				}
			}
		}

		// EXT_ASSEMBLIES包含的程序集
		foreach (Assembly assembly in EXT_ASSEMBLIES) {
			string assemblyName = assembly.GetName().Name;
			if (!dllNamesDict.ContainsKey(assemblyName)) {
				List<Type> types = new List<Type>();
				dllNamesDict[assemblyName] = types;
				Type[] assemblyTypes = assembly.GetTypes();
				foreach (Type type in assemblyTypes) {
					types.Add(type);
					if (type.IsDefined(typeof(ExtensionAttribute), false)) {
						MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
						foreach (MethodInfo method in methods) {
							if (method.IsDefined(typeof(ExtensionAttribute), false)) {
								allExtensionMethodList.Add(method);
							}
						}
					}
				}
			}
		}

		foreach (string dllName in dllNamesDict.Keys) {
			Dictionary<string, byte[]> fileDict = new Dictionary<string, byte[]>();
			List<Type> types = dllNamesDict[dllName];
			foreach (Type type in types) {
				GenType(type, GetExtensionMethods(type, allExtensionMethodList), out string fileName, out string content);
				fileDict[fileName] = Encoding.UTF8.GetBytes(content);

				Type baseType = type.BaseType;
				while (baseType != null && baseType.IsGenericType) {
					GenType(baseType, GetExtensionMethods(baseType, allExtensionMethodList), out string baseFileName, out string baseContent);
					fileDict[baseFileName] = Encoding.UTF8.GetBytes(baseContent);
					baseType = baseType.BaseType;
				}
			}

			HashSet<string> nameSpaceSet = new HashSet<string>();
			foreach (Type type in types) {
				string nameSpace = type.Namespace;
				if (nameSpace != null && !nameSpaceSet.Contains(nameSpace)) {
					nameSpaceSet.Add(nameSpace);
				}
			}

			foreach (string nameSpace in nameSpaceSet) {
				string fileName = nameSpace + ".ns.lua";
				StringBuilder contentSb = new StringBuilder();
				contentSb.Append("---@class ");
				contentSb.Append(nameSpace);
				contentSb.AppendLine();
				contentSb.AppendLine("local m = {}");
				
				contentSb.AppendLine();
				
				string[] typeNameParts = nameSpace.Split('.');
				int partLength = typeNameParts.Length;
				if (partLength == 1) {
					contentSb.Append(nameSpace);
					contentSb.AppendLine(" = m");
				} else {
					for (int index = 0; index < partLength; ++index) {
						if (index == 0) {
							contentSb.Append(typeNameParts[index]);
							contentSb.Append(" = ");
							contentSb.Append(typeNameParts[index]);
							contentSb.AppendLine(" or {}");
						} else if (index < partLength - 1) {
							contentSb.Append("local ");
							contentSb.Append(typeNameParts[index]);
							contentSb.Append(" = ");
							contentSb.Append(typeNameParts[index - 1]);
							contentSb.Append(".");
							contentSb.Append(typeNameParts[index]);
							contentSb.AppendLine(" or {}");
				
							contentSb.Append(typeNameParts[index - 1]);
							contentSb.Append(".");
							contentSb.Append(typeNameParts[index]);
							contentSb.Append(" = ");
							contentSb.AppendLine(typeNameParts[index]);
						} else {
							contentSb.Append(typeNameParts[index - 1]);
							contentSb.Append(".");
							contentSb.Append(typeNameParts[index]);
							contentSb.AppendLine(" = m");
						}
					}
				}
				
				contentSb.AppendLine();
				
				contentSb.AppendLine("return m");
				string content = contentSb.ToString();
				fileDict[fileName] = Encoding.UTF8.GetBytes(content);
			}

			if (compressToZip) {
				string zipFileName = OUTPUT_PATH + "/" + dllName + ".zip";
				WriteToZip(zipFileName, fileDict);
			} else {
				string dirPath = OUTPUT_PATH + "/" + dllName;
				WriteToFolder(dirPath, fileDict, true);
			}
			
			Debug.Log(dllName + " generating is complete!");
		}
	}

	private static List<MethodInfo> GetExtensionMethods(Type extendedType, List<MethodInfo> allExtensionMethodList) {
		List<MethodInfo> extensionMethodList = new List<MethodInfo>();
		foreach (MethodInfo extensionMethod in allExtensionMethodList) {
			ParameterInfo[] parameters = extensionMethod.GetParameters();
			Type thisType = parameters[0].ParameterType;
			if (thisType == extendedType) {
				extensionMethodList.Add(extensionMethod);
			} else if (thisType.IsGenericParameter && thisType.BaseType == extendedType) {
				extensionMethodList.Add(extensionMethod);
			}
		}
		return extensionMethodList;
	}

	private static void GenType(Type type, List<MethodInfo> extensionMethodList, out string fileName, out string content) {
		string typeName = TypeToString(type, false, true);
		string typeFileName = typeName + ".lua";
		StringBuilder typeScriptSb = new StringBuilder();
		typeScriptSb.Append("---@class ");
		typeScriptSb.Append(typeName);
		typeScriptSb.Append(" : ");
		if (type.BaseType != null) {
			typeScriptSb.Append(TypeToString(type.BaseType, false, true));
		} else if (type.IsInterface) {
			typeScriptSb.Append("System.Object");
		} else {
			typeScriptSb.Append("userdata");
		}

		typeScriptSb.AppendLine();

		FieldInfo[] staticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
		foreach (FieldInfo field in staticFields) {
			typeScriptSb.Append("---@field public ");
			typeScriptSb.Append(field.Name);
			typeScriptSb.Append(" ");
			typeScriptSb.Append(TypeToString(field.FieldType));
			typeScriptSb.Append(" @static");
			typeScriptSb.AppendLine();
		}

		PropertyInfo[] staticProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
		foreach (PropertyInfo property in staticProperties) {
			typeScriptSb.Append("---@field public ");
			typeScriptSb.Append(property.Name);
			typeScriptSb.Append(" ");
			typeScriptSb.Append(TypeToString(property.PropertyType));
			typeScriptSb.Append(" @static");
			typeScriptSb.AppendLine();
		}

		FieldInfo[] instanceFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		foreach (FieldInfo field in instanceFields) {
			typeScriptSb.Append("---@field public ");
			typeScriptSb.Append(field.Name);
			typeScriptSb.Append(" ");
			typeScriptSb.Append(TypeToString(field.FieldType));
			typeScriptSb.AppendLine();
		}

		PropertyInfo[] instanceProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		foreach (PropertyInfo property in instanceProperties) {
			typeScriptSb.Append("---@field public ");
			typeScriptSb.Append(property.Name);
			typeScriptSb.Append(" ");
			typeScriptSb.Append(TypeToString(property.PropertyType));
			typeScriptSb.AppendLine();
		}

		// 按XLua的使用方式导出，如果使用方式不兼容，请自行修改。
		EventInfo[] events = type.GetEvents();
		foreach (EventInfo evt in events) {
			typeScriptSb.Append("---@field public ");
			typeScriptSb.Append(evt.Name);
			typeScriptSb.Append(" fun(self: any, operator: string, delegate: ");
			typeScriptSb.Append(TypeToString(evt.EventHandlerType));
			typeScriptSb.Append(") @Event");
			typeScriptSb.AppendLine();
		}
		typeScriptSb.AppendLine();

		typeScriptSb.Append("---@type ");
		typeScriptSb.Append(typeName);
		ConstructorInfo[] instanceConstructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		foreach (ConstructorInfo constructor in instanceConstructors) {
			typeScriptSb.Append("|");
			typeScriptSb.Append(MethodToString(constructor.GetParameters(), Array.Empty<ParameterInfo>()));
		}
		typeScriptSb.AppendLine();
		typeScriptSb.Append("local m = {}");
		typeScriptSb.AppendLine();

		Dictionary<string, List<MethodInfo>> methodNameDict = new Dictionary<string, List<MethodInfo>>();
		MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		foreach (MethodInfo method in methods) {
			string methodName = method.Name;
			if (!methodName.StartsWith("get_") && !methodName.StartsWith("set_")) {
				if (!methodNameDict.ContainsKey(methodName)) {
					methodNameDict.Add(methodName, new List<MethodInfo>());
				}

				methodNameDict[methodName].Add(method);
			}
		}

		foreach (MethodInfo method in extensionMethodList) {
			string methodName = method.Name;
			if (!methodName.StartsWith("get_") && !methodName.StartsWith("set_")) {
				if (!methodNameDict.ContainsKey(methodName)) {
					methodNameDict.Add(methodName, new List<MethodInfo>());
				}

				methodNameDict[methodName].Add(method);
			}
		}

		foreach (string methodName in methodNameDict.Keys) {
			typeScriptSb.AppendLine();

			List<MethodInfo> methodList = methodNameDict[methodName];
			List<List<ParameterInfo>> paramListList = new List<List<ParameterInfo>>();
			List<List<ParameterInfo>> returnListList = new List<List<ParameterInfo>>();
			List<MethodInfo> fromMethodList = new List<MethodInfo>();
			foreach (MethodInfo method in methodList) {
				List<ParameterInfo> paramList = new List<ParameterInfo>();
				List<ParameterInfo> returnList = new List<ParameterInfo>();
				if (method.ReturnParameter?.ParameterType != typeof(void)) {
					returnList.Add(method.ReturnParameter);
				}

				ParameterInfo[] parameters = method.GetParameters();
				for (int paramIndex = 0; paramIndex < parameters.Length; paramIndex++) {
					if (!extensionMethodList.Contains(method) || paramIndex != 0) {
						ParameterInfo param = parameters[paramIndex];
						if (!param.IsOut) {
							paramList.Add(param);
						}

						if (param.ParameterType.IsByRef) {
							returnList.Add(param);
						}
					}
				}

				paramListList.Add(paramList);
				returnListList.Add(returnList);
				fromMethodList.Add(method);
				for (int paramIndex = paramList.Count - 1; paramIndex >= 0; paramIndex--) {
					ParameterInfo param = paramList[paramIndex];
					if (param.IsOptional || param.IsDefined(typeof(ParamArrayAttribute), false)) {
						List<ParameterInfo> overloadParamList = new List<ParameterInfo>();
						for (int index = 0; index < paramIndex; index++) {
							overloadParamList.Add(paramList[index]);
						}

						paramListList.Add(overloadParamList);
						returnListList.Add(returnList);
						fromMethodList.Add(method);
					}
				}
			}

			for (int overloadIndex = 1; overloadIndex < paramListList.Count; overloadIndex++) {
				typeScriptSb.Append("---@overload ");
				typeScriptSb.Append(MethodToString(paramListList[overloadIndex], returnListList[overloadIndex]));
				
				// MethodInfo method = fromMethodList[overloadIndex];
				// if (method.IsStatic) {
				// 	typeScriptSb.Append(extensionMethodList.Contains(method) ? " @extension" : " @static");
				// }
				// if (method.IsAbstract) {
				// 	typeScriptSb.Append(" @abstract");
				// } else if (method.IsVirtual) {
				// 	typeScriptSb.Append(" @virtual");
				// }
				
				typeScriptSb.AppendLine();
			}

			{
				List<ParameterInfo> paramList = paramListList[0];
				List<ParameterInfo> returnList = returnListList[0];
				MethodInfo method = fromMethodList[0];
				foreach (var attribute in method.GetCustomAttributes()) {
					if (attribute is ObsoleteAttribute obsoleteAttribute) {
						if (!string.IsNullOrEmpty(obsoleteAttribute.Message)) {
							string[] messages = obsoleteAttribute.Message.Split(new []{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
							typeScriptSb.Append("--- obsolete:");
							typeScriptSb.AppendLine(messages[0]);
							for (int i = 1, length = messages.Length; i < length; ++i) {
								typeScriptSb.Append("---   ");
								typeScriptSb.AppendLine(messages[i]);
							}
						} else {
							typeScriptSb.AppendLine("--- obsolete");
						}
					}
				}
				
				if (method.IsStatic) {
					// typeScriptSb.AppendLine(extensionMethodList.Contains(method) ? "---@extension" : "---@static");
					typeScriptSb.AppendLine(extensionMethodList.Contains(method) ? "--- extension" : "--- static");
				}

				if (method.IsAbstract) {
					// typeScriptSb.AppendLine("---@abstract");
					typeScriptSb.AppendLine("--- abstract");
				} else if (method.IsVirtual) {
					// typeScriptSb.AppendLine("---@virtual");
					typeScriptSb.AppendLine("--- virtual");
				}

				foreach (ParameterInfo param in paramList) {
					if (param.IsDefined(typeof(ParamArrayAttribute), false)) {
						typeScriptSb.Append("---@vararg ");
						typeScriptSb.Append(TypeToString(param.ParameterType.GetElementType()));
						typeScriptSb.AppendLine();
					} else {
						typeScriptSb.Append("---@param ");
						typeScriptSb.Append(GetParamName(param));
						typeScriptSb.Append(" ");
						typeScriptSb.Append(TypeToString(param.ParameterType));
						typeScriptSb.AppendLine();
					}
				}

				if (returnList.Count > 0) {
					typeScriptSb.Append("---@return ");
					typeScriptSb.Append(TypeToString(returnList[0].ParameterType));
					for (int returnIndex = 1; returnIndex < returnList.Count; returnIndex++) {
						typeScriptSb.Append(", ");
						typeScriptSb.Append(TypeToString(returnList[returnIndex].ParameterType));
					}

					typeScriptSb.AppendLine();
				}

				typeScriptSb.Append("function m");
				typeScriptSb.Append(method.IsStatic && !extensionMethodList.Contains(method) ? "." : ":");
				typeScriptSb.Append(methodName);
				typeScriptSb.Append("(");
				for (int paramIndex = 0; paramIndex < paramList.Count; paramIndex++) {
					if (paramIndex > 0) {
						typeScriptSb.Append(", ");
					}

					ParameterInfo param = paramList[paramIndex];
					typeScriptSb.Append(param.IsDefined(typeof(ParamArrayAttribute), false) ? "..." :
							GetParamName(param));
				}

				typeScriptSb.Append(") end");
				typeScriptSb.AppendLine();
			}
		}

		if (type.IsGenericTypeDefinition) {
			typeScriptSb.AppendLine();
			
			Type[] arguments = type.GetGenericArguments();
			for (int index = 0; index < arguments.Length; index++) {
				typeScriptSb.Append("---@param t");
				typeScriptSb.Append(index + 1);
				typeScriptSb.Append(" ");
				typeScriptSb.Append(TypeToString(arguments[index]));
				typeScriptSb.AppendLine();
			}
			typeScriptSb.Append("---@return ");
			typeScriptSb.Append(typeName);
			typeScriptSb.AppendLine();
			
			typeScriptSb.Append("local function MakeGeneric(");
			for (int index = 0; index < arguments.Length; index++) {
				if (index > 0) {
					typeScriptSb.Append(", ");
				}
				typeScriptSb.Append("t");
				typeScriptSb.Append(index + 1);
			}
			typeScriptSb.Append(") end");
			typeScriptSb.AppendLine();
			
			string definitionName = GenericDefinitionTypeToString(type);
			typeScriptSb.Append(definitionName);
			typeScriptSb.AppendLine(" = MakeGeneric");
		}

		typeScriptSb.AppendLine();

		string[] typeNameParts = typeName.Split('.');
		int partLength = typeNameParts.Length;
		if (partLength == 1) {
			typeScriptSb.Append(typeName);
			typeScriptSb.AppendLine(" = m");
		} else {
			for (int index = 0; index < partLength; ++index) {
				if (index == 0) {
					typeScriptSb.Append(typeNameParts[index]);
					typeScriptSb.Append(" = ");
					typeScriptSb.Append(typeNameParts[index]);
					typeScriptSb.AppendLine(" or {}");
				} else if (index < partLength - 1) {
					typeScriptSb.Append("local ");
					typeScriptSb.Append(typeNameParts[index]);
					typeScriptSb.Append(" = ");
					typeScriptSb.Append(typeNameParts[index - 1]);
					typeScriptSb.Append(".");
					typeScriptSb.Append(typeNameParts[index]);
					typeScriptSb.AppendLine(" or {}");
				
					typeScriptSb.Append(typeNameParts[index - 1]);
					typeScriptSb.Append(".");
					typeScriptSb.Append(typeNameParts[index]);
					typeScriptSb.Append(" = ");
					typeScriptSb.AppendLine(typeNameParts[index]);
				} else {
					typeScriptSb.Append(typeNameParts[index - 1]);
					typeScriptSb.Append(".");
					typeScriptSb.Append(typeNameParts[index]);
					typeScriptSb.AppendLine(" = m");
				}
			}
		}
		
		typeScriptSb.AppendLine();
		
		// typeScriptSb.Append(typeName);
		// typeScriptSb.AppendLine(" = m");
		// typeScriptSb.AppendLine();
		
		typeScriptSb.AppendLine("return m");

		fileName = typeFileName;
		content = typeScriptSb.ToString();
	}

	private static string TypeToString(Type type, bool inFun = false, bool classDefine = false) {
		if (!classDefine) {
			if (type != null && type.IsByRef) {
				type = type.GetElementType();
			}
			if (type != null && type.IsGenericParameter) {
				type = type.BaseType;
			}
			if (type == null || type == typeof(object)) {
				return "any";
			}
			if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) ||
					type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
					type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(char)) {
				return "number";
			}
			if (type == typeof(string) || type == typeof(byte[])) {
				return "string";
			}
			if (type == typeof(bool)) {
				return "boolean";
			}
			if (type == typeof(XLua.LuaTable)) {
				return "table";
			}
			if (type.IsArray) {
				return TypeToString(type.GetElementType(), inFun) + "[]|System.Array";
			}
			if (type.IsGenericType) {
				Type[] genericArgTypes = type.GetGenericArguments();
				switch (genericArgTypes.Length) {
					case 1 when typeof(IList<>).MakeGenericType(genericArgTypes).IsAssignableFrom(type):
						return TypeToString(genericArgTypes[0], inFun) + "[]|System.Collections.IList|System.Collections.ICollection";
					case 2 when typeof(IDictionary<,>).MakeGenericType(genericArgTypes).IsAssignableFrom(type):
						return "table<" + TypeToString(genericArgTypes[0]) + ", " + TypeToString(genericArgTypes[1]) + ">|System.Collections.IDictionary|System.Collections.ICollection";
				}
			}
			if (typeof(Delegate).IsAssignableFrom(type)) {
				MethodInfo method = type == typeof(Delegate) || type == typeof(MulticastDelegate) ?
						type.GetMethod("DynamicInvoke") : type.GetMethod("Invoke");
				return MethodToString(method, inFun);
			}

			if (type.FullName == null) {
				// GenericTypeDefinition like T
				// ReSharper disable once TailRecursiveCall
				return TypeToString(type.BaseType ?? typeof(object), inFun);
			}
		}

		string typeName = type.ToString();
		if (type.Namespace != null) {
			typeName = Regex.Replace(typeName, $"(?<=[\\[,]){type.Namespace}\\.", "");
		}
		char[] typeNameChars = typeName.ToCharArray();
		StringBuilder sb = new StringBuilder();
		int brackets = 0;
		foreach (char ch in typeNameChars) {
			// Generic: “`[,]”，ByRef：“&”，Nested：“+”，Other：“<>$=”
			// We want no “.” in “[]” or "<>"
			char c = ch;
			switch (c) {
				case '[':
				case '<':
					brackets++;
					c = '_';
					break;
				case ']':
				case '>':
					brackets--;
					c = '_';
					break;
				case '.':
				case '+':
					c = brackets > 0 ? '_' : '.';
					break;
				case '`':
				case ',':
				case '$':
				case '=':
					c = '_';
					break;
			}

			if (c != '&') {
				sb.Append(c);
			}
		}

		if (!classDefine && type.IsGenericType && !type.IsGenericTypeDefinition) {
			sb.Append("|");
			sb.Append(TypeToString(type.GetGenericTypeDefinition(), inFun));
		}
		return sb.ToString();
	}
	
	public static string GenericDefinitionTypeToString(Type type) {
		char[] typeNameChars = type.ToString().ToCharArray();
		StringBuilder sb = new StringBuilder();
		int brackets = 0;
		foreach (char ch in typeNameChars) {
			// Generic: “`[,]”，ByRef：“&”，Nested：“+”，Other：“<>$=”
			// We want no “.” in “[]” or "<>"
			char c = ch;
			if (c == '`') {
				break;
			}
			switch (c) {
				case '[':
				case '<':
					brackets++;
					c = '_';
					break;
				case ']':
				case '>':
					brackets--;
					c = '_';
					break;
				case '.':
				case '+':
					c = brackets > 0 ? '_' : '.';
					break;
				case '$':
				case '=':
					c = '_';
					break;
			}
			if (c != '&') {
				sb.Append(c);
			}
		}
		return sb.ToString();
	}

	private static string MethodToString(MethodInfo method, bool inFun = false) {
		if (method == null) {
			return "any";
		}

		List<ParameterInfo> paramList = new List<ParameterInfo>();
		List<ParameterInfo> returnList = new List<ParameterInfo>();
		if (method.ReturnParameter?.ParameterType != typeof(void)) {
			returnList.Add(method.ReturnParameter);
		}

		ParameterInfo[] parameters = method.GetParameters();
		foreach (ParameterInfo param in parameters) {
			if (!param.IsOut) {
				// !out
				paramList.Add(param);
			}
			if (param.ParameterType.IsByRef) {
				// out | ref
				returnList.Add(param);
			}
		}

		return MethodToString(paramList, returnList, inFun);
	}

	private static string MethodToString(IReadOnlyList<ParameterInfo> paramList, IReadOnlyList<ParameterInfo> returnList, bool inFun = false) {
		StringBuilder sb = new StringBuilder();
		if (inFun) {
			sb.Append("(");
		}

		sb.Append("fun(");
		for (int paramIndex = 0; paramIndex < paramList.Count; paramIndex++) {
			if (paramIndex > 0) {
				sb.Append(", ");
			}

			ParameterInfo param = paramList[paramIndex];
			if (param.IsDefined(typeof(ParamArrayAttribute), false)) {
				sb.Append("...:");
				sb.Append(TypeToString(param.ParameterType.GetElementType()));
				sb.Append("|");
			} else {
				sb.Append(GetParamName(param));
				sb.Append(":");
			}

			sb.Append(TypeToString(param.ParameterType, true));
		}

		sb.Append(")");
		if (returnList.Count > 0) {
			sb.Append(":");

			for (int returnIndex = 0; returnIndex < returnList.Count; returnIndex++) {
				if (returnIndex > 0) {
					sb.Append(", ");
				}
				sb.Append(TypeToString(returnList[returnIndex].ParameterType, returnList.Count == 1));
			}
		}

		if (inFun) {
			sb.Append(")");
		}
		return sb.ToString();
	}

	private static string GetParamName(ParameterInfo param) {
		string paramName = param.Name;
		foreach (string keyword in LUA_KEYWORDS) {
			if (string.Equals(paramName, keyword)) {
				paramName = "_" + paramName;
				break;
			}
		}
		return paramName;
	}

	private static void WriteToZip(string zipFileName, Dictionary<string, byte[]> fileDict, int compressionLevel = 9) {
		FileInfo zipFile = new FileInfo(zipFileName);
		if (zipFile.Exists) {
			zipFile.IsReadOnly = false;
			zipFile.Delete();
		}
		DirectoryInfo dir = zipFile.Directory;
		if (dir != null && !dir.Exists) {
			dir.Create();
		}

		using (FileStream fileStream = zipFile.Create()) {
			using (ZipOutputStream zipStream = new ZipOutputStream(fileStream)) {
				zipStream.SetLevel(compressionLevel);
				foreach (string fileName in fileDict.Keys) {
					zipStream.PutNextEntry(new ZipEntry(fileName));
					byte[] buffer = fileDict[fileName];
					zipStream.Write(buffer, 0, buffer.Length);
				}
			}
		}
	}

	private static void WriteToFolder(string dirPath, Dictionary<string, byte[]> fileDict, bool clearDir) {
		if (Directory.Exists(dirPath)) {
			if (clearDir) {
				string[] existFilePaths = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
				foreach (string filePath in existFilePaths) {
					FileInfo file = new FileInfo(filePath) {
						IsReadOnly = false
					};
					file.Delete();
				}
				Directory.Delete(dirPath, true);
				Directory.CreateDirectory(dirPath);
			}
		} else {
			Directory.CreateDirectory(dirPath);
		}
		
		foreach (string filename in fileDict.Keys) {
			string filePath = @"\\?\" + dirPath + "/" + filename.Replace('|', '_');
			byte[] bytes = fileDict[filename];
			FileInfo file = new FileInfo(filePath);
			if (!clearDir && file.Exists) {
				file.IsReadOnly = false;
				file.Delete();
			}
			using (FileStream fs = file.OpenWrite()) {
				fs.Write(bytes, 0, bytes.Length); 
				fs.Flush();
			}
			file.IsReadOnly = true;
		}
	}
}
