
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

	static void PrefixBasenameWithSlot(ref string fp, int slot) {
			fp = PathUtil.ModParts(fp, (p) => {p.name=$"Slot_{slot}_{p.name}{slot}"; return p;});
	}
	
	static void PostfixBasenameWithSlot(ref string fp, int slot) {
			fp = PathUtil.ModParts(fp, (p) => {p.name=$"{p.name}_Slot_{slot}"; return p;});
	}

	[HarmonyPostfix, HarmonyPatch(typeof(Game), "savePath", MethodType.Getter)]
	private static void get_savePath(ref string __result)
	{
		// Cloud saves are supported only for the barename
		if (saveSlot != 0) {
			// Change save filename, otherwise use default
			PostfixBasenameWithSlot(ref __result, saveSlot);
		}
		Tools.MaybeLogInfo(-1, $" -> slot {saveSlot} ({__result})");
	}

	[HarmonyPrefix, HarmonyPatch(typeof(DevSaveFileManager), "CreateNew")]
	private static void get_devsavePath(ref string fileName)
	{
		// Change save filename
		PrefixBasenameWithSlot(ref fileName, saveSlot);
		Tools.MaybeLogInfo(-1, $" -> devsave slot {saveSlot} ({fileName})");
	}
}

internal class PatchAlsoSaveRawSaves
{
	static string PostfixedBasename(string fp, string pf) {
			return PathUtil.ModParts(fp, (p) => {p.name=$"{p.name}{pf}"; return p;});
	}
	
	[HarmonyPrefix, HarmonyPatch(typeof(SaveThread), "SaveDesktop")]
	private static void SaveDesktop(SaveThread __instance, ref string saveData) {
		var st = __instance;
		var cryptSavePath = st.savePath;
		var rawSavePath = PathUtil.PostfixBasename(cryptSavePath, "_raw");
		Atomic.WriteFile(rawSavePath, saveData);
	}

}