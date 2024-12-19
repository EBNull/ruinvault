using System;
using System.Reflection;
using System.Linq;

using ruinvault;

using HarmonyLib; // For AddToArray

// Baton is the global process-local structure holding state
// across plugin instances. ::Get gets the current Baton
// and ::Commit sets it. ::Commit is required after changing
// values.
public class Baton
{
    public bool AppliedForeverPatches;
    public string[] PluginAssemblies = [];

    public bool OkToSave;

    public static Baton Get()
    {
        return BatonSerializer.GetCurrent();
    }

    public void Commit()
    {
        BatonSerializer.SetCurrent(this);
    }
}

// BatonSerializer manages a typed process-local Baton and manages
// converting the fields to and from the string value.
//
// New loads of this assembly may add or remove fields.
//
internal class BatonSerializer
{
    public static Baton GetCurrent()
    {
        var b = new Baton { };
        var v = new BatonPasser().Value;
        JSONObject jo = new(v, true);
        Tools.LogInfo($"Loading baton: {v}");
        if (jo.type != JSONObject.Type.OBJECT)
        {
            Tools.LogInfo($"Invalid Baton; type is {jo.type}");
            return b;
        }
        jo.GetField(ref b.AppliedForeverPatches, nameof(b.AppliedForeverPatches));
        jo.GetField(ref b.OkToSave, nameof(b.OkToSave));
        var pa = jo.GetField(nameof(b.PluginAssemblies)) ?? JSONObject.arr;
        for (var i = 0; i < pa.Count; i++)
        {
            if (pa[i].type == JSONObject.Type.STRING)
            {
                b.PluginAssemblies = b.PluginAssemblies.AddToArray(pa[i].str);
            }
        }
        return b;
    }
    public static void SetCurrent(Baton value)
    {
        JSONObject jo = JSONObject.obj;
        jo.SetField(nameof(value.AppliedForeverPatches), value.AppliedForeverPatches);
        jo.SetField(nameof(value.OkToSave), value.OkToSave);
        var a = JSONObject.arr;
        foreach (var v in value.PluginAssemblies)
        {
            a.Add(v);
        }
        jo.SetField(nameof(value.PluginAssemblies), a);
        var s = jo.ToString();
        Tools.LogInfo($"Committing baton: {s}");
        new BatonPasser().Value = s;
    }
}

// BatonPasser manages a process-local global type by name that crosses assemblies.
// The type has a single string field, Value, which is get/set across assembly instances.
internal class BatonPasser
{
    readonly Type universalBatonType;

    readonly static string universalAssemblyName = "UniversalBaton";
    readonly static string universalTypeName = "BatonType";
    readonly static string universalFieldName = "Value";

    private Type CreateNewBaton()
    {

        var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(universalAssemblyName), System.Reflection.Emit.AssemblyBuilderAccess.Run);
        // Assemblies can contain multiple modules; but this is usually an obscure implementation detail. Most single-module assemblies have their module named the same.
        // https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2008/zst29sk2(v=vs.90)
        var module = assembly.DefineDynamicModule(universalAssemblyName);
        var type = module.DefineType(universalTypeName);
        var field = type.DefineField(universalFieldName, typeof(string), FieldAttributes.Static | FieldAttributes.Public);
        return type.CreateType();
    }

    private Type? MaybeGetExistingBaton()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == universalAssemblyName);
        if (asm == null)
        {
            return null;
        }
        var type = asm.GetType(universalTypeName);
        return type;
    }

    public BatonPasser()
    {
        universalBatonType = MaybeGetExistingBaton() ?? CreateNewBaton();
    }

    public string Value
    {
        get
        {
            string s = (string)universalBatonType.GetField(universalFieldName).GetValue(null) ?? "";
            if (s == "")
            {
                return "{}";
            }
            return s;
        }
        set
        {
            universalBatonType.GetField(universalFieldName).SetValue(null, value);
        }
    }
}