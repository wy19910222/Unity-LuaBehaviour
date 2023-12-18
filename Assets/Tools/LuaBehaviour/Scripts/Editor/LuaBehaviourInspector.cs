/*
 * @Author: wangyun
 * @CreateTime: 2022-02-24 22:13:25 463
 * @LastEditor: wangyun
 * @EditTime: 2022-02-24 22:13:23 560
 */

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using XLua;

using UObject = UnityEngine.Object;

namespace LuaApp {
	[CustomEditor(typeof(LuaBehaviour), true)]
	public class LuaBehaviourInspector : Editor {
		protected readonly HashSet<LuaTable> m_TableCache = new HashSet<LuaTable>();
		protected readonly Dictionary<LuaFunction, string> m_FuncArgsMap = new Dictionary<LuaFunction, string>();

		private LuaBehaviour LuaBehaviour => target as LuaBehaviour;

		protected bool m_AutoRepaint;
		protected string m_LuaCode;

		protected virtual void OnEnable() {
			EditorApplication.update += Update;
		}

		private int m_FrameCount;
		private void Update() {
			if (m_AutoRepaint) {
				if (m_FrameCount < 10) {
					m_FrameCount++;
				} else {
					m_FrameCount = 0;
					Repaint();
				}
			}
		}

		private void OnDisable() {
			EditorApplication.update -= Update;
			foreach (var table in m_TableCache) {
				table.Dispose();
			}
			foreach (var pair in m_FuncArgsMap) {
				pair.Key.Dispose();
			}
		}

		public override void OnInspectorGUI() {
			EditorGUI.BeginChangeCheck();
			DrawLuaTable();
			DrawLuaRunner();
			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(LuaBehaviour);
			}
		}

		protected void DrawLuaTable() {
			FieldInfo fi = LuaBehaviour.GetType().GetField("m_LuaTable", BindingFlags.Instance | BindingFlags.NonPublic);
			if (fi?.GetValue(LuaBehaviour) is LuaTable table) {
				m_AutoRepaint = GUILayout.Toggle(m_AutoRepaint, "自动刷新", "Button");
				LuaDataDrawer.DrawTable(table, m_TableCache, m_FuncArgsMap);
			}
		}

		protected void DrawLuaRunner() {
			if (Application.isPlaying) {
				GUILayout.BeginHorizontal();
				GUILayout.Space(-16F);
				GUILayout.BeginVertical();
				GUILayout.Label("执行Lua代码(以当前Table为self):");
				string newLuaCode = EditorGUILayout.TextArea(m_LuaCode);
				if (newLuaCode != m_LuaCode) {
					Undo.RecordObject(this, "LuaCode");
					m_LuaCode = newLuaCode;
				}
				if (GUILayout.Button("执行")) {
					LuaEnv luaEnv = LuaMain.Instance.LuaEnv;
					LuaTable metaTable = luaEnv.NewTable();
					metaTable.Set("__index", luaEnv.Global);
					LuaTable envTable = luaEnv.NewTable();
					envTable.SetMetaTable(metaTable);
					FieldInfo fi = LuaBehaviour.GetType().GetField("m_LuaTable", BindingFlags.Instance | BindingFlags.NonPublic);
					envTable.Set("self", fi?.GetValue(LuaBehaviour));
					luaEnv.DoString(m_LuaCode, "LuaBehaviour", envTable);
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}
	}
}