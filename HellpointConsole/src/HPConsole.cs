﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UI;
using System.Xml.Serialization;
using System.IO;
using Debugs;
using UnityEngine.UI;
using UnhollowerRuntimeLib;
using Definitions;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Configuration;

namespace HellpointConsole
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class HPConsole : BasePlugin
    {
        public const string GUID = "com.sinai.hellpoint.console";
        public const string NAME = "HellpointConsole";
        public const string VERSION = "2.0.0";

        static HPConsole Instance;

        public static ConfigEntry<KeyCode> Toggle_Key;

        private const string InputControlName = "ConsoleInputControl";
        private static bool focusWanted = true;
        private static bool executeWanted = false;

        public static bool ShowMenu;
        private static float m_timeOfLastInput = -1f;

        private static Rect m_windowRect = new Rect(5, 5, 700, 250);
        private static Vector2 scroll = Vector2.zero;

        private static string m_input = "";
        private static bool m_invalidCommand = false;
        private static string[] m_currentOptions = new string[0];
        private static ConsoleCommand m_currentCommand = null;

        private static int m_selectedAutocompleteIndex = -1;
        private static bool m_wantToChooseSelectedAutocomplete = false;

        public override void Load()
        {
            Instance = this;

            Toggle_Key = Config.Bind("Settings", "Console Toggle Key", KeyCode.Pause);

            ClassInjector.RegisterTypeInIl2Cpp<ConsoleBehaviour>();
            var go = new GameObject("ConsoleBehaviour");
            go.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<ConsoleBehaviour>();

            new Harmony(GUID).PatchAll();
        }

        public class ConsoleBehaviour : MonoBehaviour
        {
            public ConsoleBehaviour(IntPtr ptr) : base(ptr) { }

            internal void Update() => HPConsole.Update();
            internal void OnGUI() => HPConsole.OnGUI();
        }

        #region MISC DEBUG SNIPPETS

        // // find Dargass Tower:
        // 
        // void Test()
        // {
        //     foreach (var swi in Resources.FindObjectsOfTypeAll<Gameplay.Switch>()) 
        //     {
        //         if (swi.key && swi.key.name == "M02_LowCity_Key2")
        //         {
        //             Characters.Player.Players[0].Teleport(swi.Position, 0f, true);
        //         }
        //     }
        // }


        // Selected item:

        //private void Test()
        //{
        //    var player = Resources.FindObjectsOfTypeAll<Characters.Player>()[0];
        //    var menu = Resources.FindObjectsOfTypeAll<Menu.EquipItemSelector>()[0];
        //    player.RemoveItem(menu.selectedItem, true);
        //    //// or inspect:
        //    //Explorer.WindowManager.InspectObject(menu.selectedItem, out bool _);
        //}


        // Find Coins:

        //private void FindCoins()
        //{
        //    int found = 0;
        //    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        //    var items = Resources.FindObjectsOfTypeAll<Items.LootItem>();
        //    foreach (var item in items)
        //    {
        //        if (item.gameObject.scene != scene) continue;

        //        foreach (var itemCount in item.items)
        //        {
        //            if (itemCount.definition.title == "Coin")
        //            {
        //                found++;
        //            }
        //        }
        //    }

        //    MelonLogger.Log($"Found {found} coins in " + scene.name);
        //}

        #endregion

        [HarmonyPatch(typeof(MultiEventSystem), "Update")]
        public class MultiEventSystem_Update
        {
            [HarmonyPrefix]
            public static bool Prefix() => !ShowMenu;
        }

        // ======== OnUpdate ========

        public static void Update()
        {
            if (!UDebug.instance)
                return;

            // update input

            if (UnityEngine.Time.time - m_timeOfLastInput > 0.1f)
            {
                if (Input.GetKeyDown(Toggle_Key.Value))
                    ToggleMenu();
                else if (ShowMenu && Input.GetKeyDown(KeyCode.Return))
                {
                    m_timeOfLastInput = UnityEngine.Time.time;

                    if (m_selectedAutocompleteIndex >= 0)
                        m_wantToChooseSelectedAutocomplete = true;
                    else
                        executeWanted = true;
                }
            }

            // update autocomplete options

            var input = m_input.ToLower();

            // split input by space characters
            var inputArray = m_input.Split(' ');

            if (inputArray.Length <= 1)
            {
                m_currentCommand = null;

                // doing first command, use commandByName dict.
                var list = new List<string>();
                foreach (var opt in UDebug.commandByName)
                {
                    if (string.IsNullOrEmpty(input) || opt.key.ToLower().Contains(input))
                        list.Add(opt.key);
                }
                m_currentOptions = list.ToArray();
            }
            else
            {
                // chosen first command, use GetOptions from the ConsoleCommand class.

                var cmdString = inputArray[0].ToLower();

                if (UDebug.commandByName.ContainsKey(cmdString))
                {
                    // base command
                    m_currentCommand = UDebug.commandByName[cmdString];

                    // index of current actual argument
                    var lastArgIndex = inputArray.Length - 1;

                    // get options of 'currentIndex - 1' (because we should ignore base command index for this)
                    var opts = m_currentCommand.GetOptions(lastArgIndex - 1, null);

                    // current actual argument
                    var currentArg = inputArray[lastArgIndex].ToLower();

                    if (string.IsNullOrEmpty(currentArg) || !opts.Contains(currentArg))
                    {
                        var list = new List<string>();
                        int lim = 0;
                        foreach (var opt in opts)
                        {
                            if (lim >= 50) break;

                            if (string.IsNullOrEmpty(currentArg) || opt.ToLower().Contains(currentArg))
                            {
                                list.Add(opt);
                                lim++;
                            }
                        }
                        m_currentOptions = list.ToArray();
                    }
                }
                else
                    m_invalidCommand = true;
            }

            if (m_selectedAutocompleteIndex >= m_currentOptions.Length)
                m_selectedAutocompleteIndex = -1;
        }

        private static void ToggleMenu()
        {
            if (UnityEngine.Time.time -  m_timeOfLastInput < 0.25f)
                return;

            m_timeOfLastInput = UnityEngine.Time.time;

            ShowMenu = !ShowMenu;
            if (ShowMenu)
                focusWanted = true;
        }

        // ======== GUI Draw and Interaction ========

        public static void OnGUI()
        {
            if (ShowMenu)
            {
                // check GUI Input event
                Event e = Event.current;
                if (UnityEngine.Time.time - m_timeOfLastInput > 0.1f && e.type != EventType.Layout && e.type != EventType.Repaint)
                {
                    if (e.keyCode == KeyCode.Return)
                    {
                        m_timeOfLastInput = UnityEngine.Time.time;
                        e.Use();

                        if (m_selectedAutocompleteIndex >= 0)
                            m_wantToChooseSelectedAutocomplete = true;
                        else if (!string.IsNullOrEmpty(m_input))
                            executeWanted = true;
                    }
                    else if (e.keyCode == KeyCode.Escape || e.keyCode == Toggle_Key.Value)
                    {
                        e.Use();
                        ToggleMenu();
                    }
                }

                // GUI Draw
                GUILayout.BeginArea(m_windowRect, GUI.skin.box);
                GUILayout.BeginHorizontal(null);

                if (GUILayout.Button("X", new GUILayoutOption[] { GUILayout.Width(30) }))
                    ShowMenu = false;

                GUI.SetNextControlName(InputControlName);
                m_input = GUILayout.TextField(m_input, null);

                if (GUILayout.Button("Go", new GUILayoutOption[] { GUILayout.Width(30) }))
                    executeWanted = true;

                if (executeWanted)
                    ExecuteConsole();

                GUILayout.EndHorizontal();

                AutoCompleteCurrentInput();

                GUILayout.EndArea();

                if (focusWanted)
                {
                    GUI.FocusControl(InputControlName);
                    focusWanted = false;
                }
            }
        }

        // Send the current input to the 'UDebug.Execute()' method (the game's console command method)
        public static void ExecuteConsole()
        {
            try
            {
                UDebug.Execute(m_input.Trim());
                m_input = "";
                executeWanted = false;
                m_selectedAutocompleteIndex = -1;
                m_wantToChooseSelectedAutocomplete = false;
            }
            catch (Exception e)
            {
                Instance.Log.LogWarning($"Exception executing command '{m_input}'!");
                Instance.Log.LogMessage($"{e.GetType()}, message: {e.Message}\r\nStack: {e.StackTrace}");
            }
        }

        // get the current list of console options and show autocomplete buttons for them
        private static void AutoCompleteCurrentInput()
        {
            GUILayout.BeginVertical(GUI.skin.box, null);
            scroll = GUILayout.BeginScrollView(scroll, GUI.skin.box);

            var orig = GUI.skin.button;
            GUI.skin.button = GUI.skin.label;

            bool justScrolled = CheckArrowInput(m_currentOptions.Length);

            if (m_currentCommand == null)
            {
                // choosing first command
                int i = 0;
                foreach (var opt in m_currentOptions)
                {
                    bool selected = i == m_selectedAutocompleteIndex;
                    var cmd = UDebug.commandByName[opt];
                    var lbl = "<b><color=cyan>" + opt + "</color></b> (" + cmd.Description + ")";
                    AutoCompleteButton(lbl, opt, selected);
                    
                    if (selected && justScrolled)
                    {
                        var f = GetVerticalScrollPos(i, m_currentOptions.Length);
                        scroll.y = f;
                    }
                    i++;
                }
            }
            else
            {
                if (m_invalidCommand)
                    GUILayout.Label("<b><color=red>Invalid command!</color></b>", null);
                else
                {
                    // chose first command
                    int i = 0;
                    foreach (var opt in m_currentOptions)
                    {
                        bool selected = i == m_selectedAutocompleteIndex;
                        AutoCompleteButton(opt, opt, selected);

                        if (selected && justScrolled)
                        {
                            var f = GetVerticalScrollPos(i, m_currentOptions.Length);
                            scroll.y = f;
                        }
                        i++;
                    }
                }
            }

            GUI.skin.button = orig;

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // display a string as an AutoComplete result. Shows just as a Label, but acts as a Button.
        private static void AutoCompleteButton(string s, string rawString = null, bool selected = false)
        {
            if (selected)
                s = "<i><color=green>" + s + "</color></i>";

            bool select = false;

            if (GUILayout.Button(s, null))
                select = true;

            if (select || (m_wantToChooseSelectedAutocomplete && selected))
            {
                // split input by space
                var splitInput = m_input.Split(' ');

                // update the last entry of the command to the autocomplete value
                splitInput[splitInput.Length - 1] = rawString ?? s;

                // reset input
                m_input = "";

                // rebuild input from split, with updated value now set.
                foreach (var split in splitInput)
                {
                    if (m_input != "") m_input += " ";
                    m_input += split;
                }

                // reset chosen autocomplete option
                m_selectedAutocompleteIndex = -1;
                m_wantToChooseSelectedAutocomplete = false;

                // fix end of input and carot position
                m_input += " ";

                GUI.FocusControl(InputControlName);

                try
                {
                    var stateObj = GUIUtility.GetStateObject(Il2CppType.Of<TextEditor>(), GUIUtility.keyboardControl);

                    if (stateObj != null)
                    {
                        var obj = stateObj.TryCast<TextEditor>();

                        obj.MoveTextEnd();

                        focusWanted = true;
                    }
                }
                catch { }
            }
        }

        private static float GetVerticalScrollPos(int index, int maxcount)
        {
            return index * 25;

            //if (maxcount > 50) maxcount = 50;

            //var ratio = index / (decimal)maxcount;
            //return (float)ratio * 1000;
        }

        private static bool CheckArrowInput(int maxCount)
        {
            if (UnityEngine.Time.time - m_timeOfLastInput < 0.1f)
                return false;

            bool justScrolled = false;
            Event e = Event.current;
            if (e.type != EventType.Repaint && e.type != EventType.Layout)
            {
                if (e.keyCode == KeyCode.UpArrow)
                {
                    m_timeOfLastInput = UnityEngine.Time.time;
                    justScrolled = true;
                    e.Use();
                    if (m_selectedAutocompleteIndex > 0)
                        m_selectedAutocompleteIndex--;
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    m_timeOfLastInput = UnityEngine.Time.time;
                    justScrolled = true;
                    e.Use();
                    if (m_selectedAutocompleteIndex < maxCount - 1)
                        m_selectedAutocompleteIndex++;
                }
            }
            return justScrolled;
        }
    }
}
