// /*
//  * @Author: wangyun
//  * @CreateTime: 2023-02-03 22:07:23 781
//  * @LastEditor: wangyun
//  * @EditTime: 2023-02-03 22:07:23 785
//  */
//
// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using XLua;
// using LuaApp;
//
// using UObject = UnityEngine.Object;
//
// namespace Control {
// 	public enum ProcessStepTypeLua {
// 		// LuaBehaviour self, string[0] luaCode, List<Injection7> parameters
// 		LUA_CODE_EXECUTE = 901,
// 		// LuaBehaviour luaBehaviour, string[0] fieldName, string[1] fieldPath, bool[0] isInstanceInvoke, List<Injection7>[0] value
// 		LUA_SET_VALUE = 902,
// 		// LuaBehaviour luaBehaviour, string[0] funcName, string[1] funcPath, bool[0] isInstanceInvoke, List<Injection7> parameters
// 		LUA_FUNCTION_INVOKE = 903,
// 	}
// 	[Serializable]
// 	public partial class ProcessStepLua : ProcessStepBase {
// 		[HideInInspector]
// 		public List<Injection7> luaInjectionList = new List<Injection7>();
// 		
// 		public ProcessStepLua() {
// 			Type = (ProcessStepTypeBase) ProcessStepTypeLua.LUA_CODE_EXECUTE;
// 		}
//
// 		public override void DoStep(BaseTriggerCtrl trigger) {
// #if UNITY_EDITOR
// 			IsTriggered = true;
// #endif
// 			switch ((ProcessStepTypeLua) Type) {
// 				case ProcessStepTypeLua.LUA_CODE_EXECUTE:
// 					DoStepLuaCodeExecute();
// 					break;
// 				case ProcessStepTypeLua.LUA_SET_VALUE:
// 					DoStepLuaSetValue();
// 					break;
// 				case ProcessStepTypeLua.LUA_FUNCTION_INVOKE:
// 					DoStepLuaFunctionInvoke();
// 					break;
// 			}
// 		}
//
// 		protected override void ResetByType() {
// 			obj = null;
// 			sArguments.Clear();
// 			bArguments.Clear();
// 			iArguments.Clear();
// 			lArguments.Clear();
// 			fArguments.Clear();
// 			objArguments.Clear();
// 			unityEvent = null;
// 			tween = false;
// 			luaInjectionList.Clear();
// 			
// 			switch ((ProcessStepTypeLua) Type) {
// 				case ProcessStepTypeLua.LUA_CODE_EXECUTE:
// 					sArguments.Add(string.Empty);	// luaCode
// 					break;
// 				case ProcessStepTypeLua.LUA_SET_VALUE:
// 					sArguments.Add(string.Empty);	// fieldName
// 					sArguments.Add(string.Empty);	// fieldPath
// 					luaInjectionList.Add(new Injection7());	// value
// 					break;
// 				case ProcessStepTypeLua.LUA_FUNCTION_INVOKE:
// 					sArguments.Add(string.Empty);	// funcName
// 					sArguments.Add(string.Empty);	// funcPath
// 					bArguments.Add(true);	// isInstanceInvoke
// 					break;
// 			}
// 		}
// 		
// 		private void DoStepLuaCodeExecute() {
// 			string luaCode = GetSArgument(0);
// 			if (!string.IsNullOrEmpty(luaCode)) {
// 				LuaTable selfTable = (obj as LuaBehaviour)?.LuaTable;
// 				LuaEnv luaEnv = LuaMain.GetLuaEnv();
// 				LuaTable metaTable = luaEnv.NewTable();
// 				metaTable.Set("__index", luaEnv.Global);
// 				LuaTable envTable = LuaInjectionData.ToDictTable(luaInjectionList);
// 				envTable.SetMetaTable(metaTable);
// 				envTable.Set("self", selfTable);
// 				luaEnv.DoString(luaCode, "LuaCodeExecute", envTable);
// 			}
// 		}
// 		
// 		private void DoStepLuaSetValue() {
// 			LuaTable table = (obj as LuaBehaviour)?.LuaTable ?? LuaMain.GetLuaEnv().Global;
// 			string fieldName = sArguments[0];
// 			string fieldPath = string.IsNullOrEmpty(fieldName) ? sArguments[1] : fieldName;
// 			object value = LuaInjectionData.ToLuaValue(luaInjectionList[0]);
// 			table.SetInPath(fieldPath, value);
// 		}
// 		
// 		private void DoStepLuaFunctionInvoke() {
// 			LuaTable table = (obj as LuaBehaviour)?.LuaTable ?? LuaMain.GetLuaEnv().Global;
// 			string funcPath = string.IsNullOrEmpty(sArguments[0]) ? sArguments[1] : sArguments[0];
// 			bool isInstanceInvoke = bArguments[0];
// 			int pointIndex = funcPath.LastIndexOf('.');
// 			if (isInstanceInvoke) {
// 				LuaTable caller;
// 				string funcName;
// 				if (pointIndex == -1) {
// 					caller = table;
// 					funcName = funcPath;
// 				} else {
// 					string callerPath = funcPath.Substring(0, pointIndex);
// 					caller = table.GetInPath<LuaTable>(callerPath);
// 					funcName = funcPath.Substring(pointIndex + 1);
// 				}
// 				int parameterCount = luaInjectionList.Count;
// 				object[] parameters = new object[parameterCount + 1];
// 				parameters[0] = caller;
// 				for (int i = 0; i < parameterCount; i++) {
// 					parameters[i + 1] = LuaInjectionData.ToLuaValue(luaInjectionList[i]);
// 				}
// 				LuaMain.GetLuaTableFuncInvoke()(caller, funcName, parameters);
// 			} else {
// 				object[] parameters = luaInjectionList.ConvertAll(LuaInjectionData.ToLuaValue).ToArray();
// 				LuaMain.GetLuaTableFuncInvoke()(table, funcPath, parameters);
// 			}
// 		}
// 	}
// }