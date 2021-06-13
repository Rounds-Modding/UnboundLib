using System;
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

        public static void TriggerHook(string hook)
        {
            GameModeManager.CurrentHandler?.TriggerHook(hook);
        }

        public static void AddHook(string key, Action<IGameModeHandler> action)
        {
            key = key.ToLower();
            foreach (var handler in GameModeManager.handlers.Values)
            {
                handler.AddHook(key, action);
            }
        }

        public static void RemoveHook(string key, Action<IGameModeHandler> action)
        {
            key = key.ToLower();
            foreach (var handler in GameModeManager.handlers.Values)
            {
                handler.RemoveHook(key, action);
            }
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
            go.transform.SetParent(GameObject.Find("/Game/Code/Game Modes").transform);
            go.AddComponent(type);
        }
    }
}
