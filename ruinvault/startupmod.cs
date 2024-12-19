using UnityEngine;

namespace ruinvault;

[Feature(DefaultEnabled = true, Priority = -100)]
class StartupMod
{
	public StartupMod()
	{
		Tools.LogInfo("Applying startup debug options");
		// Start with the base "disabled" options (opts initially points to a clean one)
		DebugOptions.Options opts = DebugOptions.opts.Copy();
		// Point the dynamic debug options (part of a scriptableobject) to this clean one
		MonoSingleton<DebugOptions>.Instance.options.options = opts;

		// Useful while debugging
		if (!Tools.IsOnSteamDeck())
		{
			opts.dontLockMouse = true;
			opts.dontPauseOnChangingApplicationFocus = true;

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
}