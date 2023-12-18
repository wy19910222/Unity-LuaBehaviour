/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:25 027
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:25 022
 */

using System;
using XLua;

namespace LuaApp {
	public class GUIListener : BehaviourListener {
		public Action<LuaTable> onGUI;

		private void OnGUI() {
			onGUI?.Invoke(luaTable);
		}

		public override void Dispose() {
			onGUI = null;
			base.Dispose();
		}
	}
}
