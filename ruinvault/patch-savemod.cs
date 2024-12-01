
using System;
using System.Data.SqlTypes;
using System.Drawing.Text;
using System.IO;
using HarmonyLib;
using SpaceGame.Ship;

namespace ruinvault;

class PatchEnableSaveSlots
{
	[HarmonyPostfix, HarmonyPatch(typeof(Game), "savePath", MethodType.Getter)]
	private static void get_savePath(ref string __result)
	{
		__result = BetterSaves.GetCurrentSaveFilename(__result);
		Tools.MaybeLogInfo(-1, $" -> slot {BetterSaves.saveSlot} ({__result})");
	}

	[HarmonyPrefix, HarmonyPatch(typeof(DevSaveFileManager), "CreateNew")]
	private static void get_devsavePath(ref string fileName)
	{
		fileName = BetterSaves.GetCurrentDevSaveFilename(fileName);
		Tools.MaybeLogInfo(-1, $" -> devsave slot {BetterSaves.saveSlot} ({fileName})");
	}
}

class PatchAlsoSaveRawSaves
{
	[HarmonyPrefix, HarmonyPatch(typeof(SaveThread), "SaveDesktop")]
	private static void SaveDesktop(SaveThread __instance, ref string saveData)
	{
		var rawSavePath = BetterSaves.GetExtraRawSaveFilename(__instance.savePath);
		BetterSaves.WriteRawSaveFile(rawSavePath, saveData);
		Tools.MaybeLogInfo(-1, $"Wrote raw save to {rawSavePath}");
	}
}

class PatchLoadRawSaves
{
	[HarmonyPrefix, HarmonyPatch(typeof(CryptSaving), "ReadFromFile")]
	private static bool ReadFromFile(ref string filePath, ref string? __result)
	{
		// This is a prefix patch to enable us to load and observe both the crypted save and raw save.
		// We need to skip the original by returning false.
		var cryptSavePath = filePath ?? "";
		var rawSavePath = BetterSaves.GetExtraRawSaveFilename(cryptSavePath);
		var csl = SaveLoader.FromAnyFile(cryptSavePath);
		if (!Game.IsInitialized)
		{
			// This never happens, but it couldn't hurt?
			Tools.LogMessage($"Loading crypted save {cryptSavePath} because game instance is not initialized");
			__result = csl.data;
			return false;
		}
		var rsl = SaveLoader.FromAnyFile(rawSavePath);
		var sel = BetterSaves.SelectSave(csl, rsl);
		__result = sel.file.data;
		Tools.LogMessage(sel.reason);
		var gsi = sel.file.GetGSI();
		Tools.LogMessage($"Loaded gsi location is {gsi.locationName}; play time {gsi.totalPlayTime}");
		return false;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(Game), "currentLocation", MethodType.Setter)]
	private static void set_currentLocation(ref Game __instance, ref StoryLocation __0)
	{
		var cl = __instance.currentLocation;
		var cls = cl?.uniqueObjectIdentifier ?? "";
		Tools.LogInfo($"Game setting location from {cls} to {__0?.uniqueObjectIdentifier ?? ""}");
	}
}