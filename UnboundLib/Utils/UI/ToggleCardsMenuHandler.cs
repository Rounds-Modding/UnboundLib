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

        public static readonly Dictionary<GameObject, Action> cardObjs = new Dictionary<GameObject, Action>();
        public static readonly List<Action> defaultCardActions = new List<Action>();

        private static readonly List<Button> ButtonsToDisable = new List<Button>();
        private static readonly List<Toggle> TogglesToDisable = new List<Toggle>();

        public static GameObject cardMenuCanvas;

        private GameObject cardObjAsset;
        private GameObject cardScrollViewAsset;
        private GameObject categoryButtonAsset;

        private Transform scrollViewTrans;
        private Transform categoryContent;

        public static bool disableEscapeButton;

        public static bool menuOpenFromOutside;

        private static bool disabled;

        private static bool sortedByName = true;

        private static TextMeshProUGUI cardAmountText;

        private static int currentColumnAmount = 5;
        private static string CurrentCategory => (from scroll in ScrollViews where scroll.Value.gameObject.activeInHierarchy select scroll.Key).FirstOrDefault();

        // if need to toggle all on or off
        private bool toggledAll;

        private void Start()
        {
            instance = this;
            var mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();

            var cardMenu = Unbound.toggleUI.LoadAsset<GameObject>("CardMenuCanvas");

            cardObjAsset = Unbound.toggleUI.LoadAsset<GameObject>("CardObj");

            cardScrollViewAsset = Unbound.toggleUI.LoadAsset<GameObject>("CardScrollView");
            categoryButtonAsset = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");

            cardMenuCanvas = Instantiate(cardMenu);
            DontDestroyOnLoad(cardMenuCanvas);

            var canvas = cardMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            cardMenuCanvas.SetActive(false);

            scrollViewTrans = cardMenuCanvas.transform.Find("CardMenu/ScrollViews");

            categoryContent = cardMenuCanvas.transform.Find("CardMenu/Top/Categories/ButtonsScroll/Viewport/Content");

            // Create and set search bar
            var searchBar = cardMenuCanvas.transform.Find("CardMenu/Top/InputField").gameObject;
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
            cardMenuCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");
            var sortButton = cardMenuCanvas.transform.Find("CardMenu/Top/SortBy").GetComponent<Button>();
            sortButton.onClick.AddListener(() =>
            {
                sortedByName = !sortedByName;
                cardMenuCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");

                SortCardMenus(sortedByName);
            });

            Transform cardAmountObject = cardMenuCanvas.transform.Find("CardMenu/Top/CardAmount");
            cardAmountText = cardAmountObject.GetComponentInChildren<TextMeshProUGUI>();

            var cardAmountSlider = cardAmountObject.GetComponentsInChildren<Slider>();
            foreach (Slider slider in cardAmountSlider)
            {
                slider.onValueChanged.AddListener(amount =>
                {
                    int integerAmount = (int) amount;
                    ChangeCardColumnAmountMenus(integerAmount);
                });
            }

            // Create and set toggle all button
            var toggleAllButton = cardMenuCanvas.transform.Find("CardMenu/Top/ToggleAll").GetComponent<Button>();
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
                        cardObjs[obj].Invoke();
                    }
                }
                else
                {
                    var objs = GetCardObjs(cardsInCategory);
                    foreach (var obj in objs)
                    {
                        CardManager.cards[obj.name].enabled = true;
                        cardObjs[obj].Invoke();
                    }
                }
            });

            // get and set info button
            var infoButton = cardMenuCanvas.transform.Find("CardMenu/Top/Help").GetComponent<Button>();
            var infoMenu = cardMenuCanvas.transform.Find("CardMenu/InfoMenu").gameObject;
            infoButton.onClick.AddListener(() =>
            {
                infoMenu.SetActive(!infoMenu.activeInHierarchy);
            });

            this.ExecuteAfterSeconds(0.5f, () =>
            {
                cardMenuCanvas.SetActive(true);
                // Create category scrollViews
                foreach (var category in CardManager.categories)
                {
                    var scrollView = Instantiate(cardScrollViewAsset, scrollViewTrans);
                    scrollView.SetActive(true);
                    SetActive(scrollView.transform, false);
                    scrollView.name = category;
                    ScrollViews.Add(category, scrollView.transform);
                    if (category == "Vanilla")
                    {
                        SetActive(scrollView.transform, true);
                    }
                }

                // Create cardObjects
                foreach (var card in CardManager.cards)
                {
                    Card cardValue = card.Value;
                    var parentScroll = ScrollViews[cardValue.category].Find("Viewport/Content");
                    var crdObj = Instantiate(cardObjAsset, parentScroll);

                    crdObj.name = card.Key;

                    if (cardValue != null)
                    {
                        // UnityEngine.Debug.Log("CardValue not null: " + card.Key);
                        CardInfo cardInfo = cardValue.cardInfo;
                        if (cardInfo != null)
                        {
                            // UnityEngine.Debug.Log("CardInfo not null: " + card.Key);
                            SetupCardVisuals(cardInfo, crdObj); //Instantiate(cardInfo, crdObj.transform);
                        }
                    }

                    void CardAction()
                    {
                        if (cardValue.enabled)
                        {
                            CardManager.DisableCard(cardValue.cardInfo);
                            UpdateVisualsCardObj(crdObj, false);
                        }
                        else
                        {
                            CardManager.EnableCard(cardValue.cardInfo);
                            UpdateVisualsCardObj(crdObj, true);
                        }
                    }

                    cardObjs[crdObj] = CardAction;
                    defaultCardActions.Add(CardAction);

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

                UpdateCardColumnAmountMenus();

                var viewingText = cardMenuCanvas.transform.Find("CardMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

                // Create category buttons
                // sort categories
                // always have Vanilla first, then sort most cards -> least cards, followed by "Modded" at the end (if it exists)
                List<string> sortedCategories = new[] { "Vanilla" }.Concat(CardManager.categories.OrderByDescending(x => CardManager.GetCardsInCategory(x).Length).ThenBy(x => x).Except(new[] { "Vanilla", "Modded" })).ToList();
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

                    void UpdateCategoryVisuals(bool enabledVisuals, bool firstTime = false)
                    {
                        foreach (var obj in ScrollViews.Where(obj => obj.Key == category))
                        {
                            obj.Value.Find("Darken").gameObject.SetActive(!enabledVisuals);
                            if (enabledVisuals)
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
                            foreach (GameObject cardObj in cardObjs.Keys.Where(o => cardsInCategory.Contains(o.name)))
                            {
                                UpdateVisualsCardObj(cardObj, enabledVisuals);
                            }
                        }

                        toggle.isOn = CardManager.IsCategoryActive(category);
                    }

                    UpdateCategoryVisuals(CardManager.IsCategoryActive(category), true);
                }
                for (var i = 0; i < cardObjs.Keys.Count; i++)
                {
                    var buttonEvent = new Button.ButtonClickedEvent();
                    var unityAction = new UnityAction(cardObjs.ElementAt(i).Value);
                    buttonEvent.AddListener(unityAction);
                    cardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
                }
                cardMenuCanvas.SetActive(false);
            });
        }

        private static List<Transform> GetAllChildren(Transform parent, List<Transform> transformList = null)
        {
            if (transformList == null) transformList = new List<Transform>();

            foreach (Transform child in parent)
            {
                transformList.Add(child);
                GetAllChildren(child, transformList);
            }
            return transformList;
        }

        private static void SetupCardVisuals(CardInfo cardInfo, GameObject parent)
        {
            GameObject cardObject = Instantiate(cardInfo.gameObject, parent.gameObject.transform);
            cardObject.AddComponent<MenuCard>();
            cardObject.SetActive(true);

            GameObject cardFrontObject = FindObjectInChildren(cardObject, "Front");
            if (cardFrontObject == null) return;

            GameObject back = FindObjectInChildren(cardObject, "Back");
            Destroy(back);

            foreach (CardVisuals componentsInChild in cardObject.GetComponentsInChildren<CardVisuals>())
            {
                componentsInChild.firstValueToSet = true;
            }

            FindObjectInChildren(cardObject, "BlockFront")?.SetActive(false);

            var canvasGroups = cardObject.GetComponentsInChildren<CanvasGroup>();
            foreach (var canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = 1;
            }

            // // Creates problems if it's not in the game scene and also is the main cause of lag
            GameObject uiParticleObject = FindObjectInChildren(cardFrontObject.gameObject, "UI_ParticleSystem");
            if (uiParticleObject != null)
            {
                Destroy(uiParticleObject);
            }

            if (cardInfo.cardArt != null)
            {
                var artObject = FindObjectInChildren(cardFrontObject.gameObject, "Art");
                if (artObject != null)
                {
                    var cardAnimationHandler = cardObject.AddComponent<CardAnimationHandler>();
                    cardAnimationHandler.ToggleAnimation(false);
                }
            }

            var backgroundObj = FindObjectInChildren(cardFrontObject.gameObject, "Background");
            if (backgroundObj == null) return;

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

            RectTransform rect = cardObject.GetOrAddComponent<RectTransform>();
            rect.localScale = 8f * Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var cardColor = CardChoice.instance.GetCardColor(cardInfo.colorTheme);
            var edgePieces = cardFrontObject.GetComponentsInChildren<Image>(true)
                .Where(x => x.gameObject.transform.name.Contains("FRAME")).ToList();
            foreach (Image edgePiece in edgePieces)
            {
                edgePiece.color = cardColor;
            }

            var textName = cardFrontObject.transform.GetChild(1);
            if (textName != null)
            {
                var textComponent = textName.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = cardInfo.cardName.ToUpper();
                    textComponent.color = cardColor;
                }
            }

            if (cardInfo.rarity == CardInfo.Rarity.Common) return;

            var colorFromRarity = cardInfo.rarity == CardInfo.Rarity.Uncommon
                ? uncommonColor
                : rareColor;
            foreach (var imageComponentLoop in FindObjectsInChildren(cardFrontObject.gameObject,
                             "Triangle").Select(triangleObject =>
                             triangleObject.GetComponent<Image>())
                         .Where(imageComponentLoop => imageComponent != null))
            {
                imageComponentLoop.color = colorFromRarity;
            }
        }
        internal static Color uncommonColor = new Color(0, 0.5f, 1, 1);
        internal static Color rareColor = new Color(1, 0.2f, 1, 1);

        private static IEnumerable<GameObject> FindObjectsInChildren(GameObject gameObject, string gameObjectName)
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
            Vector2 cellSize = new Vector2(220, 300);
            float localScale = 1.5f;

            if (amount > 3)
            {
                switch (amount)
                {
                    case 4:
                        {
                            cellSize = new Vector2(170, 240);
                            localScale = 1.2f;
                            break;
                        }
                    default:
                        {
                            cellSize = new Vector2(136, 192);
                            localScale = 0.9f;
                            break;
                        }
                    case 6:
                        {
                            cellSize = new Vector2(112, 158);
                            localScale = 0.75f;
                            break;
                        }
                    case 7:
                        {
                            cellSize = new Vector2(97, 137);
                            localScale = 0.65f;
                            break;
                        }
                    case 8:
                        {
                            cellSize = new Vector2(85, 120);
                            localScale = 0.55f;
                            break;
                        }
                    case 9:
                        {
                            cellSize = new Vector2(75, 106);
                            localScale = 0.45f;
                            break;
                        }
                    case 10:
                        {
                            cellSize = new Vector2(68, 96);
                            localScale = 0.4f;
                            break;
                        }
                }
            }
            currentColumnAmount = amount;
            cardAmountText.text = "Cards Per Line: " + amount;
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = ScrollViews[category].Find("Viewport/Content");
                var gridLayout = categoryMenu.gameObject.GetComponent<GridLayoutGroup>();
                gridLayout.cellSize = cellSize;
                gridLayout.constraintCount = amount;
                gridLayout.childAlignment = TextAnchor.UpperCenter;

                List<Transform> cardsInMenu = new List<Transform>();
                cardsInMenu.AddRange(categoryMenu.Cast<Transform>());

                foreach (var rect in cardsInMenu.Select(cardTransform => cardTransform.GetChild(2).gameObject.GetOrAddComponent<RectTransform>()))
                {
                    rect.localScale = localScale * Vector3.one * 10;
                }
            }
        }

        public static void UpdateCardColumnAmountMenus()
        {
            ChangeCardColumnAmountMenus(currentColumnAmount);
        }

        /// <summary> This is used for opening and closing menus </summary>
        public static void SetActive(Transform trans, bool active)
        {
            // Main camera changes when going back to menu and glow disappears if we don't se the camera again to the canvas
            Camera mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
            Canvas canvas = cardMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

            if (trans.gameObject != null)
            {
                trans.gameObject.SetActive(active);
            }

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                foreach (var card in cardObjs)
                {
                    UpdateVisualsCardObj(card.Key, CardManager.cards[card.Key.name].enabled);
                }
            });
        }

        /// <summary>This method allows you to opens the menu with settings from outside unbound</summary>
        /// <param name="escape"> disable closing the menu when you press escape</param>
        /// <param name="toggleAll"> disable the toggleAll button</param>
        /// <param name="buttonActions"> actions for all the cardInfo buttons if null will use current actions</param>
        /// <param name="interactionDisabledCards"> array of cardNames of cards that need their interactivity disabled</param>
        public static void Open(bool escape, bool toggleAll, Action[] buttonActions = null, string[] interactionDisabledCards = null)
        {
            menuOpenFromOutside = true;
            SetActive(cardMenuCanvas.transform, true);
            disableEscapeButton = escape;
            EnableButtonsMethod();
            cardMenuCanvas.transform.Find("CardMenu/Top/Help")?.gameObject.SetActive(false);

            if (toggleAll) cardMenuCanvas.transform.Find("CardMenu/Top/ToggleAll").gameObject.SetActive(false);

            foreach (Transform trans in cardMenuCanvas.transform.Find(
                "CardMenu/Top/Categories/ButtonsScroll/Viewport/Content"))
            {
                trans.Find("Toggle").gameObject.SetActive(false);
            }
            foreach (Transform trans in cardMenuCanvas.transform.Find(
                "CardMenu/ScrollViews"))
            {
                trans.Find("Darken").gameObject.SetActive(false);
            }

            foreach (var card in cardObjs)
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
                    if (obj == null) throw new ArgumentNullException("obj", '"' + card + '"' + " is not a valid cardInfo name");
                    obj.GetComponent<Button>().interactable = false;
                    obj.transform.Find("Darken/Darken").gameObject.SetActive(true);
                }
            }

            for (int i = 0; i < cardObjs.Keys.Count; i++)
            {
                var buttonEvent = new Button.ButtonClickedEvent();
                var unityAction = new UnityAction(cardObjs.ElementAt(i).Value);
                buttonEvent.AddListener(unityAction);
                cardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
            }
        }

        public static void Close()
        {
            menuOpenFromOutside = false;
            SetActive(cardMenuCanvas.transform, false);
            disableEscapeButton = false;
            DisableButtonsMethod();
            cardMenuCanvas.transform.Find("CardMenu/Top/Help").gameObject.SetActive(true);
            cardMenuCanvas.transform.Find("CardMenu/Top/ToggleAll").gameObject.SetActive(true);
            ResetCardActions();
            foreach (Transform trans in cardMenuCanvas.transform.Find(
                "CardMenu/Top/Categories/ButtonsScroll/Viewport/Content"))
            {
                trans.Find("Toggle").gameObject.SetActive(true);
            }
            foreach (var card in cardObjs)
            {
                UpdateVisualsCardObj(card.Key, CardManager.cards[card.Key.name].enabled);
            }
            foreach (Transform trans in cardMenuCanvas.transform.Find(
                "CardMenu/ScrollViews"))
            {
                trans.Find("Darken").gameObject.SetActive(!CardManager.categoryBools[trans.name].Value);
            }

            for (int i = 0; i < cardObjs.Keys.Count; i++)
            {
                var buttonEvent = new Button.ButtonClickedEvent();
                var unityAction = new UnityAction(cardObjs.ElementAt(i).Value);
                buttonEvent.AddListener(unityAction);
                cardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
            }
        }

        public static GameObject[] GetCardObjs(string[] cardNames)
        {
            return cardNames.Select(GetCardObj).ToArray();
        }

        public static GameObject GetCardObj(string cardName)
        {
            return cardObjs.FirstOrDefault(obj => obj.Key.name == cardName).Key;
        }

        public static void SetAllButtonActions(Action[] actions)
        {
            for (var i = 0; i < cardObjs.Count; i++)
            {
                var obj = cardObjs.ElementAt(i).Key;
                cardObjs[obj] = actions[i];
            }
        }

        public static void ResetCardActions()
        {
            for (var i = 0; i < cardObjs.Count; i++)
            {
                var obj = cardObjs.ElementAt(i).Key;
                cardObjs[obj] = defaultCardActions[i];
            }
        }

        public static int GetActionIndex(GameObject cardObj)
        {
            for (var i = 0; i < cardObjs.Count; i++)
            {
                var obj = cardObjs.ElementAt(i).Key;
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
            foreach (GameObject cardObj in cardObjs.Keys)
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