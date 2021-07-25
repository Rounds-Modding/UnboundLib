using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jotunn.Utils;
using TMPro;
using UnboundLib.GameModes;
using UnboundLib.Networking;
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
        public const string Version = "2.3.1";

        internal static readonly ModCredits modCredits = new ModCredits("UNBOUND", new string[] { "Willis (Creation, design, networking, custom cards, custom maps, and more)", "Tilastokeskus (Custom game modes, networking, structure)", "Pykess (Custom cards, menus, modded lobby syncing)", "Ascyst (Quickplay)", "Boss Sloth Inc. (Menus, UI, custom maps, modded lobby syncing)"}, "Github", "https://github.com/Rounds-Modding/UnboundLib");

        public static Unbound Instance { get; private set; }

        private Canvas _canvas;
        public Canvas canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = new GameObject("UnboundLib Canvas").AddComponent<Canvas>();
                    _canvas.gameObject.AddComponent<GraphicRaycaster>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.pixelPerfect = false;
                    DontDestroyOnLoad(_canvas);
                }
                return _canvas;
            }
        }

        struct NetworkEventType
        {
            public const string
                StartHandshake = "ModLoader_HandshakeStart",
                FinishHandshake = "ModLoader_HandshakeFinish";
        }

        internal static CardInfo templateCard;

        internal static CardInfo[] defaultCards;
        internal static List<CardInfo> activeCards = new List<CardInfo>();
        internal static List<CardInfo> inactiveCards = new List<CardInfo>();

        internal static string[] defaultLevels;
        internal static List<string> activeLevels = new List<string>();
        internal static List<string> inactiveLevels = new List<string>();

        public delegate void OnJoinedDelegate();
        public delegate void OnLeftDelegate();
        public static event OnJoinedDelegate OnJoinedRoom;
        public static event OnLeftDelegate OnLeftRoom;

        internal static List<string> loadedGUIDs = new List<string>();
        internal static List<string> loadedMods = new List<string>();
        internal static List<string> loadedVersions = new List<string>();

        internal static List<Action> handShakeActions = new List<Action>();

        internal static AssetBundle UIAssets;
        private static GameObject modalPrefab;

        public Unbound()
        {
            // Add UNBOUND text to the main menu screen
            TextMeshProUGUI text = null;
            bool firstTime = true;
            bool canCreate;

            On.MainMenuHandler.Awake += (orig, self) =>
            {
                // reapply cards and levels
                this.ExecuteAfterSeconds(0.1f, () =>
                {
                    activeLevels.AddRange(inactiveLevels);
                    inactiveLevels.Clear();
                    MapManager.instance.levels = activeLevels.ToArray();
                    CardChoice.instance.cards = activeCards.ToArray();
                });


                // create unbound text
                canCreate = true;
                this.ExecuteAfterSeconds(firstTime ? 4f : 0.1f, () =>
                {
                    if (!canCreate) return;
                    text = MenuHandler.CreateTextAt("UNBOUND", Vector2.zero);
                    text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    text.fontSize = 30;
                    text.color = (Color.yellow + Color.red) / 2;
                    text.font = ((TextMeshProUGUI) FindObjectOfType<ListMenuButton>().GetFieldValue("text")).font;
                    text.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group"), true);
                    text.transform.SetAsFirstSibling();
                    text.rectTransform.localScale = Vector3.one;
                    text.rectTransform.localPosition = new Vector3(0, 350, text.rectTransform.localPosition.z);
                });

                ModOptions.Instance.CreateModOptions(firstTime);
                Utils.UI.Credits.Instance.CreateCreditsMenu(firstTime);

                firstTime = false;

                orig(self);
            };

            On.MainMenuHandler.Close += (orig, self) =>
            {
                canCreate = false;
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


            // apply cards on game start
            IEnumerator ResetCardsOnStart(IGameModeHandler gm)
            {
                CardChoice.instance.cards = activeCards.ToArray();
                yield break;
            }
            GameModeManager.AddHook(GameModeHooks.HookInitStart, ResetCardsOnStart);
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
            // store default cards
            defaultCards = (CardInfo[]) CardChoice.instance.cards.Clone();

            // request mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.StartHandshake, (data) =>
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

                CardChoice.instance.cards = defaultCards;
            });

            // receive mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.FinishHandshake, (data) =>
            {
                // attempt to syncronize levels and cards with other players
                CardChoice.instance.cards = activeCards.ToArray();
                NetworkingManager.RPC(typeof(Unbound), nameof(RPC_MapHandshake), (object)activeLevels.ToArray());

                if (data.Length > 0)
                {
                    GameModeManager.SetGameMode((string) data[0], false);
                    GameModeManager.CurrentHandler.SetSettings((GameSettings) data[1]);
                }
            });

            // fetch card to use as a template for all custom cards
            templateCard = (from c in CardChoice.instance.cards
                            where c.cardName.ToLower() == "huge"
                            select c).FirstOrDefault();
            defaultCards = CardChoice.instance.cards;
            activeCards.AddRange(defaultCards);

            // register default cards with toggle menu
            foreach (var card in defaultCards)
            {
                CardToggleMenuHandler.Instance.AddCardToggle(card, false);
            }

            // add default activeLevels to level list
            defaultLevels = MapManager.instance.levels;
            activeLevels.AddRange(MapManager.instance.levels);

            // hook up Photon callbacks
            var networkEvents = gameObject.AddComponent<NetworkEventCallbacks>();
            networkEvents.OnJoinedRoomEvent += OnJoinedRoomAction;
            networkEvents.OnLeftRoomEvent += OnLeftRoomAction;

            // sync modded clients
            networkEvents.OnJoinedRoomEvent += SyncModClients.RequestSync;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !ModOptions.noDeprecatedMods)
            {
                ModOptions.showModUi = !ModOptions.showModUi;
            }

            GameManager.lockInput = ModOptions.showModUi || DevConsole.isTyping;
        }

        private void OnGUI()
        {
            if (!ModOptions.showModUi) return;

            GUILayout.BeginVertical();

            bool showingSpecificMod = false;
            foreach (var md in ModOptions.GUIListeners.Keys)
            {
                var data = ModOptions.GUIListeners[md];
                if (data.guiEnabled)
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
            }

            if (showingSpecificMod) return;

            GUILayout.Label("UnboundLib Options\nThis menu is deprecated");
            if (GUILayout.Button("Toggle Cards"))
            {
                CardToggleMenuHandler.Instance.Show();
            }

            GUILayout.Label("Mod Options:");
            foreach (var md in ModOptions.GUIListeners.Keys)
            {
                var data = ModOptions.GUIListeners[md];
                if (GUILayout.Button(data.modName))
                {
                    data.guiEnabled = true;
                }
            }
            GUILayout.EndVertical();
        }

        private void LoadAssets()
        {
            UIAssets = AssetUtils.LoadAssetBundleFromResources("unboundui", typeof(Unbound).Assembly);
            if (UIAssets != null)
            {
                modalPrefab = UIAssets.LoadAsset<GameObject>("Modal");
                Instantiate(UIAssets.LoadAsset<GameObject>("Card Toggle Menu"), canvas.transform).AddComponent<CardToggleMenuHandler>();
            }
        }

        private void OnJoinedRoomAction()
        {
            if (!PhotonNetwork.OfflineMode)
                CardChoice.instance.cards = defaultCards;
            NetworkingManager.RaiseEventOthers(NetworkEventType.StartHandshake);

            // send available card pool to the master client
            if (!PhotonNetwork.IsMasterClient)
            {
                var cardSelection = new List<CardInfo>();
                cardSelection.AddRange(activeCards);
                cardSelection.AddRange(inactiveCards);
                NetworkingManager.RPC_Others(typeof(Unbound), nameof(RPC_CardHandshake), (object)cardSelection.Select(c => c.cardName).ToArray());
            }


            OnJoinedRoom?.Invoke();
            foreach (var handshake in handShakeActions)
            {
                handshake?.Invoke();
            }
        }
        private void OnLeftRoomAction()
        {
            OnLeftRoom?.Invoke();
        }
        
        [UnboundRPC]
        private static void RPC_CardHandshake(string[] cards)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            // disable any cards which aren't shared by other players
            foreach (var c in CardToggleHandler.toggles)
            {
                c.SetValue(cards.Contains(c.info.cardName) && c.isEnabled.Value);
            }

            // reply to all users with new list of valid cards
            NetworkingManager.RPC_Others(typeof(Unbound), nameof(RPC_HostCardHandshakeResponse), (object)activeCards.Select(c => c.cardName).ToArray());
        }

        [UnboundRPC]
        private static void RPC_HostCardHandshakeResponse(string[] cards)
        {
            // enable only cards that the host has specified are allowed
            foreach (var c in CardToggleHandler.toggles)
            {
                c.SetValue(cards.Contains(c.info.cardName));
            }
        }

        [UnboundRPC]
        private static void RPC_MapHandshake(string[] maps)
        {
            var difference = maps.Except(activeLevels).ToArray();

            inactiveLevels.AddRange(difference);

            foreach (var c in difference)
            {
                activeLevels.Remove(c);
            }

            MapManager.instance.levels = activeLevels.ToArray();
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
        public static void RegisterCredits(string modName, string[] contributors = null)
        {

        }

        public static void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null)
        {
            ModOptions.Instance.RegisterMenu(name, buttonAction, guiAction, parent);
        }

        public static void RegisterGUI(string modName, Action guiAction)
        {
            ModOptions.RegisterGUI(modName, guiAction);
        }

        public static void RegisterCredits(string modName, string[] credits = null, string linkText = "", string linkURL = "")
        {
            Utils.UI.Credits.Instance.RegisterModCredits(new ModCredits(modName, credits, linkText, linkURL));
        }

        public static void RegisterClientSideMod(string GUID)
        {
            SyncModClients.RegisterClientSideMod(GUID);
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

        public static void RegisterMaps(AssetBundle assetBundle)
        {
            RegisterMaps(assetBundle.GetAllScenePaths());
        }

        public static void RegisterMaps(IEnumerable<string> paths)
        {
            activeLevels.AddRange(paths);
            activeLevels = activeLevels.Distinct().ToList();
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
                    currentMatches -= (float)Mathf.Abs(formattedMessage.Length - text.Length) * 0.001f;
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
                BuildModal()
                    .Title("Error Loading Level")
                    .Message($"No map found named:\n{message}\n\nError:\n{e.ToString()}")
                    .CancelButton("Copy", () =>
                    {
                        BuildInfoPopup("Copied Message!");
                        GUIUtility.systemCopyBuffer = e.ToString();
                    })
                    .CancelButton("Cancel", () => { })
                    .Show();
            }
        }
    }
}
