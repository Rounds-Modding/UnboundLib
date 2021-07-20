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

namespace UnboundLib.UI
{
    public class MenuHandler
    {

        internal static List<ModMenu> modMenus = new List<ModMenu>();
        internal static Dictionary<string, GUIListener> GUIListeners = new Dictionary<string, GUIListener>();
        internal static List<Action> handShakeActions = new List<Action>();

        private GameObject menuBase;
        private GameObject buttonBase;
        private GameObject textBase;
        private GameObject toggleBase;
        private GameObject inputFieldBase;
        private GameObject modOptionsMenu;

        internal static bool showModUi = false;
        internal static bool noDeprecatedMods = false;

        public static MenuHandler Instance = new MenuHandler();

        private MenuHandler()
        {
            // singleton first time setup

            MenuHandler.Instance = this;

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
        }

        public void CreateModOptions(bool firstTime)
        {
            // create mod options
            Unbound.Instance.ExecuteAfterSeconds(firstTime ? 0.2f : 0, () =>
            {
                // Create mod options menu
                modOptionsMenu = CreateMenu("MOD OPTIONS", null, MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main").gameObject, 60, true, false, true, 4);


                // Fix main menu layout
                void fixMainMenuLayout()
                {
                    var mainMenu = MainMenuHandler.instance.transform.Find("Canvas/ListSelector");
                    var logo = mainMenu.Find("Main/Group/Rounds_Logo2_White").gameObject.AddComponent<LayoutElement>();
                    logo.GetComponent<RectTransform>().sizeDelta = new Vector2(logo.GetComponent<RectTransform>().sizeDelta.x, 80);
                    mainMenu.Find("Main").transform.position = new Vector3(0, 1.7f, mainMenu.Find("Main").transform.position.z);
                    mainMenu.Find("Main/Group").GetComponent<VerticalLayoutGroup>().spacing = 10;
                }

                var visibleObj = new GameObject("visible");
                var visible = visibleObj.AddComponent<ActionOnBecameVisible>();
                visibleObj.AddComponent<SpriteRenderer>();
                visible.visibleAction += fixMainMenuLayout;
                visibleObj.transform.parent = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main");

                // Create toggle cards button
                CreateButton("Toggle Cards", modOptionsMenu, () => { CardToggleMenuHandler.Instance.Show(); });

                // Create menu's for mods with new UI
                foreach (var menu in modMenus)
                {
                    var mmenu = CreateMenu(menu.menuName, menu.buttonAction, modOptionsMenu, 60, true, true);
                    menu.guiAction.Invoke(mmenu);
                }

                // Create menu's for mods that do not use the new UI
                if (GUIListeners.Count != 0) { CreateText(" ", modOptionsMenu, out _); }
                foreach (var modMenu in GUIListeners.Keys)
                {
                    var menu = CreateMenu(modMenu, () =>
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
                        "This mod has not yet been updated to the new UI system.\nPlease use the old UI system in the top left.", menu, out _, 60, false);
                }

                // check if there are no deprecated ui's and disable the f1 menu
                if (GUIListeners.Count == 0) noDeprecatedMods = true;
            });
        }
        public void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null)
        {
            if (parent == null)
            {
                parent = modOptionsMenu;
            }
            modMenus.Add(new ModMenu(name,buttonAction,guiAction, parent));
        }

        // Creates a menu and returns its gameObject
        public GameObject CreateMenu(string Name, UnityAction buttonAction, GameObject parent = null, int size = 50, bool forceUpper = true, bool setBarHeight = false, bool setFontSize = true, int siblingIndex = -1)
        {
            var obj = UnityEngine.GameObject.Instantiate(menuBase, MainMenuHandler.instance.transform.Find("Canvas/ListSelector"));
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
            
            var button = UnityEngine.GameObject.Instantiate(buttonBase, buttonParent);
            button.GetComponent<ListMenuButton>().setBarHeight = setBarHeight ? size : 0;
            button.name = Name;
            button.GetComponent<RectTransform>().sizeDelta += new Vector2(400, 0);
            if (siblingIndex != -1) button.transform.SetSiblingIndex(siblingIndex);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(button.GetComponent<RectTransform>().sizeDelta.x, size+12);
            var uGUI = button.GetComponentInChildren<TextMeshProUGUI>();
            if (forceUpper) uGUI.text = Name.ToUpper();
            else uGUI.text = Name;
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
        public GameObject CreateText(string text, GameObject parent, out TextMeshProUGUI uGUI, int fontSize = 60, bool forceUpper = true, Color? color = null, TMPro.TMP_FontAsset font = null, Material fontMaterial = null, TMPro.TextAlignmentOptions? alignmentOptions = null)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var textObject = UnityEngine.GameObject.Instantiate(textBase, parent.transform);
            uGUI = textObject.GetComponent<TextMeshProUGUI>();
            if (forceUpper)
            {
                uGUI.text = text.ToUpper();
            }
            else
            {
                uGUI.text = text;
            }
            uGUI.fontSizeMax = fontSize;
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TMPro.TextAlignmentOptions)alignmentOptions; }

            return textObject;
        }
        // Creates a UI Toggle
        public GameObject CreateToggle(bool value, string text, GameObject parent, UnityAction<bool> onValueChangedAction = null, int fontSize = 60, bool forceUpper = true, Color? color = null, TMPro.TMP_FontAsset font = null, Material fontMaterial = null, TMPro.TextAlignmentOptions? alignmentOptions = null)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var toggleObject = UnityEngine.GameObject.Instantiate(toggleBase, parent.transform);
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = value;
            if (onValueChangedAction != null) toggle.onValueChanged.AddListener(onValueChangedAction); 
            var uGUI = toggleObject.GetComponentInChildren<TextMeshProUGUI>();
            if (forceUpper)
            {
                uGUI.text = text.ToUpper();
            }
            else
            {
                uGUI.text = text;
            }
            uGUI.fontSizeMax = fontSize;
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TMPro.TextAlignmentOptions)alignmentOptions; }

            return toggleObject;
        }

        // Creates a UI Button
        public GameObject CreateButton(string text, GameObject parent, UnityAction onClickAction = null, int fontSize = 60, bool forceUpper = true, Color? color = null, TMPro.TMP_FontAsset font = null, Material fontMaterial = null, TMPro.TextAlignmentOptions? alignmentOptions = null)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var buttonObject = UnityEngine.GameObject.Instantiate(buttonBase, parent.transform);
            var button = buttonObject.GetComponent<Button>();
            if (onClickAction != null) { button.onClick.AddListener(onClickAction); }
            var uGUI = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (forceUpper)
            {
                uGUI.text = text.ToUpper();
            }
            else
            {
                uGUI.text = text;
            }
            uGUI.fontSizeMax = fontSize;
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TMPro.TextAlignmentOptions)alignmentOptions; }

            buttonObject.GetComponent<RectTransform>().sizeDelta += new Vector2(400, 0);
            
            return buttonObject;
        }

        // Creates a UI InputField
        public GameObject CreateInputField(string placeholderText, int fontSize, GameObject parent, UnityAction<string> onValueChangedAction)
        {
            parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            var inputObject = UnityEngine.GameObject.Instantiate(inputFieldBase, parent.transform);
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

        public static TextMeshProUGUI CreateTextAt(string text, Vector2 position)
        {
            var newText = new GameObject("Unbound Text Object").AddComponent<TextMeshProUGUI>();
            newText.text = text;
            newText.fontSize = 100;
            newText.transform.SetParent(Unbound.Instance.canvas.transform);

            var anchorPoint = new Vector2(0.5f, 0.5f);
            newText.rectTransform.anchorMax = anchorPoint;
            newText.rectTransform.anchorMin = anchorPoint;
            newText.rectTransform.pivot = anchorPoint;
            newText.overflowMode = TextOverflowModes.Overflow;
            newText.alignment = TextAlignmentOptions.Center;
            newText.rectTransform.position = position;
            newText.enableWordWrapping = false;

            Unbound.Instance.StartCoroutine(FadeIn(newText.gameObject.AddComponent<CanvasGroup>(), 4));

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
