
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Translation;

namespace ruinvault;

internal class PatchFastFades
{
	private static void shortenLss(LoadingScreenSetup lss)
	{
		if (lss == null)
		{
			return;
		}
		var tgt = 0.5f;
		if (lss.customSplashScreenPagePrefabs != null)
		{
			foreach (var p in lss.customSplashScreenPagePrefabs)
			{
				p.fadeInTime = tgt;
				p.fadeOutTime = tgt;
			}
		}
		lss.fadeOutTime = Math.Min(lss.fadeOutTime, tgt);
		lss.fadeInTime = Math.Min(lss.fadeInTime, tgt);

		//TODO: Make some of the below adjustable, perhaps?
		//lss.autoContinueAfterReadyTime = 2f;
		//lss.offerContinueButton = false;
	}
	private static void shortenScls(StoryCatchupLayoutSettings ls)
	{
		if (ls == null)
		{
			return;
		}
		ls.lineEndDelay = 0;
		ls.attributionEndDelay = 0;
		ls.headerEndDelay = 0;
		ls.lineBreakDelay = 0;
		ls.punctuationDelay = 0;
		ls.delayPerWord = 0f;
		ls.wordFadeAnimTime = 0f;
		ls.wordPositionAnimTime = 0;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(LoadingScreenController), "Begin")]
	private static void LSC_Begin(ref LoadingScreenController __instance, ref LoadingScreenSetup setup)
	{
		shortenLss(setup);
		string hdr = "";
		if (setup != null && setup.storyLines != null)
		{
			var hls = setup.storyLines.FilteredTempList((cu) => cu.type == StoryCatchupLine.LineType.Header);
			if (hls.Count > 0)
			{
				hdr = hls[0].text;
			}
		}
		var lsc = __instance;
		var scsp = lsc.storyCatchupSplashPage;
		if (scsp != null)
		{
			shortenScls(scsp.layoutSettings);
			scsp.fadeInTime = 0.5f;
			scsp.fadeOutTime = 0.1f;
		}
		var lss = setup;
		Tools.MaybeLogInfo(-1, $"type={lss?.type} hdr='{hdr}' in={lss?.fadeInTime} out={lss?.fadeOutTime} offerContinueButton={lss?.offerContinueButton} ");
	}

	// GameController

	[HarmonyPrefix, HarmonyPatch(typeof(GameController), "LoadStoryScene")]
	private static void GC_LSS(ref GameController.LocationLoadParams loadParams)
	{
		loadParams.fadeOut = false;
		Tools.MaybeLogInfo(-1, "loadParams=" + loadParams.ToString());
	}

	// Blackout

	[HarmonyPrefix, HarmonyPatch(typeof(Blackout), "FadeIn", new[] { typeof(float), typeof(Action), typeof(bool) })]
	private static void Blackout_FadeIn(ref float duration)
	{
		duration = Math.Min(duration, 0.4f);
		Tools.MaybeLogInfo(-1, "duration=" + duration);
	}

	[HarmonyPrefix, HarmonyPatch(typeof(Blackout), "FadeOut", new[] { typeof(float), typeof(Action), typeof(bool), typeof(bool) })]
	private static void Blackout_FadeOut(ref float duration)
	{
		duration = Math.Min(duration, 0.4f);
		Tools.MaybeLogInfo(-1, "duration=" + duration);
	}

}