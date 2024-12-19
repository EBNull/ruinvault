// ScriptEngine loads BepInEx plugins from the BepInEx scripts directory instead of the plugins directory.
//
// This is a modification of https://github.com/BepInEx/BepInEx.Debug/blob/master/src/ScriptEngine/ScriptEngine.cs,
// and as such, this file is under the LGPLv3 license - https://github.com/BepInEx/BepInEx.Debug/blob/master/LICENSE
//
// Changes from upstream:
//
//   Functional:
//     - Wrap a bunch of calls in Safely()
//     - Load and Destroy plugins immediately, not in a coroutine
//     - Set the default LoadOnStart to true
//     - Set the default EnableFilesystemWatcher to true
//
//   Technical:
//     - Moved Common namespace from another file to this one - now only this souce file is needed.
//     - Updated csproj file to use the SDK format, and use net35 as the target version.
//
// Startup Overview:
//
// - Preloader patches Unity game
// - Preloader starts BepInEx
// - BepInEx finds ScriptEngine in plugin directory
// - ScriptEngine object is created and tries to reload plugins immediately
//
// Reload Overview:
//
// - DestroyImmediate existing Unity GameObject that has children that are previously loaded plugin instances
// - Plugin's OnDestroy methods are called by Unity
// - ScriptEngine iterates over DLLs in scripts directory and loads their metadata
// - The assembly is given a dynamic name (and logical filename)
// - If enabled, the assembly is written to disk and loaded from there
// - Otherwise, the assebly is loaded into memory and loaded from there
// - A new Unity GameObject representing the parent of these plugins is created
// - Plugins are loaded as children of this gameobject
//
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using System.Diagnostics;
using Common;
using static ScriptEngine.Tools;

namespace Common
{
    internal static class Metadata
    {
        public const string Version = "11.5";
    }

}

namespace ScriptEngine
{
    public class Tools
    {
        public delegate void ExceptionAction(Exception e);

        public static ManualLogSource Log = new ManualLogSource("scriptengine");

        public static void Safely(string action, System.Action tryfunc, ExceptionAction exceptfunc = null)
        {
            try
            {
                if (tryfunc is null)
                {
                    throw new Exception("tryfunc is null!");
                }
                Log.LogInfo($"About to call {action}");
                Console.Out.Flush();
                Console.Error.Flush();
                tryfunc();
                Log.LogInfo($"Completed {action}");
                Console.Out.Flush();
                Console.Error.Flush();
            }
            catch (System.Exception e)
            {
                Log.LogError($"Failed to run {action}: {e}");
                Console.Out.Flush();
                Console.Error.Flush();
                exceptfunc?.Invoke(e);
            }
        }
        public static void HandleUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((object sender, UnhandledExceptionEventArgs args) =>
            {
                Log.LogError($"Unhandled Exception (terminating={args.IsTerminating}) from '{sender}': {args.ExceptionObject}");
            });
        }
    }

    [BepInPlugin(GUID, "scriptengine", Version)]
    public class ScriptEngine : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.scriptengine";
        public const string Version = Metadata.Version;

        public string ScriptDirectory => Path.Combine(Paths.BepInExRootPath, "scripts");

        private GameObject scriptManager;

        private ConfigEntry<bool> LoadOnStart { get; set; }
        private ConfigEntry<KeyboardShortcut> ReloadKey { get; set; }
        private ConfigEntry<bool> QuietMode { get; set; }
        private ConfigEntry<bool> EnableFileSystemWatcher { get; set; }
        private ConfigEntry<bool> IncludeSubdirectories { get; set; }
        private ConfigEntry<float> AutoReloadDelay { get; set; }

        private ConfigEntry<bool> DumpAssemblies { get; set; }
        private static readonly string DumpedAssembliesPath = Utility.CombinePaths(Paths.BepInExRootPath, "ScriptEngineDumpedAssemblies");

        private FileSystemWatcher fileSystemWatcher;
        private bool shouldReload;
        private float autoReloadTimer;

        ScriptEngine()
        {
            HandleUnhandledExceptions();
        }

        private void Awake()
        {
            Safely("Awake", () =>
            {
                LoadOnStart = Config.Bind("General", "LoadOnStart", true, new ConfigDescription("Load all plugins from the scripts folder when starting the application. This is done from inside of Chainloader's Awake, therefore not all plugis might be loaded yet. BepInDependency attributes are ignored."));
                QuietMode = Config.Bind("General", "QuietMode", false, new ConfigDescription("Disable all logging except for error messages."));
                IncludeSubdirectories = Config.Bind("General", "IncludeSubdirectories", false, new ConfigDescription("Also load plugins from subdirectories of the scripts folder."));
                EnableFileSystemWatcher = Config.Bind("AutoReload", "EnableFileSystemWatcher", true, new ConfigDescription("Watches the scripts directory for file changes and automatically reloads all plugins if any of the files gets changed (added/removed/modified)."));
                AutoReloadDelay = Config.Bind("AutoReload", "AutoReloadDelay", 3.0f, new ConfigDescription("Delay in seconds from detecting a change to files in the scripts directory to plugins being reloaded. Affects only EnableFileSystemWatcher."));
                DumpAssemblies = Config.Bind<bool>("AutoReload", "DumpAssemblies", false, "If enabled, BepInEx will save patched assemblies & symbols into BepInEx/ScriptEngineDumpedAssemblies.\nThis can be used by developers to inspect and debug plugins loaded by ScriptEngine.");

                if (Directory.Exists(DumpedAssembliesPath))
                    Directory.Delete(DumpedAssembliesPath, true);

                ReloadPlugins();
            });
        }

        private void Update()
        {
            if (ReloadKey == null)
            {
                ReloadKey = Config.Bind("General", "ReloadKey", new KeyboardShortcut(KeyCode.F6), new ConfigDescription("Press this key to reload all the plugins from the scripts folder"));

                if (EnableFileSystemWatcher.Value)
                    StartFileSystemWatcher();
            }

            if (ReloadKey.Value.IsDown())
            {
                ReloadPlugins();
            }
            else if (shouldReload)
            {
                autoReloadTimer -= Time.unscaledDeltaTime;
                if (autoReloadTimer <= .0f)
                    ReloadPlugins();
            }
        }

        private void ReloadPlugins()
        {
            Safely("ReloadPlugins", () =>
            {
                shouldReload = false;
                if (!QuietMode.Value) Logger.Log(LogLevel.Info, "Unloading old plugin instances");

                if (scriptManager != null)
                {
                    if (!QuietMode.Value) Logger.Log(LogLevel.Info, "Unloading old plugin instances");

                    foreach (var previouslyLoadedPlugin in scriptManager.GetComponents<BaseUnityPlugin>())
                    {
                        var metadataGUID = previouslyLoadedPlugin.Info.Metadata.GUID;
                        if (Chainloader.PluginInfos.ContainsKey(metadataGUID))
                            Chainloader.PluginInfos.Remove(metadataGUID);
                    }

                    DestroyImmediate(scriptManager);
                }

                scriptManager = new GameObject($"ScriptEngine_{DateTime.Now.Ticks}");
                DontDestroyOnLoad(scriptManager);

                var files = Directory.GetFiles(ScriptDirectory, "*.dll", IncludeSubdirectories.Value ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    foreach (string path in Directory.GetFiles(ScriptDirectory, "*.dll", IncludeSubdirectories.Value ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        Logger.LogMessage($"Loading {path}");
                        LoadDLL(path, scriptManager);
                    }

                    if (!QuietMode.Value)
                        Logger.LogMessage("Reloaded all plugins!");
                }
                else
                {
                    if (!QuietMode.Value)
                        Logger.LogMessage("No plugins to reload");
                }
            });
        }

        private void LoadDLL(string path, GameObject obj)
        {
            var defaultResolver = new DefaultAssemblyResolver();
            defaultResolver.AddSearchDirectory(ScriptDirectory);
            defaultResolver.AddSearchDirectory(Paths.ManagedPath);
            defaultResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);

            if (!QuietMode.Value)
                Logger.Log(LogLevel.Info, $"Loading plugins from {path}");

            using (var dll = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                AssemblyResolver = defaultResolver,
                ReadSymbols = true
            }))
            {
                dll.Name.Name = $"{dll.Name.Name}-{DateTime.Now.Ticks}";
                Assembly ass;

                if (DumpAssemblies.Value)
                {
                    // Dump assembly & load it from disk
                    if (!Directory.Exists(DumpedAssembliesPath))
                        Directory.CreateDirectory(DumpedAssembliesPath);

                    string assemblyDumpPath = Path.Combine(DumpedAssembliesPath, dll.Name.Name + Path.GetExtension(dll.MainModule.Name));

                    using (FileStream outFileStream = new FileStream(assemblyDumpPath, FileMode.Create))
                    {
                        dll.Write((Stream)outFileStream, new WriterParameters()
                        {
                            WriteSymbols = true
                        });
                    }

                    ass = Assembly.LoadFile(assemblyDumpPath);
                    if (!QuietMode.Value)
                        Logger.Log(LogLevel.Info, $"Loaded dumped Assembly from {assemblyDumpPath}");
                }
                else
                {
                    // Load from memory
                    using (var ms = new MemoryStream())
                    {
                        dll.Write(ms);
                        ass = Assembly.Load(ms.ToArray());
                    }
                }


                foreach (Type type in GetTypesSafe(ass))
                {
                    try
                    {
                        if (!typeof(BaseUnityPlugin).IsAssignableFrom(type)) continue;

                        var metadata = MetadataHelper.GetMetadata(type);
                        if (metadata == null) continue;

                        if (!QuietMode.Value)
                            Logger.Log(LogLevel.Info, $"Loading {metadata.GUID}");

                        if (Chainloader.PluginInfos.TryGetValue(metadata.GUID, out var existingPluginInfo))
                            throw new InvalidOperationException($"A plugin with GUID {metadata.GUID} is already loaded! ({existingPluginInfo.Metadata.Name} v{existingPluginInfo.Metadata.Version})");

                        var typeDefinition = dll.MainModule.Types.First(x => x.FullName == type.FullName);
                        var pluginInfo = Chainloader.ToPluginInfo(typeDefinition);

                        //StartCoroutine(DelayAction(() =>
                        {
                            try
                            {
                                // Need to add to PluginInfos first because BaseUnityPlugin constructor (called by AddComponent below)
                                // looks in PluginInfos for an existing PluginInfo and uses it instead of creating a new one.
                                Chainloader.PluginInfos[metadata.GUID] = pluginInfo;

                                var instance = obj.AddComponent(type);

                                // Fill in properties that are normally set by Chainloader
                                var tv = Traverse.Create(pluginInfo);
                                tv.Property<BaseUnityPlugin>(nameof(pluginInfo.Instance)).Value = (BaseUnityPlugin)instance;
                                // Loading the assembly from memory causes Location to be lost
                                tv.Property<string>(nameof(pluginInfo.Location)).Value = path;
                            }
                            catch (Exception e)
                            {
                                Logger.LogError($"Failed to load plugin {metadata.GUID} because of exception: {e}");
                                Chainloader.PluginInfos.Remove(metadata.GUID);
                            }
                        }
                        //));
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to load plugin {type.Name} because of exception: {e}");
                    }
                }
            }
        }

        private void StartFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(ScriptDirectory)
            {
                IncludeSubdirectories = IncludeSubdirectories.Value
            };
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fileSystemWatcher.Filter = "*.dll";
            fileSystemWatcher.Changed += FileChangedEventHandler;
            fileSystemWatcher.Deleted += FileChangedEventHandler;
            fileSystemWatcher.Created += FileChangedEventHandler;
            fileSystemWatcher.Renamed += FileChangedEventHandler;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void FileChangedEventHandler(object sender, FileSystemEventArgs args)
        {
            if (!QuietMode.Value)
                Logger.LogInfo($"File {Path.GetFileName(args.Name)} changed. Delayed recompiling...");
            shouldReload = true;
            autoReloadTimer = AutoReloadDelay.Value;
        }

        private IEnumerable<Type> GetTypesSafe(Assembly ass)
        {
            try
            {
                return ass.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var sbMessage = new StringBuilder();
                sbMessage.AppendLine("\r\n-- LoaderExceptions --");
                foreach (var l in ex.LoaderExceptions)
                    sbMessage.AppendLine(l.ToString());
                sbMessage.AppendLine("\r\n-- StackTrace --");
                sbMessage.AppendLine(ex.StackTrace);
                Logger.LogError(sbMessage.ToString());
                return ex.Types.Where(x => x != null);
            }
        }

        private IEnumerator DelayAction(Action action)
        {
            yield return null;
            action();
        }
    }
}
