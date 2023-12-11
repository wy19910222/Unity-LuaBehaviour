/*
 * @Author: wangyun
 * @CreateTime: 2023-12-12 01:19:13 067
 * @LastEditor: wangyun
 * @EditTime: 2023-12-12 01:19:13 073
 */

using System;
using System.Collections.Generic;
using XLua;

namespace CSLike {
	public static class ExampleConfig {
		[CSharpCallLua]
		public static List<Type> CSharpCallLua => new List<Type>() {
			typeof(Func<string, int, string>),	// debug.traceback
		};
	}
}
