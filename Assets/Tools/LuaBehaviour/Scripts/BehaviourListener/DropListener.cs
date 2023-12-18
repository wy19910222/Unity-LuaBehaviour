/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 02:06:57 952
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 02:06:57 957
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace LuaApp {
	/// <summary>
	/// Drop事件不会冒泡
	/// </summary>
	public class DropListener : BehaviourListener, IDropHandler {
		public Action<LuaTable, PointerEventData> onDrop;

		public void OnDrop(PointerEventData eventData) {
			onDrop?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onDrop = null;
			base.Dispose();
		}
	}
}
