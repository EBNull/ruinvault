
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ruinvault;


[Feature(DefaultEnabled = false)]
internal class SetUnityIsEditorFlag
{
	// Enables comma for space river path info
	[HarmonyPostfix, HarmonyPatch(typeof(UnityEngine.Application), "isEditor", MethodType.Getter)]
	private static void SetIsEditor(ref bool __result)
	{
		__result = true;
		Tools.MaybeLogInfo(5, " -> true");
	}
}

[Feature(DefaultEnabled = true)]
internal class EnablePhotoMode
{
	[HarmonyPostfix, HarmonyPatch(typeof(PhotoModeController), "canTogglePhotoMode", MethodType.Getter)]
	private static void canTogglePhotoMode(ref bool __result)
	{
		__result = true;
		Tools.MaybeLogInfo(5, " -> true");
	}
}

[Feature(DefaultEnabled = true)]
internal class WriteDevSaves
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

[Feature(DefaultEnabled = true)]
class DisableUnityDevConsole : MonoBehaviour
{
	[HarmonyPostfix, HarmonyPatch(typeof(Debug), nameof(Debug.developerConsoleVisible), MethodType.Getter)]
	private static void NoDevConsole(ref bool __result)
	{
		__result = false;
		Tools.MaybeLogInfo(-1, " -> false; Unity developerConsoleVisible");
	}

	public void LateUpdate()
	{
		// Never helpful, only annoying. Pops up automatically.
		Debug.developerConsoleVisible = false;
	}
}