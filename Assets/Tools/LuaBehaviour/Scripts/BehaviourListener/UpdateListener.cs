/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:42 921
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:42 917
 */

using System;
using XLua;

namespace LuaApp {
	public class UpdateListener : BehaviourListener {
		public Action<LuaTable> update;

		private void Update() {
			update?.Invoke(luaTable);
		}

		public override void Dispose() {
			update = null;
			base.Dispose();
		}
	}
}
