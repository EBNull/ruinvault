
using System;
using System.Data;
using System.Runtime.InteropServices;
using HarmonyLib;
using ruinvault;
using UnityEngine;

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