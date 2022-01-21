using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System.Linq;
using System.Collections.ObjectModel;
using TMPro;

namespace UnboundLib.GameModes
{
    public static class GameModeManager
    {
        // both of these identify existing gameobjects by name and therefore cannot be changed
        public const string SandBoxID = "Test";
        public const string ArmsRaceID = "Arms race";

        private static Dictionary<string, IGameModeHandler> handlers = new Dictionary<string, IGameModeHandler>();
        private static Dictionary<string, Type> gameModes = new Dictionary<string, Type>();
        private static Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>> hooks = new Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>>();
        private static Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>> onceHooks = new Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>>();

        // public properties that return deep copies of the handlers and gameModes dictionarys (the values in the dictionaries returned are shallow copies)
        public static ReadOnlyDictionary<string, IGameModeHandler> Handlers => new ReadOnlyDictionary<string, IGameModeHandler>(handlers.ToDictionary(kv => kv.Key, kv => kv.Value));
        public static ReadOnlyDictionary<string, Type> GameModes => new ReadOnlyDictionary<string, Type>(gameModes.ToDictionary(kv => kv.Key, kv => kv.Value));

        public static Action<IGameModeHandler> OnGameModeChanged { get; set; }

        public static string CurrentHandlerID { get; private set; }

        public static IGameModeHandler CurrentHandler
        {
            get
            {
                return CurrentHandlerID == null ? null : handlers[CurrentHandlerID];
            }
        }

        internal static void Init()
        {
            PhotonPeer.RegisterType(typeof(GameSettings), 200, GameSettings.Serialize, GameSettings.Deserialize);

            // Add preset game modes
            handlers.Add(ArmsRaceID, new ArmsRaceHandler());
            gameModes.Add(ArmsRaceID, typeof(GM_ArmsRace));
            handlers.Add(SandBoxID, new SandboxHandler());
            gameModes.Add(SandBoxID, typeof(GM_Test));

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name == "Main")
                {

                    SetupUI();

                    // Add game modes back when the main scene is reloaded
                    foreach (var id in handlers.Keys)
                    {
                        if (id != ArmsRaceID && id != SandBoxID)
                        {
                            AddGameMode(id, gameModes[id]);
                        }
                        handlers[id].SetActive(false);
                    }

                    SetGameMode(null);
                }
            };
        }
        internal static void SetupUI()
        {
            // Make existing UI buttons use our GameModeHandler logic
            var gameModeGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/GameMode");
            var onlineGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Online/Group");
            var groupGo = gameModeGo.transform.Find("Group");
            var versusGo = gameModeGo.transform.Find("Group").Find("Versus").gameObject;
            var sandboxGo = gameModeGo.transform.Find("Group").Find(SandBoxID).gameObject;
            var characterSelectGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect");
            var qMatchRem = onlineGo.transform.Find("Quick")?.gameObject;
            var tMatchRem = onlineGo.transform.Find("Twitch")?.gameObject;

            if (qMatchRem != null) { GameObject.DestroyImmediate(qMatchRem); }
            if (tMatchRem != null) { GameObject.DestroyImmediate(tMatchRem); }

            GameObject.DestroyImmediate(versusGo.GetComponent<Button>());
            var versusButton = versusGo.AddComponent<Button>();
            versusButton.onClick.AddListener(characterSelectGo.GetComponent<ListMenuPage>().Open);
            versusButton.onClick.AddListener(() => SetGameMode(ArmsRaceID));

            GameObject.DestroyImmediate(sandboxGo.GetComponent<Button>());
            var sandboxButton = sandboxGo.AddComponent<Button>();
            sandboxButton.onClick.AddListener(MainMenuHandler.instance.Close);
            sandboxButton.onClick.AddListener(() => {
                SetGameMode(SandBoxID);
                CurrentHandler.StartGame();
            });

            // destroy all other buttons in this menu
            List<GameObject> objsToDestroy = new List<GameObject>() { };
            for (int i = 0; i < groupGo.childCount; i++)
            {
                if (groupGo.GetChild(i)?.gameObject != null && groupGo.GetChild(i).gameObject.name != "Back" && groupGo.GetChild(i).gameObject.name != "Versus" && groupGo.GetChild(i).gameObject.name != SandBoxID)
                {
                    objsToDestroy.Add(groupGo.GetChild(i).gameObject);
                }
            }
            for (int i = 0; i < objsToDestroy.Count(); i++)
            {
                UnityEngine.GameObject.DestroyImmediate(objsToDestroy[i]);
            }

            var characterSelectPage = characterSelectGo.GetComponent<ListMenuPage>();

            // create gamemode buttons alphabetically
            foreach (var id in handlers.Keys.OrderByDescending(k => handlers[k].Name.ToLower()).Where(k => k!=SandBoxID && k!=ArmsRaceID))
            {
                var gameModeButtonGo = GameObject.Instantiate(versusGo, versusGo.transform.parent);
                gameModeButtonGo.SetActive(true);
                gameModeButtonGo.transform.localScale = Vector3.one;
                gameModeButtonGo.transform.SetSiblingIndex(0);

                var gameModeButtonText = gameModeButtonGo.GetComponentInChildren<TextMeshProUGUI>();
                gameModeButtonText.text = handlers[id].Name.ToUpper();

                GameObject.DestroyImmediate(gameModeButtonGo.GetComponent<Button>());
                var gameModeButton = gameModeButtonGo.AddComponent<Button>();

                gameModeButton.onClick.AddListener(characterSelectPage.Open);
                // create a copy of the string to give to the anonymous function
                string id_ = string.Copy(id); 
                gameModeButton.onClick.AddListener(() => GameModeManager.SetGameMode(id_));
            }

            // keep Versus and Sandbox at the top
            versusGo.transform.SetAsFirstSibling();
            sandboxGo.transform.SetSiblingIndex(1);

            // finally, if Versus or Sandbox were removed from the handlers/gamemodes, set their buttons to inactive - do not destroy their buttons
            versusGo.SetActive(handlers.ContainsKey(ArmsRaceID));
            sandboxGo.SetActive(handlers.ContainsKey(SandBoxID));
        }

        public static IEnumerator TriggerHook(string key)
        {
            key = key.ToLower();

            List<Func<IGameModeHandler, IEnumerator>> hooks;
            List<Func<IGameModeHandler, IEnumerator>> onceHooks;
            GameModeManager.hooks.TryGetValue(key, out hooks);
            GameModeManager.onceHooks.TryGetValue(key, out onceHooks);

            if (hooks != null && CurrentHandler != null)
            {
                foreach (var hook in hooks)
                {
                    yield return hook(CurrentHandler);
                }
            }

            if (onceHooks != null && CurrentHandler != null)
            {
                foreach (var hook in onceHooks)
                {
                    RemoveHook(key, hook);
                }

                GameModeManager.onceHooks.Remove(key);
            }
        }

        /// <summary>
        ///     Adds a hook that is automatically removed after it's triggered once.
        /// </summary>
        public static void AddOnceHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            if (action == null)
            {
                return;
            }

            // Case-insensitive keys for QoL
            key = key.ToLower();

            if (!onceHooks.ContainsKey(key))
            {
                onceHooks.Add(key, new List<Func<IGameModeHandler, IEnumerator>> { action });
            }
            else
            {
                onceHooks[key].Add(action);
            }

            AddHook(key, action);
        }

        public static void AddHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            if (action == null)
            {
                return;
            }

            // Case-insensitive keys for QoL
            key = key.ToLower();

            if (!hooks.ContainsKey(key))
            {
                hooks.Add(key, new List<Func<IGameModeHandler, IEnumerator>> { action });
            }
            else
            {
                hooks[key].Add(action);
            }
        }

        public static void RemoveHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            hooks[key.ToLower()].Remove(action);
        }

        public static T GetGameMode<T>(string gameModeId) where T : Component
        {
            return GameObject.Find("/Game/Code/Game Modes").transform.Find($"[GameMode] {gameModeId}").GetComponent<T>();
        }

        public static void SetGameMode(string id)
        {
            SetGameMode(id, true);
        }

        public static void SetGameMode(string id, bool setActive)
        {
            if (id != null && !handlers.ContainsKey(id))
            {
                throw new ArgumentException($"No such game mode handler: {id}");
            }

            if (CurrentHandlerID == id)
            {
                return;
            }

            var pm = PlayerManager.instance;

            if (CurrentHandler != null)
            {
                var pmPlayerDied = (Action<Player, int>) pm.GetFieldValue("PlayerDiedAction");
                pm.SetPropertyValue("PlayerJoinedAction", Delegate.Remove(pm.PlayerJoinedAction, new Action<Player>(CurrentHandler.PlayerJoined)));
                pm.SetFieldValue("PlayerDiedAction", Delegate.Remove(pmPlayerDied, new Action<Player, int>(CurrentHandler.PlayerDied)));
                CurrentHandler.SetActive(false);
            }

            CurrentHandlerID = id;

            if (id == null)
            {
                PlayerAssigner.instance.InvokeMethod("SetPlayersCanJoin", false);
            }
            else
            {
                PlayerAssigner.instance.InvokeMethod("SetPlayersCanJoin", true);

                if (setActive)
                {
                    CurrentHandler.SetActive(true);
                }

                var pmPlayerDied = (Action<Player, int>) pm.GetFieldValue("PlayerDiedAction");
                pm.SetPropertyValue("PlayerJoinedAction", Delegate.Combine(pm.PlayerJoinedAction, new Action<Player>(CurrentHandler.PlayerJoined)));
                pm.SetFieldValue("PlayerDiedAction", Delegate.Combine(pmPlayerDied, new Action<Player, int>(CurrentHandler.PlayerDied)));

                if (OnGameModeChanged != null)
                {
                    OnGameModeChanged(CurrentHandler);
                }
            }
        }

        public static IGameModeHandler GetHandler(string id)
        {
            return handlers[id];
        }

        public static void AddHandler<TGameMode>(string id, IGameModeHandler handler) where TGameMode : MonoBehaviour
        {
            handlers.Add(id, handler);
            gameModes.Add(id, typeof(TGameMode));
            AddGameMode(id, typeof(TGameMode));

            // rebuild UI
            SetupUI();
        }

        public static void RemoveHandler(string id)
        {
            if (handlers.ContainsKey(id)) { handlers.Remove(id); }
            if (gameModes.ContainsKey(id)) { gameModes.Remove(id); }
            RemoveGameMode(id);

            // rebuild UI
            SetupUI();
        }

        private static void AddGameMode(string id, Type type)
        {
            var go = new GameObject($"[GameMode] {id}");
            go.SetActive(false);
            go.transform.SetParent(GameObject.Find("/Game/Code/Game Modes").transform);
            go.AddComponent(type);
        }

        private static void RemoveGameMode(string id)
        {
            // do not destroy the sandbox or versus gamemodes
            if (id == SandBoxID || id == ArmsRaceID) { return; }
            var gameMode = GameObject.Find("/Game/Code/Game Modes").transform.Find($"[GameMode] {id}")?.gameObject;
            if (gameMode != null) { UnityEngine.GameObject.Destroy(gameMode); }
        }
    }
}
