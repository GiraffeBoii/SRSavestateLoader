using System;
using System.Runtime.CompilerServices;
using UModFramework.API;
using UnityEngine;

namespace SRSavestateLoader
{
    public class SRSavestateLoaderConfig
    {
        private static readonly string configVersion = "1.0";

        //Add your config vars here.
        internal static KeyCode loadSave;
        internal static KeyCode newSave;
        internal static KeyCode switchSave;

        private static string currentSaveName;

        internal static void Load()
        {
            SRSavestateLoader.Log("Loading settings.");
            try
            {
                using (UMFConfig cfg = new UMFConfig())
                {
                    string cfgVer = cfg.Read("ConfigVersion", new UMFConfigString());
                    if (cfgVer != string.Empty && cfgVer != configVersion)
                    {
                        cfg.DeleteConfig(false);
                        SRSavestateLoader.Log("The config file was outdated and has been deleted. A new config will be generated.");
                    }

                    //cfg.Write("SupportsHotLoading", new UMFConfigBool(false)); //Uncomment if your mod can't be loaded once the game has started.
                    cfg.Write("ModDependencies", new UMFConfigStringArray(new string[] { "" })); //A comma separated list of mod/library names that this mod requires to function. Format: SomeMod:1.50,SomeLibrary:0.60
                    cfg.Read("LoadPriority", new UMFConfigString("Normal"));
                    cfg.Write("MinVersion", new UMFConfigString("0.53.9"));
                    cfg.Write("MaxVersion", new UMFConfigString("0.54.99999.99999")); //This will prevent the mod from being loaded after the next major UMF release
                    cfg.Write("UpdateURL", new UMFConfigString(""));
                    cfg.Write("ConfigVersion", new UMFConfigString(configVersion));

                    SRSavestateLoader.Log("Finished UMF Settings.");

                    //Add your settings here
                    loadSave = cfg.Read("loadSave", new UMFConfigKeyCode(KeyCode.U), "Key to load the current save.");
                    newSave = cfg.Read("newSave", new UMFConfigKeyCode(KeyCode.O), "Key to create new save at current position.");
                    switchSave = cfg.Read("switchSave", new UMFConfigKeyCode(KeyCode.N), "Key to change current save.");

                    currentSaveName = cfg.Read("currentSave", new UMFConfigString(""), "Filename of the current save.");

                    SRSavestateLoader.Log("Finished loading settings.");
                }
            }
            catch (Exception e)
            {
                SRSavestateLoader.Log("Error loading mod settings: " + e.Message + "(" + e.InnerException?.Message + ")");
            }
        }

        internal static void updateCurrentSave(string saveName)
        {
            SRSavestateLoader.Log("Updating current save.");
            try
            {
                using (UMFConfig cfg = new UMFConfig())
                {
                    cfg.Write("currentSave", new UMFConfigString(saveName));
                    currentSaveName = saveName;

                    SRSavestateLoader.Log("Finished updating save.");
                }
            }
            catch (Exception e)
            {
                SRSavestateLoader.Log("Error loading mod settings: " + e.Message + "(" + e.InnerException?.Message + ")");
            }
        }

        public static string getCurrentSave()
        {
            return currentSaveName;
        }
    }
}