using System;
using System.Diagnostics;
using System.Text;

using UDebug = UnityEngine.Debug;
using UObject = UnityEngine.Object;

public enum LogLevel {
	NONE = 0,
	ERROR = 10,
	WARN = 20,
	INFO = 30,
	DEBUG = 40,
	VERBOSE = 50,
	MAX = 60
}

public static class Log {
	public static Action<LogLevel, string> OnLog { set; get; }
	private static LogLevel logLevel = LogLevel.MAX;
	public static LogLevel LogLevel {
		get => logLevel;
		set {
			logLevel = value;
			UDebug.unityLogger.logEnabled = value > LogLevel.NONE;
		}
	}

	public static void Verbose(params object[] objs) {
		if (logLevel >= LogLevel.VERBOSE) {
			string str = ObjsToString(objs);
			UDebug.Log(str);
			OnLog?.Invoke(LogLevel.VERBOSE, str);
		}
	}
	public static void VerboseGo(UObject context, params object[] objs) {
		if (logLevel >= LogLevel.VERBOSE) {
			string str = ObjsToString(objs);
			UDebug.Log(str, context);
			OnLog?.Invoke(LogLevel.VERBOSE, str);
		}
	}

	public static void Debug(params object[] objs) {
		if (logLevel >= LogLevel.DEBUG) {
			string str = ObjsToString(objs);
			UDebug.Log(str);
			OnLog?.Invoke(LogLevel.DEBUG, str);
		}
	}
	public static void DebugGo(UObject context, params object[] objs) {
		if (logLevel >= LogLevel.DEBUG) {
			string str = ObjsToString(objs);
			UDebug.Log(str, context);
			OnLog?.Invoke(LogLevel.DEBUG, str);
		}
	}
	
	public static void Info(params object[] objs) {
		if (logLevel >= LogLevel.INFO) {
			string str = ObjsToString(objs);
			UDebug.Log(str);
			OnLog?.Invoke(LogLevel.INFO, str);
		}
	}
	public static void InfoGo(UObject context, params object[] objs) {
		if (logLevel >= LogLevel.INFO) {
			string str = ObjsToString(objs);
			UDebug.Log(str, context);
			OnLog?.Invoke(LogLevel.INFO, str);
		}
	}
	
	public static void Warn(params object[] objs) {
		if (logLevel >= LogLevel.WARN) {
			string str = ObjsToString(objs);
			UDebug.LogWarning(str);
			OnLog?.Invoke(LogLevel.WARN, str);
		}
	}
	public static void WarnGo(UObject context, params object[] objs) {
		if (logLevel >= LogLevel.WARN) {
			string str = ObjsToString(objs);
			UDebug.LogWarning(str, context);
			OnLog?.Invoke(LogLevel.WARN, str);
		}
	}
	
	public static void Error(params object[] objs) {
		if (logLevel >= LogLevel.ERROR) {
			string str = ObjsToString(objs);
			UDebug.LogError(str);
			OnLog?.Invoke(LogLevel.ERROR, str);
		}
	}
	public static void ErrorGo(UObject context, params object[] objs) {
		if (logLevel >= LogLevel.ERROR) {
			string str = ObjsToString(objs);
			UDebug.LogError(str, context);
			OnLog?.Invoke(LogLevel.ERROR, str);
		}
	}
	
	[Conditional("UNITY_ASSERTIONS")]
	public static void Assert(params object[] objs) {
		if (logLevel > LogLevel.NONE) {
			string str = ObjsToString(objs);
			UDebug.LogAssertion(str);
			OnLog?.Invoke(LogLevel.ERROR, str);
		}
	}
	[Conditional("UNITY_ASSERTIONS")]
	public static void AssertGo(UObject context, params object[] objs) {
		if(logLevel > LogLevel.NONE){
			string str = ObjsToString(objs);
			UDebug.LogAssertion(str, context);
			OnLog?.Invoke(LogLevel.ERROR, str);
		}
	}
	
	public static void Exception(Exception exception) {
		if(logLevel > LogLevel.NONE){
			UDebug.LogException(exception);
			OnLog?.Invoke(LogLevel.ERROR, exception.ToString());
		}
	}
	public static void ExceptionGo(UObject context, Exception exception) {
		if(logLevel > LogLevel.NONE){
			UDebug.LogException(exception, context);
			OnLog?.Invoke(LogLevel.ERROR, exception.ToString());
		}
	}

	private static string ObjsToString(params object[] objs) {
		int length = objs.Length;
		switch (length) {
			case <= 0:
				return "";
			case 1:
				return objs[0]?.ToString() ?? "Null";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(objs[0] ?? "Null");
		for (int index = 1; index < length; ++index) {
			stringBuilder.Append("\t");
			stringBuilder.Append(objs[index] ?? "Null");
		}
		return stringBuilder.ToString();
	}
}

