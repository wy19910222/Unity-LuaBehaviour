using System;
using System.Reflection;
using UnityEngine;

using UObject = UnityEngine.Object;

namespace LuaApp {
	public static class LuaHelp {
		public static bool IsUnityEditor() {
	#if UNITY_EDITOR
			return true;
	#else
			return false;
	#endif
		}

		public static int GetHash(object obj) {
			return obj.GetHashCode();
		}

		public static string StringFormat(string format, params object[] args) {
			int argLength = args.Length;
			for (int i = 0; i < argLength; ++i) {
				args[i] = args[i] ?? "null";
			}
			return string.Format(format ?? string.Empty, args);
		}
		public static string[] StringSplit(string str, params string[] separator) {
			return str?.Split(separator, StringSplitOptions.None);
		}
		public static string StringReplace(string str, string oldValue, string newValue) {
			return str?.Replace(oldValue, newValue);
		}
		public static int StringIndexOf(string str, string value, int startIndex = 0) {
			return str == null ? -1 : StringIndexOf(str, value, startIndex, str.Length - startIndex);
		}
		public static int StringIndexOf(string str, string value, int startIndex, int count) {
			return str?.IndexOf(value, startIndex, count) ?? -1;
		}
		public static int StringLastIndexOf(string str, string value) {
			return str == null ? -1 : StringLastIndexOf(str, value, str.Length - 1);
		}
		public static int StringLastIndexOf(string str, string value, int startIndex) {
			return StringLastIndexOf(str, value, startIndex, startIndex + 1);
		}
		public static int StringLastIndexOf(string str, string value, int startIndex, int count) {
			return str?.LastIndexOf(value, startIndex, count) ?? -1;
		}
		public static int StringLength(string str) {
			return str?.Length ?? 0;
		}
		public static string StringSub(string str, int startIndex) {
			return str?.Substring(startIndex);
		}
		public static string StringSub(string str, int startIndex, int length) {
			return str?.Substring(startIndex, length);
		}

		public static AnimationState GetAnimState(Animation anim, string name) {
			return anim[name];
		}

		public static int RandomRangeInt(int min, int max) {
			return UnityEngine.Random.Range(min, max);
		}

		public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo,
				float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers,
				QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
			return Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
		}
		public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity,
				int layerMask = Physics.DefaultRaycastLayers,
				QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
			return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
		}

		public static bool IsNull(UObject uObj) {
			return !uObj;
		}

		public static bool IsNotNull(UObject uObj) {
			return uObj;
		}

		#region generic delegate

		private static MethodInfo MakeGenericMethod(string methodName, params Type[] types) {
			const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
			MethodInfo mi = typeof(LuaHelp).GetMethod(methodName, flags);
			return mi?.MakeGenericMethod(types);
		}

		#region action

		public static object MakeAction1(Action<object> action, Type type) {
			MethodInfo genericMi = MakeGenericMethod("MakeAction1", type);
			return genericMi.Invoke(null, new object[] {action});
		}
		private static Action<T> MakeAction1<T>(Action<object> action) {
			return arg => action(arg);
		}

		public static object MakeAction2(Action<object, object> action, Type type1, Type type2) {
			MethodInfo genericMi = MakeGenericMethod("MakeAction2", type1, type2);
			return genericMi.Invoke(null, new object[] {action});
		}
		private static Action<T1, T2> MakeAction2<T1, T2>(Action<object, object> action) {
			return (arg1, arg2) => action(arg1, arg2);
		}

		public static object MakeAction3(Action<object, object, object> action, Type type1, Type type2, Type type3) {
			MethodInfo genericMi = MakeGenericMethod("MakeAction3", type1, type2, type3);
			return genericMi.Invoke(null, new object[] {action});
		}
		private static Action<T1, T2, T3> MakeAction3<T1, T2, T3>(Action<object, object, object> action) {
			return (arg1, arg2, arg3) => action(arg1, arg2, arg3);
		}

		public static object MakeAction4(Action<object, object, object, object> action, Type type1, Type type2, Type type3, Type type4) {
			MethodInfo genericMi = MakeGenericMethod("MakeAction4", type1, type2, type3, type4);
			return genericMi.Invoke(null, new object[] {action});
		}
		private static Action<T1, T2, T3, T4> MakeAction4<T1, T2, T3, T4>(Action<object, object, object, object> action) {
			return (arg1, arg2, arg3, arg4) => action(arg1, arg2, arg3, arg4);
		}

		#endregion

		#region func

		public static object MakeFunc(Func<object> func, Type type) {
			MethodInfo genericMi = MakeGenericMethod("MakeFunc", type);
			return genericMi.Invoke(null, new object[] {func});
		}
		private static Func<T> MakeFunc<T>(Func<object> func) {
			return () => func() is T t ? t : default;
		}

		public static object MakeFunc1(Func<object, object> func, Type type, Type typeResult) {
			MethodInfo genericMi = MakeGenericMethod("MakeFunc1", type, typeResult);
			return genericMi.Invoke(null, new object[] {func});
		}
		private static Func<T, TResult> MakeFunc1<T, TResult>(Func<object, object> func) {
			return arg => func(arg) is TResult result ? result : default;
		}

		public static object MakeFunc2(Func<object, object, object> func, Type type1, Type type2, Type typeResult) {
			MethodInfo genericMi = MakeGenericMethod("MakeFunc2", type1, type2, typeResult);
			return genericMi.Invoke(null, new object[] {func});
		}
		private static Func<T1, T2, TResult> MakeFunc2<T1, T2, TResult>(Func<object, object, object> func) {
			return (arg1, arg2) => func(arg1, arg2) is TResult result ? result : default;
		}

		public static object MakeFunc3(Func<object, object, object, object> func, Type type1, Type type2, Type type3, Type typeResult) {
			MethodInfo genericMi = MakeGenericMethod("MakeFunc3", type1, type2, type3, typeResult);
			return genericMi.Invoke(null, new object[] {func});
		}
		private static Func<T1, T2, T3, TResult> MakeFunc3<T1, T2, T3, TResult>(Func<object, object, object, object> func) {
			return (arg1, arg2, arg3) => func(arg1, arg2, arg3) is TResult result ? result : default;
		}

		public static object MakeFunc4(Func<object, object, object, object, object> func, Type type1, Type type2, Type type3, Type type4, Type typeResult) {
			MethodInfo genericMi = MakeGenericMethod("MakeFunc4", type1, type2, type3, type4, typeResult);
			return genericMi.Invoke(null, new object[] {func});
		}
		private static Func<T1, T2, T3, T4, TResult> MakeFunc4<T1, T2, T3, T4, TResult>(Func<object, object, object, object, object> func) {
			return (arg1, arg2, arg3, arg4) => func(arg1, arg2, arg3, arg4) is TResult result ? result : default;
		}

		#endregion

		#endregion
	}

}

