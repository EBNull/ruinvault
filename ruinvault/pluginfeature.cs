
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using ruinvault;
using UnityEngine;

public class FeatureData(Type type, string Name = "", string Description = "", bool DefaultEnabled = false, bool SurviveUnload = false, int Priority = 0)
{
    public Type Type = type;
    public string Name = Name;
    string Description = Description;
    public bool DefaultEnabled = DefaultEnabled;
    public bool SurviveUnload = SurviveUnload;
    public int Priority = Priority;
}

// https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/creating-custom-attributes
[System.AttributeUsage(System.AttributeTargets.Class)]
public class FeatureAttribute(string _Name = "") : System.Attribute
{
    protected readonly string _Name = _Name;
    public string Description = "";
    public bool DefaultEnabled = false;
    public bool SurviveUnload = false;
    public int Priority = 0;

    public static Type[] GetAnnotatedTypes(Assembly asm)
    {
        return (from type in asm.GetTypes()
                where type.GetCustomAttributes(typeof(FeatureAttribute), false).Length > 0
                select type
        ).ToArray();
    }

    public static FeatureData[] GetData(Assembly asm)
    {
        var ret = (from type in GetAnnotatedTypes(asm)
                   from attr in type.GetCustomAttributes(typeof(FeatureAttribute), false).Map((object f) => (FeatureAttribute)f)
                   select new FeatureData(type, attr._Name == "" ? type.Name : attr._Name, attr.Description, attr.DefaultEnabled, attr.SurviveUnload, attr.Priority)

        ).ToArray();
        Array.Sort(ret, (FeatureData l, FeatureData r) => l.Priority - r.Priority);
        return ret;
    }
}

public interface IPluginFeature
{
    bool Enable();
    void Disable();
}

// WrapFeature wraps an arbitrary type to add enable/disable options.
// 
// On enable, any Harmony patches defined on the type will be applied.
// On disable, those patches will be removed.
// 
// Next, an instance of the type will be created. Constructors and destructors
// can be used for initialization and destruction.
//
// Additionally, if the type is a MonoBehavior, then an instance will be added as a
// child of the specificied parent (thus probably awakening it and letting
// it participate in the Unity object lifecycle).
//
public class WrapFeature(Type feature, string harmonyBaseId, GameObject? unityParent) : IPluginFeature
{
    public Type feature = feature;
    object? instance;
    readonly Harmony harmony = new($"{harmonyBaseId}-{feature.Name}");

    virtual public string Name()
    {
        return feature.Name;
    }

    virtual public bool Enable()
    {
        try
        {
            harmony.PatchAll(feature);
        }
        catch (Exception e)
        {
            Tools.LogError($"Could not apply patches for {Name()}: {e}");
            return false;
        }
        Tools.LogInfo($"Applied patches for {Name()}");
        try
        {
            instance = Activator.CreateInstance(feature);
        }
        catch (Exception e)
        {
            Tools.LogError($"Could not instantiate {Name()}: {e}");
            Disable();
        }
        var go = instance as MonoBehaviour;
        if (unityParent != null && go != null)
        {
            Tools.LogError($"Setting {go} as child of {unityParent}");
            go.transform.parent = unityParent.transform;
        }
        return true;
    }

    virtual public void Disable()
    {
        if (instance is not null)
        {
            var go = instance as MonoBehaviour;
            if (go != null)
            {
                Tools.LogError($"Unparenting {go}");
                go.transform.parent = null;
                MonoBehaviour.DestroyImmediate(go);
            }
            (instance as IDisposable)?.Dispose();
            instance = null;
        }
        Tools.LogInfo($"Removing patches for {Name()}");
        harmony.UnpatchSelf();
    }
}