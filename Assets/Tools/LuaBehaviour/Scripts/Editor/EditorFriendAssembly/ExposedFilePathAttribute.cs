/*
 * @Author: wangyun
 * @CreateTime: 2023-12-18 13:42:16 444
 * @LastEditor: wangyun
 * @EditTime: 2023-12-18 13:42:16 447
 */

#if !UNITY_2020_3_OR_NEWER
namespace LuaApp {
	[UnityEditor.FilePath("ProjectSettings/LuaBehaviourSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
	public class LuaBehaviourSettings : UnityEditor.ScriptableSingleton<LuaBehaviourSettings> {
		public string luaSrcPath = "Assets/";
		public string luaFileExtension = ".lua";
		
		public void Save() {
			Save(true);
		}
	}
}
#endif
