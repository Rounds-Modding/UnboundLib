using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using UnboundLib.Networking;
using Photon.Pun;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Jotunn.Utils;
using UnboundLib.Utils.UI;

namespace UnboundLib.Networking
{
    internal static class SyncModClients
    {
        private static readonly float timeoutTime = 5f;
        private static readonly Vector3 offset = new Vector3(0f, 60f, 0f);

        private static List<string> clientSideGUIDs = new List<string>();

        private static Dictionary<int, string[]> extra = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> missing = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> mismatch = new Dictionary<int, string[]>();

        private static Dictionary<int, string[]> clientsServerSideMods = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> clientsServerSideGUIDs = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> clientsModVersions = new Dictionary<int, string[]>();

        private static List<string> hostsServerSideMods = new List<string>() { };
        private static List<string> hostsServerSideGUIDs = new List<string>() { };
        private static List<string> hostsModVersions = new List<string>() { };

        private static Dictionary<string, PluginInfo> loadedMods = new Dictionary<string, PluginInfo>() { };
        private static List<string> loadedGUIDs = new List<string>() { };
        private static List<string> loadedModNames = new List<string>() { };
        private static List<string> loadedVersions = new List<string>() { };

        internal static void RequestSync()
        {
            UnityEngine.Debug.Log("REQUESTING SYNC...");

            NetworkingManager.RPC(typeof(SyncModClients), "SyncLobby", new object[] { });
        }

        [UnboundRPC]
        internal static void SyncLobby()
        {
            Reset();
            LocalSetup();
            if (PhotonNetwork.IsMasterClient)
            {
                UnityEngine.Debug.Log("SYNCING...");
                CheckLobby();
                Unbound.Instance.StartCoroutine(Check());

            }
        }

        internal static System.Collections.IEnumerator Check()
        {
            bool timeout = false;
            float startTime = Time.time;
            while (clientsServerSideGUIDs.Keys.Count < PhotonNetwork.PlayerList.Except(new List<Photon.Realtime.Player> { PhotonNetwork.LocalPlayer }).ToList().Count)
            {
                UnityEngine.Debug.Log("WAITING");

                if (Time.time > startTime + timeoutTime)
                {
                    timeout = true;
                    break;
                }

                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            UnityEngine.Debug.Log("FINISHED WAITING.");
            FindDifferences();
            MakeFlags(timeout);
            yield break;
        }

        internal static void LocalSetup()
        {
            loadedMods = BepInEx.Bootstrap.Chainloader.PluginInfos;

            foreach (string ID in loadedMods.Keys)
            {
                if (!clientSideGUIDs.Contains(loadedMods[ID].Metadata.GUID))
                {
                    loadedGUIDs.Add(loadedMods[ID].Metadata.GUID);
                    loadedModNames.Add(loadedMods[ID].Metadata.Name);
                    loadedVersions.Add(loadedMods[ID].Metadata.Version.ToString());
                }
            }
        }

        internal static void CheckLobby()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(SyncModClients), "SendModList", new object[] {  });
            }
        }

        internal static void Reset()
        {
            clientsServerSideMods = new Dictionary<int, string[]>();
            clientsServerSideGUIDs = new Dictionary<int, string[]>();
            clientsModVersions = new Dictionary<int, string[]>();

            extra = new Dictionary<int, string[]>();
            missing = new Dictionary<int, string[]>();
            mismatch = new Dictionary<int, string[]>();

            hostsServerSideMods = new List<string>() { };
            hostsServerSideGUIDs = new List<string>() { };
            hostsModVersions = new List<string>() { };
            loadedGUIDs = new List<string>() { };
            loadedModNames = new List<string>() { };
            loadedVersions = new List<string>() { };
        }

        internal static void FindDifferences()
        {

            foreach (int actorID in clientsServerSideGUIDs.Keys)
            {
                missing[actorID] = hostsServerSideGUIDs.Except(clientsServerSideGUIDs[actorID]).ToArray();
                extra[actorID] = clientsServerSideGUIDs[actorID].Except(hostsServerSideGUIDs).ToArray();
                mismatch[actorID] = clientsServerSideGUIDs[actorID].Except(extra[actorID]).Except(missing[actorID]).Where(guid => HostVersionFromGUID(guid) != VersionFromGUID(actorID, guid)).ToArray();
            }

        }
        private static string HostVersionFromGUID(string GUID)
        {
            return hostsModVersions.Where((v, i) => hostsServerSideGUIDs[i] == GUID).FirstOrDefault();
        }
        private static string ModIDFromGUID(int actorID, string GUID)
        {
            return clientsServerSideMods[actorID].Where((v, i) => clientsServerSideGUIDs[actorID][i] == GUID).FirstOrDefault();
        }
        private static string VersionFromGUID(int actorID, string GUID)
        {
            return clientsModVersions[actorID].Where((v, i) => clientsServerSideGUIDs[actorID][i] == GUID).FirstOrDefault();
        }

        internal static void RegisterClientSideMod(string GUID)
        {
            if (!clientSideGUIDs.Contains(GUID)) { clientSideGUIDs.Add(GUID); }
        }

        internal static void MakeFlags(bool timeout)
        {
            UnityEngine.Debug.Log("MAKING FLAGS...");

            // add a host flag for the host
            NetworkingManager.RPC(typeof(SyncModClients), "AddFlags", new object[] { PhotonNetwork.LocalPlayer.ActorNumber, new string[] { "✓ " + PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.LocalPlayer.ActorNumber).NickName, "HOST" }, false });

            // if a player timed out, figure out which one(s) it was
            if (timeout)
            {
                int[] timeoutIDs = PhotonNetwork.CurrentRoom.Players.Values.Select(p => p.ActorNumber).Except(clientsServerSideGUIDs.Keys).Except(new int[] { PhotonNetwork.LocalPlayer.ActorNumber }).ToArray();

                foreach (int timeoutID in timeoutIDs)
                {
                    NetworkingManager.RPC(typeof(SyncModClients), "AddFlags", new object[] { timeoutID, new string[] { "✗ " + PhotonNetwork.CurrentRoom.GetPlayer(timeoutID).NickName, "UNMODDED"}, false });

                }
            }


            foreach (int actorID in clientsServerSideGUIDs.Keys)
            {
                List<string> flags = new List<string>() { };

                if (missing[actorID].Length == 0 && extra[actorID].Length == 0 && mismatch[actorID].Length == 0)
                {
                    flags.Add("✓ " + PhotonNetwork.CurrentRoom.GetPlayer(actorID).NickName);
                    flags.Add("ALL MODS SYNCED");
                    UnityEngine.Debug.Log(PhotonNetwork.CurrentRoom.GetPlayer(actorID).NickName + " is synced!");

                    NetworkingManager.RPC(typeof(SyncModClients), "AddFlags", new object[] {actorID, flags.ToArray(), false});
                    continue;
                }
                else
                {
                    flags.Add("✗ " + PhotonNetwork.CurrentRoom.GetPlayer(actorID).NickName);
                }
                foreach (string missingGUID in missing[actorID])
                {
                    flags.Add("MISSING: " + ModIDFromGUID(actorID, missingGUID) + " (" + missingGUID + ") Version: "+VersionFromGUID(actorID, missingGUID));
                }
                foreach (string versionGUID in mismatch[actorID])
                {
                    flags.Add("VERSION: " + ModIDFromGUID(actorID, versionGUID) + " (" + versionGUID + ") Version: " + VersionFromGUID(actorID, versionGUID) + "\nHost has: " + HostVersionFromGUID(versionGUID));
                }
                foreach (string extraGUID in extra[actorID])
                {
                    flags.Add("EXTRA: " + ModIDFromGUID(actorID, extraGUID) + " (" + extraGUID + ") Version: " + VersionFromGUID(actorID, extraGUID));
                }
                NetworkingManager.RPC(typeof(SyncModClients), "AddFlags", new object[] { actorID, flags.ToArray(), true });
            }
        }

        [UnboundRPC]
        private static void AddFlags(int actorID, string[] flags, bool error)
        {
            UnityEngine.Debug.Log("ADDING FLAGS");

            // display the sync status of each player here

            // each player has a unique actorID, which is tied to their Nickname (displayed in the lobby) by PhotonNetwork.CurrentLobby.GetPlayer(actorID).NickName

            // AddFlags adds an array of strings for one actorID when called. the array of strings are the status and warning messages for syncing mods

            // the first entry in the array is a simple "Good" "Bad" (checkmark or X) and ideally would always be shown next to the player's name in the lobby

            // if (error=true) then the text should ideally be red

            // when a player hovers over (with mouse) the green/red check/X it should display a textbox or something with the full error/warning messages - each entry on a new line
        }

        [UnboundRPC]
        private static void SendModList()
        {
            UnityEngine.Debug.Log("SENDING MODS...");
            NetworkingManager.RPC(typeof(SyncModClients), "ReceiveModList", new object[] { loadedGUIDs.ToArray(), loadedModNames.ToArray(), loadedVersions.ToArray(), PhotonNetwork.LocalPlayer.ActorNumber });
        }

        [UnboundRPC]
        private static void ReceiveModList(string[] serverSideGUIDs, string[] serverSideMods, string[] versions, int actorID)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                UnityEngine.Debug.Log("RECEIVING MODS...");

                if (PhotonNetwork.LocalPlayer.ActorNumber == actorID)
                {
                    hostsServerSideGUIDs = serverSideGUIDs.ToList();
                    hostsServerSideMods = serverSideMods.ToList();
                    hostsModVersions = versions.ToList();
                }
                else
                {
                    clientsServerSideGUIDs[actorID] = serverSideGUIDs;
                    clientsServerSideMods[actorID] = serverSideMods;
                    clientsModVersions[actorID] = versions;
                }

            }
        }
    }
}
