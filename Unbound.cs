using System;
using System.Collections;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnboundLib.Networking;

namespace UnboundLib
{
    [BepInPlugin(ModId, ModName, "1.0.0.4")]
    [BepInProcess("Rounds.exe")]
    public class Unbound : BaseUnityPlugin
    {
        private const string ModId = "com.willis.rounds.unbound";
        private const string ModName = "Rounds Unbound";

        public static Unbound Instance { get; private set; }

        private Canvas _canvas;
        public Canvas canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = new GameObject("").AddComponent<Canvas>();
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
        internal static List<CardInfo> moddedCards = new List<CardInfo>();

        public delegate void OnJoinedDelegate();
        public delegate void OnLeftDelegate();
        public static event OnJoinedDelegate OnJoinedRoom;
        public static event OnLeftDelegate OnLeftRoom;

        internal static Dictionary<string, GUIListener> GUIListeners = new Dictionary<string, GUIListener>();
        internal static List<Action> handShakeActions = new List<Action>();

        private static bool showModUi = false;

        public Unbound()
        {
            // Add UNBOUND text to the main menu screen
            TextMeshProUGUI text = null;
            bool firstTime = true;

            On.MainMenuHandler.Awake += (orig, self) =>
            {
                orig(self);
                this.ExecuteAfterSeconds(firstTime ? 4f : 0.1f, () =>
                {
                    text = CreateTextAt("UNBOUND", new Vector2(Screen.width / 2, Screen.height * 0.75f + 25));
                    text.fontSize /= 2.5f;
                    text.color = (Color.yellow + Color.red) / 2;
                    text.font = ((TextMeshProUGUI)FindObjectOfType<ListMenuButton>().GetFieldValue("text")).font;
                });
                firstTime = false;
            };
            
            On.MainMenuHandler.Close += (orig, self) =>
            {
                orig(self);
                Destroy(text.gameObject);
            };
        }

        void Awake()
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
        }
        
        void Start()
        {
            // store default cards
            defaultCards = (CardInfo[])CardChoice.instance.cards.Clone();

            // request mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.StartHandshake, (data) =>
            {
                NetworkingManager.RaiseEvent(NetworkEventType.FinishHandshake);
                CardChoice.instance.cards = defaultCards;
            });
            // recieve mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.FinishHandshake, (data) =>
            {
                CardChoice.instance.cards = moddedCards.ToArray();
            });

            // fetch card to use as a template for all custom cards
            templateCard = (from c in CardChoice.instance.cards
                            where c.cardName.ToLower() == "huge"
                            select c).FirstOrDefault();
            defaultCards = CardChoice.instance.cards;
            moddedCards.AddRange(defaultCards);

            // hook up Photon callbacks
            var networkEvents = gameObject.AddComponent<NetworkEventCallbacks>();
            networkEvents.OnJoinedRoomEvent += OnJoinedRoomAction;
            networkEvents.OnLeftRoomEvent += OnLeftRoomAction;
        }
        
        void Update()
        {
            if (GameManager.instance.isPlaying && PhotonNetwork.OfflineMode)
            {
                CardChoice.instance.cards = moddedCards.ToArray();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                showModUi = !showModUi;
            }
            GameManager.lockInput = showModUi || DevConsole.isTyping;
        }
        
        void OnGUI()
        {
            if (!showModUi) return;

            Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
            Vector2 size = new Vector2(400, 100 * GUIListeners.Count);
            
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

        private void OnJoinedRoomAction()
        {
            NetworkingManager.RaiseEventOthers(NetworkEventType.StartHandshake);

            OnJoinedRoom?.Invoke();
            foreach (var handshake in handShakeActions)
            {
                handshake?.Invoke();
            }
        }
        private void OnLeftRoomAction()
        {
            CardChoice.instance.cards = defaultCards;
            OnLeftRoom?.Invoke();
        }

        [UnboundRPC]
        public static void BuildInfoPopup(string message)
        {
            var popup = new GameObject("Info Popup").AddComponent<InfoPopup>();
            popup.rectTransform.SetParent(Instance.canvas.transform);
            popup.Build(message);
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

        private static TextMeshProUGUI CreateTextAt(string text, Vector2 position)
        {
            var newText = new GameObject("Timer").AddComponent<TextMeshProUGUI>();
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
    }
}
