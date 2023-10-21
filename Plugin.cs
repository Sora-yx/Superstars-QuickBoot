using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;

namespace QuickBoot;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        // Plugin startup logic
        Log.LogInfo($"Mod {MyPluginInfo.PLUGIN_GUID} by Sora is loaded!");
        var harmony = new Harmony("QuickBoot");
        harmony.PatchAll();
        QuickBoot.Config.LoadConfig();
    }
}
