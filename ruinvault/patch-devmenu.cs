
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using HarmonyLib;
using SpaceGame.Ship;

namespace ruinvault;

[Feature(DefaultEnabled = true)]
class PatchEnableDevmenu
{
	// https://github.com/BepInEx/HarmonyX/wiki/Enumerator-patches#notes-on-targeting-movenext
	[HarmonyPostfix, HarmonyPatch(typeof(TitleAndPauseMainMenuPanel), "GetOptions")]
	static IEnumerable<TitleAndPauseItem> GetOptions(IEnumerable<TitleAndPauseItem> __result)
	{
		// TODO: loading these with instantiate means we never remove them; this can lead to lag as these build up
		
		var devItem = TitleAndPauseUI.Instance.buttonPrototype.Instantiate<TitleAndPauseButtonOption>(delegate (TitleAndPauseButtonOption option)
		{
			option.text.text = "Dev Menu (EVERYTHING HERE CAN BREAK YOUR SAVE)";
			option.OnSelect += () =>
			{
				GameLib.MessageBox("Warning", "Everything in this menu has the potential to break your save <b>forever</b>, even after reload.\n\n" +
				"You may lose any artifacts or transalations you've done, and you are likely to lose your place in the story.",
						"Continue?", () =>
						{
							TitleAndPauseUI.Instance.FocusDevMenu();
						}, "Cancel", () => { }
				);
			};
		});

		yield return devItem;
		foreach (var item in __result)
		{
			yield return item;
		}
	}

}