/*
 * @Author: wangyun
 * @CreateTime: 2023-12-18 13:42:16 444
 * @LastEditor: wangyun
 * @EditTime: 2023-12-18 13:42:16 447
 */

using UnityEngine;

#if !UNITY_2020_3_OR_NEWER
namespace LuaApp {
	[UnityEditor.FilePath("ProjectSettings/LuaBehaviourSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
	public class LuaBehaviourSettings : UnityEditor.ScriptableSingleton<LuaBehaviourSettings> {
		public string luaSrcPath = "Assets/";
		public string luaFileExtension = ".lua";

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
}
#endif
