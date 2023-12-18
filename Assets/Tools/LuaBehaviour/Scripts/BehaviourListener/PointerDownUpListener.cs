/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 00:57:07 950
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 00:57:07 957
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace LuaApp {
	/// <summary>
	/// Down、Up和Click事件不会冒泡，而Enter和Exit事件会，所以分开监听
	/// </summary>
	public class PointerDownUpListener : BehaviourListener, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
		public Action<LuaTable, PointerEventData> onPointerDown;
		public Action<LuaTable, PointerEventData> onPointerUp;
		public Action<LuaTable, PointerEventData> onPointerClick;

		public void OnPointerDown(PointerEventData eventData) {
			onPointerDown?.Invoke(luaTable, eventData);
		}

		public void OnPointerUp(PointerEventData eventData) {
			onPointerUp?.Invoke(luaTable, eventData);
		}

		public void OnPointerClick(PointerEventData eventData) {
			onPointerClick?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onPointerDown = null;
			onPointerUp = null;
			onPointerClick = null;
			base.Dispose();
		}
	}
}
