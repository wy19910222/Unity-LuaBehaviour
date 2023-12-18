/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 03:02:43 656
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 03:02:43 661
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace LuaApp {
	/// <summary>
	/// Scroll事件不会冒泡，它是鼠标滚轮事件，不是ScrollView的滚动事件
	/// </summary>
	public class ScrollListener : BehaviourListener, IScrollHandler {
		public Action<LuaTable, PointerEventData> onScroll;

		public void OnScroll(PointerEventData eventData) {
			onScroll?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onScroll = null;
			base.Dispose();
		}
	}
}
