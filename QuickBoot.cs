using arz;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Moon.Scene;
using Orion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine.Playables;

namespace QuickBoot
{
    public class QuickBoot
    {
        public static class Config
        {
            private enum bootStyle : byte
            {
                titleScreen,
                worldMap,
                stage
            }

            public static int saveRedirection = 0;
            private static bootStyle bootType = bootStyle.titleScreen;

            static private string FindDLLPath()
            {
                string injectedDLLPath = null;

                // Get the path of the DLL that injected the code
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in loadedAssemblies)
                {
                    if (assembly.FullName.Contains("QuickBoot"))
                    {
                        injectedDLLPath = Path.GetDirectoryName(assembly.Location);
                        break;
                    }
                }

                return injectedDLLPath;
            }

            static public void LoadConfig()
            {
                string path = FindDLLPath();

                if (path is null)
                    return;

                string configFile = Path.Combine(path, "config.xml");
                if (File.Exists(configFile))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(configFile);
                    bootType = doc.SelectSingleNode("/Configs/boot/@bootType") is XmlAttribute bootTypeAttribute ? (bootStyle)int.Parse(bootTypeAttribute.Value) : bootStyle.titleScreen;
                    saveRedirection = doc.SelectSingleNode("/Configs/save/@saveRedirection") is XmlAttribute saveRedirectionAttribute ? int.Parse(saveRedirectionAttribute.Value) : 0;
                }
            }

            public static Orion.AppSceneInfo.Scene GetBootStyle()
            {
                switch (bootType)
                {
                    case bootStyle.titleScreen:
                        return Orion.AppSceneInfo.Scene.Title;
                    case bootStyle.worldMap:
                        return Orion.AppSceneInfo.Scene.WorldMap;

                }

                return Orion.AppSceneInfo.Scene.Title;
            }
        }

        [HarmonyPatch(typeof(Orion.BootSceneController))]
        public class BootSceneControllerPatcher
        {
            [HarmonyPrefix]
            [HarmonyPatch("Start")]
            static bool Prefix() //force the game to swap to a different scene instead of logo
            {

                Orion.AppSceneInfo.Scene scene = Config.GetBootStyle();
                if (scene > Orion.AppSceneInfo.Scene.Title)
                {
                    var account = Orion.SysAccountManager.Instance;
                    var saveNum = Config.saveRedirection;
                    if (account != null && saveNum >= 0 && saveNum < 4) 
                    {
                        var loadSavedata = account.Load(SysSaveManager.SaveDataType.Story, null, saveNum);
                        account.StartCoroutine(loadSavedata);
                    }
                }

                Orion.GameSceneControllerBase.SetTransitionBeforeSetting();
                Orion.SysSceneManager.Transition(scene.ToString());

                return false; //don't call original code
            }
        }

    }
}
