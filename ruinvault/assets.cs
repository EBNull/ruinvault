using System.IO;
using HarmonyLib;
using UnityEngine;

namespace ruinvault;
internal class Assets
{
    public struct FileTextAssetMap
    {
        public string filename;
        public TextAsset asset;
    }

    public static FileTextAssetMap[] GetAssetMap()
    {
        return [
            new FileTextAssetMap {filename = "story.ink.json", asset = StoryScript.Instance.inkAsset},
            new FileTextAssetMap {filename = "clue.db.json", asset = ClueDatabase.instance.jsonDatabase},
            new FileTextAssetMap {filename = "age.db.json", asset = HistoricalAgesDatabase.instance.jsonDatabase},
            new FileTextAssetMap {filename = "location.db.json", asset = StoryLocationDatabase.instance.jsonDatabase},
            new FileTextAssetMap {filename = "timeline.db.json", asset = TimelineDatabase.instance.jsonDatabase},
            new FileTextAssetMap {filename = "translationdata.json", asset = Resources.Load<TextAsset>("Translation Game/Data/GameData")}
       ];
    }

    static void MaybeDumpAsset(string dir, string name, TextAsset asset)
    {
        var f = Path.Combine(dir, name);
        if (!File.Exists(f))
        {
            Atomic.WriteFile(Path.Combine(dir, name), asset.text);
        }
    }

    public static void MaybeDumpAssets()
    {
        var ap = PathUtil.ReplaceFilename(Game.Instance.savePath, "Assets");
        Directory.CreateDirectory(ap);
        foreach (var ai in GetAssetMap())
        {
            MaybeDumpAsset(ap, ai.filename, ai.asset);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TextAsset), nameof(TextAsset.text), MethodType.Getter)]
    public static bool LoadCustomAsset(TextAsset __instance, ref string __result)
    {
        var caller = Tools.GetStackString(2);
        var ap = PathUtil.ReplaceFilename(Game.Instance.savePath, "Assets");
        foreach (var ai in GetAssetMap())
        {
            var f = Path.Combine(ap, ai.filename);
            if (ai.asset == __instance)
            {
                if (!File.Exists(f))
                {
                    Tools.LogInfo($"Found asset {ai.filename} to replace for {caller} but {f} does not exist; skipping");
                    return true; // continue
                }
                __result = File.ReadAllText(f);
                Tools.LogInfo($"Replaced asset for {ai.filename} from {f} for {caller}");
                return false; //skip
            }
        }
        return true; // continue
    }
}