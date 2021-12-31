using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using ExitGames.Client.Photon;

namespace UnboundLib
{
    [DisallowMultipleComponent]
    public class PingMonitor : MonoBehaviourPunCallbacks
    {
        public Dictionary<int, bool> ConnectedPlayers = new Dictionary<int, bool>();
        public Dictionary<int, int> PlayerPings = new Dictionary<int, int>();
        public Action<int, int> PingUpdateAction;

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

        private void FixedUpdate()
        {
            // We only check ping if connected to a room.
            if (!PhotonNetwork.OfflineMode)
            {
                pingUpdate = pingUpdate + 1;
            }

            // We want to check our ping every 100 frames, roughly every 2 secs.
            if (pingUpdate > 99)
            {
                pingUpdate = 0;
                RPCA_UpdatePings();
            }
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            // See if the connecting player existed previously, if not register them.
            // There should be enough time between a player joining and their ping update to do so.
            if (ConnectedPlayers.TryGetValue(newPlayer.ActorNumber, out var connected))
            {
                connected = true;
                PlayerPings[newPlayer.ActorNumber] = 0;
            }
            else
            {
                ConnectedPlayers.Add(newPlayer.ActorNumber, true);
                PlayerPings.Add(newPlayer.ActorNumber, 0);
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

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            // If ping was updated, we run any actions for it now.
            if (changedProps.TryGetValue("Ping", out var ping))
            {
                PlayerPings[targetPlayer.ActorNumber] = (int)ping;

                if (PingUpdateAction != null)
                {
                    try
                    {
                        PingUpdateAction(targetPlayer.ActorNumber, (int)ping);
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
            var players = PlayerManager.instance.players.Where((player) => player.data.view.OwnerActorNr == actorNumber).ToArray();

            if (players.Length > 0)
            {
                return players;
            }
            return null;
        }

        [UnboundRPC]
        private static void RPCA_UpdatePings()
        {
            Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            if (customProperties.ContainsKey("Ping"))
            {
                customProperties["Ping"] = PhotonNetwork.GetPing();
            }
            else
            {
                customProperties.Add("Ping", PhotonNetwork.GetPing());
            }
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);
        }
    }
}
