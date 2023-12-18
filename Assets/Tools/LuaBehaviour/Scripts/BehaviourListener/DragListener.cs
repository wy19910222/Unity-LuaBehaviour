/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 01:01:57 749
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 01:01:57 755
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace LuaApp {
	/// <summary>
	/// Drag事件不会冒泡，且会劫持ScrollView的滚动，而InitializePotentialDrag、BeginDrag和EndDrag事件不会，所以分开监听
	/// </summary>
	public class DragListener : BehaviourListener, IDragHandler {
		public Action<LuaTable, PointerEventData> onDrag;

		public void OnDrag(PointerEventData eventData) {
			onDrag?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onDrag = null;
			base.Dispose();
		}
	}
}
