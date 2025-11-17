global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using UnityEngine;
global using UnityEngine.UI;
global using UniverseLib;
global using UniverseLib.Utility;
using UnityExplorer.Config;
using UnityExplorer.ObjectExplorer;
using UnityExplorer.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.Input;

namespace UnityExplorer
{
    public static class ExplorerCore
    {
        public const string NAME = "CinematicUnityExplorer";
        public const string VERSION = "1.3.0";
        public const string AUTHOR = "originalnicodr, Sinai, yukieiji";
        public const string GUID = "com.originalnicodr.cinematicunityexplorer";

        public static IExplorerLoader Loader { get; private set; }
        public static string ExplorerFolder => Path.Combine(Loader.ExplorerFolderDestination, Loader.ExplorerFolderName);
        public const string DEFAULT_EXPLORER_FOLDER_NAME = "CinematicUnityExplorer";

        public static HarmonyLib.Harmony Harmony { get; } = new HarmonyLib.Harmony(GUID);

        /// <summary>
        /// Initialize UnityExplorer with the provided Loader implementation.
        /// </summary>
        public static void Init(IExplorerLoader loader)
        {
            if (Loader != null)
                throw new Exception("CinematicUnityExplorer is already loaded.");

            Loader = loader;

            Log($"{NAME} {VERSION} initializing...");

            CheckLegacyExplorerFolder();
            Directory.CreateDirectory(ExplorerFolder);
            ConfigManager.Init(Loader.ConfigHandler);

            Universe.Init(ConfigManager.Startup_Delay_Time.Value, LateInit, Log, new()
            {
                Disable_EventSystem_Override = ConfigManager.Disable_EventSystem_Override.Value,
                Force_Unlock_Mouse = ConfigManager.Force_Unlock_Mouse.Value,
                Unhollowed_Modules_Folder = loader.UnhollowedModulesFolder
            });

            UERuntimeHelper.Init();
            ExplorerBehaviour.Setup();
            UnityCrashPrevention.Init();

            //Extra inits
            ArrowGenerator.PatchLights();

            //Patch
            UnityExplorerPlus.ParseUtility.ParseManager.Init();
            Harmony.PatchAll(typeof(CinematicUnityExplorer.Patches.CinematicUnityExplorerPatches)); // 應用所有定義在 CinematicUnityExplorerPatches 中的補丁

        }

        // Do a delayed setup so that objects aren't destroyed instantly.
        // This can happen for a multitude of reasons.
        // Default delay is 1 second which is usually enough.
        static void LateInit()
        {
            SceneHandler.Init();

            Log($"Creating UI...");

            UIManager.InitUI();

            Log($"{NAME} {VERSION} ({Universe.Context}) initialized.");

            IInputManager.Setup();

            // InspectorManager.Inspect(typeof(Tests.TestClass));

            KeypressListener.Setup();

            if (ConfigManager.Auto_Scale_UI.Value) MakeUEUIScale();
        }

        internal static void Update()
        {
            // check master toggle
            if (IInputManager.GetKeyDown(ConfigManager.Master_Toggle.Value))
            {
                UIManager.ShowMenu = !UIManager.ShowMenu;
            }
        }


        #region LOGGING

        public static void Log(object message)
            => Log(message, LogType.Log);

        public static void LogWarning(object message)
            => Log(message, LogType.Warning);

        public static void LogError(object message)
            => Log(message, LogType.Error);

        public static void LogUnity(object message, LogType logType)
        {
            if (!ConfigManager.Log_Unity_Debug.Value)
                return;

            Log($"[Unity] {message}", logType);
        }

        private static void Log(object message, LogType logType)
        {
            string log = message?.ToString() ?? "";

            LogPanel.Log(log, logType);

            switch (logType)
            {
                case LogType.Assert:
                case LogType.Log:
                    Loader.OnLogMessage(log);
                    break;

                case LogType.Warning:
                    Loader.OnLogWarning(log);
                    break;

                case LogType.Error:
                case LogType.Exception:
                    Loader.OnLogError(log);
                    break;
            }
        }

        #endregion


        #region LEGACY FOLDER MIGRATION

        // Can be removed eventually. For migration from <4.7.0
        static void CheckLegacyExplorerFolder()
        {
            string legacyPath = Path.Combine(Loader.ExplorerFolderDestination, "UnityExplorer");
            if (Directory.Exists(legacyPath))
            {
                LogWarning($"Attempting to migrate old 'UnityExplorer/' folder to 'CinematicUnityExplorer/'...");

                // If new folder doesn't exist yet, let's just use Move().
                if (!Directory.Exists(ExplorerFolder))
                {
                    try
                    {
                        Directory.Move(legacyPath, ExplorerFolder);
                        Log("Migrated successfully.");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Exception migrating folder: {ex}");
                    }
                }
                else // We have to merge
                {
                    try
                    {
                        CopyAll(new(legacyPath), new(ExplorerFolder));
                        Directory.Delete(legacyPath, true);
                        Log("Migrated successfully.");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Exception migrating folder: {ex}");
                    }
                }
            }
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles())
                fi.MoveTo(Path.Combine(target.ToString(), fi.Name));

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        #endregion

        private static void MakeUEUIScale(){
            List<CanvasScaler> canvasScalers = RuntimeHelper.FindObjectsOfTypeAll(typeof(CanvasScaler))
            .Select(obj => obj.TryCast<CanvasScaler>())
            .Where(c => c.name.Contains("unityexplorer"))
            .ToList();

            foreach (CanvasScaler scaler in canvasScalers)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
            }
        }
    }
}
