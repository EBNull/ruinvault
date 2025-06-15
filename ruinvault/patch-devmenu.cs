
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using HarmonyLib;
using SpaceGame.Ship;
using Translation;
using UnityEngine.Experimental.UIElements;

namespace ruinvault;

[Feature(DefaultEnabled = true)]
class PatchEnableDevmenu
{
	// https://github.com/BepInEx/HarmonyX/wiki/Enumerator-patches#notes-on-targeting-movenext
	[HarmonyPostfix, HarmonyPatch(typeof(TitleAndPauseMainMenuPanel), "GetOptions")]
	static IEnumerable<TitleAndPauseItem> GetOptions(IEnumerable<TitleAndPauseItem> __result)
	{
		// TODO: loading these with instantiate means we never remove them; this can lead to lag as these build up
		var e = __result.GetEnumerator();
		e.MoveNext();
		yield return e.Current; // Always put "Resume" first

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


		const int maxPages = 1;
		for (int page = 0; page < maxPages; page++)
		{
			var innerPage = page;
			var optionItem = TitleAndPauseUI.Instance.buttonPrototype.Instantiate<TitleAndPauseButtonOption>(delegate (TitleAndPauseButtonOption option)
			{
				option.text.text = "ruinvault Mod Menu";
				if (maxPages > 1)
				{
					option.text.text = $"ruinvault Mod Menu (Page {page + 1})";
				}
				option.OnSelect += () =>
				{
					var ns = (TitleAndPauseUI.FocusState)((int)ModOptionsPanel.ModOptionsFocusState + innerPage);
					Tools.LogInfo($"Setting focusstate to {ns}!");
					TitleAndPauseUI.Instance.focusState = ns;
					Tools.LogInfo($"focusstate is now {(int)TitleAndPauseUI.Instance.focusState}!");
					TitleAndPauseUI.Instance.settingsPanel.LayoutPanel(); // Needed for dynamic options
				};
			});

			yield return optionItem;
		}

		// Ugly hackery to put devmenu second to last
		TitleAndPauseItem? quit = null;
		while (e.MoveNext())
		{
			if (e.Current.text.text == "Quit")
			{
				quit = e.Current;
				continue;
			}
			yield return e.Current;
		}
		yield return devItem;

		if (quit is not null)
		{
			yield return quit;
		}
	}
}