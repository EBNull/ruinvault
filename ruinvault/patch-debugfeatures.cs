
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ruinvault;


internal class PatchSetEditorFlag
{
	// Enables comma for space river path info
	[HarmonyPostfix, HarmonyPatch(typeof(UnityEngine.Application), "isEditor", MethodType.Getter)]
	private static void SetIsEditor(ref bool __result)
	{
		__result = true;
		Tools.MaybeLogInfo(5, " -> true");
	}
}

internal class PatchEnablePhotoMode
{
	[HarmonyPostfix, HarmonyPatch(typeof(PhotoModeController), "canTogglePhotoMode", MethodType.Getter)]
	private static void EnablePhotoMode(ref bool __result)
	{
		__result = true;
		Tools.MaybeLogInfo(5, " -> true");
	}
}

internal class PatchEnableDevModeSaves
{
	// DevSaves are additional unencrypted save files.
	// They can be selected in-game via shift+L (for load) - though it might be broken
	//
	// The content is a file containing five JSON dictonaries seperated by newlines:
	//
	// saveVersion
	// metaInformationJSON
	// stateInformationJSON
	// gameDataJSON
	// inkJson
	[HarmonyPostfix, HarmonyPatch(typeof(SaveThread), "doDevModeSave", MethodType.Getter)]
	private static void EnableDevModeSaves(ref bool __result)
	{
		__result = true;
		Tools.MaybeLogInfo(-1, " -> true; Dev saves enabled");
	}
}