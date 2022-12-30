using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Utils;
using TMPro;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnboundLib
{
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class Unbound : BaseUnityPlugin
    {
        private const string ModId = "com.willis.rounds.unbound";
        private const string ModName = "Rounds Unbound";
        public const string Version = "3.2.4";

        public static Unbound Instance { get; private set; }
        public static readonly ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "UnboundLib.cfg"), true);

        private Canvas _canvas;
        public Canvas canvas
        {
            get
            {
                if (_canvas != null) return _canvas;
                _canvas = new GameObject("UnboundLib Canvas").AddComponent<Canvas>();
                _canvas.gameObject.AddComponent<GraphicRaycaster>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.pixelPerfect = false;
                DontDestroyOnLoad(_canvas);
                return _canvas;
            }
        }

        private struct NetworkEventType
        {
            public const string
                StartHandshake = "ModLoader_HandshakeStart",
                FinishHandshake = "ModLoader_HandshakeFinish";
        }

        internal static CardInfo templateCard;

        [Obsolete("This should not be used anymore instead use CardManager.defaultCards")]
        internal static CardInfo[] defaultCards => CardManager.defaultCards;
        [Obsolete("This should not be used anymore instead use CardManager.activeCards")]
        internal static List<CardInfo> activeCards => CardManager.activeCards.ToList();
        [Obsolete("This should not be used anymore instead use CardManager.inactiveCards")]
        internal static List<CardInfo> inactiveCards => CardManager.inactiveCards;

        public delegate void OnJoinedDelegate();
        public delegate void OnLeftDelegate();
        public static event OnJoinedDelegate OnJoinedRoom;
        public static event OnLeftDelegate OnLeftRoom;

        internal static List<string> loadedGUIDs = new List<string>();
        internal static List<string> loadedMods = new List<string>();
        internal static List<string> loadedVersions = new List<string>();

        internal static List<Action> handShakeActions = new List<Action>();

        public static readonly Dictionary<string, bool> lockInputBools = new Dictionary<string, bool>();

        internal static AssetBundle UIAssets;
        public static AssetBundle toggleUI;
        internal static AssetBundle linkAssets;
        private static GameObject modalPrefab;

        private TextMeshProUGUI text;

        public Unbound()
        {
            // Add UNBOUND text to the main menu screen
            bool firstTime = true;

            On.MainMenuHandler.Awake += (orig, self) =>
            {
                // reapply cards and levels
                this.ExecuteAfterFrames(5, () =>
                {
                    MapManager.instance.levels = LevelManager.activeLevels.ToArray();
                    CardManager.RestoreCardToggles();
                    ToggleCardsMenuHandler.RestoreCardToggleVisuals();

                });

                // create unbound text
                StartCoroutine(AddTextWhenReady(firstTime ? 2f : 0.1f));

                ModOptions.instance.CreateModOptions(firstTime);
                Credits.Instance.CreateCreditsMenu(firstTime);
                MainMenuLinks.AddLinks(firstTime);

                var time = firstTime;
                this.ExecuteAfterSeconds(firstTime ? 0.4f : 0, () =>
                {
                    var resumeButton = UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group/Resume").gameObject;
                    // Create options button in escapeMenu
                    var optionsMenu = Instantiate(MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Options").gameObject, UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main"));
                    var menuBut = optionsMenu.transform.Find("Group/Back").GetComponent<Button>();
                    menuBut.onClick = new Button.ButtonClickedEvent();
                    menuBut.onClick.AddListener(() =>
                    {
                        optionsMenu.transform.Find("Group").gameObject.SetActive(false);
                        UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject.SetActive(true);
                    });

                    var optionsButton = Instantiate(resumeButton, UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group"));
                    optionsButton.transform.SetSiblingIndex(2);
                    optionsButton.GetComponentInChildren<TextMeshProUGUI>().text = "OPTIONS";
                    optionsButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    optionsButton.GetComponent<Button>().onClick.AddListener((() =>
                    {
                        optionsMenu.transform.Find("Group").gameObject.SetActive(true);
                        UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject.SetActive(false);
                    }));

                    if (time)
                    {
                        CardManager.FirstTimeStart();
                    }
                });

                firstTime = false;

                orig(self);
            };

            On.MainMenuHandler.Close += (orig, self) =>
            {
                if (text != null) Destroy(text.gameObject);

                orig(self);
            };

            IEnumerator ArmsRaceStartCoroutine(On.GM_ArmsRace.orig_Start orig, GM_ArmsRace self)
            {
                yield return GameModeManager.TriggerHook(GameModeHooks.HookInitStart);
                orig(self);
                yield return GameModeManager.TriggerHook(GameModeHooks.HookInitEnd);
            }

            On.GM_ArmsRace.Start += (orig, self) =>
            {
                self.StartCoroutine(ArmsRaceStartCoroutine(orig, self));
            };

            GameModeManager.AddHook(GameModeHooks.HookGameStart, handler => SyncModClients.DisableSyncModUi(SyncModClients.uiParent));

            // hook for closing ongoing lobbies
            GameModeManager.AddHook(GameModeHooks.HookGameStart, CloseLobby);

            // Load toggleUI asset bundle
            toggleUI = AssetUtils.LoadAssetBundleFromResources("togglemenuui", typeof(ToggleLevelMenuHandler).Assembly);

            // Load toggleUI asset bundle
            linkAssets = AssetUtils.LoadAssetBundleFromResources("unboundlinks", typeof(Unbound).Assembly);

            // Add managers
            gameObject.AddComponent<LevelManager>();
            gameObject.AddComponent<CardManager>();

            // Add menu handlers
            gameObject.AddComponent<ToggleLevelMenuHandler>();
            gameObject.AddComponent<ToggleCardsMenuHandler>();

            On.CardChoice.Start += (orig, self) =>
            {
                for (int i = 0; i < self.cards.Length; i++)
                {
                    if (!((DefaultPool) PhotonNetwork.PrefabPool).ResourceCache.ContainsKey(self.cards[i].gameObject.name))
                        PhotonNetwork.PrefabPool.RegisterPrefab(self.cards[i].gameObject.name, self.cards[i].gameObject);
                }
                var children = new Transform[self.transform.childCount];
                for (int j = 0; j < children.Length; j++)
                {
                    children[j] = self.transform.GetChild(j);
                }
                self.SetFieldValue("children", children);
                self.cards = CardManager.activeCards.ToArray();
            };
        }

        private static IEnumerator CloseLobby(IGameModeHandler gm)
        {
            if (!PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode) yield break;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            yield break;
        }

        private IEnumerator AddTextWhenReady(float delay = 0f, float maxTimeToWait = 10f)
        {
            if (delay > 0f) { yield return new WaitForSecondsRealtime(delay); }

            float time = maxTimeToWait;
            while (time > 0f && MainMenuHandler.instance?.transform?.Find("Canvas/ListSelector/Main/Group") == null)
            {
                time -= Time.deltaTime;
                yield return null;
            }
            if (MainMenuHandler.instance?.transform?.Find("Canvas/ListSelector/Main/Group") == null)
            {
                yield break;
            }
            text = MenuHandler.CreateTextAt("UNBOUND", Vector2.zero);
            text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            text.fontSize = 30;
            text.color = (Color.yellow + Color.red) / 2;
            text.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group"), true);
            text.transform.SetAsFirstSibling();
            text.rectTransform.localScale = Vector3.one;
            text.rectTransform.localPosition = new Vector3(0, 350, text.rectTransform.localPosition.z);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            // Patch game with Harmony
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            LoadAssets();
            GameModeManager.Init();
        }

        private void Start()
        {
            // request mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.StartHandshake, data =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    NetworkingManager.RaiseEvent(NetworkEventType.FinishHandshake,
                        GameModeManager.CurrentHandlerID,
                        GameModeManager.CurrentHandler?.Settings);
                }
                else
                {
                    NetworkingManager.RaiseEvent(NetworkEventType.FinishHandshake);
                }
            });

            // receive mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.FinishHandshake, data =>
            {
                // attempt to syncronize levels and cards with other players
                MapManager.instance.levels = LevelManager.activeLevels.ToArray();

                if (data.Length <= 0) return;
                GameModeManager.SetGameMode((string) data[0], false);
                GameModeManager.CurrentHandler.SetSettings((GameSettings) data[1]);
            });

            // fetch card to use as a template for all custom cards
            CardInfo huge = CardChoice.instance.cards.FirstOrDefault(c => c.cardName.ToLower() == "huge");
            templateCard = Instantiate(huge.gameObject, Vector3.up * 100f, Quaternion.identity)
                .GetComponent<CardInfo>();
            templateCard.cardBase = huge.cardBase;

            templateCard.gameObject.name = "__UNBOUND_TEMPLATE_CARD__";
            DestroyImmediate(templateCard.transform.GetChild(0).gameObject);
            templateCard.GetComponent<CharacterStatModifiers>().health = 1f; // remove huge's stats
            DontDestroyOnLoad(templateCard.gameObject);

            CardManager.defaultCards = CardChoice.instance.cards;

            // register default cards with toggle menu
            foreach (var card in CardManager.defaultCards)
            {
                CardManager.cards.Add(card.name,
                    new Card("Vanilla", config.Bind("Cards: Vanilla", card.name, true), card));
            }

            // hook up Photon callbacks
            var networkEvents = gameObject.AddComponent<NetworkEventCallbacks>();
            networkEvents.OnJoinedRoomEvent += OnJoinedRoomAction;
            networkEvents.OnJoinedRoomEvent += LevelManager.OnJoinedRoomAction;
            networkEvents.OnJoinedRoomEvent += CardManager.OnJoinedRoomAction;
            networkEvents.OnLeftRoomEvent += OnLeftRoomAction;
            networkEvents.OnLeftRoomEvent += CardManager.OnLeftRoomAction;
            networkEvents.OnLeftRoomEvent += LevelManager.OnLeftRoomAction;

            // Adds the ping monitor
            gameObject.AddComponent<PingMonitor>();

            // sync modded clients
            networkEvents.OnJoinedRoomEvent += SyncModClients.RequestSync;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !ModOptions.noDeprecatedMods)
            {
                ModOptions.showModUi = !ModOptions.showModUi;
            }

            GameManager.lockInput = ModOptions.showModUi ||
                                    DevConsole.isTyping ||
                                    ToggleLevelMenuHandler.instance.mapMenuCanvas.activeInHierarchy ||

                                    (UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group") &&
                                     UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group")
                                         .gameObject.activeInHierarchy) ||

                                    (UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group") &&
                                     UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject
                                         .activeInHierarchy) ||

                                    (
                                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/MOD OPTIONS/Group") &&
                                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/MOD OPTIONS/Group").gameObject.activeInHierarchy) ||
                                    ToggleCardsMenuHandler.menuOpenFromOutside ||
                                    lockInputBools.Values.Any(b => b);
        }

        private void OnGUI()
        {
            if (!ModOptions.showModUi) return;

            GUILayout.BeginVertical();

            bool showingSpecificMod = false;
            foreach (ModOptions.GUIListener data in ModOptions.GUIListeners.Keys.Select(md => ModOptions.GUIListeners[md]).Where(data => data.guiEnabled))
            {
                if (GUILayout.Button("<- Back"))
                {
                    data.guiEnabled = false;
                }
                GUILayout.Label(data.modName + " Options");
                showingSpecificMod = true;
                data.guiAction?.Invoke();
                break;
            }

            if (showingSpecificMod) return;

            GUILayout.Label("UnboundLib Options\nThis menu is deprecated");

            GUILayout.Label("Mod Options:");
            foreach (var data in ModOptions.GUIListeners.Values)
            {
                if (GUILayout.Button(data.modName))
                {
                    data.guiEnabled = true;
                }
            }
            GUILayout.EndVertical();
        }

        private static void LoadAssets()
        {
            UIAssets = AssetUtils.LoadAssetBundleFromResources("unboundui", typeof(Unbound).Assembly);
            if (UIAssets != null)
            {
                modalPrefab = UIAssets.LoadAsset<GameObject>("Modal");
                //Instantiate(UIAssets.LoadAsset<GameObject>("Card Toggle Menu"), canvas.transform).AddComponent<CardToggleMenuHandler>();
            }
        }

        private static void OnJoinedRoomAction()
        {
            //if (!PhotonNetwork.OfflineMode)
            //   CardChoice.instance.cards = CardManager.defaultCards;
            NetworkingManager.RaiseEventOthers(NetworkEventType.StartHandshake);

            OnJoinedRoom?.Invoke();
            foreach (var handshake in handShakeActions)
            {
                handshake?.Invoke();
            }
        }
        private static void OnLeftRoomAction()
        {
            OnLeftRoom?.Invoke();
        }

        [UnboundRPC]
        public static void BuildInfoPopup(string message)
        {
            var popup = new GameObject("Info Popup").AddComponent<InfoPopup>();
            popup.rectTransform.SetParent(Instance.canvas.transform);
            popup.Build(message);
        }

        [UnboundRPC]
        public static void BuildModal(string title, string message)
        {
            BuildModal()
                .Title(title)
                .Message(message)
                .Show();
        }
        public static ModalHandler BuildModal()
        {
            return Instantiate(modalPrefab, Instance.canvas.transform).AddComponent<ModalHandler>();
        }
        public static void RegisterCredits(string modName, string[] credits = null, string[] linkTexts = null, string[] linkURLs = null)
        {
            Credits.Instance.RegisterModCredits(new ModCredits(modName, credits, linkTexts, linkURLs));
        }

        public static void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null)
        {
            ModOptions.instance.RegisterMenu(name, buttonAction, guiAction, parent);
        }

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public static void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null, bool showInPauseMenu = false)
        {
            ModOptions.instance.RegisterMenu(name, buttonAction, guiAction, parent, showInPauseMenu);
        }

        public static void RegisterGUI(string modName, Action guiAction)
        {
            ModOptions.RegisterGUI(modName, guiAction);
        }

        public static void RegisterCredits(string modName, string[] credits = null, string linkText = "", string linkURL = "")
        {
            Credits.Instance.RegisterModCredits(new ModCredits(modName, credits, linkText, linkURL));
        }

        public static void RegisterClientSideMod(string GUID)
        {
            SyncModClients.RegisterClientSideMod(GUID);
        }
        public static void AddAllCardsCallback(Action<CardInfo[]> callback)
        {
            CardManager.AddAllCardsCallback(callback);
        }

        public static void RegisterHandshake(string modId, Action callback)
        {
            // register mod handshake network events
            NetworkingManager.RegisterEvent($"ModLoader_{modId}_StartHandshake", (e) =>
            {
                NetworkingManager.RaiseEvent($"ModLoader_{modId}_FinishHandshake");
            });
            NetworkingManager.RegisterEvent($"ModLoader_{modId}_FinishHandshake", (e) =>
            {
                callback?.Invoke();
            });
            handShakeActions.Add(() => NetworkingManager.RaiseEventOthers($"ModLoader_{modId}_StartHandshake"));
        }

        #region Remove these at a later date when mod's have updated to LevelManager
        [ObsoleteAttribute("This method is obsolete. Use LevelManager.RegisterMaps() instead.", false)]
        public static void RegisterMaps(AssetBundle assetBundle)
        {
            LevelManager.RegisterMaps(assetBundle);
        }

        [ObsoleteAttribute("This method is obsolete. Use LevelManager.RegisterMaps() instead.", false)]
        public static void RegisterMaps(IEnumerable<string> paths)
        {
            RegisterMaps(paths, "Modded");
        }

        [ObsoleteAttribute("This method is obsolete. Use LevelManager.RegisterMaps() instead.", false)]
        public static void RegisterMaps(IEnumerable<string> paths, string categoryName)
        {
            LevelManager.RegisterMaps(paths);
        }
        #endregion

        public static bool IsNotPlayingOrConnected()
        {
            return (GameManager.instance && !GameManager.instance.battleOngoing) &&
                   (PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected);
        }

        internal static readonly ModCredits modCredits = new ModCredits("UNBOUND", new[]
        {
            "Willis (Creation, design, networking, custom cards, custom maps, and more)",
            "Tilastokeskus (Custom game modes, networking, structure)",
            "Pykess (Custom cards, stability, menus, syncing, extra player colors, disconnect handling, game mode framework)",
            "Ascyst (Quickplay)", "Boss Sloth Inc. (Menus, UI, custom maps, modded lobby syncing)",
            "willuwontu (Custom cards, ping UI)",
            "otDan (UI)"
        }, "Github", "https://github.com/Rounds-Modding/UnboundLib");
    }
}