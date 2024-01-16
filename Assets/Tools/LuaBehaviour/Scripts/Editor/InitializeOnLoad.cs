using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LuaApp {
	public class InitializeOnLoad : MonoBehaviour {
		[InitializeOnLoadMethod]
		private static void InitializeOnLoadMethod() {
			const string SYMBOL_LUA_BEHAVIOUR_EXIST = "LUA_BEHAVIOUR_EXIST";
			BuildTargetGroup group = BuildTargetGroup;
			List<string> symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').ToList();
			if (!symbols.Contains(SYMBOL_LUA_BEHAVIOUR_EXIST)) {
				symbols.Add(SYMBOL_LUA_BEHAVIOUR_EXIST);
				PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
			}
		}
		
		private static BuildTargetGroup BuildTargetGroup =>
#if UNITY_ANDROID
				BuildTargetGroup.Android;
#elif UNITY_IOS
				BuildTargetGroup.iOS;
#elif UNITY_STANDALONE
				BuildTargetGroup.Standalone;
#elif UNITY_WEBGL
				BuildTargetGroup.WebGL;
#endif
	}
}
