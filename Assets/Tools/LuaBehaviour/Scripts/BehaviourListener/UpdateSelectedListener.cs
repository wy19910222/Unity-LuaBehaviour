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
	/// UpdateSelected事件会在选中物体上每帧触发，所以分开监听
	/// </summary>
	public class UpdateSelectedListener : BehaviourListener, IUpdateSelectedHandler {
		public Action<LuaTable, BaseEventData> onUpdateSelected;

		public void OnUpdateSelected(BaseEventData eventData) {
			onUpdateSelected?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onUpdateSelected = null;
			base.Dispose();
		}
	}
}
