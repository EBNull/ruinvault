using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace ruinvault;

public struct FeatureWithProperties(IPluginFeature feature, bool EnabledByDefault=false, bool SurviveUnload=false) {
	public IPluginFeature feature = feature;
	public bool EnabledByDefault = EnabledByDefault;
	public bool SurviveUnload = SurviveUnload;
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	private static readonly Harmony baseHarmony = new(MyPluginInfo.PLUGIN_NAME);
	private static readonly Harmony foreverHarmony = new(MyPluginInfo.PLUGIN_NAME + "-forever");

	public FeatureWithProperties[] features = [];
	
	public Plugin() {
		features = [
			// Perma-enabled save slot support
			new FeatureWithProperties(new WrapFeature(typeof(PatchEnableSaveSlots), foreverHarmony.Id, this.gameObject), true, true),

			// Mod development tools
			new FeatureWithProperties(new WrapFeature(typeof(PatchNoSteamRestart), baseHarmony .Id, this.gameObject), true),
			new FeatureWithProperties(new WrapFeature(typeof(PatchDisableUnityDevConsole), baseHarmony.Id, this.gameObject), true),
			new FeatureWithProperties(new WrapFeature(typeof(PatchSetEditorFlag), baseHarmony.Id, this.gameObject), true),
			
			// Builtin development tools
			new FeatureWithProperties(new WrapFeature(typeof(PatchEnableDevmenu), baseHarmony.Id, this.gameObject), true),

			// Better Save Support 
			new FeatureWithProperties(new WrapFeature(typeof(PatchEnableDevModeSaves), baseHarmony.Id, this.gameObject), true),
			new FeatureWithProperties(new WrapFeature(typeof(PatchAlsoSaveRawSaves), baseHarmony.Id, this.gameObject), true),
			new FeatureWithProperties(new WrapFeature(typeof(PatchLoadRawSaves), baseHarmony.Id, this.gameObject), true),
			
			// Unreliable / Not well tested
			new FeatureWithProperties(new WrapFeature(typeof(PatchShipSpeed), baseHarmony.Id, this.gameObject), false),
			
			// Misc Options
			new FeatureWithProperties(new WrapFeature(typeof(PatchEnablePhotoMode), baseHarmony.Id, this.gameObject), false),
			new FeatureWithProperties(new WrapFeature(typeof(PatchRiverAutoReset), baseHarmony.Id, this.gameObject), false),

			// Visuals
			new FeatureWithProperties(new WrapFeature(typeof(PatchFastFades), baseHarmony.Id, this.gameObject), true),
			new FeatureWithProperties(new WrapFeature(typeof(PatchNoGhosts), baseHarmony.Id, this.gameObject), false),
			
			// WIP
			new FeatureWithProperties(new WrapFeature(typeof(Assets), baseHarmony.Id, this.gameObject), true),
		];
	}

	private void Awake()
	{
		Tools.LogInfo("Plugin ruinvault is loaded!");

		var b = Baton.Get();
		Tools.LogInfo($"{b.PluginAssemblies.Length} previous instances exist");
		b.PluginAssemblies = b.PluginAssemblies.AddToArray(Assembly.GetExecutingAssembly().GetName().Name);
		b.Commit();

		Assets.MaybeDumpAssets();

		EnableDebugOptions();

		SetupFeatures();
		
	}

	void OnDestroy()
	{
		Tools.Logger.LogInfo("Unloading ruinvault");
		foreach(var f in features) {
			if (!f.SurviveUnload) {
				f.feature.Disable();
			} else {
				var wf = f.feature as WrapFeature;
				Tools.LogInfo($"Skipping unload of {wf?.Name() ?? f.feature.GetType().Name} because it must survive");
			}
		}
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

	void EnableFeatures(FeatureWithProperties[] features)
	{
		foreach (var fp in features)
		{
			var f = fp.feature;
			var wf = f as WrapFeature;
			var ts = Tools.GetTypeString(wf?.feature ?? f.GetType());
			Tools.LogInfo($"Attempting to apply {ts}");
			Console.Out.Flush();
			Console.Error.Flush();
			bool ok = false;
			try
			{
				ok = f.Enable();
			} catch (Exception e) {
				Tools.LogError($"Failed to apply {ts}");
				Tools.LogError(e.ToString());
				Tools.LogMessage($"Failed to apply {ts}");
			}
			if (ok) {
				Tools.LogInfo($"Applied {ts}");
				Tools.LogMessage($"Applied {ts}");
			}
		}
	}

	private void SetupFeatures()
	{
		var b = Baton.Get();
		if (b.AlreadyPatchedSaveSlots) {
			Tools.LogMessage("Reloading ruinvault");
		} else {
			Tools.LogMessage("Loading ruinvault");
		}

		EnableFeatures(features.Filter((FeatureWithProperties f) => f.EnabledByDefault & !f.SurviveUnload).ToArray());

		if (b.AlreadyPatchedSaveSlots)
		{
			Tools.LogInfo("Reload detected; save slot support already patched");
			PatchLoadRawSaves.okToSave = b.OkToSave;
			return;
		}
		EnableFeatures(features.Filter((FeatureWithProperties f) => f.EnabledByDefault & f.SurviveUnload).ToArray());
		b.AlreadyPatchedSaveSlots = true;
		b.OkToSave = PatchLoadRawSaves.okToSave;
		b.Commit();
	}
}