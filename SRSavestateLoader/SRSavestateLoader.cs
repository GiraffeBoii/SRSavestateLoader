using Assets.Script.Util.Extensions;
using MonomiPark.SlimeRancher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UModFramework.API;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SRSavestateLoader
{
    //[UMFHarmony(1)] //Set this to the number of harmony patches in your mod.
    [UMFScript]
    class SRSavestateLoader : MonoBehaviour
    {
        private static int newSaveSizeX = 300;
        private static int newSaveSizeY = 100;
        private static int newSaveWinID = 1259;
        private static Rect newSaveRect = new Rect((Screen.width - newSaveSizeX) / 2, (Screen.height - newSaveSizeY) / 2, newSaveSizeX, newSaveSizeY);

        private static int switchSaveSizeX = 400;
        private static int switchSaveSizeY = 600;
        private static int switchSaveWinID = 1260;
        private static Rect switchSaveRect = new Rect(0, (Screen.height - switchSaveSizeY) / 2, switchSaveSizeX, switchSaveSizeY);
        private static Vector2 savesScrollPostion = Vector2.zero;

        private static bool showMenu = true;

        private static string gameSavesFolder = Application.persistentDataPath;
        private static string modSavesFolder = gameSavesFolder + "\\SRSavestateLoaderSaves";

        internal static readonly GUIStyle LABEL_STYLE_DEFAULT = new GUIStyle();
        internal static readonly GUIStyle LABEL_STYLE_BOLD = new GUIStyle();
        internal static readonly GUIStyle TEXT_STYLE_HEADER = new GUIStyle();

        private static string currentSave;

        private bool isSwitchingSaves = false;
        private bool isCreatingSave = false;

        private string newSaveName = string.Empty;


        internal static void Log(string text, bool clean = false)
        {
            using (UMFLog log = new UMFLog()) log.Log(text, clean);
        }
		void Awake()
		{
			Log("SRSavestateLoader v" + UMFMod.GetModVersion().ToString(), true);
            UMFGUI.RegisterPauseHandler(Pause);

            SRSavestateLoaderConfig.Load();

            currentSave = SRSavestateLoaderConfig.getCurrentSave();

            // Create saves folders if doesn't currenly exist
            System.IO.Directory.CreateDirectory(modSavesFolder);

            UMFGUI.RegisterBind("loadSave", SRSavestateLoaderConfig.loadSave.ToString(), () => loadSave($"{modSavesFolder}\\{currentSave}"));
            UMFGUI.RegisterBind("newSave", SRSavestateLoaderConfig.newSave.ToString(), () => toggleCreatingSaves());
            UMFGUI.RegisterBind("switchSave", SRSavestateLoaderConfig.switchSave.ToString(), () => toggleSwitchingSaves());
            UMFGUI.RegisterBind("freeze", "I", () => testFreeze());
            UMFGUI.RegisterBind("unfreeze", "Y", () => testUnfreeze());


        }

        public static void Pause(bool pause)
        {
            TimeDirector timeDirector = null;
            try
            {
                timeDirector = SRSingleton<SceneContext>.Instance.TimeDirector;
            }
            catch { }
            if (!timeDirector) return;
            if (pause)
            {
                if (!timeDirector.HasPauser()) timeDirector.Pause();
            }
            else timeDirector.Unpause();
        }
        
        void Update()
        {
        }
        

        void loadSave(string saveName)
        {
            string gameName = getGameName(saveName + ".sav");

            SRSavestateLoader.Log($"loading save with game name {gameName} and save name {saveName}");

            try{ 
                AutoSaveDirector autoSaveDirector = SRSingleton<GameContext>.Instance.AutoSaveDirector;

                SRSavestateLoader.Log("loading save");
                
                /*
                 * on versions prior to 1.4.4, camera locking behavior on death is slightly different. in this case calling unfreeze after a load on 1.4.4 behaves as expected, but softlocks on earlier versions
                 * to make it work on earlier versions, the death freeze must be called prior to the load. calling freeze consectutively also softlocks, however, so only call freeze if it is not already frozen from a normal death
                 * after the load, unfreezing should behave as expected always ... hopefully
                 * TLDR: games cursed idk
                 */ 
                LockOnDeath lockOnDeath = SRSingleton<LockOnDeath>.Instance;
                try
                {
                    if (!lockOnDeath.Locked())
                    {
                        lockOnDeath.Freeze();
                    }
                }
                catch { }

                autoSaveDirector.BeginLoad(gameName, saveName, null);

                //prevents softlock if loaded while dead
                lockOnDeath.Unfreeze();
            }
            catch (Exception e)
            {
                SRSavestateLoader.Log(e.Message);
            }
        }

        void createSave(string saveName)
        {
            try
            {
                AutoSaveDirector autoSaveDirector = SRSingleton<GameContext>.Instance.AutoSaveDirector;
                autoSaveDirector.SaveGameAndFlush();
                FileInfo newSav = getRecentSavFile();

                newSav.MoveTo($"{modSavesFolder}\\{saveName}.sav");

            }
            catch (Exception e)
            {
                SRSavestateLoader.Log(e.Message);
            }
        }


        FileInfo getRecentSavFile()
        {
            DirectoryInfo directory = new DirectoryInfo(gameSavesFolder);
            FileInfo mostRecentSav = directory.GetFiles($"*.sav", SearchOption.TopDirectoryOnly).OrderByDescending(f => f.CreationTime).FirstOrDefault();
            return mostRecentSav;
        }

        void OnGUI()
        {
            GUI.skin.button.fontSize = 16;
            GUI.skin.button.fontStyle = FontStyle.Normal;
            GUI.skin.textField.fontSize = 16;
            GUI.skin.textField.fontStyle = FontStyle.Normal;
            GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
            GUI.skin.toggle.fontStyle = FontStyle.Normal;
            GUI.skin.toggle.fontSize = 16;
            GUI.skin.toggle.fontStyle = FontStyle.Normal;
            GUI.skin.window.fontSize = 16;
            GUI.skin.window.fontStyle = FontStyle.Bold;

            if (!Levels.isMainMenu() && !Levels.isSpecial())
            {
                //if (showMenu && isPaused())
                //{
                //    newSaveRect = GUILayout.Window(newSaveWinID, newSaveRect, showNewSaveMenu, "TEST");
                //    switchSaveRect = GUILayout.Window(switchSaveWinID, switchSaveRect, showSwitchSaveMenu, "Select Save");
                //}

                if (isSwitchingSaves)
                {
                    Pause(true);
                    if(isPaused())
                    {
                        switchSaveRect = GUILayout.Window(switchSaveWinID, switchSaveRect, showSwitchSaveMenu, "Select Save");
                    }
                    else
                    {
                        isSwitchingSaves = false;
                    }
                }

                else if(isCreatingSave)
                {
                    Pause(true);
                    if (isPaused())
                    {
                        newSaveRect = GUILayout.Window(newSaveWinID, newSaveRect, showNewSaveMenu, "Create New Save");
                    }
                    else
                    {
                        isCreatingSave = false;
                    }
                }
            }
        }
        
        void showNewSaveMenu(int winId)
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.Label("Insert name of new save");
            newSaveName = GUILayout.TextField(newSaveName);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Confirm"))
            {
                if (!File.Exists($"{modSavesFolder}\\{newSaveName}.sav") && newSaveName != string.Empty)
                {
                    createSave(newSaveName);
                    SRSavestateLoaderConfig.updateCurrentSave(newSaveName);
                    newSaveName = string.Empty;
                    currentSave = SRSavestateLoaderConfig.getCurrentSave();
                    isCreatingSave = false;
                    Pause(false);
                }
            }

            if (GUILayout.Button("Discard"))
            {
                newSaveName = string.Empty;
                isCreatingSave = false;
                Pause(false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

        }

        void toggleCreatingSaves()
        {
            if (!Levels.isMainMenu() && !Levels.isSpecial() && !isSwitchingSaves)
            {
                isCreatingSave = !isCreatingSave;
            }
        }

        void toggleSwitchingSaves()
        {
            if (!Levels.isMainMenu() && !Levels.isSpecial() && !isCreatingSave)
            {
                isSwitchingSaves = !isSwitchingSaves;
            }
        }

        void showSwitchSaveMenu(int winId)
        {
            DirectoryInfo directory = new DirectoryInfo(modSavesFolder);

            FileInfo[] files = directory.GetFiles("*.sav");

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X"))
            {
                isSwitchingSaves = false;
                Pause(false);
            }
            GUILayout.EndHorizontal();

            savesScrollPostion = GUILayout.BeginScrollView(savesScrollPostion);
            foreach (FileInfo file in files) {
                GUILayout.BeginHorizontal();
                string fileName = file.Name.Remove(file.Name.Length - 4, 4);
                GUILayout.Label(fileName, GUILayout.Width(225));
                if (fileName != currentSave)
                {
                    if (GUILayout.Button("Select"))
                    {
                        SRSavestateLoaderConfig.updateCurrentSave(fileName);
                        currentSave = SRSavestateLoaderConfig.getCurrentSave();
                        isSwitchingSaves = false;
                        Pause(false);
                    }
                    if (GUILayout.Button("Delete"))
                    {
                        file.Delete();
                    }
                }
                else
                {
                    GUI.enabled = false;
                    if (GUILayout.Button("Select"))
                    {

                    }
                    if (GUILayout.Button("Delete"))
                    {

                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        bool isPaused()
        {
            try
            {
                TimeDirector timeDirector = SceneContext.Instance.TimeDirector;
                if (timeDirector != null)
                {
                    return timeDirector.HasPauser();
                }
            }
            catch (Exception e)
            {
                SRSavestateLoader.Log(e.Message);
            }
            return false;
        }

        string getGameName(string savePath)
        {
            using (FileStream fs = new FileStream(savePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    byte len1 = reader.ReadByte();
                    reader.ReadBytes(len1);
                    reader.ReadInt32();
                    reader.ReadBytes(2);

                    byte saveIdLength = reader.ReadByte();
                    byte[] saveIdBytes = reader.ReadBytes(saveIdLength);

                    string gameName = Encoding.UTF8.GetString(saveIdBytes);

                    return gameName;
                }
            }
        }
    }
}
