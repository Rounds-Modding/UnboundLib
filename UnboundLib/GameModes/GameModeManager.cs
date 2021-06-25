using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

namespace UnboundLib.GameModes
{
    public static class GameModeManager
    {
        private static Dictionary<string, IGameModeHandler> handlers = new Dictionary<string, IGameModeHandler>();
        private static Dictionary<string, Type> gameModes = new Dictionary<string, Type>();
        private static Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>> hooks = new Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>>();
        private static Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>> onceHooks = new Dictionary<string, List<Func<IGameModeHandler, IEnumerator>>>();

        public static Action<IGameModeHandler> OnGameModeChanged { get; set; }

        public static string CurrentHandlerID { get; private set; }

        public static IGameModeHandler CurrentHandler
        {
            get
            {
                return GameModeManager.CurrentHandlerID == null ? null : GameModeManager.handlers[GameModeManager.CurrentHandlerID];
            }
        }

        internal static void Init()
        {
            PhotonPeer.RegisterType(typeof(GameSettings), 200, GameSettings.Serialize, GameSettings.Deserialize);

            // Add preset game modes
            GameModeManager.handlers.Add("ArmsRace", new ArmsRaceHandler());
            GameModeManager.gameModes.Add("ArmsRace", typeof(GM_ArmsRace));
            GameModeManager.handlers.Add("Sandbox", new SandboxHandler());
            GameModeManager.gameModes.Add("Sandbox", typeof(GM_Test));

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name == "Main")
                {
                    // Make existing UI buttons use our GameModeHandler logic
                    var gameModeGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/GameMode");
                    var versusGo = gameModeGo.transform.Find("Group").Find("Versus").gameObject;
                    var sandboxGo = gameModeGo.transform.Find("Group").Find("Test").gameObject;
                    var characterSelectGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect");

                    GameObject.DestroyImmediate(versusGo.GetComponent<Button>());
                    var versusButton = versusGo.AddComponent<Button>();
                    versusButton.onClick.AddListener(characterSelectGo.GetComponent<ListMenuPage>().Open);
                    versusButton.onClick.AddListener(() => GameModeManager.SetGameMode("ArmsRace"));

                    GameObject.DestroyImmediate(sandboxGo.GetComponent<Button>());
                    var sandboxButton = sandboxGo.AddComponent<Button>();
                    sandboxButton.onClick.AddListener(MainMenuHandler.instance.Close);
                    sandboxButton.onClick.AddListener(() => {
                        GameModeManager.SetGameMode("Sandbox");
                        GameModeManager.CurrentHandler.StartGame();
                    });

                    // Add game modes back when the main scene is reloaded
                    foreach (var id in GameModeManager.handlers.Keys)
                    {
                        if (id != "ArmsRace" && id != "Sandbox")
                        {
                            AddGameMode(id, GameModeManager.gameModes[id]);
                        }

                        GameModeManager.handlers[id].SetActive(false);
                    }

                    GameModeManager.SetGameMode(null);
                }
            };
        }

        public static IEnumerator TriggerHook(string key)
        {
            key = key.ToLower();

            List<Func<IGameModeHandler, IEnumerator>> hooks;
            List<Func<IGameModeHandler, IEnumerator>> onceHooks;
            GameModeManager.hooks.TryGetValue(key, out hooks);
            GameModeManager.onceHooks.TryGetValue(key, out onceHooks);

            if (hooks != null && GameModeManager.CurrentHandler != null)
            {
                foreach (var hook in hooks)
                {
                    yield return hook(GameModeManager.CurrentHandler);
                }
            }

            if (onceHooks != null && GameModeManager.CurrentHandler != null)
            {
                foreach (var hook in onceHooks)
                {
                    GameModeManager.RemoveHook(key, hook);
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

            if (!GameModeManager.onceHooks.ContainsKey(key))
            {
                GameModeManager.onceHooks.Add(key, new List<Func<IGameModeHandler, IEnumerator>> { action });
            }
            else
            {
                GameModeManager.onceHooks[key].Add(action);
            }

            GameModeManager.AddHook(key, action);
        }

        public static void AddHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            if (action == null)
            {
                return;
            }

            // Case-insensitive keys for QoL
            key = key.ToLower();

            if (!GameModeManager.hooks.ContainsKey(key))
            {
                GameModeManager.hooks.Add(key, new List<Func<IGameModeHandler, IEnumerator>> { action });
            }
            else
            {
                GameModeManager.hooks[key].Add(action);
            }
        }

        public static void RemoveHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            GameModeManager.hooks[key.ToLower()].Remove(action);
        }

        public static T GetGameMode<T>(string gameModeId) where T : Component
        {
            return GameObject.Find("/Game/Code/Game Modes").transform.Find($"[GameMode] {gameModeId}").GetComponent<T>();
        }

        public static void SetGameMode(string id)
        {
            GameModeManager.SetGameMode(id, true);
        }

        public static void SetGameMode(string id, bool setActive)
        {
            if (id != null && !GameModeManager.handlers.ContainsKey(id))
            {
                throw new ArgumentException($"No such game mode handler: {id}");
            }

            if (GameModeManager.CurrentHandlerID == id)
            {
                return;
            }

            var pm = PlayerManager.instance;

            if (GameModeManager.CurrentHandler != null)
            {
                var pmPlayerDied = (Action<Player, int>) pm.GetFieldValue("PlayerDiedAction");
                pm.SetPropertyValue("PlayerJoinedAction", Delegate.Remove(pm.PlayerJoinedAction, new Action<Player>(GameModeManager.CurrentHandler.PlayerJoined)));
                pm.SetFieldValue("PlayerDiedAction", Delegate.Remove(pmPlayerDied, new Action<Player, int>(GameModeManager.CurrentHandler.PlayerDied)));
                GameModeManager.CurrentHandler.SetActive(false);
            }

            GameModeManager.CurrentHandlerID = id;

            if (id == null)
            {
                PlayerAssigner.instance.InvokeMethod("SetPlayersCanJoin", false);
            }
            else
            {
                PlayerAssigner.instance.InvokeMethod("SetPlayersCanJoin", true);

                if (setActive)
                {
                    GameModeManager.CurrentHandler.SetActive(true);
                }

                var pmPlayerDied = (Action<Player, int>) pm.GetFieldValue("PlayerDiedAction");
                pm.SetPropertyValue("PlayerJoinedAction", Delegate.Combine(pm.PlayerJoinedAction, new Action<Player>(GameModeManager.CurrentHandler.PlayerJoined)));
                pm.SetFieldValue("PlayerDiedAction", Delegate.Combine(pmPlayerDied, new Action<Player, int>(GameModeManager.CurrentHandler.PlayerDied)));

                if (GameModeManager.OnGameModeChanged != null)
                {
                    GameModeManager.OnGameModeChanged(GameModeManager.CurrentHandler);
                }
            }
        }

        public static IGameModeHandler GetHandler(string id)
        {
            return GameModeManager.handlers[id];
        }

        public static void AddHandler<TGameMode>(string id, IGameModeHandler handler) where TGameMode : MonoBehaviour
        {
            GameModeManager.handlers.Add(id, handler);
            GameModeManager.gameModes.Add(id, typeof(TGameMode));
            GameModeManager.AddGameMode(id, typeof(TGameMode));
        }

        private static void AddGameMode(string id, Type type)
        {
            var go = new GameObject($"[GameMode] {id}");
            go.SetActive(false);
            go.transform.SetParent(GameObject.Find("/Game/Code/Game Modes").transform);
            go.AddComponent(type);
        }
    }
}
