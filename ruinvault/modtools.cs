
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using MonoMod.Utils;
using SpaceGame.Ship;

namespace ruinvault;

public static class Tools
{
	public static ManualLogSource? StaticLogger;
	private static ManualLogSource? UninitLogger;

	public static ManualLogSource Logger
	{
		get
		{
			if (StaticLogger == null)
			{
				UninitLogger ??= BepInEx.Logging.Logger.CreateLogSource("ruinvault");
				return UninitLogger;
			}
			return StaticLogger;
		}
	}

	public static bool IsOnSteamDeck()
	{
		return UnityEngine.SystemInfo.operatingSystem.Contains("SteamOS");
	}

	public static void Die()
	{
		Console.Out?.Flush();
		Console.Error?.Flush();
		System.Diagnostics.Process.GetCurrentProcess().Kill();
	}

	public static Dictionary<string, int> timesPerformed = new();
	public static void MaybeDo(int maxTimes, string key, Action act)
	{
		int count = 1;
		if (timesPerformed.TryGetValue(key.ToLower(), out int value))
		{
			count = value + 1;
		}
		timesPerformed[key.ToLower()] = count;
		if (count <= maxTimes || maxTimes == -1)
		{
			act();
			if (count == maxTimes)
			{
				Logger.LogInfo($"Supressing additional log entries for {key}");
			}
		}
	}

	public static void LogInfo(string msg)
	{
		var maxTimes = -1;
		var mn = GetStackString(1);
		MaybeDo(maxTimes, mn, delegate { Logger.LogInfo(mn + ": " + msg); });
	}

	public static void LogError(string msg)
	{
		var maxTimes = -1;
		var mn = GetStackString(1);
		MaybeDo(maxTimes, mn, delegate { Logger.LogError(mn + ": " + msg); });
	}

	public static void LogMessage(string msg)
	{
		var maxTimes = -1;
		var mn = GetStackString(1);
		MaybeDo(maxTimes, mn, delegate { Logger.LogMessage(mn + ": " + msg); });
	}

	public static void MaybeLogInfo(int maxTimes, string msg)
	{
		var mn = GetStackString(1);
		MaybeDo(maxTimes, mn, delegate { Logger.LogInfo(mn + ": " + msg); });
	}

	public static void MaybeLogInfo(string key, string msg)
	{
		var maxTimes = 5;
		var mn = GetStackString(1);
		MaybeDo(maxTimes, key, delegate { Logger.LogInfo(mn + ": " + msg); });
	}

	public static void MaybeLogInfo(int maxTimes, string key, string msg)
	{
		var mn = GetStackString(1);
		MaybeDo(maxTimes, key, delegate { Logger.LogInfo(mn + ": " + msg); });
	}

	public static string GetTypeString(Type t)
	{
		return $"{t}";
	}

	public static string GetMethodString(MethodBase m)
	{
		var t = m.DeclaringType;
		var el = HarmonyLib.HarmonyMethodExtensions.GetFromMethod(m);
		var dt = el.Join((HarmonyMethod p) => $"{p.declaringType}.{p.methodName}", ", ");
		var maybeDt = "";
		if (dt.Length > 0)
		{
			maybeDt = $"[{dt}] ";
		}
		var pstr = m.GetParameters().Join((ParameterInfo p) => p.ToString(), ", ");
		return $"{maybeDt}{t}.{m.Name}({pstr})";
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string GetStackString(int back = 0)
	{
		var st = new StackTrace();
		var sf = st.GetFrame(back + 1);

		return GetMethodString(sf.GetMethod());
	}
	
	[DllImport("user32.dll", SetLastError = true, CharSet= CharSet.Unicode)]
	public static extern int MessageBoxW(IntPtr hWnd, String text, String caption, uint type);
	
	public static void MessageBox(string title, string msg) {
		MessageBoxW(IntPtr.Zero, msg??"", title??"", 0);
	}
}