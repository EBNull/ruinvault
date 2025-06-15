
using System;
using System.CodeDom;
using System.Collections;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using HarmonyLib;
using ruinvault;
using UnityEngine;
using UnityEngine.Assertions;

public class FeatureData(Type type, string Name = "", string Description = "", bool DefaultEnabled = false, bool SurviveUnload = false, int Priority = 0, bool IngameToggle = false)
{
    public Type Type = type;
    public string Name = Name;
    public string Description = Description;
    public bool DefaultEnabled = DefaultEnabled;
    public bool SurviveUnload = SurviveUnload;
    public bool IngameToggle = IngameToggle;
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
    public bool IngameToggle = false;
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
                   select new FeatureData(type, attr._Name == "" ? type.Name : attr._Name, attr.Description, attr.DefaultEnabled, attr.SurviveUnload, attr.Priority, attr.IngameToggle)

        ).ToArray();
        Array.Sort(ret, (FeatureData l, FeatureData r) => l.Priority - r.Priority);
        return ret;
    }
}

public interface IPluginFeature
{
    bool IsEnabled();
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
    private bool enabled;

    public bool IsEnabled()
    {
        return enabled;
    }

    virtual public string Name()
    {
        return feature.Name;
    }

    private static bool HasMethod(Type typ, string methodName)
    {
        return typ.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null;
    }

    virtual public bool Enable()
    {
        // TODO: Perhaps construct first, and then call an init method

        // Harmony patch lifecycle

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

        // C# & Unity instance lifecycle

        // Unity *really wants to be in charge* of object constructors,
        // so we need to use .AddComponent because there's no public way
        // to use an existing component and attach it to a gameobject.

        // First, we try it Unity's way :/

        // Unity game loop lifecycle
        var b = MaybeFeatureAsUnityComponent(feature, unityParent);

        if (b is not null)
        {
            // Did it, do some unity stuff and assign the instance

            // Unity game loop lifecycle - attach parent and set active
            if (unityParent is not null)
            {
                Tools.LogError($"Setting {b} as child of {unityParent}");
                b.transform.parent = unityParent.transform;
            }
            b.gameObject.SetActive(true);

            var mb = b as MonoBehaviour;
            if (mb is not null)
            {
                mb.enabled = true;

                if (!mb.isActiveAndEnabled)
                {
                    Tools.MessageBox("!b.isActiveAndEnabled", $"feature {feature.Name} is not activeAndEnabled");
                }

            }

            instance = b;
            enabled = true;
            return true;
        }

        // This wasn't part of Unity, activate it ourselves
        try
        {
            instance = Activator.CreateInstance(feature);
        }
        catch (Exception e)
        {
            Tools.LogError($"Could not instantiate {Name()}: {e}");
            Disable();
            return false;
        }
        if (instance is null)
        {
            Tools.LogError($"Could not instantiate {Name()}: Activator.CreateInstance returned null");
            Disable();
            return false;
        }

        enabled = true;
        return true;
    }


    static readonly string[] allUnityMethods = ["Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnGUI", "OnEnable"];

    Component? MaybeFeatureAsUnityComponent(Type feature, GameObject? parent)
    {


        var isUnitySubclass = feature.IsSubclassOf(typeof(MonoBehaviour));

        string[] featUnityMethods = [.. allUnityMethods.Where(method => HasMethod(feature, method))];
        var hasUnityMethods = featUnityMethods.Length != 0;

        var shouldBeTreatedAsUnity = isUnitySubclass || hasUnityMethods;

        if (shouldBeTreatedAsUnity && parent is null)
        {
            Tools.LogError("Could not initialize: missing parent gameObject!");
            return null;
        }


        Component? co = null;

        if (shouldBeTreatedAsUnity)
        {
            if (parent is not null)
            {
                co = parent.GetComponent(feature);
                co ??= parent.AddComponent(feature);
            }
        }
        var isTreatableAsUnity = co is not null;

        //as MonoBehavior: {go} (== null: {go == null} ; != null: {go != null} ; is null: {go is null})

        var detail = $"""
        type: {feature}
        
        component: {co}

        isUnitySubclass: {isUnitySubclass}
        hasUnityMethods: {hasUnityMethods} ({featUnityMethods.Join()})
                             
        shouldBeTreatedAsUnity: {shouldBeTreatedAsUnity}
        isTreatableAsUnity: {isTreatableAsUnity}
        """;

        var errTitle = "";
        var errMsg = "";
        if (!isTreatableAsUnity && shouldBeTreatedAsUnity)
        {
            errTitle = "Bad Feature";
            errMsg = $"Feature {feature.Name} seems to want to be in unison, but is !isUnityAssignable:\n\n{detail}";
        }
        if (isTreatableAsUnity && !shouldBeTreatedAsUnity)
        {
            errTitle = "Unoptimal Feature";
            errMsg = $"Feature {feature.Name} doesn't need Unity behavior, but isUnityAssignable:\n\n{detail}";
        }
        if (errMsg != "")
        {
            Tools.LogError(errMsg);
            Tools.MessageBox(errTitle, errMsg);
        }

        return co;
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
                // Don't use DestroyImmediate here: "Destroying object multiple times. Don't use DestroyImmediate on the same object in OnDisable or OnDestroy."
                MonoBehaviour.Destroy(go);
            }
            (instance as IDisposable)?.Dispose();
            instance = null;
        }
        Tools.LogInfo($"Removing patches for {Name()}");
        harmony.UnpatchSelf();
        enabled = false;
    }
}