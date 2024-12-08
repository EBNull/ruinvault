
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing.Text;
using System.IO;
using HarmonyLib;
using SpaceGame.Ship;

namespace ruinvault;

class PatchEnableDevmenu
{
	// https://github.com/BepInEx/HarmonyX/wiki/Enumerator-patches#notes-on-targeting-movenext
	[HarmonyPostfix, HarmonyPatch(typeof(TitleAndPauseMainMenuPanel), "GetOptions")]
	static IEnumerable<TitleAndPauseItem> GetOptions(IEnumerable<TitleAndPauseItem> __result)
	{
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
		var i = 0;
		foreach (var item in __result)
		{
			if (i == 1)
			{
				yield return devItem;
			}
			yield return item;
			i += 1;
		}
	}

}