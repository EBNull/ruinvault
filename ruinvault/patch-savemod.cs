
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ruinvault;

internal class PatchEnableSaveSlots
{
	// Only the zeroth (unnumbered) slot will sync to steam - https://steamdb.info/app/774201/ufs/
	static int saveSlot = 2;
	[HarmonyPostfix, HarmonyPatch(typeof(Game), "savePath", MethodType.Getter)]
	private static void get_savePath(ref string __result)
	{
		if (saveSlot != 0) {
			// Change save filename, otherwise use default
			var oldDir = new FileInfo(__result).Directory.FullName;
			var oldName = Path.GetFileNameWithoutExtension(__result);
			var oldExt = Path.GetExtension(__result)??"";
			__result = Path.Combine(oldDir, $"{oldName}_Slot_{saveSlot}{oldExt}");
		}
		Tools.MaybeLogInfo(-1, $" -> slot {saveSlot} ({__result})");
	}

	[HarmonyPrefix, HarmonyPatch(typeof(DevSaveFileManager), "CreateNew")]
	private static void get_devsavePath(ref string fileName)
	{
		// Change save filename, otherwise use default
		var oldDir = new FileInfo(fileName).Directory.FullName;
		var oldName = Path.GetFileNameWithoutExtension(fileName);
		var oldExt = Path.GetExtension(fileName)??"";
		fileName = Path.Combine(oldDir, $"Slot_{saveSlot}_{oldName}{oldExt}");
		Tools.MaybeLogInfo(-1, $" -> devsave slot {saveSlot} ({fileName})");
	}
}