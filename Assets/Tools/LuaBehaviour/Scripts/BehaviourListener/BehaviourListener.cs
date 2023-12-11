/*
 * @Author: wangyun
 * @CreateTime: 2022-06-07 20:00:03 745
 * @LastEditor: wangyun
 * @EditTime: 2022-06-07 20:00:03 743
 */

using System;
using UnityEngine;
using XLua;

namespace CSLike {
	public abstract class BehaviourListener : MonoBehaviour, IDisposable {
		public LuaTable luaTable;

		public virtual void Dispose() {
			Destroy(this);
		}
	}
}
