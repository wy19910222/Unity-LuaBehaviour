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
	/// Select和Deselect事件不会冒泡，因为UpdateSelected事件会在选中物体上每帧触发，所以分开监听
	/// </summary>
	public class SelectDeselectListener : BehaviourListener, ISelectHandler, IDeselectHandler {
		public Action<LuaTable, BaseEventData> onSelect;
		public Action<LuaTable, BaseEventData> onDeselect;

		public void OnSelect(BaseEventData eventData) {
			onSelect?.Invoke(luaTable, eventData);
		}

		public void OnDeselect(BaseEventData eventData) {
			onDeselect?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onSelect = null;
			onDeselect = null;
			base.Dispose();
		}
	}
}
