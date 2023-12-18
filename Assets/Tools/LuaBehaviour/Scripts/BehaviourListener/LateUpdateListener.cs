/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:29 372
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:29 362
 */

using System;
using XLua;

namespace LuaApp {
	public class LateUpdateListener : BehaviourListener {
		public Action<LuaTable> lateUpdate;

		public void LateUpdate() {
			lateUpdate?.Invoke(luaTable);
		}

		public override void Dispose() {
			lateUpdate = null;
			base.Dispose();
		}
	}
}
