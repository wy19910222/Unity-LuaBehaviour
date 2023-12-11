/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:38 621
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:38 613
 */

using System;
using UnityEngine;
using XLua;

namespace CSLike {
	public class PhysicsListener : BehaviourListener {
		public Action<LuaTable, Collider> onTriggerEnter;
		public Action<LuaTable, Collider> onTriggerStay;
		public Action<LuaTable, Collider> onTriggerExit;
		public Action<LuaTable, Collision> onCollisionEnter;
		public Action<LuaTable, Collision> onCollisionStay;
		public Action<LuaTable, Collision> onCollisionExit;

		private void OnTriggerEnter(Collider other) {
			onTriggerEnter?.Invoke(luaTable, other);
		}

		private void OnTriggerStay(Collider other) {
			onTriggerStay?.Invoke(luaTable, other);
		}

		private void OnTriggerExit(Collider other) {
			onTriggerExit?.Invoke(luaTable, other);
		}

		private void OnCollisionEnter(Collision collision) {
			onCollisionEnter?.Invoke(luaTable, collision);
		}

		private void OnCollisionStay(Collision collision) {
			onCollisionStay?.Invoke(luaTable, collision);
		}

		private void OnCollisionExit(Collision collision) {
			onCollisionExit?.Invoke(luaTable, collision);
		}

		public override void Dispose() {
			onTriggerEnter = null;
			onTriggerStay = null;
			onTriggerExit = null;
			onCollisionEnter = null;
			onCollisionStay = null;
			onCollisionExit = null;
			base.Dispose();
		}
	}
}
