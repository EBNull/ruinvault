
using System;
using System.Data.SqlTypes;
using System.Drawing.Text;
using System.IO;
using HarmonyLib;
using SpaceGame.Ship;
using UnityEngine;

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
	private static bool SaveDesktop(SaveThread __instance, ref string saveData, ref string __result)
	{
		if (!PatchLoadRawSaves.okToSave) {
			__result = "Skipping save because save slot message has not been accepted";
			Tools.LogMessage(__result);
			return false;
		}
		var rawSavePath = BetterSaves.GetExtraRawSaveFilename(__instance.savePath);
		BetterSaves.WriteRawSaveFile(rawSavePath, saveData);
		Tools.MaybeLogInfo(-1, $"Wrote raw save to {rawSavePath}");
		return true;
	}
}

class PatchLoadRawSaves
{
	static int loadCount = 0;
	public static bool okToSave = false;

	static void MaybeMessageSaveNotice(BetterSaves.SaveSelectionInfo? sel) {
		loadCount++;
		var slot = BetterSaves.saveSlot;
		var maybeWarn = "";
		if (slot != 0) {
			maybeWarn = "<size=80%>This save file <b>will not</b> be synced to the cloud.<br>";
		} else {
			maybeWarn = "<size=50%>This save file <b>will</b> be synced to the cloud.<br>";
		}
		if (loadCount == 1) {
			var ls = LoadingScreenController.Instance;
			ls.enabled = false;
			var filename = Path.GetFileName(Game.Instance.savePath);
			var msg = $"{maybeWarn}<size=50%>Starting a new game at {filename}";
			if (sel != null) {
				var gi = sel?.file.GetSimpleGameData();
				msg = $"{maybeWarn}";
				msg += $"<size=50%>{sel?.file.Describe()}\n\n";
				msg += $"<size=50%>Loading {Path.GetFileName(sel?.file.name)}\n{sel?.reason}";
			}
			if (Tools.IsOnSteamDeck()) {
				msg += "\n\n<size=45%>Set Steam launch options to `./hv.sh %command% -slot=#` to select slot";
			} else {
				msg += "\n\n<size=45%>Set Steam launch options to `%command% -slot=#` to select slot";
			}
			GameLib.MessageBox($"Using save slot {BetterSaves.saveSlot}", msg, "Continue", () => {
				okToSave = true;
				ls.enabled = true;
			}, "Exit", () => {
				Tools.Die();
			});
		}

	}


	[HarmonyPostfix, HarmonyPatch(typeof(Game), "DesktopSaveFileExists")]
	private static void DesktopSaveFileExist(ref bool __result)
	{
		if (!__result) {
			MaybeMessageSaveNotice(null);
		}
	}

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
		MaybeMessageSaveNotice(sel);
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