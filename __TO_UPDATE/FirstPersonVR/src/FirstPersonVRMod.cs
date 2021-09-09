using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using Harmony;
using UnityEngine;
using Characters;
using Managers;
using Cameraman;

namespace FirstPersonVR
{
    public class FirstPersonVRMod : MelonMod
    {
        public class ModInfo
        {
            public const string GUID = "com.sinai.hellpoint.firstpersonvr";
            public const string NAME = "First Person VR";
            public const string AUTHOR = "Sinai";
            public const string VERSION = "1.0.0";
            public const string GAME_NAME = "Hellpoint";
            public const string GAME_COMPANY = "Cradle Games";
        }

        public static Player CurrentPlayer
        {
            get
            {
                if (!m_currentPlayer && Player.Players.Count > 0)
                {
                    m_currentPlayer = Player.Players[0];
                }
                return m_currentPlayer;
            }
        }
        private static Player m_currentPlayer;

        [HarmonyPatch(typeof(CameraBehaviour), nameof(CameraBehaviour.UpdateContextInternal))]
        public class CameraBehaviour_UpdateContextInternal
        {
            [HarmonyPostfix]
            public static void Postfix(CameraBehaviour __instance)
            {
                if (Player.Players.Count > 0)
                {
                    var cam = __instance;
                    var player = cam.owner.GetComponent<Player>();

                    cam.transform.position =  player.head.transform.position + (Vector3.up * 0.25f);

                    // todo disable character visuals
                    // not sure where they are stored in code but they're on Player GameObject

                    // will need to also smooth the camera when targeting, and fix a few bugs associated with that
                    // also fix bug when using off-hand weapon
                }
            }
        }
    }
}
