using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnboundLib.Utils.UI
{
    public class ToggleCardsMenuHandler : MonoBehaviour
    {
        public static ToggleCardsMenuHandler instance;

        private static readonly Dictionary<string, Transform> ScrollViews = new Dictionary<string, Transform>();

        public static readonly Dictionary<GameObject, Action> CardObjs = new Dictionary<GameObject, Action>();
        public static readonly List<Action> DefaultCardActions = new List<Action>();

        private static readonly List<Button> ButtonsToDisable = new List<Button>();
        private static readonly List<Toggle> TogglesToDisable = new List<Toggle>();

        public static GameObject toggleCardsCanvas;

        private GameObject cardObjAsset;
        private GameObject scrollViewAsset;
        private GameObject categoryButtonAsset;

        private Transform scrollViewTrans;
        private Transform categoryContent;

        public static bool disableEscapeButton;

        public static bool menuOpenFromOutside;

        private static bool disabled;

        private static bool sortedByName = true;

        internal static Color positiveColor = new Color(0.465f, 0.603f, 0.390f, 1);
        internal static Color negativeColor = new Color(0.698f, 0.326f, 0.326f, 1);
        // internal static Color commonColor = new Color(0.698f, 0.326f, 0.326f, 1);
        internal static Color uncommonColor = new Color(0, 0.5f, 1, 1);
        internal static Color rareColor = new Color(1, 0.2f, 1, 1);

        private static TextMeshProUGUI cardAmountText;

        private static string CurrentCategory => (from scroll in ScrollViews where scroll.Value.gameObject.activeInHierarchy select scroll.Key).FirstOrDefault();

        // if need to toggle all on or off
        private bool toggledAll;

        private void Start()
        {
            instance = this;
            var mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();

            var cardsCanvas = Unbound.toggleUI.LoadAsset<GameObject>("ToggleCardsCanvas");

            cardObjAsset = Unbound.toggleUI.LoadAsset<GameObject>("CardObj");

            scrollViewAsset = Unbound.toggleUI.LoadAsset<GameObject>("ScrollView2");
            categoryButtonAsset = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");

            toggleCardsCanvas = Instantiate(cardsCanvas);
            DontDestroyOnLoad(toggleCardsCanvas);
            var canvas = toggleCardsCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            toggleCardsCanvas.SetActive(false);

            scrollViewTrans = toggleCardsCanvas.transform.Find("CardMenu/ScrollViews");

            categoryContent = toggleCardsCanvas.transform.Find("CardMenu/Top/Categories/ButtonsScroll/Viewport/Content");

            // Create and set searchbar
            var searchBar = toggleCardsCanvas.transform.Find("CardMenu/Top/InputField").gameObject;
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                foreach (var card in ScrollViews.SelectMany(scrollView => scrollView.Value.GetComponentsInChildren<Button>(true)))
                {
                    if (value == "")
                    {
                        card.gameObject.SetActive(true);
                        continue;
                    }

                    card.gameObject.SetActive(card.name.ToUpper().Contains(value.ToUpper()));
                }
            });

            // create and set sort button (making use of the unused "Switch profile" button)
            toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");
            var sortButton = toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").GetComponent<Button>();
            sortButton.onClick.AddListener(() =>
            {
                sortedByName = !sortedByName;
                toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");

                SortCardMenus(sortedByName);
            });

            Transform cardAmountObject = toggleCardsCanvas.transform.Find("CardMenu/Top/CardAmount");
            cardAmountText = cardAmountObject.GetComponentInChildren<TextMeshProUGUI>();

            var cardAmountSlider = cardAmountObject.GetComponentsInChildren<Slider>();
            foreach (Slider slider in cardAmountSlider)
            {
                slider.onValueChanged.AddListener((amount =>
                {
                    int integerAmount = (int) amount;
                    ChangeCardColumnAmountMenus(integerAmount);
                }));
            }

            // Create and set toggle all button
            var toggleAllButton = toggleCardsCanvas.transform.Find("CardMenu/Top/ToggleAll").GetComponent<Button>();
            ButtonsToDisable.Add(toggleAllButton);
            toggleAllButton.onClick.AddListener(() =>
            {
                if (CurrentCategory == null) return;

                toggledAll = !toggledAll;

                var cardsInCategory = CardManager.GetCardsInCategory(CurrentCategory);
                if (toggledAll)
                {
                    var objs = GetCardObjs(cardsInCategory);
                    foreach (var obj in objs)
                    {
                        CardManager.cards[obj.name].enabled = false;
                        CardObjs[obj].Invoke();
                    }
                }
                else
                {
                    var objs = GetCardObjs(cardsInCategory);
                    foreach (var obj in objs)
                    {
                        CardManager.cards[obj.name].enabled = true;
                        CardObjs[obj].Invoke();
                    }
                }
            });

            // get and set info button
            var infoButton = toggleCardsCanvas.transform.Find("CardMenu/Top/Help").GetComponent<Button>();
            var infoMenu = toggleCardsCanvas.transform.Find("CardMenu/InfoMenu").gameObject;
            infoButton.onClick.AddListener(() =>
            {
                infoMenu.SetActive(!infoMenu.activeInHierarchy);
            });

            this.ExecuteAfterSeconds(0.85f, () =>
            {
                toggleCardsCanvas.SetActive(true);
                // Create category scrollViews
                foreach (var category in CardManager.categories)
                {
                    var scrollView = Instantiate(scrollViewAsset, scrollViewTrans);
                    scrollView.SetActive(true);
                    SetActive(scrollView.transform, false);
                    scrollView.name = category;
                    ScrollViews.Add(category, scrollView.transform);
                    if (category == "Vanilla")
                    {
                        SetActive(scrollView.transform, true);
                    }
                }

                // Create cardObjs
                foreach (var card in CardManager.cards)
                {
                    Card cardValue = card.Value;
                    var parentScroll = ScrollViews[cardValue.category].Find("Viewport/Content");
                    var crdObj = Instantiate(cardObjAsset, parentScroll);

                    crdObj.name = card.Key;

                    if (cardValue != null)
                    {
                        CardInfo cardInfo = cardValue.cardInfo;
                        if (cardInfo != null)
                        {
                            GameObject cardBase = cardInfo.cardBase;
                            if (cardBase != null)
                            {
                                Transform cardTransform = cardBase.transform;
                                Transform cardChild1 = cardTransform.GetChild(0);
                                if (cardChild1 != null)
                                {
                                    Transform frontCard = cardChild1.GetChild(0);
                                    if (frontCard != null)
                                    {
                                        // Needs to be immediate or else awake is called on it and throws null
                                        var cardRarityComponents = frontCard.GetComponentsInChildren<CardRarityColor>();
                                        foreach (var cardRarityComponent in cardRarityComponents)
                                        {
                                            DestroyImmediate(cardRarityComponent);
                                        }

                                        var cardBaseObj = Instantiate(frontCard, crdObj.transform, true);

                                        Vector3 sizeCalculated = new Vector3(0.1f, 0.1f, 1f);

                                        cardBaseObj.name = "CardPreview";
                                        var cardBaseTransform = cardBaseObj.transform;
                                        cardBaseTransform.localPosition = new Vector3(0, -10, 1f);
                                        cardBaseTransform.localScale = sizeCalculated;
                                        cardBaseObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
                                        cardBaseObj.transform.SetSiblingIndex(1);
                                        cardBaseObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 1500);

                                        var canvasGroups = cardBaseObj.GetComponentsInChildren<CanvasGroup>();
                                        foreach (var canvasGroup in canvasGroups)
                                        {
                                            canvasGroup.alpha = 1;
                                        }

                                        // Creates problems if it's not in the game scene and also is the main cause of lag
                                        GameObject uiParticleObject = FindObjectInChildren(cardBaseObj.gameObject, "UI_ParticleSystem");
                                        if (uiParticleObject != null)
                                        {
                                            Destroy(uiParticleObject);
                                        }

                                        if (cardInfo.cardArt != null)
                                        {
                                            var artObject = FindObjectInChildren(cardBaseObj.gameObject, "Art");
                                            if (artObject != null)
                                            {
                                                var canvasComponent = artObject.AddComponent<Canvas>();
                                                canvasComponent.sortingOrder = 1;

                                                var cardArtObj = Instantiate(cardInfo.cardArt, artObject.transform, true);
                                                cardArtObj.transform.localPosition = new Vector3(0, 0, 1);
                                                cardArtObj.transform.localScale = new Vector3(1f, 1f, 1);
                                                cardArtObj.transform.localRotation = Quaternion.identity;

                                                var cardAnimationHandler = cardBaseObj.transform.parent.gameObject
                                                    .AddComponent<CardAnimationHandler>();
                                                cardAnimationHandler.ToggleAnimation(false);
                                            }

                                            var blockFrontObject = FindObjectInChildren(cardBaseObj.gameObject, "BlockFront");
                                            if (blockFrontObject != null)
                                            {
                                                blockFrontObject.SetActive(true);
                                            }
                                        }

                                        var backgroundObj = FindObjectInChildren(cardBaseObj.gameObject, "Background");
                                        if (backgroundObj != null)
                                        {
                                            backgroundObj.transform.localScale = new Vector3(1, 1, 1);
                                            var rectTransform = backgroundObj.GetComponent<RectTransform>();
                                            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                                            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                                            rectTransform.sizeDelta = new Vector2(1500f, 1500f);

                                            var imageComponent = backgroundObj.gameObject.GetComponentInChildren<Image>(true);
                                            if (imageComponent != null)
                                            {
                                                imageComponent.preserveAspect = true;
                                                imageComponent.color = new Color(0.16f, 0.16f, 0.16f, 1f);
                                            }

                                            var maskComponent = backgroundObj.gameObject.GetComponentInChildren<Mask>(true);
                                            if (maskComponent != null)
                                            {
                                                maskComponent.showMaskGraphic = true;
                                            }
                                        }

                                        var cardColor = CardChoice.instance.GetCardColor(cardInfo.colorTheme);
                                        var edgePieces = cardBaseObj.GetComponentsInChildren<Image>(true).Where(x => x.gameObject.transform.name.Contains("FRAME")).ToList();
                                        foreach (Image edgePiece in edgePieces)
                                        {
                                            edgePiece.color = cardColor;
                                        }
                                        var textName = cardBaseObj.transform.GetChild(1);
                                        if (textName != null)
                                        {
                                            var textComponent = textName.GetComponent<TextMeshProUGUI>();
                                            if (textComponent != null)
                                            {
                                                textComponent.text = cardInfo.cardName.ToUpper();
                                                textComponent.color = cardColor;
                                            }
                                        }

                                        if (cardInfo.rarity != CardInfo.Rarity.Common)
                                        {
                                            var colorFromRarity = cardInfo.rarity == CardInfo.Rarity.Uncommon ? uncommonColor : rareColor;
                                            foreach (var imageComponent in FindObjectsInChildren(cardBaseObj.gameObject, "Triangle").Select(triangleObject => triangleObject.GetComponent<Image>()).Where(imageComponent => imageComponent != null))
                                            {
                                                imageComponent.color = colorFromRarity;
                                            }
                                        }

                                        if (cardValue.category == "Vanilla")
                                        {
                                            var gridObject = FindObjectInChildren(cardBaseObj.gameObject, "Grid");
                                            gridObject.transform.localScale = new Vector3(1, 1, 1);

                                            var effectsText = Instantiate(FindObjectInChildren(cardBaseObj.gameObject, "EffectText"));
                                            if (effectsText != null)
                                            {
                                                effectsText.SetActive(true);
                                                effectsText.transform.SetParent(gridObject.transform);
                                                effectsText.transform.localScale = new Vector3(1, 1, 1);
                                                var effectsTextComponent = effectsText.GetComponent<TextMeshProUGUI>();
                                                effectsTextComponent.text = cardInfo.cardDestription;
                                            }

                                            var statObject = FindObjectInChildren(cardBaseObj.gameObject, "StatObject");
                                            foreach (CardInfoStat cardStat in cardInfo.cardStats)
                                            {
                                                GameObject newGameObject = Instantiate<GameObject>(statObject, gridObject.transform.position, gridObject.transform.rotation, gridObject.transform);
                                                newGameObject.SetActive(true);
                                                newGameObject.transform.localScale = Vector3.one;
                                                TextMeshProUGUI component1 = newGameObject.transform.GetChild(0)
                                                    .GetComponent<TextMeshProUGUI>();
                                                TextMeshProUGUI component2 = newGameObject.transform.GetChild(1)
                                                    .GetComponent<TextMeshProUGUI>();
                                                component1.text = cardStat.stat;
                                                if (cardStat.simepleAmount != CardInfoStat.SimpleAmount.notAssigned &&
                                                    !Optionshandler.showCardStatNumbers)
                                                    component2.text = cardStat.GetSimpleAmount();
                                                else
                                                    component2.text = cardStat.amount;
                                                component2.color = cardStat.positive ? positiveColor : negativeColor;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    void CardAction()
                    {
                        if (cardValue.enabled)
                        {
                            CardManager.DisableCard(cardValue.cardInfo);
                            //card.Value.enabled = false;
                            UpdateVisualsCardObj(crdObj, false);
                        }
                        else
                        {
                            CardManager.EnableCard(cardValue.cardInfo);
                            //card.Value.enabled = true;
                            UpdateVisualsCardObj(crdObj, true);
                        }
                    }

                    CardObjs[crdObj] = CardAction;
                    DefaultCardActions.Add(CardAction);

                    ButtonsToDisable.Add(crdObj.GetComponent<Button>());

                    if (cardValue.config.Value)
                    {
                        CardManager.EnableCard(cardValue.cardInfo);
                    }
                    else
                    {
                        CardManager.DisableCard(cardValue.cardInfo);
                    }
                    UpdateVisualsCardObj(crdObj, cardValue.config.Value);
                }

                var viewingText = toggleCardsCanvas.transform.Find("CardMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

                // Create category buttons
                // sort categories
                // always have Vanilla first, then sort most cards -> least cards, followed by "Modded" at the end (if it exists)
                List<string> sortedCategories = (new string[] { "Vanilla" }).Concat(CardManager.categories.OrderByDescending(x => CardManager.GetCardsInCategory(x).Count()).ThenBy(x => x).Except(new string[] { "Vanilla", "Modded" })).ToList();
                if (CardManager.categories.Contains("Modded"))
                {
                    sortedCategories.Add("Modded");
                }
                foreach (var category in sortedCategories)
                {
                    var categoryObj = Instantiate(categoryButtonAsset, categoryContent);
                    categoryObj.SetActive(true);
                    categoryObj.name = category;
                    categoryObj.GetComponentInChildren<TextMeshProUGUI>().text = category;
                    categoryObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        foreach (var scroll in ScrollViews)
                        {
                            SetActive(scroll.Value, false);
                        }

                        ScrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        SetActive(ScrollViews[category].transform, true);

                        viewingText.text = "Viewing: " + category;
                    });
                    var toggle = categoryObj.GetComponentInChildren<Toggle>();
                    TogglesToDisable.Add(toggle);
                    toggle.onValueChanged.AddListener(value =>
                    {
                        if (!value)
                        {
                            UpdateCategoryVisuals(false);
                            CardManager.DisableCategory(category);
                        }
                        else
                        {
                            UpdateCategoryVisuals(true);
                            CardManager.EnableCategory(category);
                        }
                    });

                    void UpdateCategoryVisuals(bool enabled, bool firstTime = false)
                    {
                        foreach (var obj in ScrollViews.Where(obj => obj.Key == category))
                        {
                            obj.Value.Find("Darken").gameObject.SetActive(!enabled);
                            if (enabled)
                            {
                                CardManager.categoryBools[category].Value = true;
                                if (firstTime) { continue; }
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (!trs.Find("Darken/Darken").gameObject.activeInHierarchy)
                                    {
                                        CardManager.EnableCard(CardManager.GetCardInfoWithName(trs.name), true);
                                    }
                                }
                            }
                            else
                            {
                                CardManager.categoryBools[category].Value = false;
                                if (firstTime) { continue; }
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (!trs.Find("Darken/Darken").gameObject.activeInHierarchy)
                                    {
                                        CardManager.DisableCard(CardManager.GetCardInfoWithName(trs.name), true);
                                    }
                                }
                            }
                        }
                        if (!firstTime)
                        {
                            string[] cardsInCategory = CardManager.GetCardsInCategory(category);
                            foreach (GameObject cardObj in CardObjs.Keys.Where(o => cardsInCategory.Contains(o.name)))
                            {
                                UpdateVisualsCardObj(cardObj, enabled);
                            }
                        }

                        toggle.isOn = CardManager.IsCategoryActive(category);
                    }

                    UpdateCategoryVisuals(CardManager.IsCategoryActive(category), true);
                }
                for (var i = 0; i < CardObjs.Keys.Count; i++)
                {
                    var buttonEvent = new Button.ButtonClickedEvent();
                    var unityAction = new UnityAction(CardObjs.ElementAt(i).Value);
                    buttonEvent.AddListener(unityAction);
                    CardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
                }
                toggleCardsCanvas.SetActive(false);
            });
        }

        private static List<GameObject> FindObjectsInChildren(GameObject gameObject, string gameObjectName)
        {
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
            return (from item in children where item.name == gameObjectName select item.gameObject).ToList();
        }

        private static GameObject FindObjectInChildren(GameObject gameObject, string gameObjectName)
        {
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
            return (from item in children where item.name == gameObjectName select item.gameObject).FirstOrDefault();
        }

        public static void UpdateVisualsCardObj(GameObject cardObj, bool cardEnabled)
        {
            if (cardEnabled)
            {
                cardObj.transform.Find("Darken/Darken").gameObject.SetActive(false);
                foreach (CurveAnimation curveAnimation in cardObj.GetComponentsInChildren<CurveAnimation>())
                {
                    if (curveAnimation.gameObject.activeInHierarchy)
                    {
                        curveAnimation.PlayIn();
                    }
                }
            }
            else
            {
                cardObj.transform.Find("Darken/Darken").gameObject.SetActive(true);
                foreach (CurveAnimation curveAnimation in cardObj.GetComponentsInChildren<CurveAnimation>())
                {
                    if (curveAnimation.gameObject.activeInHierarchy)
                    {
                        curveAnimation.PlayOut();
                    }
                }
            }
        }

        internal void SortCardMenus(bool alph)
        {
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = ScrollViews[category].Find("Viewport/Content");

                List<Transform> cardsInMenu = new List<Transform>() { };
                cardsInMenu.AddRange(categoryMenu.Cast<Transform>());

                List<Transform> sorted = alph ? cardsInMenu.OrderBy(t => t.name).ToList() : cardsInMenu.OrderBy(t => CardManager.cards[t.name].cardInfo.rarity).ThenBy(t => t.name).ToList();

                int i = 0;
                foreach (Transform cardInMenu in sorted)
                {
                    cardInMenu.SetSiblingIndex(i);
                    i++;
                }
            }
        }

        private static void ChangeCardColumnAmountMenus(int amount)
        {
            Vector2 cellSize = new Vector2(221, 312);
            Vector3 localScale = new Vector3(0.17f, 0.17f, 1);
            int y = -10;

            if (amount > 3)
            {
                switch (amount)
                {
                    case 4:
                        {
                            cellSize = new Vector2(170, 240);
                            localScale = new Vector3(0.13f, 0.13f, 1);
                            break;
                        }
                    default:
                    case 5:
                        {
                            cellSize = new Vector2(136, 192);
                            localScale = new Vector3(0.1f, 0.1f, 1);
                            break;
                        }
                    case 6:
                        {
                            cellSize = new Vector2(112, 158);
                            localScale = new Vector3(0.08f, 0.08f, 1);
                            y = -5;
                            break;
                        }
                    case 7:
                        {
                            cellSize = new Vector2(97, 137);
                            localScale = new Vector3(0.065f, 0.065f, 1);
                            y = -4;
                            break;
                        }
                    case 8:
                        {
                            cellSize = new Vector2(85, 120);
                            localScale = new Vector3(0.055f, 0.055f, 1);
                            y = -3;
                            break;
                        }
                    case 9:
                        {
                            cellSize = new Vector2(75, 106);
                            localScale = new Vector3(0.045f, 0.045f, 1);
                            y = -2;
                            break;
                        }
                    case 10:
                        {
                            cellSize = new Vector2(68, 96);
                            localScale = new Vector3(0.04f, 0.04f, 1);
                            y = -2;
                            break;
                        }
                }
            }

            cardAmountText.text = "Cards Per Line: " + amount;
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = ScrollViews[category].Find("Viewport/Content");
                var gridLayout = categoryMenu.gameObject.GetComponent<GridLayoutGroup>();
                gridLayout.cellSize = cellSize;
                gridLayout.constraintCount = amount;
                gridLayout.childAlignment = TextAnchor.UpperCenter;

                List<Transform> cardsInMenu = new List<Transform>() { };
                cardsInMenu.AddRange(categoryMenu.Cast<Transform>());

                foreach (Transform cardTransform in cardsInMenu)
                {
                    Transform cardTransformChild = cardTransform.GetChild(1);
                    cardTransformChild.localScale = localScale;
                    cardTransformChild.localPosition = new Vector3(0, y, 1);
                }
            }
        }

        /// <summary> This is used for opening and closing menus </summary>
        public static void SetActive(Transform trans, bool active)
        {
            // Main camera changes when going back to menu and glow disappears if we don't se the camera again to the canvas
            Camera mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
            Canvas canvas = toggleCardsCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

            trans.gameObject.SetActive(active);
            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                foreach (var card in CardObjs)
                {
                    UpdateVisualsCardObj(card.Key, CardManager.cards[card.Key.name].enabled);
                }
            });
        }

        /// <summary>This method allows you to opens the menu with settings from outside unbound</summary>
        /// <param name="escape"> disable closing the menu when you press escape</param>
        /// <param name="toggleAll"> disable the toggleAll button</param>
        /// <param name="buttonActions"> actions for all the card buttons if null will use current actions</param>
        /// <param name="interactionDisabledCards"> array of cardNames of cards that need their interactivity disabled</param>
        public static void Open(bool escape, bool toggleAll, Action[] buttonActions = null, string[] interactionDisabledCards = null)
        {
            menuOpenFromOutside = true;
            SetActive(toggleCardsCanvas.transform, true);
            disableEscapeButton = escape;
            EnableButtonsMethod();
            toggleCardsCanvas.transform.Find("CardMenu/Top/Help")?.gameObject.SetActive(false);

            if (toggleAll) toggleCardsCanvas.transform.Find("CardMenu/Top/ToggleAll").gameObject.SetActive(false);

            foreach (Transform trans in toggleCardsCanvas.transform.Find(
                "CardMenu/Top/Categories/ButtonsScroll/Viewport/Content"))
            {
                trans.Find("Toggle").gameObject.SetActive(false);
            }
            foreach (Transform trans in toggleCardsCanvas.transform.Find(
                "CardMenu/ScrollViews"))
            {
                trans.Find("Darken").gameObject.SetActive(false);
            }

            foreach (var card in CardObjs)
            {
                UpdateVisualsCardObj(card.Key, true);
            }

            if (buttonActions != null) SetAllButtonActions(buttonActions);

            EnableButtonsMethod();

            if (interactionDisabledCards != null)
            {
                foreach (var card in interactionDisabledCards)
                {
                    var obj = GetCardObj(card);
                    if (obj == null) throw new ArgumentNullException("obj", '"' + card + '"' + " is not a valid card name");
                    obj.GetComponent<Button>().interactable = false;
                    obj.transform.Find("Darken/Darken").gameObject.SetActive(true);
                }
            }

            for (int i = 0; i < CardObjs.Keys.Count; i++)
            {
                var buttonEvent = new Button.ButtonClickedEvent();
                var unityAction = new UnityAction(CardObjs.ElementAt(i).Value);
                buttonEvent.AddListener(unityAction);
                CardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
            }
        }

        public static void Close()
        {
            menuOpenFromOutside = false;
            SetActive(toggleCardsCanvas.transform, false);
            disableEscapeButton = false;
            DisableButtonsMethod();
            toggleCardsCanvas.transform.Find("CardMenu/Top/Help").gameObject.SetActive(true);
            toggleCardsCanvas.transform.Find("CardMenu/Top/ToggleAll").gameObject.SetActive(true);
            ResetCardActions();
            foreach (Transform trans in toggleCardsCanvas.transform.Find(
                "CardMenu/Top/Categories/ButtonsScroll/Viewport/Content"))
            {
                trans.Find("Toggle").gameObject.SetActive(true);
            }
            foreach (var card in CardObjs)
            {
                UpdateVisualsCardObj(card.Key, CardManager.cards[card.Key.name].enabled);
            }
            foreach (Transform trans in toggleCardsCanvas.transform.Find(
                "CardMenu/ScrollViews"))
            {
                trans.Find("Darken").gameObject.SetActive(!CardManager.categoryBools[trans.name].Value);
            }

            for (int i = 0; i < CardObjs.Keys.Count; i++)
            {
                var buttonEvent = new Button.ButtonClickedEvent();
                var unityAction = new UnityAction(CardObjs.ElementAt(i).Value);
                buttonEvent.AddListener(unityAction);
                CardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
            }
        }

        public static GameObject[] GetCardObjs(string[] cardNames)
        {
            return cardNames.Select(GetCardObj).ToArray();
        }

        public static GameObject GetCardObj(string cardName)
        {
            return CardObjs.FirstOrDefault(obj => obj.Key.name == cardName).Key;
        }

        public static void SetAllButtonActions(Action[] actions)
        {
            for (var i = 0; i < CardObjs.Count; i++)
            {
                var obj = CardObjs.ElementAt(i).Key;
                CardObjs[obj] = actions[i];
            }
        }

        public static void ResetCardActions()
        {
            for (var i = 0; i < CardObjs.Count; i++)
            {
                var obj = CardObjs.ElementAt(i).Key;
                CardObjs[obj] = DefaultCardActions[i];
            }
        }

        public static int GetActionIndex(GameObject cardObj)
        {
            for (var i = 0; i < CardObjs.Count; i++)
            {
                var obj = CardObjs.ElementAt(i).Key;
                if (obj == cardObj)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void EnableButtonsMethod()
        {
            foreach (var button in ButtonsToDisable)
            {
                button.interactable = true;
            }
            foreach (var toggle in TogglesToDisable)
            {
                toggle.interactable = true;
            }
            disabled = false;
        }

        private static void DisableButtonsMethod()
        {
            foreach (var button in ButtonsToDisable)
            {
                button.interactable = false;
            }
            foreach (var toggle in TogglesToDisable)
            {
                toggle.interactable = false;
            }
            disabled = true;
        }

        internal static void RestoreCardToggleVisuals()
        {
            foreach (GameObject cardObj in CardObjs.Keys)
            {
                UpdateVisualsCardObj(cardObj, CardManager.cards[cardObj.name].config.Value);
            }
        }

        private void Update()
        {
            if (menuOpenFromOutside) return;
            switch (GameManager.instance.isPlaying)
            {
                case true when !disabled:
                    DisableButtonsMethod();
                    break;
                case false when disabled:
                    EnableButtonsMethod();
                    break;
            }
        }
    }
}