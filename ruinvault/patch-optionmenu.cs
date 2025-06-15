
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using SoftMasking.Extensions;
using SpaceGame.Ship;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.UIElements;

namespace ruinvault;

[Feature(DefaultEnabled = true, Description = "WIP")]
class PatchModOptionsPanel : MonoBehaviour
{
	static ModOptionsPanel? panel;

	[HarmonyPostfix, HarmonyPatch(typeof(TitleAndPauseUI), nameof(TitleAndPauseUI.currentPanel), MethodType.Getter)]
	private static void TitleAndPauseUI_currentPanel_getter(ref TitleAndPauseUI __instance, ref BaseTitleAndPausePanel __result)
	{
		ref var tpi = ref __instance;
		if (tpi.focusState < ModOptionsPanel.ModOptionsFocusState)
		{
			return;
		}

		if (panel is null)
		{
			panel = new ModOptionsPanel { };
		}

		// We use the same instance, technically, so we always return the parent panel
		__result = tpi.settingsPanel;
	}


	// https://github.com/BepInEx/HarmonyX/wiki/Enumerator-patches#notes-on-targeting-movenext
	[HarmonyPostfix, HarmonyPatch(typeof(TitleAndPauseSettingsPanel), "GetOptions")]
	static IEnumerable<TitleAndPauseItem> GetOptions(IEnumerable<TitleAndPauseItem> __result, TitleAndPauseSettingsPanel __instance)
	{
		Tools.LogInfo($"getoptions; focusstate={(int)TitleAndPauseUI.Instance.focusState}");
		if (TitleAndPauseUI.Instance.focusState < ModOptionsPanel.ModOptionsFocusState)
		{
			foreach (var item in __result)
			{
				yield return item;
			}
			yield break;
		}

		if (panel is null)
		{
			yield break;
		}

		var opts = panel.GetOptions();
		var e = opts.GetEnumerator();
		var page = (int)TitleAndPauseUI.Instance.focusState - (int)ModOptionsPanel.ModOptionsFocusState;
		int pageSize = 14;
		while (page > 0)
		{
			page--;
			for (int i = 0; i < pageSize; i++)
			{
				if (!e.MoveNext())
				{
					break;
				}
				e.Current.prototype.ReturnToPool();
			}
		}
		for (int i = 0; i < pageSize; i++)
		{
			if (!e.MoveNext())
			{
				break;
			}
			yield return e.Current;
		}
		while (e.MoveNext())
		{
			{
				e.Current.prototype.ReturnToPool();
			}
		}
	}
}

public class ModOptionsPanel : TitleAndPauseSettingsPanel
{
	public static readonly TitleAndPauseUI.FocusState ModOptionsFocusState = (TitleAndPauseUI.FocusState)5;

	public override IEnumerable<TitleAndPauseItem> GetOptions()
	{
		yield return MonoSingleton<TitleAndPauseUI>.Instance.sliderPrototype.Instantiate(delegate (TitleAndPauseSliderOption option)
		{
			option.text.text = "Speedup Multiplier";
			option.minValue = 1f;
			option.maxValue = 10f;
			option.currentValue = DebugOptions.Instance.options.options.fastScalar;
			option.OnChange = (Action<float>)Delegate.Combine(option.OnChange, (Action<float>)delegate (float newVal)
			{
				DebugOptions.Instance.options.options.fastScalar = newVal;
				Tools.LogMessage($"Run multiplier set to {newVal}");
			});
		});
			yield return MonoSingleton<TitleAndPauseUI>.Instance.smallLabel.Instantiate(delegate (TitleAndPauseItem option)
			{
				option.text.text = "Hold shift to enable speedup";
			});

		var pl = Tools.CurrentPlugin;
		if (pl is null)
		{
			Tools.LogError($"Tools.CurrentPlugin is null!");
			yield break;
		}
		SortedList<string, FeatureWithProperties> fl = [];
		foreach (var f in pl.features)
		{
			fl.Add(f.Name, f);
		}
		foreach (var fkv in fl)
		{
			var f = fkv.Value;
			if (f.SurviveUnload)
			{
				continue;
			}
			if (!f.IngameToggle)
			{
				continue;
			}
			yield return MonoSingleton<TitleAndPauseUI>.Instance.togglePrototype.Instantiate(delegate (TitleAndPauseToggleOption option)
			{
				option.text.text = $"{f.Name}";
				option.currentValue = f.feature.IsEnabled();
				option.OnChange += delegate (bool newVal)
				{
					if (newVal)
					{
						f.feature.Enable();
					}
					else
					{
						f.feature.Disable();
					}
					Tools.LogMessage($"Changed {f.Name}.Enabled to {newVal}");
				};
			});
			if (f.Description != "")
			{
				yield return MonoSingleton<TitleAndPauseUI>.Instance.smallLabel.Instantiate(delegate (TitleAndPauseItem option)
				{
					option.text.text = f.Description;
				});
			}
		}
	}

	private new void DidPressReset()
	{
		DialogBoxParams dialogBoxParams = new("Reset Mod Settings", "Reset Mod settings to default?", "Sorry this doesn't work yet", delegate
		{
			LayoutOptions();
		}, "Cancel")
		{
			delayBeforeShowingOptions = 0f
		};
		MonoSingleton<DialogBoxController>.Instance.Show(dialogBoxParams);
	}
}