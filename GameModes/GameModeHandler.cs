using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UnboundLib.GameModes
{
    // When possible, use these keys when adding game hooks to a game mode
    public static class GameModeHooks
    {
        /// <summary>
        ///     Should be called before the game mode does any initialization.
        /// </summary>
        public const string HookInitStart = "InitStart";

        /// <summary>
        ///     Should be called after the game mode has done its initialization.
        /// </summary>
        public const string HookInitEnd = "InitEnd";

        /// <summary>
        ///     Should be called when the game begins for the first time or after a rematch.
        /// </summary>
        public const string HookGameStart = "GameStart";

        /// <summary>
        ///     Should be called after the last round of the game ends. A rematch can be issued after this hook.
        /// </summary>
        public const string HookGameEnd = "GameEnd";

        /// <summary>
        ///     Should be called right after a round begins, after players have picked new cards.
        /// </summary>
        public const string HookRoundStart = "RoundStart";

        /// <summary>
        ///     Should be called right after a round ends, before players can pick new cards.
        /// </summary>
        public const string HookRoundEnd = "RoundEnd";

        /// <summary>
        ///     Should be called right after a player or team has received a point and all players have been revived for the next battle.
        ///     Should be called after RoundStart hook when applicable.
        /// </summary>
        public const string HookPointStart = "PointStart";

        /// <summary>
        ///     Should be called right after a player or team has received a point.
        ///     Should be called before RoundEnd hook when applicable.
        /// </summary>
        public const string HookPointEnd = "PointEnd";

        /// <summary>
        ///     Should be called when all players are vulnerable and can start fighting.
        /// </summary>
        public const string HookBattleStart = "BattleStart";

        /// <summary>
        ///     Should be called when players or teams are presented with new cards.
        /// </summary>
        public const string HookPickStart = "PickStart";

        /// <summary>
        ///     Should be called after all players or teams have picked new cards, before the next round begins.
        /// </summary>
        public const string HookPickEnd = "PickEnd";

        /// <summary>
        ///     Should be called each time a player or team is presented with new cards.
        /// </summary>
        public const string HookPlayerPickStart = "PlayerPickStart";

        /// <summary>
        ///     Should be called each time a player or team has chosen a new card.
        /// </summary>
        public const string HookPlayerPickEnd = "PlayerPickEnd";
    }

    public static class GameModeHandler
    {
        private static Dictionary<string, IGameModeHandler> handlers = new Dictionary<string, IGameModeHandler>();
        private static Dictionary<string, Type> gameModes = new Dictionary<string, Type>();

        /// <summary>
        ///     ID of the currently selected GameModeHandler.
        /// </summary>
        public static string CurrentHandler { get; private set; }

        internal static void Init()
        {
            // Add preset game modes
            GameModeHandler.handlers.Add("ArmsRace", new ArmsRaceHandler("Arms race"));
            GameModeHandler.gameModes.Add("ArmsRace", typeof(GM_ArmsRace));
            GameModeHandler.handlers.Add("Sandbox", new SandboxHandler("Test"));
            GameModeHandler.gameModes.Add("Sandbox", typeof(GM_Test));

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
                    versusButton.onClick.AddListener(() => GameModeHandler.SetGameMode("ArmsRace"));

                    GameObject.DestroyImmediate(sandboxGo.GetComponent<Button>());
                    var sandboxButton = sandboxGo.AddComponent<Button>();
                    sandboxButton.onClick.AddListener(MainMenuHandler.instance.Close);
                    sandboxButton.onClick.AddListener(() => {
                        GameModeHandler.SetGameMode("Sandbox");
                        GameModeHandler.handlers[GameModeHandler.CurrentHandler].StartGame();
                    });

                    // Add game modes back when the main scene is reloaded
                    foreach (var id in GameModeHandler.handlers.Keys)
                    {
                        if (id != "ArmsRace" && id != "Sandbox")
                        {
                            AddGameMode(id, GameModeHandler.gameModes[id]);
                        }

                        GameModeHandler.handlers[id].SetActive(false);
                    }

                    if (GameModeHandler.CurrentHandler != null)
                    {
                        GameModeHandler.handlers[GameModeHandler.CurrentHandler].SetActive(true);
                    }
                }
            };
        }

        public static void TriggerHook(string handler, string hook)
        {
            GameModeHandler.handlers[handler].TriggerHook(hook);
        }

        public static void AddHook(string key, Action<IGameModeHandler> action)
        {
            key = key.ToLower();
            foreach (var handler in GameModeHandler.handlers.Values)
            {
                handler.AddHook(key, action);
            }
        }

        public static void RemoveHook(string key, Action<IGameModeHandler> action)
        {
            key = key.ToLower();
            foreach (var handler in GameModeHandler.handlers.Values)
            {
                handler.RemoveHook(key, action);
            }
        }

        public static T GetGameMode<T>(string id) where T : Component
        {
            return GameObject.Find("/Game/Code/Game Modes").transform.Find($"[GameMode] {id}").GetComponent<T>();
        }

        public static void SetGameMode(string id)
        {
            if (!GameModeHandler.handlers.ContainsKey(id))
            {
                throw new ArgumentException($"No such game mode handler: {id}");
            }

            if (GameModeHandler.CurrentHandler == id)
            {
                return;
            }

            if (GameModeHandler.CurrentHandler != null)
            {
                GameModeHandler.handlers[GameModeHandler.CurrentHandler].SetActive(false);
            }

            GameModeHandler.CurrentHandler = id;
            GameModeHandler.handlers[id].SetActive(true);
        }

        public static IGameModeHandler GetHandler(string id)
        {
            return GameModeHandler.handlers[id];
        }

        public static void AddGameModeHandler<TGameMode>(string id, IGameModeHandler handler) where TGameMode : MonoBehaviour
        {
            GameModeHandler.handlers.Add(id, handler);
            GameModeHandler.gameModes.Add(id, typeof(TGameMode));
            GameModeHandler.AddGameMode(id, typeof(TGameMode));
        }

        private static void AddGameMode(string id, Type type)
        {
            var go = new GameObject($"[GameMode] {id}");
            go.transform.SetParent(GameObject.Find("/Game/Code/Game Modes").transform);
            go.AddComponent(type);
        }
    }

    public abstract class GameModeHandler<T> : IGameModeHandler where T : Component
    {
        public void AddHook(string key, Action<IGameModeHandler> action)
        {
            // Case-insensitive keys for QoL
            key = key.ToLower();

            if (!this.hooks.ContainsKey(key))
            {
                this.hooks.Add(key, action);
            }
            else
            {
                this.hooks[key] += action;
            }
        }

        public void RemoveHook(string key, Action<IGameModeHandler> action)
        {
            this.hooks[key.ToLower()] -= action;
        }

        public void TriggerHook(string key)
        {
            Action<IGameModeHandler> hook;
            this.hooks.TryGetValue(key.ToLower(), out hook);

            if (hook != null)
            {
                hook(this);
            }
        }

        public T GameMode {
            get
            {
                return GameModeHandler.GetGameMode<T>(this.id);
            }
        }

        public abstract GameSettings Settings { get; protected set; }
        public abstract string Name { get; }

        private string id;
        private Dictionary<string, Action<IGameModeHandler>> hooks = new Dictionary<string, Action<IGameModeHandler>>();

        protected GameModeHandler(string id)
        {
            this.id = id;
        }

        public virtual void SetSettings(GameSettings settings)
        {
            this.Settings = settings;
        }

        public virtual void ChangeSetting(string name, object value)
        {
            var newSettings = new GameSettings();

            foreach (var entry in this.Settings)
            {
                newSettings.Add(entry.Key, entry.Key == name ? value : entry.Value);
            }

            this.Settings = newSettings;
        }

        public abstract void SetActive(bool active);

        public abstract void StartGame();
    }
}
