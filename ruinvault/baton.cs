using System;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;
using System.Linq;
using System.Net;
using Steamworks;
using UnityEngine;
using ActionIcon;

[Serializable]
public struct Baton {
    public bool AlreadyPatchedSaveSlots;

    public static Baton GetCurrent() {
        var v = new BatonPasser().Value;
        return new Baton{
            // This is gross but I am lazy
            AlreadyPatchedSaveSlots = bool.Parse(v)
        };
    }
    public static void SetCurrent(Baton value) {
        new BatonPasser().Value = value.AlreadyPatchedSaveSlots.ToString();
    }
}

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