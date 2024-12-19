using System;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;
using System.Linq;
using System.Net;
using Steamworks;
using UnityEngine;
using ActionIcon;
using ruinvault;

public class Baton {
    public bool AlreadyPatchedSaveSlots;
    public bool OkToSave;
    
    public static Baton Get() {
        return BatonSerializer.GetCurrent();
    }

    public void Commit() {
        BatonSerializer.SetCurrent(this);
    }
}

// BatonSerializer manages a typed process-local Baton and manages
// converting the fields to and from the string value.
//
// New loads of this assembly may add or remove fields.
//
internal class BatonSerializer {
    public static Baton GetCurrent() {
        var b = new Baton{};
        var v = new BatonPasser().Value;
        JSONObject jo = new(v, true);
        Tools.LogInfo($"Loading baton: {v}");
        if (jo.type != JSONObject.Type.OBJECT) {
            Tools.LogInfo($"Invalid Baton; type is {jo.type}");
            return b;
        }
        jo.GetField(ref b.AlreadyPatchedSaveSlots, "AlreadyPatchedSaveSlots");
        jo.GetField(ref b.OkToSave, "OkToSave");
        return b;
    }
    public static void SetCurrent(Baton value) {
        JSONObject jo = new(JSONObject.Type.OBJECT);
        jo.SetField("AlreadyPatchedSaveSlots", value.AlreadyPatchedSaveSlots);
        jo.SetField("OkToSave", value.OkToSave);
        var s = jo.ToString();
        Tools.LogInfo($"Committing baton: {s}");
        new BatonPasser().Value = s;
    }
}

// BatonPasser manages a process-local global type by name that crosses assemblies.
// The type has a single string field, Value, which is get/set across assembly instances.
internal class BatonPasser {
    readonly Type universalBatonType;

    readonly static string universalAssemblyName = "UniversalBaton";
    readonly static string universalTypeName = "BatonType";
    readonly static string universalFieldName = "Value";

    private Type CreateNewBaton() {
        
        var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(universalAssemblyName), System.Reflection.Emit.AssemblyBuilderAccess.Run);
        // Assemblies can contain multiple modules; but this is usually an obscure implementation detail. Most single-module assemblies have their module named the same.
        // https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2008/zst29sk2(v=vs.90)
        var module = assembly.DefineDynamicModule(universalAssemblyName); 
        var type = module.DefineType(universalTypeName);
        var field = type.DefineField(universalFieldName, typeof(string), FieldAttributes.Static | FieldAttributes.Public);
        return type.CreateType();
    }
    
    private Type? MaybeGetExistingBaton() {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == universalAssemblyName);
        if (asm == null) {
            return null;
        }
        var type = asm.GetType(universalTypeName);
        return type;
    }

    public BatonPasser() {
        universalBatonType = MaybeGetExistingBaton() ?? CreateNewBaton();
    }
    
    public string Value { get {
        string s = (string) universalBatonType.GetField(universalFieldName).GetValue(null) ?? "";
        if (s == "") {
            return "False";
        }
        return s;
    } set {
        universalBatonType.GetField(universalFieldName).SetValue(null, value);
    }
    }
}