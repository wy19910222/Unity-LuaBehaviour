// /*
//  * @Author: wangyun
//  * @CreateTime: 2023-02-03 22:15:07 941
//  * @LastEditor: wangyun
//  * @EditTime: 2023-02-03 22:15:07 945
//  */
//
// #if UNITY_EDITOR
//
// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEditor;
// using LuaApp;
// using Sirenix.OdinInspector.Editor;
// using Sirenix.Utilities;
//
// namespace Control {
// 	public partial class ProcessStepLua {
// 		protected override (List<string>, List<int>) GetTypeOptions() {
// 			Array allTypes = Enum.GetValues(typeof(ProcessStepTypeLua));
// 			List<int> values = new List<int>(allTypes.Convert(_type => (int) (ProcessStepTypeLua) _type));
// 			List<string> names = new List<string>(allTypes.Convert(_type => 
// 					s_TypeLuaNameDict.TryGetValue((ProcessStepTypeLua) _type, out string _name) ? _name : _type.ToString()));
// 			return (names, values);
// 		}
// 		
// 		protected override void DrawFeature(InspectorProperty property) {
// 			switch ((ProcessStepTypeLua) Type) {
// 				case ProcessStepTypeLua.LUA_CODE_EXECUTE:
// 					DrawLuaCodeExecute(property);
// 					break;
// 				case ProcessStepTypeLua.LUA_SET_VALUE:
// 					DrawLuaSetValue(property);
// 					break;
// 				case ProcessStepTypeLua.LUA_FUNCTION_INVOKE:
// 					DrawLuaFunctionInvoke(property);
// 					break;
// 			}
// 		}
// 		
// 		private void DrawLuaCodeExecute(InspectorProperty property) {
// 			LuaBehaviour newObj = DrawCompField<LuaBehaviour>("self", obj);
// 			if (newObj != obj) {
// 				property.RecordForUndo("Obj");
// 				obj = newObj;
// 			}
//
// 			EditorGUILayout.BeginHorizontal();
// 			EditorGUILayout.LabelField("参数", CustomEditorGUI.LabelWidthOption);
// 			int valueCount = property.SerializationRoot.ValueEntry.ValueCount;
// 			BaseTriggerCtrl trigger = null;
// 			for (int i = 0; i < valueCount; i++) {
// 				trigger = property.SerializationRoot.ValueEntry.WeakValues[i] as BaseTriggerCtrl;
// 				if (trigger != null) {
// 					break;
// 				}
// 			}
// 			LuaInjectionDataDrawer.DrawDict(luaInjectionList, trigger, true);
// 			EditorGUILayout.EndHorizontal();
//
// 			EditorGUILayout.BeginHorizontal();
// 			EditorGUILayout.LabelField("代码", CustomEditorGUI.LabelWidthOption);
// 			string newLuaCode = EditorGUILayout.TextArea(sArguments[0]);
// 			if (newLuaCode != sArguments[0]) {
// 				property.RecordForUndo("SArguments");
// 				sArguments[0] = newLuaCode;
// 			}
// 			EditorGUILayout.EndHorizontal();
// 		}
//
// 		private void DrawLuaSetValue(InspectorProperty property) {
// 			LuaBehaviour newObj = DrawCompField<LuaBehaviour>("作用域", obj);
// 			if (newObj != obj) {
// 				property.RecordForUndo("Obj");
// 				obj = newObj;
// 			}
// 			if (newObj) {
// 				EditorGUILayout.BeginHorizontal();
// 				string[] fieldNames = LuaInjectionDataDeduce.GetFieldNames(newObj.m_LuaPath);
// 				int count = fieldNames.Length;
// 				string[] displayOptions = new string[count + 1];
// 				int[] optionValues = new int[count + 1];
// 				displayOptions[0] = "手动填写";
// 				optionValues[0] = -1;
// 				for (int i = 0; i < count; ++i) {
// 					displayOptions[i + 1] = fieldNames[i];
// 					optionValues[i + 1] = i;
// 				}
// 				string fieldName = sArguments[0];
// 				int index = Array.IndexOf(fieldNames, fieldName);
// 				int newIndex = EditorGUILayout.IntPopup("相对路径", index, displayOptions, optionValues);
// 				if (newIndex != index) {
// 					property.RecordForUndo("SArguments");
// 					sArguments[0] = fieldName = newIndex == -1 ? null : fieldNames[newIndex];
// 				}
// 				if (string.IsNullOrEmpty(fieldName)) {
// 					string newFieldPath = EditorGUILayout.TextField(sArguments[1], GUILayout.Width(s_ContextWidth * 0.3F));
// 					if (newFieldPath != sArguments[1]) {
// 						property.RecordForUndo("SArguments");
// 						sArguments[1] = newFieldPath;
// 					}
// 				}
// 				EditorGUILayout.EndHorizontal();
// 			} else {
// 				sArguments[0] = null;
// 				string newFieldPath = EditorGUILayout.TextField("绝对路径", sArguments[1]);
// 				if (newFieldPath != sArguments[1]) {
// 					property.RecordForUndo("SArguments");
// 					sArguments[1] = newFieldPath;
// 				}
// 			}
// 			EditorGUILayout.BeginHorizontal();
// 			EditorGUILayout.LabelField("参数", CustomEditorGUI.LabelWidthOption);
// 			int valueCount = property.SerializationRoot.ValueEntry.ValueCount;
// 			BaseTriggerCtrl trigger = null;
// 			for (int i = 0; i < valueCount; i++) {
// 				trigger = property.SerializationRoot.ValueEntry.WeakValues[i] as BaseTriggerCtrl;
// 				if (trigger != null) {
// 					break;
// 				}
// 			}
//
// 			LuaInjectionDataDrawer.DrawSingle(luaInjectionList[0], trigger, true);
// 			EditorGUILayout.EndHorizontal();
// 		}
//
// 		private void DrawLuaFunctionInvoke(InspectorProperty property) {
// 			LuaBehaviour newObj = DrawCompField<LuaBehaviour>("作用域", obj);
// 			if (newObj != obj) {
// 				property.RecordForUndo("Obj");
// 				obj = newObj;
// 			}
// 			if (newObj) {
// 				EditorGUILayout.BeginHorizontal();
// 				string[] funcNames = LuaInjectionDataDeduce.GetMethodNames(newObj.m_LuaPath);
// 				int count = funcNames.Length;
// 				string[] displayOptions = new string[count + 1];
// 				int[] optionValues = new int[count + 1];
// 				displayOptions[0] = "手动填写";
// 				optionValues[0] = -1;
// 				for (int i = 0; i < count; ++i) {
// 					displayOptions[i + 1] = funcNames[i];
// 					optionValues[i + 1] = i;
// 				}
// 				string funcName = sArguments[0];
// 				int index = Array.IndexOf(funcNames, funcName);
// 				int newIndex = EditorGUILayout.IntPopup("相对路径", index, displayOptions, optionValues);
// 				if (newIndex != index) {
// 					property.RecordForUndo("SArguments");
// 					sArguments[0] = funcName = newIndex == -1 ? null : funcNames[newIndex];
// 				}
// 				if (string.IsNullOrEmpty(funcName)) {
// 					string newFuncPath = EditorGUILayout.TextField(sArguments[1], GUILayout.Width(s_ContextWidth * 0.3F));
// 					if (newFuncPath != sArguments[1]) {
// 						property.RecordForUndo("SArguments");
// 						sArguments[1] = newFuncPath;
// 					}
// 				}
// 				EditorGUILayout.EndHorizontal();
// 			} else {
// 				sArguments[0] = null;
// 				string newFuncPath = EditorGUILayout.TextField("绝对路径", sArguments[1]);
// 				if (newFuncPath != sArguments[1]) {
// 					property.RecordForUndo("SArguments");
// 					sArguments[1] = newFuncPath;
// 				}
// 			}
// 			bool newIsInstanceInvoke = DrawEnumButtons("调用方式", bArguments[0] ? 1 : 0, new [] { ".Call", ":Call" }, new [] { 0, 1 }) > 0;
// 			if (newIsInstanceInvoke != bArguments[0]) {
// 				property.RecordForUndo("BArguments");
// 				bArguments[0] = newIsInstanceInvoke;
// 			}
// 			EditorGUILayout.BeginHorizontal();
// 			EditorGUILayout.LabelField("参数", CustomEditorGUI.LabelWidthOption);
// 			int valueCount = property.SerializationRoot.ValueEntry.ValueCount;
// 			BaseTriggerCtrl trigger = null;
// 			for (int i = 0; i < valueCount; i++) {
// 				trigger = property.SerializationRoot.ValueEntry.WeakValues[i] as BaseTriggerCtrl;
// 				if (trigger != null) {
// 					break;
// 				}
// 			}
// 			LuaInjectionDataDrawer.DrawList(luaInjectionList, trigger, true);
// 			EditorGUILayout.EndHorizontal();
// 		}
// 		
// 		protected static Dictionary<ProcessStepTypeLua, string> s_TypeLuaNameDict = new Dictionary<ProcessStepTypeLua, string> {
// 			{ProcessStepTypeLua.LUA_CODE_EXECUTE, "Lua - 代码执行"},
// 			{ProcessStepTypeLua.LUA_SET_VALUE, "Lua - 赋值"},
// 			{ProcessStepTypeLua.LUA_FUNCTION_INVOKE, "Lua - 函数调用"},
// 		};
// 	}
// }
// #endif