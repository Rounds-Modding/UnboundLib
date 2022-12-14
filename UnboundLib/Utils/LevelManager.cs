using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using UnboundLib.Networking;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Utils
{
    public class LevelManager : MonoBehaviour
    {
        // A sorted dictionary of all levels in alphabetical order
        public static readonly SortedDictionary<string, Level> levels = new SortedDictionary<string, Level>();

        // A dictionary of visual name against the real name
        private static readonly Dictionary<string, string> levelPaths = new Dictionary<string, string>();

        // A string array of all levels not in proper order
        private static string[] allLevels
        {
            get
            {
                List<string> _allLevels = new List<string>();
                _allLevels.AddRange(activeLevels);
                _allLevels.AddRange(inactiveLevels);
                _allLevels.Sort((x, y) => string.CompareOrdinal(x, y));
                return _allLevels.ToArray();
            }
        }

        // All default, active and inactive levels
        private static string[] defaultLevels;
        internal static ObservableCollection<string> activeLevels;
        internal static readonly List<string> inactiveLevels = new List<string>();
        internal static ObservableCollection<string> previousActiveLevels = new ObservableCollection<string>();
        internal static readonly List<string> previousInactiveLevels = new List<string>();

        // List of all categories
        internal static readonly List<string> categories = new List<string>();
        // Dictionary of category name against if it is enabled
        internal static readonly Dictionary<string, ConfigEntry<bool>> categoryBools = new Dictionary<string, ConfigEntry<bool>>();

        public static LevelManager instance;

        private void Start()
        {
            // add default level to list
            defaultLevels = MapManager.instance.levels;
            // add default levels to active levels
            activeLevels = new ObservableCollection<string>(defaultLevels);
            activeLevels.CollectionChanged += LevelsChanged;

            // Populate the levels dictionary
            foreach (var level in allLevels)
            {
                levels[level] = new Level(level, IsLevelActive(level));
            }

            instance = this;

            // Sort some of the default levels to a separate category
            foreach (var level in levels.Values)
            {
                if (level.name.Contains("Phys") || level.name.Contains("Destructible") || level.name.Contains("Grape") || level.name.Contains("Serendipity") || level.name.Contains("Jumbo"))
                {
                    level.category = "Default physics";
                }
            }

            // Populate the categories list
            foreach (var level in levels.Values.Where(level => !categories.Contains(level.category)))
            {
                categories.Add(level.category);
            }

            // Populate the categoryBools dictionary
            foreach (var category in categories)
            {
                categoryBools[category] = Unbound.config.Bind("Level categories", category, true);
            }

            // Disable all default levels that are disabled in config
            foreach (var level in allLevels)
            {
                levelPaths[GetVisualName(level)] = level;
                if (!Unbound.config.Bind("Levels: " + levels[level].category, GetVisualName(level), true).Value)
                {
                    DisableLevel(level);
                }
            }
        }

        // This gets executed when activeLevels changes
        private static void LevelsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            MapManager.instance.levels = activeLevels.ToArray();
            if (args.NewItems != null)
            {
                foreach (string levelName in args.NewItems)
                {
                    if (!levels.ContainsKey(levelName))
                    {
                        levels[levelName] = new Level(levelName, IsLevelActive(levelName));
                    }
                }
            }
            activeLevels = new ObservableCollection<string>(activeLevels.OrderBy(i => i));
            activeLevels.CollectionChanged += LevelsChanged;
            // Sync levels over clients
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(LevelManager), nameof(RPC_HostMapHandshakeResponse), (object) activeLevels.Select(c => c).ToArray());
            }
        }

        public static void EnableLevels(string[] levelNames, bool saved = true)
        {
            foreach (var levelName in levelNames)
            {
                EnableLevel(levelName, saved);
            }
        }

        public static void EnableLevel(string levelName, bool saved = true)
        {
            if (activeLevels.Contains(levelName)) return;
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


            if (!saved) return;
            levels[levelName].enabled = true;
            Unbound.config.Bind("Levels: " + levels[levelName].category, GetVisualName(levelName), true).Value = true;
        }

        public static void DisableLevels(string[] levelNames, bool saved = true)
        {
            foreach (var levelName in levelNames)
            {
                DisableLevel(levelName, saved);
            }
        }

        public static void DisableLevel(string levelName, bool saved = true)
        {
            if (inactiveLevels.Contains(levelName)) return;
            if (activeLevels.Contains(levelName))
            {
                activeLevels.Remove(levelName);
            }
            if (!inactiveLevels.Contains(levelName))
            {
                inactiveLevels.Add(levelName);
                inactiveLevels.Sort(string.CompareOrdinal);
            }

            if (!saved) return;
            levels[levelName].enabled = false;
            Unbound.config.Bind("Levels: " + levels[levelName].category, GetVisualName(levelName), true).Value = false;
        }


        public static void RemoveLevels(string[] levelNames)
        {
            foreach (var levelName in levelNames)
            {
                RemoveLevel(levelName);
            }
        }

        public static void RemoveLevel(string levelName)
        {
            if (inactiveLevels.Contains(levelName))
            {
                inactiveLevels.Remove(levelName);
            }

            if (activeLevels.Contains(levelName))
            {
                activeLevels.Remove(levelName);
            }

            if (levels.ContainsKey(levelName))
            {
                levels.Remove(levelName);
            }
        }

        public static void EnableCategory(string categoryName)
        {
            if (categoryBools.ContainsKey(categoryName)) categoryBools[categoryName].Value = true;
        }

        public static void DisableCategory(string categoryName)
        {
            if (categoryBools.ContainsKey(categoryName)) categoryBools[categoryName].Value = false;
        }

        public static bool IsLevelActive(string levelName)
        {
            return activeLevels.Contains(levelName);
        }

        public static bool IsCategoryActive(string categoryName)
        {
            return categoryBools.ContainsKey(categoryName) && categoryBools[categoryName].Value;
        }

        public static string[] GetLevelsInCategory(string category)
        {
            return (from level in levels where level.Value.category.Contains(category) select level.Key).ToArray();
        }

        public static string GetVisualName(string path)
        {
            var visualName = path
                .Replace(" ", "")
                .Replace("Assets", "")
                .Replace(".unity", "");
            visualName = Regex.Replace(visualName, "/.*/", string.Empty);
            visualName = visualName.Replace("/", "_");
            visualName = visualName.Replace("\\", "_");
            visualName = visualName.Replace(":", "_");
            return visualName;
        }

        public static string GetFullName(string visualName)
        {
            return levelPaths[visualName];
        }

        public static void RegisterMaps(AssetBundle assetBundle, string category = "Modded")
        {
            RegisterMaps(assetBundle.GetAllScenePaths(), category);
        }

        public static void RegisterMaps(IEnumerable<string> paths, string category = "Modded")
        {
            RegisterNamedMaps(paths, new Dictionary<string, string>(), category);
        }
        public static void RegisterNamedMaps(IEnumerable<string> paths, IDictionary<string, string> mapNames , string category = "Modded")
        {
            foreach (var path in paths.Distinct())
            {
                if (mapNames.ContainsKey(path))
                    RegisterMap(path, mapNames[path], category);
                else
                    RegisterMap(path, path, category);
            }
            // Sort activeLevels
            activeLevels = new ObservableCollection<string>(activeLevels.OrderBy(i => i));
            activeLevels.CollectionChanged += LevelsChanged;

            activeLevels = new ObservableCollection<string>(activeLevels.Distinct().ToList());
            activeLevels.CollectionChanged += LevelsChanged;
            MapManager.instance.levels = activeLevels.ToArray();
        }
        private static void RegisterMap(string path, string name, string category)
        {
            if (!levels.ContainsKey(path))
            {
                levels[path] = new Level(name, true, false, category);
            }
            else
            {
                levels[path].category = category;
            }

            activeLevels.Add(path);

            if (!categories.Contains(category))
            {
                categories.Add(category);
                categoryBools[category] = Unbound.config.Bind("Level categories", category, true);
            }
            if (!Unbound.config.Bind("Levels: " + levels[path].category, GetVisualName(path), true).Value)
            {
                DisableLevel(path);
            }
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
                    if (!(currentMatches > 0.1f) || !(currentMatches > bestMatches)) continue;
                    bestMatches = currentMatches;
                    levelId = i;
                }
                if (levelId != -1)
                {
                    MapManager.instance.LoadLevelFromID(levelId, false, true);
                }
                else
                {
                    Unbound.BuildInfoPopup("Can't find level: " + formattedMessage);
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

        #region Syncing

        public static void OnLeftRoomAction()
        {
            foreach (var level in previousActiveLevels)
            {
                EnableLevel(level);
                foreach (var obj in ToggleLevelMenuHandler.instance.lvlObjs.Where(c => c.name == level))
                {
                    ToggleLevelMenuHandler.UpdateVisualsLevelObj(obj);
                }
            }
            foreach (var level in previousInactiveLevels)
            {
                DisableLevel(level);
                foreach (var obj in ToggleLevelMenuHandler.instance.lvlObjs.Where(c => c.name == level))
                {
                    ToggleLevelMenuHandler.UpdateVisualsLevelObj(obj);
                }
            }
        }

        // Do the map handshake on joined room
        public static void OnJoinedRoomAction()
        {
            foreach (var level in activeLevels)
            {
                previousActiveLevels.Add(level);
            }
            previousInactiveLevels.AddRange(inactiveLevels);
            // send available map pool to the master client
            if (!PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(LevelManager), nameof(RPC_MapHandshake), (object) allLevels.Select(c => c).ToArray());
            }
        }

        [UnboundRPC]
        private static void RPC_MapHandshake(string[] levels)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            List<string> disabledLevels = new List<string>();

            // disable any levels which aren't shared by other players
            foreach (var map in allLevels)
            {
                if (levels.Contains(map)) continue;
                DisableLevel(map, false);
                disabledLevels.Add(map);
                foreach (var obj in ToggleLevelMenuHandler.instance.lvlObjs.Where(t => t.name == map))
                {
                    ToggleLevelMenuHandler.UpdateVisualsLevelObj(obj);
                }
            }

            if (disabledLevels.Count != 0)
            {
                Unbound.BuildModal()
                    .Title("These levels have been disabled because someone didn't have them")
                    .Message(string.Join(", ", disabledLevels.ToArray()))
                    .CancelButton("Copy", () =>
                    {
                        Unbound.BuildInfoPopup("Copied Message!");
                        GUIUtility.systemCopyBuffer = string.Join(", ", disabledLevels.ToArray());
                    })
                    .CancelButton("Cancel", () => { })
                    .Show();
            }

            // reply to all users with new list of valid levels
            NetworkingManager.RPC_Others(typeof(LevelManager), nameof(RPC_HostMapHandshakeResponse), (object) activeLevels.Select(c => c).ToArray());
        }

        [UnboundRPC]
        private static void RPC_HostMapHandshakeResponse(string[] levels)
        {
            // enable and disable only levels that the host has specified are allowed
            foreach (var map in allLevels)
            {
                //c.SetValue(cards.Contains(c.info.cardName));
                if (levels.Contains(map))
                {
                    EnableLevel(map);
                    foreach (var obj in ToggleLevelMenuHandler.instance.lvlObjs.Where(t => t.name == map))
                    {
                        ToggleLevelMenuHandler.UpdateVisualsLevelObj(obj);
                    }
                }
                else
                {
                    DisableLevel(map);
                    foreach (var obj in ToggleLevelMenuHandler.instance.lvlObjs.Where(t => t.name == map))
                    {
                        ToggleLevelMenuHandler.UpdateVisualsLevelObj(obj);
                    }
                }
            }
        }

        #endregion
    }

    public class Level
    {
        public string name;
        public bool enabled;
        public bool selected;
        public string category;

        public Level(string name, bool enabled, bool selected = false, string category = "Vanilla")
        {
            this.name = name;
            this.enabled = enabled;
            this.selected = selected;
            this.category = category;
        }
    }
    public class Category
    {
        public string name;
        public bool enabled;

        public Category(string name, bool enabled = true)
        {
            this.name = name;
            this.enabled = enabled;
        }
    }
}