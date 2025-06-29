using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using System.Linq;
using System.Security;
using System.Diagnostics;

namespace ruinvault;

public struct FeatureWithProperties(IPluginFeature feature, string Name, bool EnabledByDefault = false, bool SurviveUnload = false, int Priority = 0)
{
	public IPluginFeature feature = feature;
	public string Name = Name;
	public bool EnabledByDefault = EnabledByDefault;
	public bool SurviveUnload = SurviveUnload;
	public int Priority = Priority;
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	private static readonly Harmony baseHarmony = new(MyPluginInfo.PLUGIN_NAME);
	private static readonly Harmony foreverHarmony = new(MyPluginInfo.PLUGIN_NAME + "-forever");

	public FeatureWithProperties[] features = [];

	public Plugin()
	{
		if (this.gameObject is null)
		{
			throw new Exception("gameObject is null");
		}

		features = FeatureAttribute.GetData(Assembly.GetExecutingAssembly()).Map((FeatureData fd) =>
			new FeatureWithProperties(new WrapFeature(fd.Type, foreverHarmony.Id, this.gameObject), fd.Name, fd.DefaultEnabled, fd.SurviveUnload, fd.Priority)
		).ToArray();
		Array.Sort(features, (FeatureWithProperties l, FeatureWithProperties r) => l.Priority - r.Priority);

		foreach (var f in features)
		{
			Tools.LogInfo($"Loaded FeatureData: {f.Name} EnabledByDefault={f.EnabledByDefault} SurviveUnload={f.SurviveUnload}, Priority = {f.Priority}");
		}
		if (features.Length == 0)
		{
			Debugger.Break();
		}
	}

	private void Awake()
	{
		Tools.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");

		var b = Baton.Get();
		Tools.LogInfo($"{b.PluginAssemblies.Length} previous instances exist");
		b.PluginAssemblies = b.PluginAssemblies.AddToArray(Assembly.GetExecutingAssembly().GetName().Name);
		b.Commit();

		SetupFeatures();
	}

	void OnDestroy()
	{
		Tools.LogInfo($"Unloading {MyPluginInfo.PLUGIN_NAME}");
		foreach (var f in features)
		{
			if (!f.SurviveUnload)
			{
				f.feature.Disable();
			}
			else
			{
				var wf = f.feature as WrapFeature;
				Tools.LogInfo($"Skipping unload of {wf?.Name() ?? f.feature.GetType().Name} because it must survive");
			}
		}
		Tools.LogInfo($"Plugin.OnDestroy() complete");
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
			}
			catch (Exception e)
			{
				Tools.LogError($"Failed to apply {ts}");
				Tools.LogError(e.ToString());
				Tools.LogMessage($"Failed to apply {ts}");
			}
			if (ok)
			{
				Tools.LogInfo($"Applied {ts}");
				Tools.LogMessage($"Applied {ts}");
			}
		}
	}

	private void SetupFeatures()
	{
		var b = Baton.Get();
		if (b.AppliedForeverPatches)
		{
			Tools.LogMessage($"Reloading {MyPluginInfo.PLUGIN_NAME}");
		}
		else
		{
			Tools.LogMessage($"Loading {MyPluginInfo.PLUGIN_NAME}");
		}

		EnableFeatures(features.Filter((FeatureWithProperties f) => f.EnabledByDefault & !f.SurviveUnload).ToArray());

		if (b.AppliedForeverPatches)
		{
			Tools.LogInfo("Reload detected; skipping application of forever-patches");
			PatchLoadRawSaves.okToSave = b.OkToSave;
			return;
		}
		EnableFeatures(features.Filter((FeatureWithProperties f) => f.EnabledByDefault & f.SurviveUnload).ToArray());
		b.AppliedForeverPatches = true;
		b.OkToSave = PatchLoadRawSaves.okToSave;
		b.Commit();
	}
}