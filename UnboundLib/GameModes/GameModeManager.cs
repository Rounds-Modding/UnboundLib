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
using Object = UnityEngine.Object;
using System.Reflection;
using System.Diagnostics;

namespace UnboundLib.GameModes
{
    public static class GameModeManager
    {
        // both of these identify existing gameobjects by name and therefore cannot be changed
        public const string SandBoxID = "Sandbox";
        public const string ArmsRaceID = "Arms race";

        private static Dictionary<string, IGameModeHandler> handlers = new Dictionary<string, IGameModeHandler>();
        private static Dictionary<string, Type> gameModes = new Dictionary<string, Type>();
        private static Dictionary<string, List<GameModeHooks.HookRegistration>> hooks = new Dictionary<string, List<GameModeHooks.HookRegistration>>();
        private static Dictionary<string, List<GameModeHooks.HookRegistration>> onceHooks = new Dictionary<string, List<GameModeHooks.HookRegistration>>();

        // public properties that return deep copies of the handlers and gameModes dictionarys (the values in the dictionaries returned are shallow copies)
        public static ReadOnlyDictionary<string, IGameModeHandler> Handlers => new ReadOnlyDictionary<string, IGameModeHandler>(handlers.ToDictionary(kv => kv.Key, kv => kv.Value));
        public static ReadOnlyDictionary<string, Type> GameModes => new ReadOnlyDictionary<string, Type>(gameModes.ToDictionary(kv => kv.Key, kv => kv.Value));

        public static Action<IGameModeHandler> OnGameModeChanged { get; set; }

        public static string CurrentHandlerID { get; private set; }

        public static IGameModeHandler CurrentHandler => CurrentHandlerID == null ? null : handlers[CurrentHandlerID];

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
                if (scene.name != "Main") return;

                // rename test gamemode object to SandboxID
                GetGameMode<GM_Test>("Test").name = $"[GameMode] {SandBoxID}";

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
            };
        }

        internal static void SetupUI()
        {
            // have to do this every time the scene is reloaded

            // Make existing UI buttons use our GameModeHandler logic
            // as well as make the Local menu a scrollview
            var origGameModeGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/GameMode");
            if (origGameModeGo != null)
            {
                var origGroupGo = origGameModeGo.transform.Find("Group");
                var characterSelectGo_ = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect");

                var newLocalMenu = Utils.UI.MenuHandler.CreateMenu("LOCAL",
                    () => { MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group/Local").GetComponent<Button>().onClick.Invoke(); },
                    MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main").gameObject,
                    60, true, false, null, true, 1);

                var content = newLocalMenu.transform.Find("Group/Grid/Scroll View/Viewport/Content");

                content.GetComponent<VerticalLayoutGroup>().spacing = 60;

                Object.DestroyImmediate(origGameModeGo.gameObject);

                // do not destroy local button since RWF relies on it
                MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group/Local").gameObject.SetActive(false);

                var newVersusGo = Utils.UI.MenuHandler.CreateButton("VERSUS", newLocalMenu, () => { characterSelectGo_.GetComponent<ListMenuPage>().Open(); SetGameMode(ArmsRaceID); });
                newVersusGo.name = "Versus";
                var newSandboxGo = Utils.UI.MenuHandler.CreateButton("SANDBOX", newLocalMenu, () => { MainMenuHandler.instance.Close(); SetGameMode(SandBoxID); CurrentHandler.StartGame(); });
                newSandboxGo.name = "Test";

                // Select the local button so selection doesn't look weird
                Unbound.Instance.ExecuteAfterFrames(15, () =>
                {
                    GameObject.Find("Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/LOCAL").GetComponent<ListMenuButton>().OnPointerEnter(null);
                });

                // finally, restore the main menu button order
                Unbound.Instance.ExecuteAfterFrames(5, () =>
                {
                    Transform group = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group");
                    group.Find("LOCAL")?.SetAsLastSibling();
                    group.Find("Local")?.SetAsLastSibling();
                    group.Find("Online")?.SetAsLastSibling();
                    group.Find("Options")?.SetAsLastSibling();
                    group.Find("MODS")?.SetAsLastSibling();
                    group.Find("CREDITS")?.SetAsLastSibling();
                    group.Find("Quit")?.SetAsLastSibling();
                });
            }
            var gameModeGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/LOCAL");
            var onlineGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Online/Group");
            var contentGo = gameModeGo.transform.Find("Group/Grid/Scroll View/Viewport/Content");

            // fix spacing
            contentGo.GetComponent<VerticalLayoutGroup>().spacing = 0f;

            var versusGo = contentGo.Find("Versus").gameObject;
            var sandboxGo = contentGo.Find("Test").gameObject;
            var characterSelectGo = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/CharacterSelect");
            var qMatchGo = onlineGo.transform.Find("Quick")?.gameObject;
            var tMatchGo = onlineGo.transform.Find("Twitch")?.gameObject;

            if (qMatchGo != null) { Object.DestroyImmediate(qMatchGo); }
            if (tMatchGo != null) { Object.DestroyImmediate(tMatchGo); }

            // restore GoBack target
            characterSelectGo.transform.GetChild(0).GetComponent<GoBack>().target = gameModeGo.GetComponent<ListMenuPage>();

            // destroy all other buttons in this menu
            List<GameObject> objsToDestroy = new List<GameObject>() { };
            for (int i = 0; i < contentGo.childCount; i++)
            {
                if (contentGo.GetChild(i)?.gameObject != null && contentGo.GetChild(i).gameObject.name != "Back" && contentGo.GetChild(i).gameObject.name != "Versus" && contentGo.GetChild(i).gameObject.name != "Test")
                {
                    objsToDestroy.Add(contentGo.GetChild(i).gameObject);
                }
            }
            for (int i = 0; i < objsToDestroy.Count(); i++)
            {
                Object.DestroyImmediate(objsToDestroy[i]);
            }

            var characterSelectPage = characterSelectGo.GetComponent<ListMenuPage>();

            // create gamemode buttons alphabetically (creating them in reverse order)
            // non-team gamemodes at the top, team gamemodes at the bottom
            foreach (var id in handlers.Keys.Where(k => !handlers[k].OnlineOnly && handlers[k].AllowTeams).OrderByDescending(k => handlers[k].Name.ToLower()).Where(k => k != SandBoxID && k != ArmsRaceID))
            {
                // create a copy of the string to give to the anonymous function
                string id_ = string.Copy(id);
                var gamemodeButtonGo = Utils.UI.MenuHandler.CreateButton(handlers[id].Name.ToUpper(), gameModeGo.gameObject, () => { characterSelectGo.GetComponent<ListMenuPage>().Open(); SetGameMode(id_); });
                gamemodeButtonGo.name = id;
                gamemodeButtonGo.transform.SetAsFirstSibling();
            }

            // add a small separator
            Utils.UI.MenuHandler.CreateText(" ", gameModeGo, out TextMeshProUGUI _, 30).transform.SetAsFirstSibling();

            foreach (var id in handlers.Keys.Where(k => !handlers[k].OnlineOnly && !handlers[k].AllowTeams).OrderByDescending(k => handlers[k].Name.ToLower()).Where(k => k != SandBoxID && k != ArmsRaceID))
            {
                // create a copy of the string to give to the anonymous function
                string id_ = string.Copy(id);
                var gamemodeButtonGo = Utils.UI.MenuHandler.CreateButton(handlers[id].Name.ToUpper(), gameModeGo.gameObject, () => { characterSelectGo.GetComponent<ListMenuPage>().Open(); SetGameMode(id_); });
                gamemodeButtonGo.name = id;
                gamemodeButtonGo.transform.SetAsFirstSibling();
            }

            // add a small separator
            Utils.UI.MenuHandler.CreateText(" ", gameModeGo, out TextMeshProUGUI _, 30).transform.SetAsFirstSibling();

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

            GameModeManager.hooks.TryGetValue(key, out List<GameModeHooks.HookRegistration> hooks);
            GameModeManager.onceHooks.TryGetValue(key, out List<GameModeHooks.HookRegistration> onceHooks);

            if (hooks != null && CurrentHandler != null)
            {
                foreach (var hook in hooks.OrderByDescending(h => h.Hook.Priority))
                {
                    yield return ErrorTolerantHook(key, hook);
                }
            }

            if (onceHooks == null || CurrentHandler == null) yield break;
            foreach (var hook in onceHooks)
            {
                RemoveHook(key, hook.Hook);
            }

            GameModeManager.onceHooks.Remove(key);
        }

        private static IEnumerator ErrorTolerantHook(string key, GameModeHooks.HookRegistration hookReg)
        {
            var current = hookReg.Hook.Action(CurrentHandler);
            while (true)
            {
                object ret;
                try
                {
                    if (!current.MoveNext())
                    {
                        break;
                    }
                    ret = current.Current;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[{key.ToUpper()} HOOK] [{hookReg.Identifier}] threw the following exception:");
                    UnityEngine.Debug.LogException(ex);
                    UnityEngine.Debug.LogError($"Stack trace from when the above hook was added:\n{hookReg.CallStack}");
                    break;
                }
                yield return ret;
            }
        }

        /// <summary>
        ///     Adds a hook that is automatically removed after it's triggered once.
        /// </summary>
        public static void AddOnceHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            var hook = new GameModeHooks.Hook(action, GameModeHooks.Priority.Normal);
            var reg = new GameModeHooks.HookRegistration(hook, Assembly.GetCallingAssembly(), new StackTrace());
            AddOnceHook(key, reg);
        }
        /// <summary>
        ///     Adds a hook that is automatically removed after it's triggered once.
        /// </summary>
        public static void AddOnceHook(string key, Func<IGameModeHandler, IEnumerator> action, int priority)
        {
            var hook = new GameModeHooks.Hook(action, priority);
            var reg = new GameModeHooks.HookRegistration(hook, Assembly.GetCallingAssembly(), new StackTrace());
            AddOnceHook(key, reg);
        }

        /// <summary>
        ///     Adds a hook that is automatically removed after it's triggered once.
        /// </summary>
        public static void AddOnceHook(string key, GameModeHooks.Hook hook)
        {
            var reg = new GameModeHooks.HookRegistration(hook, Assembly.GetCallingAssembly(), new StackTrace());
            AddOnceHook(key, reg);
        }

        private static void AddOnceHook(string key, GameModeHooks.HookRegistration hookReg)
        {
            if (hookReg?.Hook == null)
            {
                return;
            }

            // Case-insensitive keys for QoL
            key = key.ToLower();

            if (!onceHooks.ContainsKey(key))
            {
                onceHooks.Add(key, new List<GameModeHooks.HookRegistration> { hookReg });
            }
            else
            {
                onceHooks[key].Add(hookReg);
            }

            AddHook(key, hookReg);
        }

        public static void AddHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            var hook = new GameModeHooks.Hook(action, GameModeHooks.Priority.Normal);
            var reg = new GameModeHooks.HookRegistration(hook, Assembly.GetCallingAssembly(), new StackTrace());
            AddHook(key, reg);
        }

        public static void AddHook(string key, Func<IGameModeHandler, IEnumerator> action, int priority)
        {
            var hook = new GameModeHooks.Hook(action, priority);
            var reg = new GameModeHooks.HookRegistration(hook, Assembly.GetCallingAssembly(), new StackTrace());
            AddHook(key, reg);
        }

        public static void AddHook(string key, GameModeHooks.Hook hook)
        {
            var reg = new GameModeHooks.HookRegistration(hook, Assembly.GetCallingAssembly(), new StackTrace());
            AddHook(key, reg);
        }

        private static void AddHook(string key, GameModeHooks.HookRegistration hookReg)
        {
            if (hookReg?.Hook == null)
            {
                return;
            }
            // Case-insensitive keys for QoL
            key = key.ToLower();
            if (!hooks.ContainsKey(key))
            {
                hooks.Add(key, new List<GameModeHooks.HookRegistration> { hookReg });
            }
            else
            {
                hooks[key].Add(hookReg);
            }
        }

        public static void RemoveHook(string key, Func<IGameModeHandler, IEnumerator> action)
        {
            RemoveHook(key, hooks[key.ToLower()].Where(h => h.Hook.Action == action).FirstOrDefault());
        }

        public static void RemoveHook(string key, GameModeHooks.Hook hook)
        {
            RemoveHook(key, hooks[key.ToLower()].FirstOrDefault(h => h.Hook == hook));
        }

        private static void RemoveHook(string key, GameModeHooks.HookRegistration hookReg)
        {
            hooks[key.ToLower()].Remove(hookReg);
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
                    CurrentHandler?.SetActive(true);
                }

                var pmPlayerDied = (Action<Player, int>) pm.GetFieldValue("PlayerDiedAction");
                pm.SetPropertyValue("PlayerJoinedAction", Delegate.Combine(pm.PlayerJoinedAction, new Action<Player>(CurrentHandler.PlayerJoined)));
                pm.SetFieldValue("PlayerDiedAction", Delegate.Combine(pmPlayerDied, new Action<Player, int>(CurrentHandler.PlayerDied)));

                OnGameModeChanged?.Invoke(CurrentHandler);
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
            if (gameMode != null) { Object.Destroy(gameMode); }
        }
    }
}
