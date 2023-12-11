/*
 * @Author: wangyun
 * @CreateTime: 2022-03-09 18:52:05 140
 * @LastEditor: wangyun
 * @EditTime: 2023-01-25 23:55:21 541
 */

using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CSLike;

using UObject = UnityEngine.Object;

// [CanEditMultipleObjects]
[CustomEditor(typeof(LuaScriptableData), true)]
public class LuaScriptableDataInspector : Editor {
	private LuaInjectionDataDrawer m_InjectionDrawer;

	private void OnEnable() {
		m_InjectionDrawer = new LuaInjectionDataDrawer();
	}
	
	public override void OnInspectorGUI() {
		FieldInfo injectionListField = typeof(LuaScriptableData).GetField("m_InjectionList", BindingFlags.Instance | BindingFlags.NonPublic);
		List<Injection7> injections = injectionListField?.GetValue(target) as List<Injection7>;
		if (injections != null) {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space(-14F, false);
			m_InjectionDrawer.DrawDict(injections, target);
			EditorGUILayout.EndHorizontal();
		}
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space(-14F, false);
		EditorGUILayout.LabelField("Lua Class", GUILayout.Width(80F));
		
		FieldInfo classNameField = typeof(LuaScriptableData).GetField("m_LuaClassName", BindingFlags.Instance | BindingFlags.NonPublic);
		string className = classNameField?.GetValue(target) as string;
		string newClassName = EditorGUILayout.TextField(className);
		if (newClassName != className) {
			Undo.RecordObject(target, "LuaClassName");
			classNameField?.SetValue(target, newClassName);
		}
		
		if (GUILayout.Button("Deduce Fields", GUILayout.Width(100F))) {
			if (injections != null) {
				LuaInjectionDataDeduce.DeduceFieldsByClassName(injections, newClassName);
			}
		}
		EditorGUILayout.EndHorizontal();
		
		if (GUI.changed) {
			EditorUtility.SetDirty(target);
		}
	}
}
