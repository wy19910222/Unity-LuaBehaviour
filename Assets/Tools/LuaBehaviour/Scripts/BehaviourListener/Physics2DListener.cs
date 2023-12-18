/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:33 491
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:33 482
 */

using System;
using UnityEngine;
using XLua;

namespace LuaApp {
	public class Physics2DListener : BehaviourListener {
		public Action<LuaTable, Collider2D> onTriggerEnter2D;
		public Action<LuaTable, Collider2D> onTriggerStay2D;
		public Action<LuaTable, Collider2D> onTriggerExit2D;
		public Action<LuaTable, Collision2D> onCollisionEnter2D;
		public Action<LuaTable, Collision2D> onCollisionStay2D;
		public Action<LuaTable, Collision2D> onCollisionExit2D;

		private void OnTriggerEnter2D(Collider2D other) {
			onTriggerEnter2D?.Invoke(luaTable, other);
		}

		private void OnTriggerStay2D(Collider2D other) {
			onTriggerStay2D?.Invoke(luaTable, other);
		}

		private void OnTriggerExit2D(Collider2D other) {
			onTriggerExit2D?.Invoke(luaTable, other);
		}

		private void OnCollisionEnter2D(Collision2D collision) {
			onCollisionEnter2D?.Invoke(luaTable, collision);
		}

		private void OnCollisionStay2D(Collision2D collision) {
			onCollisionStay2D?.Invoke(luaTable, collision);
		}

		private void OnCollisionExit2D(Collision2D collision) {
			onCollisionExit2D?.Invoke(luaTable, collision);
		}

		public override void Dispose() {
			onTriggerEnter2D = null;
			onTriggerStay2D = null;
			onTriggerExit2D = null;
			onCollisionEnter2D = null;
			onCollisionStay2D = null;
			onCollisionExit2D = null;
			base.Dispose();
		}
	}
}
