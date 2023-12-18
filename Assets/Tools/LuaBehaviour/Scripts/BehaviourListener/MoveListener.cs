/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 03:41:55 470
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 03:41:55 475
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace LuaApp {
	/// <summary>
	/// Move事件会在选中物体上触发，它是Input里的横竖轴事件，不是坐标改变事件
	/// </summary>
	public class MoveListener : BehaviourListener, IMoveHandler {
		public Action<LuaTable, AxisEventData> onMove;

		public void OnMove(AxisEventData eventData) {
			onMove?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onMove = null;
			base.Dispose();
		}
	}
}
