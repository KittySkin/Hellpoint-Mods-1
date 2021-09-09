using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using Harmony;
using UnityEngine;
using System.IO;
using Characters;
using System.Xml.Serialization;
using Menu;

namespace SpeedrunTimer
{
    public class HPSpeedrunTimer : MelonMod
    {
        public const string GUID = "com.sinai.hellpoint.speedruntimer";
        public const string NAME = "Hellpoint Speedrun Timer";
        public const string AUTHOR = "Sinai";
        public const string VERSION = "1.0.0";
        public const string GAME_NAME = "Hellpoint";
        public const string GAME_COMPANY = "Cradle Games";

        public HPSpeedrunTimer Instance;

        public Settings settings = new Settings();
        public const string SETTINGS_PATH = @"Mods\SpeedrunTimer\SpeedrunTimer.xml";
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(Settings));

        public KeyCode StartKey;
        public KeyCode StopKey;

        private const string DEFAULT_TIME = "0:00.000";
        public float m_Time = 0.0f;
        public string timeString = DEFAULT_TIME;
        public bool timerRunning = false;
        private bool runCompleted = false;

        private bool IsGameplayRunning()
        {
            return !LoadingView.Loading 
                && Player.Players != null 
                && Player.Players.Count > 0 
                && Player.Players[0].Initialized
                && Player.Players[0].gameObject.activeInHierarchy;
        }

        public override void OnApplicationStart()
        {
            Instance = this;

            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll();

            LoadSettings();

            StartKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StartKey);
            StopKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StopKey);
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(StartKey))
            {
                timerRunning = true;
                m_Time = 0;
                timeString = DEFAULT_TIME;
                runCompleted = false;
            }

            if (Input.GetKeyDown(StopKey))
            {
                timerRunning = false;
                runCompleted = false;
            }

            if (IsGameplayRunning() && timerRunning)
            {
                if (CreditsView.Showing)
                {
                    timerRunning = false;
                    runCompleted = true;
                }

                if (timerRunning)
                {
                    m_Time += Time.deltaTime;

                    TimeSpan time = TimeSpan.FromSeconds(m_Time);

                    timeString = (time.Hours > 0 ? (time.Hours + ":") : "")
                        + time.Minutes 
                        + ":"
                        + time.Seconds.ToString("00")
                        + "."
                        + time.Milliseconds.ToString("000");
                }
            }
        }

        private void LoadSettings()
        {
            bool needNew = true;

            var dir = @"Mods\SpeedrunTimer";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(SETTINGS_PATH))
            {
                using (var file = File.OpenRead(SETTINGS_PATH))
                {
                    settings = (Settings)serializer.Deserialize(file);
                    needNew = false;
                }
            }

            if (needNew)
            {
                settings = new Settings();
            }

            if (Enum.IsDefined(typeof(KeyCode), settings.StartKey))
                StartKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StartKey);
            else
                Debug.LogWarning($"[ERROR] Could not parse StartKey '{settings.StartKey}' to a KeyCode!");

            if (Enum.IsDefined(typeof(KeyCode), settings.StopKey))
                StopKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.StopKey);
            else
                Debug.LogWarning($"[ERROR] Could not parse StopKey '{settings.StopKey}' to a KeyCode!");

            if (needNew)
            {
                if (File.Exists(SETTINGS_PATH)) File.Delete(SETTINGS_PATH);

                using (var file = File.Create(SETTINGS_PATH))
                {
                    serializer.Serialize(file, settings);
                }
            }
        }

        // ============= GUI ==============

        public override void OnGUI()
        {
            int origFontsize = GUI.skin.label.fontSize;

            // Timer
            GUI.skin.label.fontSize = 27;
            // shadowtext
            GUI.color = Color.black;
            GUI.Label(new Rect(4, 10, 349, 79), timeString);
            // main text
            if (!timerRunning || !IsGameplayRunning())
                if (runCompleted)
                    GUI.color = Color.green;
                else
                    GUI.color = Color.yellow;
            else
                GUI.color = Color.white;
            GUI.Label(new Rect(3, 9, 350, 35), timeString);

            // [StartKey] to start...
            if (!timerRunning)
            {
                GUI.skin.label.fontSize = 13;
                GUI.Label(new Rect(3, 40, 350, 30), StartKey.ToString() + " to start...");
            }

            GUI.skin.label.fontSize = origFontsize;
            GUI.color = Color.white;
        }
    }

    public class Settings
    {
        public string StartKey = "F8";
        public string StopKey = "F9";
        public string ConditionKey = "F10";
    }
}
