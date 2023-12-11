/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 00:57:07 950
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 00:57:07 957
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace CSLike {
	/// <summary>
	/// Enter和Exit事件会冒泡，而Down、Up和Click事件不会，所以分开监听
	/// </summary>
	public class PointerEnterExitListener : BehaviourListener, IPointerEnterHandler, IPointerExitHandler {
		public Action<LuaTable, PointerEventData> onPointerEnter;
		public Action<LuaTable, PointerEventData> onPointerExit;

		public void OnPointerEnter(PointerEventData eventData) {
			onPointerEnter?.Invoke(luaTable, eventData);
		}

		public void OnPointerExit(PointerEventData eventData) {
			onPointerExit?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onPointerEnter = null;
			onPointerExit = null;
			base.Dispose();
		}
	}
}
