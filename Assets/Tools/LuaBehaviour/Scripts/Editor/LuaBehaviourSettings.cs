/*
 * @Author: wangyun
 * @CreateTime: 2023-12-18 13:42:07 037
 * @LastEditor: wangyun
 * @EditTime: 2023-12-18 13:42:07 042
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuaApp {
#if UNITY_2020_3_OR_NEWER
	[FilePath("ProjectSettings/LuaBehaviourSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class LuaBehaviourSettings : ScriptableSingleton<LuaBehaviourSettings> {
		[SerializeReference] public string luaSrcPath = "Assets/";
		[SerializeReference] public string luaFileExtension = ".lua";

		public string LuaPathToFilePath(string luaPath) {
			if (string.IsNullOrEmpty(luaPath)) {
				Debug.LogError("LuaPath is null or empty!");
				return string.Empty;
			}
			return luaSrcPath + luaPath.Replace(".", "/") + luaFileExtension;
		}
		
		public void Save() {
			Save(true);
		}
	}
#endif

	public class LuaBehaviourSettingsProvider : SettingsProvider {
		[SettingsProvider]
		public static SettingsProvider CreateLuaBehaviourSettingsProvider() {
			return new LuaBehaviourSettingsProvider("Project/LuaBehaviour", SettingsScope.Project);
		}

		private LuaBehaviourSettings m_Settings;
		private string m_CachedLuaSrcPath;
		private string m_CachedLuaFileExtension;

		public LuaBehaviourSettingsProvider(string path, SettingsScope scopes,
				IEnumerable<string> keywords = null) : base(path, scopes, keywords) {
		}

		public override void OnActivate(string searchContext, VisualElement rootElement) {
			m_Settings = LuaBehaviourSettings.instance;
			m_CachedLuaSrcPath = m_Settings.luaSrcPath;
			m_CachedLuaFileExtension = m_Settings.luaFileExtension;
		}

		public override void OnGUI(string searchContext) {
			base.OnGUI(searchContext);

			EditorGUILayout.BeginHorizontal();
			string newLuaSrcPath = EditorGUILayout.TextField("Lua根目录", m_Settings.luaSrcPath);
			if (!newLuaSrcPath.EndsWith("/")) {
				newLuaSrcPath += "/";
			}
			if (newLuaSrcPath != m_Settings.luaSrcPath) {
				Undo.RecordObject(m_Settings, "LuaSrcPath");
				m_Settings.luaSrcPath = newLuaSrcPath;
			}
			if (GUILayout.Button("…", "ButtonMid", GUILayout.Width(24))) {
				newLuaSrcPath = EditorUtility.OpenFolderPanel("Select Folder", m_Settings.luaSrcPath, "").Replace('\\', '/');
				if (!string.IsNullOrEmpty(newLuaSrcPath)) {
					if (newLuaSrcPath.StartsWith(Application.dataPath)) {
						newLuaSrcPath = newLuaSrcPath.Substring(Application.dataPath.Length - "Assets".Length);
						if (newLuaSrcPath != m_Settings.luaSrcPath) {
							Undo.RecordObject(m_Settings, "LuaSrcPath");
							m_Settings.luaSrcPath = newLuaSrcPath;
						}
					} else {
						EditorUtility.DisplayDialog("错误", "请选择工程内的目录", "确定");
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			
			string newLuaFileExtension = EditorGUILayout.TextField("Lua文件扩展名", m_Settings.luaFileExtension);
			if (!newLuaFileExtension.StartsWith(".")) {
				newLuaFileExtension = "." + newLuaFileExtension;
			}
			if (newLuaFileExtension != m_Settings.luaFileExtension) {
				Undo.RecordObject(m_Settings, "LuaFileExtension");
				m_Settings.luaFileExtension = newLuaFileExtension;
			}

			bool isDirty = false;
			if (m_Settings.luaSrcPath != m_CachedLuaSrcPath) {
				m_CachedLuaSrcPath = m_Settings.luaSrcPath;
				isDirty = true;
			}
			if (m_Settings.luaFileExtension != m_CachedLuaFileExtension) {
				m_CachedLuaFileExtension = m_Settings.luaFileExtension;
				isDirty = true;
			}
			if (isDirty) {
				m_Settings.Save();
			}
		}
	}
}
