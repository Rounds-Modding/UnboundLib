﻿using BepInEx;
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
        public const string Version = "2.2.1";

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

        internal static List<ModMenu> modMenus = new List<ModMenu>();
        internal static Dictionary<string, GUIListener> GUIListeners = new Dictionary<string, GUIListener>();
        internal static List<Action> handShakeActions = new List<Action>();
        
        private static GameObject menuBase;
        private static GameObject buttonBase;
        private static GameObject textBase;
        private static GameObject toggleBase;
        private static GameObject inputFieldBase;
        private static GameObject modOptionsMenu;

        private static bool showModUi = false;
        private static bool noDeprecatedMods = false;

        internal static AssetBundle UIAssets;
        private static GameObject modalPrefab;

        public Unbound()
        {
            // Add UNBOUND text to the main menu screen
            TextMeshProUGUI text = null;
            bool firstTime = true;
            bool canCreate = true;
            
            // load options ui base objects
            var modOptionsUI = AssetUtils.LoadAssetBundleFromResources("modoptionsui", typeof(Unbound).Assembly);
            if (modOptionsUI == null)
            {
                UnityEngine.Debug.LogError("Couldn't find ModOptionsUI AssetBundle?");
            }
            
            // Get base UI objects
            var baseObjects = modOptionsUI.LoadAsset<GameObject>("BaseObjects");
            menuBase = modOptionsUI.LoadAsset<GameObject>("EmptyMenuBase");
            buttonBase = baseObjects.transform.Find("Group/Grid/ButtonBaseObject").gameObject;
            textBase = baseObjects.transform.Find("Group/Grid/TextBaseObject").gameObject;
            toggleBase = baseObjects.transform.Find("Group/Grid/ToggleBaseObject").gameObject;
            inputFieldBase = baseObjects.transform.Find("Group/Grid/InputFieldBaseObject").gameObject;

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
                    var pos = new Vector2(Screen.width / 2, Screen.height * 0.75f - 40);
                    text = CreateTextAt("UNBOUND", Vector2.zero);
                    text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    text.fontSize = 30;
                    text.color = (Color.yellow + Color.red) / 2;
                    text.font = ((TextMeshProUGUI) FindObjectOfType<ListMenuButton>().GetFieldValue("text")).font;
                    text.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group"), true);
                    text.transform.SetAsFirstSibling();
                    text.rectTransform.localScale = Vector3.one;
                    text.rectTransform.localPosition = new Vector3(0, 325, text.rectTransform.localPosition.z);
                });

                // create mod options
                var time = firstTime;
                this.ExecuteAfterSeconds(firstTime ? 0.2f : 0, () =>
                {
                    // Create mod options menu
                    modOptionsMenu = CreateMenu("MOD OPTIONS", null,MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main").gameObject, 60, false, true,4);

                    
                    // Fix main menu layout
                    void fixMainMenuLayout()
                    {
                        var mainMenu = MainMenuHandler.instance.transform.Find("Canvas/ListSelector");
                        var logo = mainMenu.Find("Main/Group/Rounds_Logo2_White").gameObject.AddComponent<LayoutElement>();
                        logo.GetComponent<RectTransform>().sizeDelta = new Vector2(logo.GetComponent<RectTransform>().sizeDelta.x, 80);
                        mainMenu.Find("Main").transform.position = new Vector3(0,1.7f,mainMenu.Find("Main").transform.position.z);
                        mainMenu.Find("Main/Group").GetComponent<VerticalLayoutGroup>().spacing = 10;
                    }
                    
                    var visibleObj = new GameObject("visible");
                    var visible = visibleObj.AddComponent<ActionOnBecameVisible>();
                    visibleObj.AddComponent<SpriteRenderer>();
                    visible.visibleAction += fixMainMenuLayout;
                    visibleObj.transform.parent = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main");
                    
                    // Create toggle cards button
                    CreateButton("Toggle Cards", 70, modOptionsMenu, () => { CardToggleMenuHandler.Instance.Show();});
                    
                    // Create menu's for mods with new UI
                    foreach (var menu in modMenus)
                    {
                        var mmenu = CreateMenu(menu.menuName, menu.buttonAction, modOptionsMenu, 75, true, true);
                        menu.guiAction.Invoke(mmenu);
                    }
                    
                    // Create menu's for mods that do not use the new UI
                    if (GUIListeners.Count != 0) {CreateText("<color=red>Not updated mods</color>", 50, modOptionsMenu, out _);}
                    foreach (var modMenu in GUIListeners.Keys)
                    {
                        var menu =CreateMenu(modMenu, () =>
                                {
                                    foreach (var list in GUIListeners.Values.Where(list => list.guiEnabled))
                                    {
                                        list.guiEnabled = false;
                                    }
                                    GUIListeners[modMenu].guiEnabled = true;
                                    showModUi = true;
                                }, modOptionsMenu,
                                75,
                                true, false);
                        CreateText(
                            "This mod has not yet been updated to the new UI system.\nUse the old UI system in the top right",
                            60, menu, out _);
                    }

                    // check if there are no deprecated ui's and disable the f1 menu
                    if (GUIListeners.Count == 0) noDeprecatedMods = true;
                });
                
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
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !noDeprecatedMods)
            {
                showModUi = !showModUi;
            }

            GameManager.lockInput = showModUi || DevConsole.isTyping;
        }

        private void OnGUI()
        {
            if (!showModUi) return;

            GUILayout.BeginVertical();

            bool showingSpecificMod = false;
            foreach (var md in GUIListeners.Keys)
            {
                var data = GUIListeners[md];
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
            foreach (var md in GUIListeners.Keys)
            {
                var data = GUIListeners[md];
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
                c.SetValue(cards.Contains(c.info.cardName) && c.Value);
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

        public static void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null)
        {
            if (parent == null)
            {
                parent = modOptionsMenu;
            }
            modMenus.Add(new ModMenu(name,buttonAction,guiAction, parent));
        }

        // Creates a menu and returns its gameObject
        public static GameObject CreateMenu(string Name, UnityAction buttonAction, GameObject parent = null, int size = 50, bool setBarHeight = false, bool setFontSize = true, int siblingIndex = -1)
        {
            var obj = Instantiate(menuBase, MainMenuHandler.instance.transform.Find("Canvas/ListSelector"));
            obj.name = Name;
            void disableOldMenu()
            {
                if (GUIListeners.ContainsKey(Name))
                {
                    GUIListeners[Name].guiEnabled = false;
                    showModUi = false;
                }
            }

            // set default parent
            if (parent == null)
            {
                parent = modOptionsMenu;
            }
            
            // Assign back objects
            var goBackObject = parent.GetComponentInParent<ListMenuPage>();
            obj.GetComponentInChildren<GoBack>(true).target = goBackObject;
            obj.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(ClickBack(goBackObject) + disableOldMenu);
            obj.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(ClickBack(goBackObject) + disableOldMenu);

            // Create button to menu
            Transform buttonParent = null;
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content")) buttonParent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content");
            else if (parent.transform.Find("Group")) buttonParent = parent.transform.Find("Group");
            
            var button = Instantiate(buttonBase, buttonParent);
            button.GetComponent<ListMenuButton>().setBarHeight = setBarHeight ? size : 0;
            button.name = Name;
            button.GetComponent<RectTransform>().sizeDelta += new Vector2(400, 0);
            if (siblingIndex != -1) button.transform.SetSiblingIndex(siblingIndex);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(button.GetComponent<RectTransform>().sizeDelta.x, size+12);
            var uGUI = button.GetComponentInChildren<TextMeshProUGUI>();
            uGUI.text = Name;
            uGUI.fontSize = setFontSize ? size : 50;
            if (buttonAction == null)
            {
                buttonAction = () => 
                {
                    obj.GetComponent<ListMenuPage>().Open();
                };
            }
            else
            {
                buttonAction += () => 
                {
                    obj.GetComponent<ListMenuPage>().Open();
                };
            }
            
            button.GetComponent<Button>().onClick.AddListener(buttonAction);

            return obj;
        }

        private static UnityAction ClickBack(ListMenuPage backObject)
        {
            showModUi = false;
            return backObject.Open;
        }

        // Creates a UI text
        public static GameObject CreateText(string text, int fontSize, GameObject parent, out TextMeshProUGUI uGUI)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var textObject = Instantiate(textBase, parent.transform);
            uGUI = textObject.GetComponent<TextMeshProUGUI>();
            uGUI.text = text;
            uGUI.fontSizeMax = fontSize;

            return textObject;
        }

        // Creates a UI Toggle
        public static GameObject CreateToggle(bool value, string text, int fontSize, GameObject parent, UnityAction<bool> onValueChangedAction)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var toggleObject = Instantiate(toggleBase, parent.transform);
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(onValueChangedAction); 
            var uGUI = toggleObject.GetComponentInChildren<TextMeshProUGUI>();
            uGUI.text = text;
            uGUI.fontSizeMax = fontSize;

            return toggleObject;
        }

        // Creates a UI Button
        public static GameObject CreateButton(string text, int fontSize, GameObject parent,
            UnityAction onClickAction)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var buttonObject = Instantiate(buttonBase, parent.transform);
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClickAction);
            var uGUI = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            uGUI.text = text;
            uGUI.fontSizeMax = fontSize;

            buttonObject.GetComponent<RectTransform>().sizeDelta += new Vector2(400, 0);
            
            return buttonObject;
        }

        // Creates a UI InputField
        public static GameObject CreateInputField(string placeholderText, int fontSize, GameObject parent, UnityAction<string> onValueChangedAction)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var inputObject = Instantiate(inputFieldBase, parent.transform);
            var inputField = inputObject.GetComponentInChildren<TMP_InputField>();
            inputField.pointSize = fontSize;
            inputField.onValueChanged.AddListener(onValueChangedAction);
            var inputFieldColors = inputField.colors;
            inputFieldColors.colorMultiplier = 0.75f;
            inputField.colors = inputFieldColors;
            
            var placeHolder = (TextMeshProUGUI) inputField.placeholder;
            placeHolder.text = placeholderText;
            
            return inputObject;
        }

        public static void RegisterGUI(string modName, Action guiAction)
        {
            GUIListeners.Add(modName, new GUIListener(modName, guiAction));
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

        public static TextMeshProUGUI CreateTextAt(string text, Vector2 position)
        {
            var newText = new GameObject("Unbound Text Object").AddComponent<TextMeshProUGUI>();
            newText.text = text;
            newText.fontSize = 100;
            newText.transform.SetParent(Instance.canvas.transform);

            var anchorPoint = new Vector2(0.5f, 0.5f);
            newText.rectTransform.anchorMax = anchorPoint;
            newText.rectTransform.anchorMin = anchorPoint;
            newText.rectTransform.pivot = anchorPoint;
            newText.overflowMode = TextOverflowModes.Overflow;
            newText.alignment = TextAlignmentOptions.Center;
            newText.rectTransform.position = position;
            newText.enableWordWrapping = false;

            Instance.StartCoroutine(FadeIn(newText.gameObject.AddComponent<CanvasGroup>(), 4));

            return newText;
        }

        public static void RegisterMaps(AssetBundle assetBundle)
        {
            Unbound.RegisterMaps(assetBundle.GetAllScenePaths());
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

        private static IEnumerator FadeIn(CanvasGroup target, float seconds)
        {
            float startTime = Time.time;
            target.alpha = 0;
            while (Time.time - startTime < seconds)
            {
                target.alpha = (Time.time - startTime) / seconds;
                yield return null;
            }
            target.alpha = 1;
        }

        internal class GUIListener
        {
            public bool guiEnabled = false;
            public string modName;
            public Action guiAction;
            public GUIListener(string modName, Action guiAction)
            {
                this.modName = modName;
                this.guiAction = guiAction;
            }
        }

        internal class ModMenu
        {
            public string menuName;
            public UnityAction buttonAction;
            public Action<GameObject> guiAction;
            public GameObject parent;

            public ModMenu(string menuName, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent)
            {
                this.menuName = menuName;
                this.buttonAction = buttonAction;
                this.guiAction = guiAction;
                this.parent = parent;
            }
        }
    }
}
