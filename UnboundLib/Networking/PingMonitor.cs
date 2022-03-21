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

        private int pingUpdate = 0;

        private void Start()
        {
            if (!PhotonNetwork.OfflineMode && PhotonNetwork.CurrentRoom != null)
            {
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    ConnectedPlayers.Add(player.ActorNumber, true);
                }
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
                return;
            }
        }

        private void FixedUpdate()
        {
            // We only check ping if connected to a room.
            if (!PhotonNetwork.OfflineMode)
            {
                pingUpdate++;
            }

            // We want to check our ping every 100 frames, roughly every 2 secs.
            if (pingUpdate > 99)
            {
                pingUpdate = 0;
                RPCA_UpdatePings();
            }
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
            this.ExecuteAfterSeconds(0.5f, () =>
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
            if (changedProps.TryGetValue("Ping", out var ping))
            {
                PlayerPings[targetPlayer.ActorNumber] = (int) ping;

                if (PingUpdateAction != null)
                {
                    try
                    {
                        PingUpdateAction(targetPlayer.ActorNumber, (int) ping);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
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
            var players = PlayerManager.instance.players.Where((player) => player.data.view.OwnerActorNr == actorNumber).ToArray();

            // If it's not an empty array, return it.
            if (players.Length > 0)
            {
                return players;
            }

            // Default to null.
            return null;
        }

        public PingColor GetPingColors(int ping)
        {
            PingColor result;

            if (ping <= 150)
            {
                result = new PingColor(new Color(255f / 255f, 255f / 255f, 255f / 255f), "#FFFFFF");
            }
            else if (ping <= 200)
            {
                result = new PingColor(new Color(191f / 255f, 101f / 255f, 17f / 255f), "#bf6511");
            }
            else
            {
                result = new PingColor(new Color(219f / 255f, 0f / 255f, 0f / 255f), "#db0000");
            }

            return result;
        }

        public struct PingColor
        {
            public Color color;
            public string HTMLCode;

            public PingColor(Color colorColor, string code)
            {
                color = colorColor;
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
