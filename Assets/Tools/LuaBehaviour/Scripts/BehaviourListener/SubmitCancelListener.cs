/*
 * @Author: wangyun
 * @CreateTime: 2023-04-09 03:45:09 270
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 03:45:09 276
 */

using System;
using UnityEngine.EventSystems;
using XLua;

namespace CSLike {
	/// <summary>
	/// Submit和Cancel事件不会冒泡，它是Input里的确定和取消事件，不是文本框事件
	/// </summary>
	public class SubmitCancelListener : BehaviourListener, ISubmitHandler, ICancelHandler {
		public Action<LuaTable, BaseEventData> onSubmit;
		public Action<LuaTable, BaseEventData> onCancel;

		public void OnSubmit(BaseEventData eventData) {
			onSubmit?.Invoke(luaTable, eventData);
		}

		public void OnCancel(BaseEventData eventData) {
			onCancel?.Invoke(luaTable, eventData);
		}

		public override void Dispose() {
			onSubmit = null;
			onCancel = null;
			base.Dispose();
		}
	}
}
