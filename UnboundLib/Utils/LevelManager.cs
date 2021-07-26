using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using Photon.Pun;
using UnboundLib.Networking;
using UnityEngine;

namespace UnboundLib.Utils
{
    public class LevelManager
    {
        public static LevelManager instance = new LevelManager();

        public Dictionary<string, Level> levels = new Dictionary<string, Level>();

        internal static string[] allLevels
        {
            get
            {
                List<string> _allLevels = new List<string>();
                _allLevels.AddRange(activeLevels);
                _allLevels.AddRange(inactiveLevels);
                return _allLevels.ToArray();
            }
        }
        internal static string[] defaultLevels;
        internal static ObservableCollection<string> activeLevels;
        internal static List<string> inactiveLevels = new List<string>();

        private LevelManager()
        {
            // add default activeLevels to level list
            defaultLevels = MapManager.instance.levels;
            activeLevels = new ObservableCollection<string>(defaultLevels);
            activeLevels.CollectionChanged += LevelsChanged;

            if (levels == null)
            {
                foreach (var level in allLevels)
                {
                    //levels[level] = new Level(level, Unbound.)
                }
            }
        }
        
        internal static void LevelsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            MapManager.instance.levels = activeLevels.ToArray();
            activeLevels = new ObservableCollection<string>(activeLevels.OrderBy(i => i));
            activeLevels.CollectionChanged += LevelsChanged;
        }

        public static void OnJoinedRoomAction()
        {
            // send available map pool to the master client
            if (!PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(LevelManager), nameof(RPC_MapHandshake), (object)allLevels.Select(c => c).ToArray());
            }
        }
        
        [UnboundRPC]
        private static void RPC_MapHandshake(string[] levels)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            // disable any levels which aren't shared by other players
            foreach (var c in allLevels)
            {
                if (!levels.Contains(c))
                {
                    DisableLevel(c);
                }
                //c.SetValue(levels.Contains(c.info.cardName) && c.isEnabled.Value);
            }

            // reply to all users with new list of valid levels
            NetworkingManager.RPC_Others(typeof(LevelManager), nameof(RPC_HostMapHandshakeResponse), (object)activeLevels.Select(c => c).ToArray());
        }
        
        
        [UnboundRPC]
        private static void RPC_HostMapHandshakeResponse(string[] levels)
        {
            // enable and disable only levels that the host has specified are allowed
            foreach (var c in allLevels)
            {
                //c.SetValue(cards.Contains(c.info.cardName));
                if (levels.Contains(c))
                {
                    EnableLevel(c);
                }
                if (!levels.Contains(c))
                {
                    DisableLevel(c);
                }
            }
        }
        
        public static void EnableLevel(string levelName)
        {
            UnityEngine.Debug.LogWarning("Trying to enable: " + levelName);
            if (!activeLevels.Contains(levelName))
            {
                activeLevels.Add(levelName);
                activeLevels = new ObservableCollection<string>(activeLevels.OrderBy(i => i));
                activeLevels.CollectionChanged += LevelsChanged;
            }
            if (inactiveLevels.Contains(levelName))
            {
                inactiveLevels.Remove(levelName);
            }
        }

        // Remove this before merge
        public static void DisableLevel(string levelName)
        {
            UnityEngine.Debug.LogWarning("Trying to disable: " + levelName);
            if (activeLevels.Contains(levelName))
            {
                activeLevels.Remove(levelName);
            }
            if (!inactiveLevels.Contains(levelName))
            {
                inactiveLevels.Add(levelName);
                inactiveLevels.Sort((x, y) => string.CompareOrdinal(x, y));
            }
        }
        
        public static void RegisterMaps(AssetBundle assetBundle)
        {
            RegisterMaps(assetBundle.GetAllScenePaths());
        }

        public static void RegisterMaps(IEnumerable<string> paths)
        {
            foreach (var path in paths.Distinct())
            {
                activeLevels.Add(path);
            }
            // Sort activeLevels
            activeLevels = new ObservableCollection<string>(activeLevels.OrderBy(i => i));
            activeLevels.CollectionChanged += LevelsChanged;
            
            activeLevels = new ObservableCollection<string>(activeLevels.Distinct().ToList());
            activeLevels.CollectionChanged += LevelsChanged;
            MapManager.instance.levels = activeLevels.ToArray();
        }

        // loads a map in via its name prefixed with a forward-slash
        internal static void SpawnMap(string message)
        {
            if (!message.StartsWith("/"))
            {
                return;
            }
            // search code copied from card search 
            try
            {
                var currentLevels = MapManager.instance.levels;
                var levelId = -1;
                var bestMatches = 0f;

                // parse message
                var formattedMessage = message.ToUpper()
                    .Replace(" ", "_")
                    .Replace("/", "");

                for (var i = 0; i < currentLevels.Length; i++)
                {
                    var text = currentLevels[i].ToUpper()
                        .Replace(" ", "")
                        .Replace("ASSETS", "")
                        .Replace(".UNITY", "");
                    text = Regex.Replace(text, "/.*/", string.Empty);
                    text = text.Replace("/", "");

                    var currentMatches = 0f;
                    for (int j = 0; j < formattedMessage.Length; j++)
                    {
                        if (text.Length > j && formattedMessage[j] == text[j])
                        {
                            currentMatches += 1f / formattedMessage.Length;
                        }
                    }
                    currentMatches -= Mathf.Abs(formattedMessage.Length - text.Length) * 0.001f;
                    if (currentMatches > 0.1f && currentMatches > bestMatches)
                    {
                        bestMatches = currentMatches;
                        levelId = i;
                    }
                }
                if (levelId != -1)
                {
                    MapManager.instance.LoadLevelFromID(levelId, false, true);
                }
            }
            catch (Exception e)
            {
                Unbound.BuildModal()
                    .Title("Error Loading Level")
                    .Message($"No map found named:\n{message}\n\nError:\n{e}")
                    .CancelButton("Copy", () =>
                    {
                        Unbound.BuildInfoPopup("Copied Message!");
                        GUIUtility.systemCopyBuffer = e.ToString();
                    })
                    .CancelButton("Cancel", () => { })
                    .Show();
            }
        }
    }

    public class Level
    {
        public bool enabled;
        public string name;
        public string category;

        public Level(string name, bool enabled)
        {
            this.name = name;
            //this.category = category;
            this.enabled = enabled;
        }
    }
}