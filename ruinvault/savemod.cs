using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using JetBrains.Annotations;
using Steamworks;

namespace ruinvault;

public class BetterSaves
{

    /* Util functions */
    static void PrefixBasenameWithSlot(ref string fp, int slot)
    {
        fp = PathUtil.ModParts(fp, (p) => { p.name = $"Slot_{slot}_{p.name}{slot}"; return p; });
    }

    static void PostfixBasenameWithSlot(ref string fp, int slot)
    {
        fp = PathUtil.ModParts(fp, (p) => { p.name = $"{p.name}_Slot_{slot}"; return p; });
    }

    /* "Current" save slot functions */

    // Only the zeroth (unnumbered) slot will sync to steam - https://steamdb.info/app/774201/ufs/
    public static int saveSlot = 2;
    static BetterSaves() {
        var env = Environment.GetEnvironmentVariables();
        foreach(DictionaryEntry e in env) {
           if (((string)e.Key).ToLower() == "slot") {
                int slot = 2;
                if (Int32.TryParse((string)e.Value, out slot)) {
                    saveSlot = slot;
                    Tools.LogInfo($"Using save SLOT={saveSlot} from env");
                } else {
                    Tools.LogError($"Could not parse save slot SLOT={e.Value}");
                }
           }
        }
    }
    public static string GetCurrentSaveFilename(string originalPath)
    {
        // Cloud saves are supported only for the barename
        if (saveSlot != 0)
        {
            // Change save filename, otherwise use default
            PostfixBasenameWithSlot(ref originalPath, saveSlot);
        }
        return originalPath;
    }

    public static string GetCurrentDevSaveFilename(string originalPath)
    {
        PrefixBasenameWithSlot(ref originalPath, saveSlot);
        return originalPath;
    }

    public static string GetExtraRawSaveFilename(string originalPath)
    {
        return PathUtil.PostfixBasename(originalPath, "_raw");
    }

    public struct SaveSelectionInfo(SaveLoader file, string reason)
    {
        public SaveLoader file = file;
        public string reason = reason;
    }
    public static SaveSelectionInfo SelectSave(SaveLoader cryptFile, SaveLoader rawFile)
    {
        if (!rawFile.Valid())
        {
            var ret = cryptFile;
            return new SaveSelectionInfo(ret, $"Loading crypted save because raw is missing or not valid");
        }
        if (!cryptFile.Valid())
        {
            var ret = rawFile;
            return new SaveSelectionInfo(ret, $"Loading raw save because crypt is missing or not valid");
        }
        if (rawFile > cryptFile)
        {
            var ret = rawFile;
            return new SaveSelectionInfo(ret, $"Loading raw save because raw is newer than crypt ({rawFile.GetPlaytime()} > {cryptFile.GetPlaytime()})");
        }
        {
            var ret = cryptFile;
            return new SaveSelectionInfo(ret, $"Loading crypt save because crypt is newer than raw ({cryptFile.GetPlaytime()} > {rawFile.GetPlaytime()})");
        }
    }
    /* General I/O Functions */

    public static bool WriteRawSaveFile(string rawSavePath, string saveData)
    {
        return Atomic.WriteFile(rawSavePath, saveData);
    }
}

public class BaseSaveLoader
{
    public string name;
    public string data;
    private Game.LoadResultInfo? lri = null;
    public BaseSaveLoader(string? name, string? data)
    {
        this.name = name ?? "";
        this.data = data ?? "";
        this.ParseAndCheck();
    }
    public void ParseAndCheck()
    {
        if (this.lri != null)
        {
            return;
        }
        this.lri = new Game.LoadResultInfo();
        lri.didRetrieveLoadData = true;
        lri.dryRun = true;
        var game = Game.Instance;
        game.ParseRawLoadData(this.data, lri);
        if (lri.didParseLoadData)
        {
            //Game.DryRunLoad(lri);
            game.TryDoLoad(lri);
        }
    }
    public GameStateInformation GetGSI()
    {
        ParseAndCheck();
        var gsi = new GameStateInformation();
        if (lri == null)
        {
            return gsi;
        }
        UnityEngine.JsonUtility.FromJsonOverwrite(lri.stateInformationJSON, gsi);
        return gsi;
    }
    public bool Valid()
    {
        return lri != null && lri.didParseLoadData && !lri.hadError;
    }
    public float GetPlaytime()
    {
        ParseAndCheck();
        if (!Valid())
        {
            return 0;
        }
        var gsi = GetGSI();
        return gsi.totalPlayTime;
    }
}

public class SaveLoader(string? name, string? data) : BaseSaveLoader(name, data)
{
    public static bool operator true(SaveLoader x) { x.ParseAndCheck(); return x.Valid(); }
    public static bool operator false(SaveLoader x) { x.ParseAndCheck(); return !x.Valid(); }
    public static bool operator <(SaveLoader l, SaveLoader r)
    {
        if (!l.Valid())
        {
            return true;
        }
        if (!r.Valid())
        {
            return true;
        }
        return l.GetPlaytime() < r.GetPlaytime();
    }
    public static bool operator ==(SaveLoader l, SaveLoader r)
    {
        return l.data == r.data;
    }
    public static bool operator !=(SaveLoader l, SaveLoader r)
    {
        return l.data != r.data;
    }
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        var other = (SaveLoader)obj;
        return this.data.Equals(other.data);
    }
    public override int GetHashCode()
    {
        return data.GetHashCode();
    }
    public static bool operator >(SaveLoader l, SaveLoader r)
    {
        var isLess = l < r;
        if (isLess) { return false; }
        if (l == r) { return false; }
        return true;
    }
    public static SaveLoader FromRawFile(string filePath)
    {
        var rawText = "";
        try
        {
            rawText = File.ReadAllText(filePath);
        }
        catch (Exception e)
        {
            Tools.LogInfo($"Exception while loading ${filePath}: ${e}");
            throw;
        }
        return new SaveLoader(filePath, rawText);
    }
    public static SaveLoader FromAnyFile(string filePath)
    {
        var bytes = new byte[] { };
        try
        {
            bytes = File.ReadAllBytes(filePath);
        }
        catch (Exception e)
        {
            Tools.LogError($"Save file {filePath} could not be loaded: {e}");
            return new SaveLoader(filePath, "");
        }
        var rawSave = "";
        if (bytes.Length < 2)
        {
            Tools.LogError($"Save file {filePath} is too small (only {bytes.Length} bytes)");
            return new SaveLoader(filePath, System.Text.Encoding.UTF8.GetString(bytes));
        }
        if (bytes[0] == 'H' && bytes[1] == 'V')
        {
            // Crypted format
            Tools.LogInfo($"Save file {filePath} is in crypted format");
            // Use ReadFromBytes because it's simpler than trying to figure out Harmony's method for calling original function
            using MemoryStream memoryStream = new MemoryStream(bytes);
            try
            {
                rawSave = CryptSaving.ReadFromStream(memoryStream, false);
            }
            catch (Exception e)
            {
                Tools.LogError($"Save file {filePath} could not be decrypted - {e}");
            }
        }
        else
        {
            // Raw format
            Tools.LogInfo($"Save file {filePath} is in raw format");
            rawSave = System.Text.Encoding.UTF8.GetString(bytes);
        }
        return new SaveLoader(filePath, rawSave);
    }
}