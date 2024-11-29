
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using MonoMod.Utils;
using SpaceGame.Ship;

namespace ruinvault;

internal class PatchNoSteamRestart
{
	[HarmonyPrefix, HarmonyPatch(typeof(Steamworks.SteamAPI), "RestartAppIfNecessary")]
	private static bool NoRestart(ref bool __result)
	{
		__result = false;
		Tools.MaybeLogInfo("log_skippedSteam", "Skipped SteamAPI.RestartAppIfNecessary");
		return false;
	}
}