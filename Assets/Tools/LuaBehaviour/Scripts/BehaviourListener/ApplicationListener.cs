/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:08 363
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:08 363
 */

using System;
using XLua;

namespace CSLike {
	public class ApplicationListener : BehaviourListener {
		public Action<LuaTable, bool> onApplicationFocus;
		public Action<LuaTable, bool> onApplicationPause;
		public Action<LuaTable> onApplicationQuit;

		private void OnApplicationFocus(bool hasFocus) {
			onApplicationFocus?.Invoke(luaTable, hasFocus);
		}

		private void OnApplicationPause(bool pauseStatus) {
			onApplicationPause?.Invoke(luaTable, pauseStatus);
		}

		private void OnApplicationQuit() {
			onApplicationQuit?.Invoke(luaTable);
		}

		public override void Dispose() {
			onApplicationFocus = null;
			onApplicationPause = null;
			onApplicationQuit = null;
			base.Dispose();
		}
	}
}
