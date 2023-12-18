/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:19 625
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:19 623
 */

using System;
using XLua;

namespace LuaApp {
	public class FixedUpdateListener : BehaviourListener {
		public Action<LuaTable> fixedUpdate;

		private void FixedUpdate() {
			fixedUpdate?.Invoke(luaTable);
		}

		public override void Dispose() {
			fixedUpdate = null;
			base.Dispose();
		}
	}
}
