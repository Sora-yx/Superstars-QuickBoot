using arz;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Moon.Scene;
using Orion;
using OriPlayerAction;
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
using static Orion.GameSceneBlockBuild;

namespace QuickBoot
{
    public class QuickBoot
    {
        public static class BootHelper
        {
            private static List<string> ZonesList = new()
            {
               "None",
               "Zone01_Act1",
               "Zone01_Act2",
               "Zone02_Act1",
               "Zone02_Act2",
               "Zone02_Act3",
               "Zone03_Act1",
               "Zone04_Act1",
               "Zone04_Act2",
               "Zone05_Act1",
               "Zone05_Act2",
               "Zone05_Act3",
               "Zone06_Act1",
               "Zone07_Act1",
               "Zone07_Act2",
               "Zone08_Act1",
               "Zone08_Act2",
               "Zone08_Act3",
               "Zone09_Act1",
               "Zone10_Act1",
               "Zone10_Act2",
               "Zone10_Act3",
               "Zone11_Act1",
               "Zone11_Act2",
               "Zone12_Act1",
            };

            private static string getZoneName(int id)
            {
                if (id < ZonesList.Count && id > 0)
                {
                    return ZonesList[id];
                }

                return "None";
            }

            public static void SetZone(ref Orion.GameScenePassing passingData)
            {
                string zone = getZoneName(Config.zoneID);

                if (zone != "None")
                {
                    DB_StageSelectInfo.Param stageInfo = AppSceneInfo.GetStageInfo(zone);
                    passingData = new()
                    {
                        SceneName = stageInfo.StageName,
                        ID = stageInfo.ID,
                    };
                }
            }

            public static void LoadSaveFile()
            {
                var account = Orion.SysAccountManager.Instance;
                var saveNum = Config.saveRedirection;
                if (account != null && saveNum >= 0 && saveNum < 4)
                {
                    var loadSavedata = account.Load(SysSaveManager.SaveDataType.Story, null, saveNum);
                    account.StartCoroutine(loadSavedata);
                }
            }
        }
       

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
            public static int zoneID = 0;

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
                    saveRedirection = doc.SelectSingleNode("/Configs/saves/@saveRedirection") is XmlAttribute saveRedirectionAttribute ? int.Parse(saveRedirectionAttribute.Value) : 0;
                    zoneID = doc.SelectSingleNode("/Configs/zones/@zoneID") is XmlAttribute zoneAttribute ? int.Parse(zoneAttribute.Value) : 0; 
                }
            }

            public static Orion.AppSceneInfo.Scene GetBootType()
            {
                switch (bootType)
                {
                    case bootStyle.titleScreen:
                        return Orion.AppSceneInfo.Scene.Title;
                    case bootStyle.worldMap:
                        return Orion.AppSceneInfo.Scene.WorldMap;
                    case bootStyle.stage:
                        return Orion.AppSceneInfo.Scene.StageSelect; //don't match but whatever
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
                Orion.AppSceneInfo.Scene sceneType = Config.GetBootType();
                DB_TransitionsEachTimeDefine.EAnimType color = DB_TransitionsEachTimeDefine.EAnimType.FadeWhite; //set Color / Anim transition

                if (sceneType != Orion.AppSceneInfo.Scene.Title)
                {
                    BootHelper.LoadSaveFile();
                    color = DB_TransitionsEachTimeDefine.EAnimType.WipeYellow;
                }

                Orion.GameScenePassing passingData = null;
                string sceneName = sceneType.ToString(); 

                if (sceneType == Orion.AppSceneInfo.Scene.StageSelect && Config.zoneID > 0)
                {
                    BootHelper.SetZone(ref passingData);
                    sceneName = Orion.GameSceneControllerBase.GameMainSceneName(passingData.SceneName);
                }

                DB_TransitionsEachTimeDefine.SceneTransition(color, sceneName, passingData ?? null);
                return false; //don't call original code
            }
        }
    }
}
