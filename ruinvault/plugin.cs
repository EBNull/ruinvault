using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ruinvault;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public static Type[] patchClasses = [
		//typeof(PatchSetEditorFlag),
		typeof(PatchEnableSaveSlots),
		typeof(PatchNoSteamRestart),
		//typeof(PatchRiverAutoReset),
		typeof(PatchShipSpeed), // maybe doesn't work? I haven't done a lot of a/b testing
		typeof(PatchEnableDevModeSaves),
		typeof(PatchEnablePhotoMode),
		typeof(PatchFastFades),
		typeof(PatchNoGhosts)
	];

	private void Awake()
	{
		Tools.StaticLogger = base.Logger;
		Tools.Logger.LogInfo("Plugin ruinvault is loaded!");

		EnableDebugOptions();

		ApplyPatches();


	}

	private void Update()
	{
		// TODO: hotkeys and such
		var sm = SpaceGame.Pathfinding.ShipPatherManager.Instance;
		if (sm != null && sm.settings != null)
		{
			sm.settings.timeOutsideRiverToBreakPath = 3f;
			sm.settings.skipMaxTimeToReachFromHere = 0f;
			sm.settings.skipMinUntravelledRiverTime = 0f;
		}
		var srm = SpaceGame.SpaceRuinManager.Instance;
		if (srm != null && srm._settings != null)
		{

			srm._settings.minRuinCreationDistanceRangeOnStart = 100f;
			srm._settings.ruinCreationDistanceRange = new UnityEngine.Vector2(100f, 100f);
		}
		var sgi = SpaceGame.SpaceGameMaster.Instance;
		if (sgi != null && sgi.gameObject != null)
		{
			sgi.gameObject.GetOrAddComponent<SpaceGame.SpaceTestPositioner>();
		}
	}

	private void EnableDebugOptions()
	{
		// Start with the base "disabled" options (opts initially points to a clean one)
		DebugOptions.Options opts = DebugOptions.opts.Copy();
		// Point the dynamic debug options (part of a scriptableobject) to this clean one
		MonoSingleton<DebugOptions>.Instance.options.options = opts;

		// Useful while debugging
		if (!Tools.IsOnSteamDeck())
		{
			//opts.dontLockMouse = true;
			//opts.dontPauseOnChangingApplicationFocus = true;

			// Keep the title screen
			opts.skipTitleScreenOnLoad = true;
		}

		//opts.skipQuoteScreenOnLaunch = true;
		opts.skipSplashScreensOnLaunch = true;
		//opts.skipStoryIntroFade = true;
		opts.allowTeleportOnMap = true;


		opts.dontRequirePlotCourseToExitPlanner = true;
		opts.allowCheatControls = true; // H to heal
		opts.clickToSkip = true;
		opts.skipStoryIntroFade = true;
		opts.showStoryAsyncProgress = true;

		// Set speed (hold shift to enable)
		opts.fastScalar = 3f; // Base game is 1f, Dev default is 5f, this is somewhere in between

		MonoSingleton<DebugOptions>.Instance.useInBuilds = true;
		MonoSingleton<DebugOptions>.Instance.enabled = true;

	}

	private void ApplyPatches()
	{
		foreach (Type pc in patchClasses)
		{
			var ts = Tools.GetTypeString(pc);
			Exception? e = null;
			try
			{
				Harmony.CreateAndPatchAll(pc);
				Tools.LogInfo($"Applied {ts}");
				Tools.LogMessage($"Applied {ts}");
				continue;
			}
			catch (Exception ie)
			{
				e = ie;
			}
			Tools.LogError($"Failed to apply {ts}");
			Tools.LogError(e.ToString());
			Tools.LogMessage($"Failed to apply {ts}");
		}
	}
}