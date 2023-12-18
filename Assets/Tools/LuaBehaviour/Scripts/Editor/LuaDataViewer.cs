/*
 * @Author: wangyun
 * @CreateTime: 2022-04-15 02:19:07 361
 * @LastEditor: wangyun
 * @EditTime: 2022-05-02 16:46:11 355
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using XLua;

using UObject = UnityEngine.Object;

namespace LuaApp {
	public class LuaDataViewer : EditorWindow {
		[MenuItem("Window/LuaDataViewer")]
		private static void Init() {
			var window = GetWindow<LuaDataViewer>();
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}

		private static readonly GUILayoutOption BTN_WIDTH_OPTION = GUILayout.Width(80F);

		protected readonly HashSet<LuaTable> m_TableCache = new HashSet<LuaTable>();
		private readonly Dictionary<LuaFunction, string> m_FuncArgsMap = new Dictionary<LuaFunction, string>();

		[SerializeField]
		private string m_Expression;
		private string m_CurExpression;
		[SerializeField]
		private string m_ReloadLuaPath;
		[SerializeField]
		private string m_LuaCode;

		// 如果目标是table，则Value直接储存table并展示
		// 否则如果是通过路径获取，则Value储存父table，并通过ValueName展示数据
		// 否则，Value直接储存目标数据，并展示输入框让用户输入表达式修改数据
		[SerializeField] private object m_Value;
		private string m_ValueName;
		private bool m_ValueIsDirty;
		private string m_SetExpression = "XXX = value";

		private Vector2 m_ScrollPos = Vector2.zero;

		public void OnEnable() {
			m_Expression = EditorPrefs.GetString("LuaDataViewer.Expression");
			m_ReloadLuaPath = EditorPrefs.GetString("LuaDataViewer.ReloadLuaPath");
			m_LuaCode = EditorPrefs.GetString("LuaDataViewer.LuaCode");
		}

		private void OnDisable() {
			foreach (var table in m_TableCache) {
				table.Dispose();
			}
			foreach (var pair in m_FuncArgsMap) {
				pair.Key.Dispose();
			}
		}

		private void OnGUI() {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Lua表达式:", GUILayout.Width(64F));
			var newExpression = EditorGUILayout.TextArea(m_Expression);
			if (newExpression != m_Expression) {
				Undo.RecordObject(this, "Expression");
				m_Expression = newExpression;
				EditorPrefs.SetString("LuaDataViewer.Expression", newExpression);
			}
			GUI.enabled = Application.isPlaying;
			if (GUILayout.Button("查看", BTN_WIDTH_OPTION)) {
				if (Application.isPlaying) {
					m_CurExpression = m_Expression;
					ParseContent(m_CurExpression);
				}
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();

			m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);

			GUI.enabled = Application.isPlaying;
			GUILayout.Label(m_CurExpression);
			ShowValue();
			GUI.enabled = true;

			GUILayout.BeginHorizontal();
			GUI.enabled = Application.isPlaying;
			var newReloadLuaPath = EditorGUILayout.TextArea(m_ReloadLuaPath);
			if (newReloadLuaPath != m_ReloadLuaPath) {
				Undo.RecordObject(this, "ReloadLuaPath");
				m_ReloadLuaPath = newReloadLuaPath;
				EditorPrefs.SetString("LuaDataViewer.ReloadLuaPath", newReloadLuaPath);
			}
			if (GUILayout.Button("重新载入", BTN_WIDTH_OPTION)) {
				LuaMain.Instance.LuaEnv.DoString("package.loaded[\"" + m_ReloadLuaPath + "\"] = nil; require(\"" + m_ReloadLuaPath + "\");", "LuaDataViewer");
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();

			GUILayout.Label("执行Lua代码:");
			string newLuaCode = EditorGUILayout.TextArea(m_LuaCode);
			if (newLuaCode != m_LuaCode) {
				Undo.RecordObject(this, "LuaCode");
				m_LuaCode = newLuaCode;
				EditorPrefs.SetString("LuaDataViewer.LuaCode", newLuaCode);
			}
			GUI.enabled = Application.isPlaying;
			if (GUILayout.Button("执行")) {
				LuaMain.Instance.LuaEnv.DoString(m_LuaCode, "LuaDataViewer");
			}
			GUI.enabled = true;

			GUILayout.EndScrollView();
		}

		private void ParseContent(string content) {
			if (string.IsNullOrEmpty(content)) {
				m_Value = null;
				return;
			}

			var luaEnv = LuaMain.Instance.LuaEnv;
			// 是否直接是个变量路径
			if (Regex.IsMatch(m_Expression, @"^[a-zA-Z_]\w*(\.[a-zA-Z_]\w*)*$")) {
				var global = luaEnv.Global;
				var value = global.GetInPath<object>(m_Expression);
				// 结果是个table，就显示table内容
				if (value is LuaTable table) {
					m_Value = table;
					m_ValueName = null;
					return;
				}
				// 结果不是个table，就记录父table再显示内容（为了实现修改数据）
				if (value != null) {
					var index = m_Expression.LastIndexOf('.');
					var tablePath = index == -1 ? string.Empty : m_Expression.Substring(0, index);
					m_Value = tablePath == string.Empty ? global : global.GetInPath<LuaTable>(tablePath);
					m_ValueName = index == -1 ? m_Expression : m_Expression.Substring(index + 1);
					return;
				}
			}

			// 如果没有return关键字，则在最前面加上return
			if (!content.Contains("return")) {
				content = "return " + content;
			}

			try {
				// 直接执行表达式
				var values = luaEnv.DoString(content, "LuaDataViewer");
				var value = values.Length > 0 ? values[0] : null;
				m_Value = value;
				m_ValueName = null;
				m_ValueIsDirty = false;
			} catch (Exception e) {
				Debug.LogError(e);
				m_Value = null;
				m_ValueName = null;
			}
		}

		private void ShowValue() {
			if (m_Value is LuaTable table) {
				if (m_ValueName == null) {
					LuaDataDrawer.DrawData(table, m_TableCache, m_FuncArgsMap);
				} else {
					var value = table.Get<object>(m_ValueName);
					var newValue = LuaDataDrawer.DrawData(value, m_TableCache, m_FuncArgsMap);
					if (newValue.GetHashCode() != value.GetHashCode()) {
						Undo.RecordObject(this, "value");
						table.Set(m_ValueName, newValue);
					}
				}
			} else if (m_Value != null) {
				var newValue = LuaDataDrawer.DrawData(m_Value, m_TableCache, m_FuncArgsMap);
				if (newValue.GetHashCode() != m_Value.GetHashCode()) {
					m_ValueIsDirty = true;
					m_Value = newValue;
				}

				if (m_ValueIsDirty) {
					GUILayout.BeginHorizontal();
					var setContent = EditorGUILayout.TextArea(m_SetExpression);
					if (setContent != m_SetExpression) {
						Undo.RecordObject(this, "SetContent");
						m_SetExpression = setContent;
					}
					if (GUILayout.Button("设置value", BTN_WIDTH_OPTION)) {
						if (Application.isPlaying) {
							LuaEnv luaEnv = LuaMain.Instance.LuaEnv;
							LuaTable metaTable = luaEnv.NewTable();
							metaTable.Set("__index", luaEnv.Global);
							LuaTable envTable = luaEnv.NewTable();
							envTable.SetMetaTable(metaTable);
							envTable.Set("value", m_Value);
							luaEnv.DoString(setContent, "LuaDataViewer", envTable);
							ParseContent(m_CurExpression);
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		private void OnInspectorUpdate() {
			Repaint();
		}
	}
}