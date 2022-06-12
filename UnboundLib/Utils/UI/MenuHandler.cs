using System;
using System.Collections;
using Jotunn.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UnboundLib.Utils.UI
{
    public class MenuHandler
    {
        private static GameObject menuBase;
        private static GameObject buttonBase;
        private static GameObject textBase;
        private static GameObject toggleBase;
        private static GameObject inputFieldBase;
        private static GameObject sliderBase;

        public static MenuHandler Instance = new MenuHandler();

        public static AssetBundle modOptionsUI;

        private MenuHandler()
        {
            // singleton first time setup

            Instance = this;

            // load options ui base objects
            modOptionsUI = AssetUtils.LoadAssetBundleFromResources("modoptionsui", typeof(Unbound).Assembly);
            if (modOptionsUI == null)
            {
                UnityEngine.Debug.LogError("Couldn't find ModOptionsUI AssetBundle?");
                return;
            }

            // Get base UI objects
            var baseObjects = modOptionsUI.LoadAsset<GameObject>("BaseObjects");
            menuBase = modOptionsUI.LoadAsset<GameObject>("EmptyMenuBase");
            buttonBase = baseObjects.transform.Find("Group/Grid/ButtonBaseObject").gameObject;
            textBase = baseObjects.transform.Find("Group/Grid/TextBaseObject").gameObject;
            toggleBase = baseObjects.transform.Find("Group/Grid/ToggleBaseObject").gameObject;
            inputFieldBase = baseObjects.transform.Find("Group/Grid/InputFieldBaseObject").gameObject;
            sliderBase = baseObjects.transform.Find("Group/Grid/SliderBaseObject").gameObject;
        }

        // Creates a menu and returns its gameObject
        public static GameObject CreateMenu(string Name, UnityAction buttonAction, GameObject parentForButton, int size = 50, bool forceUpper = true, bool setBarHeight = true, GameObject parentForMenu = null,  bool setFontSize = true, int siblingIndex = -1)
        {
            var obj = parentForMenu is null ? Object.Instantiate(menuBase, MainMenuHandler.instance.transform.Find("Canvas/ListSelector")) : Object.Instantiate(menuBase, parentForMenu.transform);
            obj.name = Name;
            
            // Assign back objects
            var goBackObject = parentForButton.GetComponentInParent<ListMenuPage>();
            obj.GetComponentInChildren<GoBack>(true).target = goBackObject;
            obj.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(ClickBack(goBackObject));
            obj.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(ClickBack(goBackObject));

            // GetParent for button
            Transform buttonParent = null;
            if (parentForButton.transform.Find("Group/Grid/Scroll View/Viewport/Content")) buttonParent = parentForButton.transform.Find("Group/Grid/Scroll View/Viewport/Content");
            else if (parentForButton.transform.Find("Group")) buttonParent = parentForButton.transform.Find("Group");
            
            // Create button to menu
            var button = Object.Instantiate(buttonBase, buttonParent);
            button.GetComponent<ListMenuButton>().setBarHeight = setBarHeight ? size : 0;
            button.name = Name;
            button.GetComponent<RectTransform>().sizeDelta += new Vector2(400, 0);
            if (siblingIndex != -1) button.transform.SetSiblingIndex(siblingIndex);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(button.GetComponent<RectTransform>().sizeDelta.x, size+12);
            var uGUI = button.GetComponentInChildren<TextMeshProUGUI>();
            uGUI.text = forceUpper ? Name.ToUpper() : Name;
            uGUI.fontSize = setFontSize ? size : 50;
            if (buttonAction == null)
            {
                buttonAction = () => 
                {
                    obj.GetComponent<ListMenuPage>().Open();
                    goBackObject.Close();
                    obj.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 1;
                };
            }
            else
            {
                buttonAction += () => 
                {
                    obj.GetComponent<ListMenuPage>().Open();
                    goBackObject.Close();
                    obj.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 1;
                };
            }
            button.GetComponent<Button>().onClick.AddListener(buttonAction);

            return obj;
        }

        private static UnityAction ClickBack(ListMenuPage backObject)
        {
            return backObject.Open;
        }

        // Creates a UI text
        public static GameObject CreateText(string text, GameObject parent, out TextMeshProUGUI uGUI, int fontSize = 60, bool forceUpper = true, Color? color = null, TMP_FontAsset font = null, Material fontMaterial = null, TextAlignmentOptions? alignmentOptions = null)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }
            var textObject = Object.Instantiate(textBase, parent.transform);
            uGUI = textObject.GetComponent<TextMeshProUGUI>();
            uGUI.text = forceUpper ? text.ToUpper() : text;
            uGUI.fontSizeMax = fontSize;
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TextAlignmentOptions)alignmentOptions; }

            return textObject;
        }

        // Creates a UI Toggle
        public static GameObject CreateToggle(bool value, string text, GameObject parent, UnityAction<bool> onValueChangedAction = null, int fontSize = 60, bool forceUpper = true, Color? color = null, TMP_FontAsset font = null, Material fontMaterial = null, TextAlignmentOptions? alignmentOptions = null)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }
            var toggleObject = Object.Instantiate(toggleBase, parent.transform);
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = value;
            if (onValueChangedAction != null) toggle.onValueChanged.AddListener(onValueChangedAction); 
            var uGUI = toggleObject.GetComponentInChildren<TextMeshProUGUI>();
            uGUI.text = forceUpper ? text.ToUpper() : text; 
            uGUI.fontSizeMax = fontSize;
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TextAlignmentOptions)alignmentOptions; }

            return toggleObject;
        }

        // Creates a UI Button
        public static GameObject CreateButton(string text, GameObject parent, UnityAction onClickAction = null, int fontSize = 60, bool forceUpper = true, Color? color = null, TMP_FontAsset font = null, Material fontMaterial = null, TextAlignmentOptions? alignmentOptions = null)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }
            var buttonObject = Object.Instantiate(buttonBase, parent.transform);
            var button = buttonObject.GetComponent<Button>();
            if (onClickAction != null) { button.onClick.AddListener(onClickAction); }
            var uGUI = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            uGUI.text = forceUpper ? text.ToUpper() : text; 
            uGUI.fontSizeMax = fontSize;
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TextAlignmentOptions)alignmentOptions; }

            buttonObject.GetComponent<RectTransform>().sizeDelta += new Vector2(400, 0);
            
            return buttonObject;
        }

        // Creates a UI InputField
        public static GameObject CreateInputField(string placeholderText, int fontSize, GameObject parent, UnityAction<string> onValueChangedAction)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }
            var inputObject = Object.Instantiate(inputFieldBase, parent.transform);
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

        // Creates a UI Slider
        public static GameObject CreateSlider(string text, GameObject parent, int fontSize, float minValue, float maxValue, float defaultValue,
            UnityAction<float> onValueChangedAction, out Slider slider, bool wholeNumbers = false, Color? sliderColor = null, Slider.Direction direction = Slider.Direction.LeftToRight, bool forceUpper = true, Color? color = null, TMP_FontAsset font = null, Material fontMaterial = null, TextAlignmentOptions? alignmentOptions = null)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }
            var sliderObject = Object.Instantiate(sliderBase, parent.transform);
            var pos = sliderObject.transform.Find("GameObject").position;
            pos.x = 0;
            sliderObject.transform.Find("GameObject").position = pos;

            // Update inputField
            var inputField = sliderObject.GetComponentInChildren<TMP_InputField>();
            var vector2 = inputField.gameObject.GetComponent<RectTransform>().sizeDelta;
            vector2.x = 65;
            inputField.gameObject.GetComponent<RectTransform>().sizeDelta = vector2;
            onValueChangedAction += arg0 => inputField.text = arg0.ToString((wholeNumbers)? "N0":"N");
            inputField.contentType = wholeNumbers
                ? TMP_InputField.ContentType.IntegerNumber
                : TMP_InputField.ContentType.DecimalNumber;
            
            // Set slider values
            slider = sliderObject.GetComponentInChildren<Slider>();
            var sliderColors = slider.colors;
            sliderColors.colorMultiplier = 0.75f;
            slider.colors = sliderColors;
            slider.direction = direction;
            slider.wholeNumbers = wholeNumbers;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.onValueChanged.AddListener(onValueChangedAction);
            slider.value = defaultValue;
            slider.transform.Find("Fill Area/Fill").GetComponent<Image>().color = sliderColor ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            
            inputField.text = slider.value.ToString();
            var slider1 = slider;
            inputField.onValueChanged.AddListener(arg1 =>
            {
                if(arg1 != "") slider1.value = Convert.ToSingle(arg1);
            });

            // Set text values
            var uGUI = sliderObject.GetComponentsInChildren<TextMeshProUGUI>()[2];
            uGUI.text = text;
            uGUI.fontSizeMax = fontSize;
            uGUI.text = forceUpper ? text.ToUpper() : text; 
            uGUI.color = color ?? new Color(0.902f, 0.902f, 0.902f, 1f);
            if (font != null) { uGUI.font = font; }
            if (fontMaterial != null) { uGUI.fontMaterial = fontMaterial; }
            if (alignmentOptions != null) { uGUI.alignment = (TextAlignmentOptions)alignmentOptions; }
            
            return sliderObject;
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

    }
}
