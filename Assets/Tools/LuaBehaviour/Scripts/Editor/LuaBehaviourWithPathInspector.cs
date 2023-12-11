/*
 * @Author: wangyun
 * @CreateTime: 2022-02-24 22:13:25 463
 * @LastEditor: wangyun
 * @EditTime: 2022-02-24 22:13:23 560
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using CSLike;

using UObject = UnityEngine.Object;

[CustomEditor(typeof(LuaBehaviourWithPath), true)]
public class LuaBehaviourWithPathInspector : LuaBehaviourInspector {
	protected const string LUA_SRC_PATH = "Assets/Scripts/Lua/";
	protected const string LUA_FILE_EXT = ".lua";
	
	protected LuaBehaviourWithPath LuaBehaviour => target as LuaBehaviourWithPath;

	private LuaInjectionDataDrawer m_InjectionDrawer;

	protected override void OnEnable() {
		base.OnEnable();
		m_InjectionDrawer = new LuaInjectionDataDrawer(true);
	}

	public override void OnInspectorGUI() {
		EditorGUI.BeginChangeCheck();
		DrawLuaData();
		DrawLuaPath();
		DrawLuaTable();
		DrawLuaRunner();
		if (EditorGUI.EndChangeCheck()) {
			EditorUtility.SetDirty(LuaBehaviour);
		}
	}
	
	protected void DrawLuaData() {
		Type type = LuaBehaviour.GetType();
		FieldInfo field = type.GetField("m_InjectionList", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field?.GetValue(LuaBehaviour) is List<Injection7> injections) {
			EditorGUI.BeginDisabledGroup(Application.isPlaying);
			m_InjectionDrawer.DrawDict(injections, LuaBehaviour);
			EditorGUI.EndDisabledGroup();
		}
	}

	protected void DrawLuaPath() {
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Lua Script", GUILayout.Width(80F));
		
		EditorGUI.BeginDisabledGroup(Application.isPlaying);
		string luaPath = LuaBehaviour.luaPath;
		bool luaPathIsEmpty = string.IsNullOrEmpty(luaPath);
		UObject asset = luaPathIsEmpty ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(LUA_SRC_PATH + luaPath.Replace(".", "/") + LUA_FILE_EXT);
		if (luaPathIsEmpty || asset) {
			UObject newAsset = EditorGUILayout.ObjectField(asset, typeof(TextAsset), true);
			if (newAsset != asset) {
				// 如果根据lua文件记录lua路径
				if (!newAsset) {
					asset = null;
					luaPath = string.Empty;
					Undo.RecordObject(LuaBehaviour, "LuaPath");
					LuaBehaviour.luaPath = luaPath;
				} else {
					string newLuaPath = AssetDatabase.GetAssetPath(newAsset);
					if (newLuaPath.StartsWith(LUA_SRC_PATH) && newLuaPath.EndsWith(LUA_FILE_EXT)) {
						int length = newLuaPath.Length - LUA_SRC_PATH.Length - LUA_FILE_EXT.Length;
						luaPath = newLuaPath.Substring(LUA_SRC_PATH.Length, length).Replace("/", ".");
						asset = newAsset;
						Undo.RecordObject(LuaBehaviour, "LuaPath");
						LuaBehaviour.luaPath = luaPath;
					}
				}
			}
			// 如果找不到lua文件，则显示文本框
			if (asset && GUILayout.Button("Deduce Fields", GUILayout.Width(100F))) {
				Type type = LuaBehaviour.GetType();
				FieldInfo field = type.GetField("m_InjectionList", BindingFlags.Instance | BindingFlags.NonPublic);
				if (field?.GetValue(target) is List<Injection7> injections) {
					LuaInjectionDataDeduce.DeduceFields(injections, luaPath);
				}
			}
		} else {
			// 如果找不到lua文件，则显示文本框
			string newLuaPath = EditorGUILayout.TextField(luaPath);
			if (newLuaPath != luaPath) {
				Undo.RecordObject(LuaBehaviour, "LuaPath");
				LuaBehaviour.luaPath = newLuaPath;
			}
		}
		EditorGUI.EndDisabledGroup();
		
		GUILayout.EndHorizontal();
	}
}
