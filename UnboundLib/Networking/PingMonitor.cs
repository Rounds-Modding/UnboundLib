using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.Networking;
using UnityEngine;

namespace UnboundLib
{
    [DisallowMultipleComponent]
    public class PingMonitor : MonoBehaviourPunCallbacks
    {
        public Dictionary<int, bool> ConnectedPlayers = new Dictionary<int, bool>();
        public Dictionary<int, int> PlayerPings = new Dictionary<int, int>();
        public Action<int, int> PingUpdateAction;
        public static PingMonitor instance;
        private int pingUpdate;

        private void Start()
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null) return;
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                ConnectedPlayers.Add(player.ActorNumber, true);
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                DestroyImmediate(this);
            }
        }

        private void FixedUpdate()
        {
            // We only check ping if connected to a room.
            if (PhotonNetwork.OfflineMode) return;
            pingUpdate++;
            // We want to check our ping every 25 frames.
            if (pingUpdate <= 25) return;
            pingUpdate = 0;
            RPCA_UpdatePings();
        }

        public override void OnJoinedRoom()
        {
            // Refresh our variables once more, just to make sure they're clean.
            pingUpdate = 0;
            ConnectedPlayers = new Dictionary<int, bool>();
            PlayerPings = new Dictionary<int, int>();

            // Add each other player to our list
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                ConnectedPlayers.Add(player.ActorNumber, true);
                PlayerPings.Add(player.ActorNumber, 0);
            }

            // Run an RPC after a half second, to give the client time to connect to the lobby completely.
            this.ExecuteAfterFrames(15, () =>
            {
                NetworkingManager.RPC_Others(typeof(PingMonitor), nameof(RPCA_UpdatePings));
                RPCA_UpdatePings();
            });
        }

        public override void OnLeftRoom()
        {
            // Clear our variables after leaving the room, to reflect that we're no longer there.
            ConnectedPlayers.Clear();
            ConnectedPlayers = new Dictionary<int, bool>();
            PlayerPings.Clear();
            PlayerPings = new Dictionary<int, int>();
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            // The player is no longer marked as connected and is set to having 0 ping.
            ConnectedPlayers[otherPlayer.ActorNumber] = false;
            PlayerPings[otherPlayer.ActorNumber] = 0;
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            ConnectedPlayers[newPlayer.ActorNumber] = true;
            PlayerPings[newPlayer.ActorNumber] = 0;
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            // If ping was updated, we run any actions for it now.
            if (!changedProps.TryGetValue("Ping", out var ping)) return;
            PlayerPings[targetPlayer.ActorNumber] = (int) ping;

            if (PingUpdateAction == null) return;
            try
            {
                PingUpdateAction(targetPlayer.ActorNumber, (int)ping);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        /// <summary>
        /// Check the players to see which ones are controlled by a specific actor number.
        /// </summary>
        /// <param name="actorNumber">Actor number to check for.</param>
        /// <returns>An array of players who are owned by the actor number. Returns null if none are found.</returns>
        public Player[] GetPlayersByOwnerActorNumber(int actorNumber)
        {
            // Get each player with the same actor number
            var players = PlayerManager.instance.players.Where(player => player.data.view.OwnerActorNr == actorNumber).ToArray();

            // If it's not an empty array, return it or default to null.
            return players.Length > 0 ? players : null;
        }

        public PingColor GetPingColors(int ping)
        {
            var gradient = GetColorGradient(Normalize(ping, 40, 220, 0, 1));
            PingColor result = new PingColor("#" + ColorUtility.ToHtmlStringRGB(gradient));
            return result;
        }

        private static float Normalize(float val, float valmin, float valmax, float min, float max)
        {
            return ((val - valmin) / (valmax - valmin) * (max - min)) + min;
        }

        private static Color GetColorGradient(float percentage)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];

            colorKeys[0].color = Color.green;
            colorKeys[0].time = 0;
            colorKeys[1].color = Color.yellow;
            colorKeys[1].time = 0.5f;
            colorKeys[2].color = Color.red;
            colorKeys[2].time = 1;

            gradient.SetKeys(colorKeys, new GradientAlphaKey[3]);
            return gradient.Evaluate(percentage);
        }

        public struct PingColor
        {
            public string HTMLCode;

            public PingColor(string code)
            {
                HTMLCode = code;
            }
        }

        // Uses UnboundRPCs to send ping update requests.
        [UnboundRPC]
        private static void RPCA_UpdatePings()
        {
            // Get the current custom properties of the local photon player object.
            Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;

            // Record the ping, we don't care if we override anything.
            customProperties["Ping"] = PhotonNetwork.GetPing();

            // Send out the update to their properties.
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);
        }
    }
}
