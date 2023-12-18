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
	/// InitializePotentialDrag、BeginDrag和EndDrag事件不会冒泡，但不会劫持ScrollView的滚动，而Drag事件会，所以分开监听
	/// </summary>
	public class DragBeginEndListener : BehaviourListener, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler {
		public Action<LuaTable, PointerEventData> onInitializePotentialDrag;
		public Action<LuaTable, PointerEventData> onBeginDrag;
		public Action<LuaTable, PointerEventData> onEndDrag;
		
		public void OnInitializePotentialDrag(PointerEventData eventData) {
			onInitializePotentialDrag?.Invoke(luaTable, eventData);
		}

		public void OnBeginDrag(PointerEventData eventData) {
			onBeginDrag?.Invoke(luaTable, eventData);
		}

		public void OnEndDrag(PointerEventData eventData) {
			onEndDrag?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onInitializePotentialDrag = null;
			onBeginDrag = null;
			onEndDrag = null;
			base.Dispose();
		}
	}
}
