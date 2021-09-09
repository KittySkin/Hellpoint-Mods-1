using System;
using System.Collections.Generic;
using System.Reflection;
using Characters;
using Gameplay;
using HarmonyLib;
using Managers;
using Menu;
using Network;
using Save;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Networking;
using Web;
using Web.Requests;
using BepInEx;
using BepInEx.IL2CPP;

namespace HellpointMLR
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MPLimitRemover : BasePlugin
    {
        static MPLimitRemover Instance;

        const string GUID = "com.sinai.hellpoint.MPLimitRemover";
        const string NAME = "Multiplayer Limit Remover";
        const string VERSION = "2.0.0";

        public override void Load()
        {
            Instance = this;
            new Harmony(GUID).PatchAll(typeof(MPLimitRemover));
        }

        [HarmonyPatch(typeof(NetworkCommunication), nameof(NetworkCommunication.ClientAskForConnection))]
        public class NetworkCommunication_ClientAskForConnection
        {
            [HarmonyPrefix]
            public static bool Prefix(NetworkMessage netMsg)
            {
                Override(netMsg);
                return false;
            }

            static void IsUserAllowedCallback(NetworkMessage netMsg, MessageConnectionRole messageConnectionRole)
            {
                SaveManager.Saver.IsUserAllowed(netMsg.conn, messageConnectionRole.User,
                    DelegateSupport.ConvertDelegate<Saver.UserAllowedCallback>(
                        (Action<NetworkConnection, bool>)NetworkCommunication.IsUserAllowedCallback));
            }

            static void Override(NetworkMessage netMsg)
            {
                MessageConnectionRole messageConnectionRole = netMsg.ReadMessage<MessageConnectionRole>();
                if (messageConnectionRole.Version != GameManager.Version.ConnectionVersion)
                {
                    netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.WrongVersion, "", "", ""));
                    return;
                }
                if (messageConnectionRole.Role != ConnectionRole.Host && !SaveManager.Saver.AllowMultiplayer)
                {
                    netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.Unavailable, "", "", ""));
                    return;
                }
                // int coop = Player.Coop;
                // int duel = Player.Duel;
                switch (messageConnectionRole.Role)
                {
                    case ConnectionRole.None:
                        netMsg.conn.Disconnect();
                        return;
                    case ConnectionRole.Host:
                        bool doesHostExist = false;
                        foreach (var player in Player.Players)
                        {
                            if (player.faction.type == FactionType.Player)
                            {
                                doesHostExist = true;
                                break;
                            }
                        }
                        if (!doesHostExist)
                        {
                            netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.RoleAvailable, "", "", ""));
                            return;
                        }
                        netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.GameFull, "", "", ""));
                        return;
                    case ConnectionRole.Coop:
                        if (!ArenaManager.Active)
                        {
                            IsUserAllowedCallback(netMsg, messageConnectionRole);
                            return;
                        }
                        netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.GameFull, "", "", ""));
                        return;
                    case ConnectionRole.Duel:
                        if (!ArenaManager.Active)
                        {
                            IsUserAllowedCallback(netMsg, messageConnectionRole); 
                            return;
                        }
                        netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.GameFull, "", "", ""));
                        return;
                    case ConnectionRole.Arena:
                        if (ArenaManager.Active)
                        {
                            IsUserAllowedCallback(netMsg, messageConnectionRole); 
                            return;
                        }
                        netMsg.conn.Send(1001, new MessageHandshakeResult(HandshakeResult.GameFull, "", "", ""));
                        return;
                    default:
                        return;
                }
            }
        }

        [HarmonyPatch(typeof(WebService), nameof(WebService.Call),
           new Type[] { typeof(string), typeof(WebRequest), typeof(WebCallback), typeof(Il2CppSystem.Object), typeof(bool) })]
        public class WebService_Call
        {
            [HarmonyPrefix]
            public static void Prefix(ref WebRequest request)
            {
                try
                {
                    if (request.TryCast<StateUpdateRequest>() is StateUpdateRequest stateRequest)
                    {
                        if (NetworkRelation.Local != null && Boss.current == null && SaveManager.Saver.AllowMultiplayer)
                        {
                            if (!stateRequest.Helpable)
                                stateRequest.Helpable = true;

                            if (!stateRequest.Invadable)
                                stateRequest.Invadable = true;
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Instance.Log.LogMessage("StateUpdateRequest Call error: " + e.ToString());
                }
            }
        }

        [HarmonyPatch(typeof(OnlineManager), nameof(OnlineManager.StateUpdateAnswered))]
        public class OnlineManager_StateUpdateAnswered
        {
            [HarmonyPrefix]
            public static bool Prefix(WebCall call)
            {
                Override(call);
                return false;
            }

            public static void Override(WebCall call)
            {
                if (!string.IsNullOrEmpty(call.Error))
                    return;

                var res = call.GetResult<StateUpdateResult>();
                if (!string.IsNullOrEmpty(res.Error))
                {
                    OnlineManager.authentication = null;
                    return;
                }

                var invite = res.Invite;
                if (invite != Il2CppSystem.Guid.Empty && PlacedInvite.Invites.ContainsKey(invite))
                {
                    var placedInvite = PlacedInvite.Invites[invite];
                    NetworkServer.Destroy(placedInvite.gameObject);
                }

                if (string.IsNullOrEmpty(res.Address) && string.IsNullOrEmpty(res.SDA64))
                    return;

                if (LoadingView.Loading)
                    return;

                var con = new OnlineManager.Connection();
                var mgr = Manager<OnlineManager>.Instance;
                con.coroutine = mgr.StartCoroutine(OnlineManager.ConnectToClientAsync(con, res.SDA64, res.Address, res.Port, res.Delay));
            }
        }

        // // I think these two patches are related to Invites (hand signs). They run every ~5 secs or so.
        // // Again, all I changed was any check on the limit to use our config instead.

        // [HarmonyPatch(typeof(OnlineManager), nameof(OnlineManager.AllInvitesAnswered))]
        // public class OnlineManager_AllInvitesAnswered
        // {
        //     [HarmonyPrefix]
        //     public static bool Prefix(WebCall call)
        //     {
        //         Override(call);
        //         return false;
        //     }

        //     public static void Override(WebCall call)
        //     {
        //         if (!string.IsNullOrEmpty(call.Error))
        //         {
        //             return;
        //         }

        //         var result = call.GetResult<AllInvitesResult>();

        //         if (!string.IsNullOrEmpty(result.Error))
        //         {
        //             return;
        //         }

        //         var hashSet = new HashSet<Il2CppSystem.Guid>();
        //         for (int i = 0; i < result.Invites.Length; i++)
        //         {
        //             if (result.invites[i] != null)
        //             {
        //                 hashSet.Add(result.Invites[i].ID);
        //             }
        //         }

        //         var array = new List<PlacedInvite>();
        //         foreach (var value in PlacedInvite.Invites.Values)
        //         {
        //             if (value != null)
        //                 array.Add(value);
        //         }

        //         // In vanilla, invites are disabled if you're in a duel. We probably don't want that.
        //         // bool destroyAll = Player.Duel > 0;

        //         for (int j = array.Count - 1; j >= 0; j--)
        //         {
        //             if (array[j].Invite.ID.ToString() == Guid.Empty.ToString() || !hashSet.Contains(array[j].Invite.ID)) // || destroyAll)
        //             {
        //                 UnityEngine.Object.Destroy(array[j].gameObject);
        //             }
        //         }

        //         //if (Player.Duel > 0)
        //         //{
        //         //    return;
        //         //}

        //         for (int k = 0; k < result.Invites.Length; k++)
        //         {
        //             var invite = result.invites[k];

        //             if (PlacedInvite.Invites.ContainsKey(invite.ID))
        //             {
        //                 continue;
        //             }

        //             if ((invite.Friend && Player.Coop >= config.Max_Coop_Players) || (!invite.friend && Player.Duel >= config.Max_Invaders))
        //             {
        //                 continue;
        //             }

        //             if (invite.Transform != null)
        //             {
        //                 var position = invite.Transform.Position;
        //                 var rotation = invite.Transform.Rotation;

        //                 var gameObject = GameObject.Instantiate(invite.Friend
        //                     ? Manager<OnlineManager>.Instance.inviteHelpPrefab.gameObject
        //                     : Manager<OnlineManager>.Instance.inviteDuelPrefab.gameObject, position, rotation);

        //                 var component = gameObject.GetComponent<PlacedInvite>();
        //                 component.Invite = invite;

        //                 PlacedInvite.Invites.Add(invite.ID, component);
        //                 NetworkServer.Spawn(gameObject);
        //             }
        //         }
        //     }
        // }
    }
}
