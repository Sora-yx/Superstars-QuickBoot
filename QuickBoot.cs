using arz;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Moon.Scene;
using Orion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Orion.ActivityPS5;

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

            private static bootStyle bootType = bootStyle.titleScreen;

            static public void LoadConfig()
            {
                string path = Directory.GetCurrentDirectory();

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
            static bool Prefix() //force the game to swap to title screen scene instead of logo
            {

                Orion.AppSceneInfo.Scene scene = Orion.AppSceneInfo.Scene.Title;
                if (scene > Orion.AppSceneInfo.Scene.Title)
                {
                    var account = Orion.SysAccountManager.Instance;
                    if (account != null) 
                    {
                        var loadSavedata = account.Load(SysSaveManager.SaveDataType.Story, null, 0);
                        account.StartCoroutine(loadSavedata);
                    }
                }

                Orion.GameSceneControllerBase.SetTransitionBeforeSetting();
                Orion.SysSceneManager.Transition(scene.ToString());

                return false; //don't call original code
            }
        }



        [HarmonyPatch(typeof(Orion.SysAccountManager))]
        public class SysAccountManagerPatcher
        {
            [HarmonyPrefix]
            [HarmonyPatch("Load")]
            static void Prefix(ref SysSaveManager.SaveDataType type, Action<bool> callback = null, int slotNo = -1)
            {
                callback = callback;
                Console.WriteLine("Load save on save type:" + type.ToString() + " slot num: " + slotNo.ToString());
            }


            [HarmonyPrefix]
            [HarmonyPatch("SetCurrentSlotNo")]
            static void Prefix(int slot)
            {

                Console.WriteLine("current slot is " + slot.ToString());
            }

        }

        [HarmonyPatch(typeof(Orion.SysSaveManager))]
        public class SysSaveManagerPatcher
        {
            [HarmonyPrefix]
            [HarmonyPatch("LoadSlot")]
            static void Prefix(ref SysSaveManager.SaveDataType type, Action<bool> callback = null, int slotNo = -1)
            {
                callback = callback;
                Console.WriteLine("Load SLOT save on save type:" + type.ToString() + " slot num: " + slotNo.ToString());
            }
        }
    }
}
