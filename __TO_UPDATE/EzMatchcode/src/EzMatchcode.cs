using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using Harmony;
using UnityEngine;
using Menu;
using UI;
using Modal;
using Characters;

namespace EzMatchcodeMod
{
    public class EzMatchcode : MelonMod
    {
        public class ModInfo
        {
            public const string GUID = "com.sinai.hellpoint.ezmatchcode";
            public const string NAME = "EzMatchcode";
            public const string AUTHOR = "Sinai";
            public const string VERSION = "1.0.0";
            public const string GAME_NAME = "Hellpoint";
            public const string GAME_COMPANY = "Cradle Games";
        }

        private static float m_timeOfLastInput = -1f;        

        //public override void OnApplicationStart()
        //{
        //    base.OnApplicationStart();

        //    var harmony = HarmonyInstance.Create(ModInfo.GUID);
        //    harmony.PatchAll();
        //}

        public static void SetValue(NumberInput __instance, int value)
        {
            __instance.values[__instance.index] = value;
            __instance.instances[__instance.index].text = value.ToString();

            SetSelection(__instance, __instance.index + 1);
        }

        public static void SetSelection(NumberInput menu, int index)
        {
            m_timeOfLastInput = Time.time;

            if (index < 0 || index > 7)
            {
                return;
            }

            menu.index = index;
            var btn = menu.selectables[index];
            btn.Select();
        }

        public static void SetFromClipboard(NumberInput __instance)
        {
            var clipboard = GUIUtility.systemCopyBuffer.Trim().Replace(" ", "");
            __instance.index = 0;
            foreach (var _char in clipboard)
            {
                if (__instance.index >= 8)
                {
                    __instance.index = 7;
                    break;
                }

                int val = int.Parse(_char.ToString());
                SetValue(__instance, val);
            }
        }

        [HarmonyPatch(typeof(NumberInput), nameof(NumberInput.UpdateInput))]
        public class NumberInput_UpdateInput
        {
            [HarmonyPrefix]
            public static bool Prefix(NumberInput __instance)
            {
                if (Time.time - m_timeOfLastInput < 0.1f)
                {
                    return false;
                }

                // Check for navigation input (left/right/backspace)
                
                // ctrl+v
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKeyDown(KeyCode.V))
                    {
                        SetFromClipboard(__instance);
                    }
                }
                // backspace/left arrow
                else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    SetSelection(__instance, __instance.index - 1);
                    return false;
                }
                // right arrow
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    SetSelection(__instance, __instance.index + 1);
                    return false;
                }
                // escape (cancel)
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    __instance.Hide();
                    return false;
                }
                else
                {
                    // Check for number key input
                    for (int i = 0; i < 10; i++)
                    {
                        var alphaKey = (KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + i);
                        var numpadKey = (KeyCode)Enum.Parse(typeof(KeyCode), "Keypad" + i);

                        if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(numpadKey))
                        {
                            SetValue(__instance, i);

                            break;
                        }
                    }
                }

                return true;
            }
        }

        // This patch was an attempt at fixing a very minor visual glitch.
        // Basically, the first number button will stay "highlighted" until you manually re-highlight, then un-highlight it.
        // I tried to force that when that menu first opens, but its not working at the moment.

        //[HarmonyPatch(typeof(View), nameof(View.Open))]
        //public class View_Open
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(View __instance)
        //    {
        //        var type = __instance.GetIl2CppType().ToString();
        //        if (type.Contains("NumberInput"))
        //        {
        //            var numInput = __instance.TryCast<NumberInput>();
        //            if (numInput && numInput.selectables != null && numInput.selectables[0])
        //            {
        //                numInput.selectables[0].Select();
        //            }
        //            else
        //            {
        //                MelonLogger.Log("Couldn't select first button!");
        //            }
        //        }
        //    }
        //}
    }
}
