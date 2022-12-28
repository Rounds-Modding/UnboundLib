using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnboundLib.Cards;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnboundLib.Utils.UI
{
    public class ToggleCardsMenuHandler : MonoBehaviour
    {
        public static ToggleCardsMenuHandler instance;

        private static readonly Dictionary<string, Transform> scrollViews = new Dictionary<string, Transform>();

        public static readonly Dictionary<GameObject, Action> cardObjs = new Dictionary<GameObject, Action>();
        public static readonly List<Action> defaultCardActions = new List<Action>();

        private readonly List<Button> buttonsToDisable = new List<Button>();
        private readonly List<Toggle> togglesToDisable = new List<Toggle>();
        private readonly Dictionary<string, List<GameObject>> cardObjectsInCategory = new Dictionary<string, List<GameObject>>();

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

        private int currentColumnAmount = 5;
        private string currentCategory = "Vanilla";
        private string currentSearch = "";

        // if need to toggle all on or off
        private bool toggledAll;
        private Coroutine cardVisualsCoroutine = null;

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
                currentSearch = value;
                foreach (var card in scrollViews[currentCategory].GetComponentsInChildren<Button>(true))
                {
                    var active = ActiveOnSearch(card.gameObject.name);
                    card.gameObject.SetActive(active);
                    if (active)
                    {
                        UpdateVisualsCardObj(card.gameObject);
                    }
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
            buttonsToDisable.Add(toggleAllButton);
            toggleAllButton.onClick.AddListener(() =>
            {
                if (currentCategory == null) return;

                toggledAll = !toggledAll;

                var cardsInCategory = CardManager.GetCardsInCategory(currentCategory);
                if (toggledAll)
                {
                    var objectsInCategory = GetCardObjects(cardsInCategory);
                    foreach (var cardObject in objectsInCategory)
                    {
                        CardManager.cards[cardObject.name].enabled = false;
                        cardObjs[cardObject].Invoke();
                    }
                }
                else
                {
                    var objectsInCategory = GetCardObjects(cardsInCategory);
                    foreach (var cardObject in objectsInCategory)
                    {
                        CardManager.cards[cardObject.name].enabled = true;
                        cardObjs[cardObject].Invoke();
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
                    scrollViews.Add(category, scrollView.transform);
                    if (category == "Vanilla")
                    {
                        SetActive(scrollView.transform, true);
                    }
                }

                // Create cardObjects
                foreach (var card in CardManager.cards)
                {
                    Card cardValue = card.Value;
                    if (cardValue == null) continue;
                    var parentScroll = scrollViews[cardValue.category].Find("Viewport/Content");
                    var cardObject = Instantiate(cardObjAsset, parentScroll);
                    cardObject.name = card.Key;
                    CardInfo cardInfo = cardValue.cardInfo;
                    if (cardInfo == null) continue;
                    SetupCardVisuals(cardInfo, cardObject);
                    cardObject.SetActive(false);
                    if (!cardObjectsInCategory.ContainsKey(cardValue.category))
                    {
                        cardObjectsInCategory.Add(cardValue.category, new List<GameObject>());
                    }
                    cardObjectsInCategory[cardValue.category].Add(cardObject);

                    void CardAction()
                    {
                        if (cardValue.enabled)
                        {
                            CardManager.DisableCard(cardValue.cardInfo);
                        }
                        else
                        {
                            CardManager.EnableCard(cardValue.cardInfo);
                        }
                        UpdateVisualsCardObj(cardObject);
                    }

                    cardObjs[cardObject] = CardAction;
                    defaultCardActions.Add(CardAction);

                    buttonsToDisable.Add(cardObject.GetComponent<Button>());

                    if (cardValue.config.Value)
                    {
                        CardManager.EnableCard(cardValue.cardInfo);
                    }
                    else
                    {
                        CardManager.DisableCard(cardValue.cardInfo);
                    }
                    UpdateVisualsCardObj(cardObject);
                }
                UpdateCardColumnAmountMenus();

                var viewingText = cardMenuCanvas.transform.Find("CardMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

                // Create category buttons
                // sort categories
                // always have Vanilla first, then sort most cards -> least cards, followed by "Modded" at the end (if it exists)
                List<string> sortedCategories = new[] { "Vanilla" }.Concat(CardManager.categories.OrderBy(x => x).Except(new[] { "Vanilla" })).ToList();
                
                foreach (var category in sortedCategories)
                {
                    var categoryObj = Instantiate(categoryButtonAsset, categoryContent);
                    categoryObj.SetActive(true);
                    categoryObj.name = category;
                    categoryObj.GetComponentInChildren<TextMeshProUGUI>().text = category;
                    categoryObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        string categoryText = "Viewing: " + category;
                        if (viewingText.text == categoryText) return;
                        viewingText.text = categoryText;

                        foreach (var scroll in scrollViews)
                        {
                            DisableCardsInCategory(scroll.Key);
                            SetActive(scroll.Value, false);
                        }

                        scrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        SetActive(scrollViews[category].transform, true);

                        // cardVisualsCoroutine = Unbound.Instance.StartCoroutine(EnableCardsInCategory(category));
                        currentCategory = category;
                    });

                    var toggle = categoryObj.GetComponentInChildren<Toggle>();
                    togglesToDisable.Add(toggle);
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
                        foreach (var cardObject in scrollViews.Where(obj => obj.Key == category))
                        {
                            cardObject.Value.Find("Darken").gameObject.SetActive(!enabledVisuals);
                            if (enabledVisuals)
                            {
                                CardManager.categoryBools[category].Value = true;
                                if (firstTime) { continue; }
                                foreach (Transform cardTransform in cardObject.Value.Find("Viewport/Content"))
                                {
                                    if (!cardTransform.Find("Darken/Darken").gameObject.activeInHierarchy)
                                    {
                                        CardManager.EnableCard(CardManager.GetCardInfoWithName(cardTransform.name));
                                    }
                                }
                            }
                            else
                            {
                                CardManager.categoryBools[category].Value = false;
                                if (firstTime) { continue; }
                                foreach (Transform cardTransform in cardObject.Value.Find("Viewport/Content"))
                                {
                                    if (!cardTransform.Find("Darken/Darken").gameObject.activeInHierarchy)
                                    {
                                        CardManager.DisableCard(CardManager.GetCardInfoWithName(cardTransform.name));
                                    }
                                }
                            }
                        }
                        if (!firstTime)
                        {
                            string[] cardsInCategory = CardManager.GetCardsInCategory(category);
                            foreach (GameObject cardObject in cardObjs.Keys.Where(o => cardsInCategory.Contains(o.name)))
                            {
                                UpdateVisualsCardObj(cardObject);
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

        private bool ActiveOnSearch(string cardName)
        {
            var result = cardName.Contains("__") ? cardName.Split(new[] { "__" }, StringSplitOptions.None) : new[] { cardName };
            var process = result.Length > 2 ? result[2] : result[0];
            return currentSearch == "" || process.ToUpper().Contains(currentSearch.ToUpper());
        }

        private void DisableCards()
        {
            foreach (GameObject cardObject in CardManager.categories.SelectMany(category => cardObjectsInCategory[category]))
            {
                cardObject.SetActive(false);
            }
        }

        private void DisableCardsInCategory(string category)
        {
            if (!cardObjectsInCategory.ContainsKey(category)) return;
            foreach (GameObject cardObject in cardObjectsInCategory[category])
            {
                cardObject.SetActive(false);
            }
        }

        private IEnumerator EnableCardsInCategory(string category)
        {
            if (!cardObjectsInCategory.ContainsKey(category)) yield break;
            foreach (GameObject cardObject in cardObjectsInCategory[category])
            {
                var active = ActiveOnSearch(cardObject.name);
                cardObject.gameObject.SetActive(active);
                UpdateVisualsCardObj(cardObject);
                yield return new WaitForEndOfFrame();
            }
        }

        internal static Color uncommonColor = new Color(0, 0.5f, 1, 1);
        internal static Color rareColor = new Color(1, 0.2f, 1, 1);

        private static void SetupCardVisuals(CardInfo cardInfo, GameObject parent)
        {
            GameObject cardObject = Instantiate(cardInfo.gameObject, parent.gameObject.transform);
            cardObject.AddComponent<MenuCard>();
            cardObject.SetActive(true);

            GameObject cardFrontObject = FindObjectInChildren(cardObject, "Front");
            if (cardFrontObject == null) return;

            // cardInfo.gameObject.name = parent.name;
            GameObject back = FindObjectInChildren(cardObject, "Back");
            Destroy(back);

            GameObject damagable = FindObjectInChildren(cardObject, "Damagable");
            Destroy(damagable);

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

        public static void UpdateVisualsCardObj(GameObject cardObject, bool? cardEnabled = null)
        {
            if (cardEnabled ?? CardManager.cards[cardObject.name].enabled)
            {
                cardObject.transform.Find("Darken/Darken").gameObject.SetActive(false);
                foreach (CurveAnimation curveAnimation in cardObject.GetComponentsInChildren<CurveAnimation>())
                {
                    if (curveAnimation.gameObject.activeInHierarchy)
                    {
                        curveAnimation.PlayIn();
                    }
                }
            }
            else
            {
                cardObject.transform.Find("Darken/Darken").gameObject.SetActive(true);
                foreach (CurveAnimation curveAnimation in cardObject.GetComponentsInChildren<CurveAnimation>())
                {
                    if (!curveAnimation.gameObject.activeInHierarchy) continue;
                    curveAnimation.PlayIn();
                    curveAnimation.PlayOut();
                }
            }
        }

        internal static void RestoreCardToggleVisuals()
        {
            foreach (GameObject cardObject in cardObjs.Keys)
            {
                UpdateVisualsCardObj(cardObject);
            }
        }

        internal void RestoreCardToggleVisuals(string category)
        {
            if (!cardObjectsInCategory.ContainsKey(category)) return;
            foreach (GameObject cardObject in cardObjs.Keys)
            {
                UpdateVisualsCardObj(cardObject);
            }
        }

        internal void SortCardMenus(bool alph)
        {
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = scrollViews[category].Find("Viewport/Content");

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

            instance.currentColumnAmount = amount;
            cardAmountText.text = "Cards Per Line: " + amount;
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = scrollViews[category].Find("Viewport/Content");
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
            ChangeCardColumnAmountMenus(instance.currentColumnAmount);
        }

        /// <summary> This is used for opening and closing menus </summary>
        public static void SetActive(Transform trans, bool active)
        {
            if (active)
            {
                // Main camera changes when going back to menu and glow disappears if we don't se the camera again to the canvas
                Camera mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
                Canvas canvas = cardMenuCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
            }

            if (trans.gameObject != null) trans.gameObject.SetActive(active);

            Unbound.Instance.ExecuteAfterFrames(1, () =>
            {
                if (active)
                {
                    if (instance.cardVisualsCoroutine != null)
                    {
                        Unbound.Instance.StopCoroutine(instance.cardVisualsCoroutine);
                    }

                    instance.cardVisualsCoroutine = Unbound.Instance.StartCoroutine(instance.currentCategory != null ? instance.EnableCardsInCategory(instance.currentCategory) : instance.EnableCardsInCategory("Vanilla"));
                }
                else
                {
                    instance.DisableCardsInCategory(instance.currentCategory ?? "Vanilla");
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

            if (buttonActions != null) SetAllButtonActions(buttonActions);

            EnableButtonsMethod();

            if (interactionDisabledCards != null)
            {
                foreach (var card in interactionDisabledCards)
                {
                    var obj = GetCardObject(card);
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

            for (int i = 0; i < cardObjs.Keys.Count; i++)
            {
                var buttonEvent = new Button.ButtonClickedEvent();
                var unityAction = new UnityAction(cardObjs.ElementAt(i).Value);
                buttonEvent.AddListener(unityAction);
                cardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
            }

            instance.DisableCards();
        }

        public static GameObject[] GetCardObjects(string[] cardNames)
        {
            return cardNames.Select(GetCardObject).ToArray();
        }

        public static GameObject GetCardObject(string cardName)
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
            foreach (var button in instance.buttonsToDisable)
            {
                button.interactable = true;
            }
            foreach (var toggle in instance.togglesToDisable)
            {
                toggle.interactable = true;
            }
            disabled = false;
        }

        private static void DisableButtonsMethod()
        {
            foreach (var button in instance.buttonsToDisable)
            {
                button.interactable = false;
            }
            foreach (var toggle in instance.togglesToDisable)
            {
                toggle.interactable = false;
            }
            disabled = true;
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