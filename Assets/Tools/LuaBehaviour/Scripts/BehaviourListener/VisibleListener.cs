/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:49 165
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:49 165
 */

using System;
using XLua;

namespace CSLike {
	public class VisibleListener : BehaviourListener {
		public Action<LuaTable> onBecameVisible;
		public Action<LuaTable> onBecameInvisible;

		private void OnBecameVisible() {
			onBecameVisible?.Invoke(luaTable);
		}

		private void OnBecameInvisible() {
			onBecameInvisible?.Invoke(luaTable);
		}

		public override void Dispose() {
			onBecameVisible = null;
			onBecameInvisible = null;
			base.Dispose();
		}
	}
}
