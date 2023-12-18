/*
 * @Author: wangyun
 * @CreateTime: 2022-02-24 22:13:25 463
 * @LastEditor: wangyun
 * @EditTime: 2022-02-24 22:13:23 560
 */

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace LuaApp {
	[CustomEditor(typeof(LuaBehaviourDynamicAdd), true)]
	public class LuaBehaviourDynamicAddInspector : LuaBehaviourInspector {
		protected LuaBehaviourDynamicAdd LuaBehaviour => target as LuaBehaviourDynamicAdd;

		public override void OnInspectorGUI() {
			EditorGUI.BeginChangeCheck();
			DrawLuaInjectionData();
			DrawLuaTable();
			DrawLuaRunner();
			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(LuaBehaviour);
			}
		}

		protected void DrawLuaInjectionData() {
			GUILayout.BeginHorizontal();

			GUILayout.Label("Lua Data", GUILayout.Width(80F));

			EditorGUI.BeginDisabledGroup(Application.isPlaying);
			LuaInjectionData injectionData = LuaBehaviour.injectionData;
			LuaInjectionData newInjectionData = EditorGUILayout.ObjectField(injectionData, typeof(LuaInjectionData), true) as LuaInjectionData;
			if (newInjectionData) {
				LuaInjectionData[] comps = newInjectionData.GetComponents<LuaInjectionData>();
				int compCount = comps.Length;
				string[] optionNames = new string[compCount + 1];
				int[] optionValues = new int[compCount + 1];
				optionNames[0] = "0.None";
				optionValues[0] = -1;
				for (int i = 0; i < compCount; ++i) {
					LuaInjectionData comp = comps[i];
					Type type = comp.GetType();
					string optionName = type.Name;
					var fi = type.GetField("m_LuaClassName", BindingFlags.Instance | BindingFlags.NonPublic);
					string className = fi?.GetValue(comp)?.ToString();
					if (!string.IsNullOrEmpty(className)) {
						optionName = $"{optionName} - {className}";
					}
					optionNames[i + 1] = $"{i + 1}.{optionName}";
					optionValues[i + 1] = i;
				}
				int index = Array.IndexOf(comps, newInjectionData);
				int newIndex = EditorGUILayout.IntPopup(index, optionNames, optionValues);
				if (newIndex != index) {
					newInjectionData = newIndex == -1 ? null : comps[newIndex];
				}
			}
			if (newInjectionData != injectionData) {
				Undo.RecordObject(LuaBehaviour, "InjectionData");
				LuaBehaviour.injectionData = newInjectionData;
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.EndHorizontal();
		}
	}
}