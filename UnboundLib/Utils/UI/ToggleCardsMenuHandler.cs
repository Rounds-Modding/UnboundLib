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

        private readonly Dictionary<string, Transform> scrollViews = new Dictionary<string, Transform>();

        public static readonly Dictionary<GameObject, Action> cardObjs = new Dictionary<GameObject, Action>();
        public static readonly List<Action> defaultCardActions = new List<Action>();

        private static readonly List<Button> ButtonsToDisable = new List<Button>();
        private static readonly List<Toggle> TogglesToDisable = new List<Toggle>();

        public static GameObject toggleCardsCanvas;

        private GameObject cardObjAsset;
        private GameObject scrollViewAsset;
        private GameObject categoryButtonAsset;

        private Transform scrollViewTrans;
        private Transform categoryContent;

        public static bool disableEscapeButton;
        public static bool disableButtons;

        public static bool menuOpenFromOutside;

        private static bool disabled;

        private static bool sortedByName = true;

        public static Color backgroundColor = new Color(1, 1, 1, 0.034f);
        internal static Color primaryColor = new Color(0.29f, 0.29f, 0.29f, 0.998f);
        internal static Color secondaryColor = new Color32(141, 149, 163, 255);
        internal static Color textColor = new Color32(242, 242, 250, 255);
        internal static Color cardTextNameColor = new Color(0.179f, 0.179f, 0.179f, 1);

        internal static Color positiveColor = new Color(0.465f, 0.603f, 0.390f, 1);
        internal static Color negativeColor = new Color(0.698f, 0.326f, 0.326f, 1);

        // internal static Color commonColor = new Color(0.698f, 0.326f, 0.326f, 1);
        internal static Color uncommonColor = new Color(0, 0.5f, 1, 1);
        internal static Color rareColor = new Color(1, 0.2f, 1, 1);

        private string CurrentCategory => (from scroll in scrollViews where scroll.Value.gameObject.activeInHierarchy select scroll.Key).FirstOrDefault();

        // if need to toggle all on or off
        private bool toggledAll;

        private void Start()
        {
            instance = this;
            var mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();

            var toggleCardsCanvas = Unbound.toggleUI.LoadAsset<GameObject>("ToggleCardsCanvas");

            cardObjAsset = Unbound.toggleUI.LoadAsset<GameObject>("CardObj");

            // Better to have it removed directly in the object but oh well
            var backgroundObject = FindObjectInChilds(cardObjAsset, "Background");
            if (backgroundObject != null)
            {
                Destroy(backgroundObject);
            }

            var descriptionObject = FindObjectInChilds(cardObjAsset, "Description");
            if (descriptionObject != null)
            {
                Destroy(descriptionObject);
            }

            var statsObject = FindObjectInChilds(cardObjAsset, "Stats");
            if (statsObject != null)
            {
                Destroy(statsObject);
            }

            scrollViewAsset = Unbound.toggleUI.LoadAsset<GameObject>("ScrollView2");
            categoryButtonAsset = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");
            // foreach (Image imageComponent in categoryButtonAsset.GetComponentsInChildren<Image>())
            // {
            //     imageComponent.color = primaryColor;
            // }

            ToggleCardsMenuHandler.toggleCardsCanvas = Instantiate(toggleCardsCanvas);
            DontDestroyOnLoad(ToggleCardsMenuHandler.toggleCardsCanvas);
            var canvas = ToggleCardsMenuHandler.toggleCardsCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            ToggleCardsMenuHandler.toggleCardsCanvas.SetActive(false);

            var cardMenuObject = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu");
            // foreach (Image imageComponent in cardMenuObject.GetComponentsInChildren<Image>())
            // {
            //     imageComponent.color = backgroundColor;
            // }

            scrollViewTrans = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/ScrollViews");

            categoryContent = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/Categories/ButtonsScroll/Viewport/Content");
            var categoriesObject = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/Categories");
            // foreach (Image imageComponent in categoriesObject.GetComponentsInChildren<Image>())
            // {
            //     imageComponent.color = primaryColor;
            // }

            // Create and set searchbar
            var searchBar = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/InputField").gameObject;
            // foreach (Image imageComponent in searchBar.GetComponents<Image>())
            // {
            //     imageComponent.color = primaryColor;
            // }
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                foreach (var card in scrollViews.SelectMany(scrollView => scrollView.Value.GetComponentsInChildren<Button>(true)))
                {
                    if (value == "")
                    {
                        card.gameObject.SetActive(true);
                        //SetActive(card.transform, true);
                        //card.GetComponent<LayoutElement>().ignoreLayout = false;
                        continue;
                    }

                    if (card.name.ToUpper().Contains(value.ToUpper()))
                    {
                        card.gameObject.SetActive(true);
                        //SetActive(card.transform, true);
                        //card.GetComponent<LayoutElement>().ignoreLayout = false;
                    }
                    else
                    {
                        card.gameObject.SetActive(false);
                        //SetActive(card.transform, false);
                        //card.GetComponent<LayoutElement>().ignoreLayout = true;
                    }
                }
            });

            // create and set sort button (making use of the unused "Switch profile" button)
            // ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").gameObject.SetActive(true);
            ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");
            var sortButton = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").GetComponent<Button>();
            // foreach (Image imageComponent in sortButton.GetComponents<Image>())
            // {
            //     imageComponent.color = primaryColor;
            // }
            sortButton.onClick.AddListener(() =>
            {
                sortedByName = !sortedByName;
                ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");

                SortCardMenus(sortedByName);
            });
            
            Transform cardAmountObject = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/CardAmount");
            var cardAmountText = cardAmountObject.GetComponentInChildren<TextMeshProUGUI>();
            var cardAmountSlider = cardAmountObject.GetComponentsInChildren<Slider>();
            foreach (Slider slider in cardAmountSlider)
            {
                slider.onValueChanged.AddListener(new UnityAction<float>((amount =>
                {
                    int integerAmount = (int) amount;
                    ChangeCardColumnAmountMenus(integerAmount);
                    cardAmountText.text = "Cards: " + integerAmount;
                })));
            }
            

            // Create and set toggle all button
            var toggleAllButton = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/ToggleAll").GetComponent<Button>();
            // foreach (Image imageComponent in toggleAllButton.GetComponents<Image>())
            // {
            //     imageComponent.color = primaryColor;
            // }
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
            var infoButton = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/Help").GetComponent<Button>();
            var infoMenu = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/InfoMenu").gameObject;
            infoButton.onClick.AddListener(() =>
            {
                infoMenu.SetActive(!infoMenu.activeInHierarchy);
            });

            this.ExecuteAfterSeconds(0.65f, () =>
            {
                ToggleCardsMenuHandler.toggleCardsCanvas.SetActive(true);
                // Create category scrollViews
                foreach (var category in CardManager.categories)
                {
                    var scrollView = Instantiate(scrollViewAsset, scrollViewTrans);
                    scrollView.SetActive(true);
                    SetActive(scrollView.transform, false);
                    scrollView.name = category;
                    scrollViews.Add(category, scrollView.transform);
                    if (category == "Vanilla")
                    {
                        SetActive(scrollView.transform, true);
                    }
                    var handleObject = scrollView.transform.Find("Scrollbar Vertical/Sliding Area/Handle");
                    // foreach (Image imageComponent in handleObject.GetComponents<Image>())
                    // {
                    //     imageComponent.color = primaryColor;
                    // }
                    var scrollbarObject = scrollView.transform.Find("Scrollbar Vertical");
                    // foreach (Image imageComponent in handleObject.GetComponents<Image>())
                    // {
                    //     imageComponent.color = secondaryColor;
                    // }
                    // foreach (Image imageComponent in scrollView.GetComponentsInChildren<Image>())
                    // {
                    //     imageComponent.color = primaryColor;
                    // }
                }

                // Create cardObjs
                foreach (var card in CardManager.cards)
                {
                    Card cardValue = card.Value;
                    var parentScroll = scrollViews[cardValue.category].Find("Viewport/Content");
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

                                        // var topObject = new GameObject("CardPreviewHolder");
                                        // topObject.transform.SetParent(crdObj.transform);
                                        // topObject.transform.localPosition = new Vector3(0, 0, 1);
                                        // topObject.transform.localScale = new Vector3(1, 1, 1);
                                        // topObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
                                        // topObject.transform.SetAsFirstSibling();

                                        var darken = crdObj.transform.Find("Darken");

                                        var cardBaseObj = Instantiate(frontCard);

                                        // var cardCanvas = cardBaseObj.gameObject.AddComponent<Canvas>();
                                        // cardCanvas.worldCamera = mainCamera;
                                        // cardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                                        Vector3 sizeCalculated = new Vector3(0.115f, 0.115f, 1f);
                                        // var renderer = crdObj.GetComponent<Renderer>();
                                        // if (renderer == null)
                                        // {
                                        //     sizeCalculated = renderer.bounds.size;
                                        // }


                                        cardBaseObj.name = "CardPreview";
                                        cardBaseObj.transform.SetParent(crdObj.transform);
                                        cardBaseObj.transform.localPosition = new Vector3(0, -10, 1f);
                                        cardBaseObj.transform.localScale = sizeCalculated;
                                        cardBaseObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
                                        cardBaseObj.transform.SetAsFirstSibling();
                                        cardBaseObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 1500);

                                        // There is no reason to have the script rely on each cards stat and icon objects as we create useless objects (probably better to just create one that each card will just refer to)
                                        // var iconObject = cardBaseObj.transform.Find("Icon");
                                        // if (iconObject != null)
                                        // {
                                        //     UnityEngine.Debug.Log($"[Unbound] iconObject.");
                                        //     Destroy(iconObject);
                                        // }
                                        // var statObject = cardBaseObj.transform.Find("StatObject");
                                        // if (statObject != null)
                                        // {
                                        //     UnityEngine.Debug.Log($"[Unbound] statObject.");
                                        //     Destroy(statObject);
                                        // }

                                        var canvasGroups = cardBaseObj.GetComponentsInChildren<CanvasGroup>();
                                        foreach (var canvasGroup in canvasGroups)
                                        {
                                            canvasGroup.alpha = 1;
                                        }

                                        // Creates problems if it's not in the game scene and also is the main cause of lag
                                        GameObject uiParticleObject = FindObjectInChilds(cardBaseObj.gameObject, "UI_ParticleSystem");
                                        if (uiParticleObject != null)
                                        {
                                            Destroy(uiParticleObject);
                                        }

                                        if (cardInfo.cardArt != null)
                                        {
                                            var artObject = FindObjectInChilds(cardBaseObj.gameObject, "Art");
                                            if (artObject != null)
                                            {
                                                var canvasComponent = artObject.AddComponent<Canvas>();
                                                canvasComponent.sortingOrder = 1;

                                                var cardArtObj = Instantiate(cardInfo.cardArt);
                                                cardArtObj.transform.SetParent(artObject.transform);
                                                cardArtObj.transform.localPosition = new Vector3(0, 0, 1);
                                                cardArtObj.transform.localScale = new Vector3(1f, 1f, 1);
                                                cardArtObj.transform.localRotation = Quaternion.identity;

                                                var cardAnimationHandler = cardBaseObj.transform.parent.gameObject
                                                    .AddComponent<CardAnimationHandler>();
                                                cardAnimationHandler.ToggleAnimation(false);
                                            }

                                            var blockFrontObject = FindObjectInChilds(cardBaseObj.gameObject, "BlockFront");
                                            if (blockFrontObject != null)
                                            {
                                                blockFrontObject.SetActive(true);
                                            }
                                        }

                                        var backgroundObj = FindObjectInChilds(cardBaseObj.gameObject, "Background");
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

                                        if (cardInfo.rarity != CardInfo.Rarity.Common)
                                        {
                                            var colorFromRarity = cardInfo.rarity == CardInfo.Rarity.Uncommon ? uncommonColor : rareColor;
                                            foreach (GameObject triangleObject in FindObjectsInChilds(cardBaseObj.gameObject, "Triangle"))
                                            {
                                                var imageComponent = triangleObject.GetComponent<Image>();
                                                if (imageComponent != null)
                                                {
                                                    imageComponent.color = colorFromRarity;
                                                }
                                            }
                                        }

                                        if (cardValue.category == "Vanilla")
                                        {
                                            var gridObject = FindObjectInChilds(cardBaseObj.gameObject, "Grid");
                                            gridObject.transform.localScale = new Vector3(1, 1, 1);
                                            var effectsText =
                                                Instantiate(FindObjectInChilds(cardBaseObj.gameObject, "EffectText"));
                                            if (effectsText != null)
                                            {
                                                effectsText.SetActive(true);
                                                effectsText.transform.SetParent(gridObject.transform);
                                                effectsText.transform.localScale = new Vector3(1, 1, 1);
                                                var effectsTextComponent = effectsText.GetComponent<TextMeshProUGUI>();
                                                effectsTextComponent.text = cardInfo.cardDestription;
                                            }

                                            var statObject = FindObjectInChilds(cardBaseObj.gameObject, "StatObject");
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


                                            var textName = cardBaseObj.transform.GetChild(1);
                                            if (textName != null)
                                            {
                                                var textComponent = textName.GetComponent<TextMeshProUGUI>();
                                                if (textComponent != null)
                                                {
                                                    textComponent.text = cardInfo.cardName.ToUpper();
                                                    textComponent.color = cardTextNameColor;
                                                }
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

                var viewingText = ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

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
                        foreach (var scroll in scrollViews)
                        {
                            SetActive(scroll.Value, false);
                        }

                        scrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        SetActive(scrollViews[category].transform, true);

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
                        foreach (var obj in scrollViews.Where(obj => obj.Key == category))
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
                            foreach (GameObject cardObj in cardObjs.Keys.Where(o => cardsInCategory.Contains(o.name)))
                            {
                                UpdateVisualsCardObj(cardObj, enabled);
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
                ToggleCardsMenuHandler.toggleCardsCanvas.SetActive(false);
                // foreach (TextMeshProUGUI textComponent in ToggleCardsMenuHandler.toggleCardsCanvas.GetComponentsInChildren<TextMeshProUGUI>())
                // {
                //     if (!textComponent.transform.parent.name.Equals("CardPreview") && !textComponent.transform.parent.name.Contains("StatObject"))
                //     {
                //         textComponent.color = textColor;
                //     }
                // }
            });
        }

        private static List<GameObject> FindObjectsInChilds(GameObject gameObject, string gameObjectName)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in children)
            {
                if (item.name == gameObjectName)
                {
                    gameObjects.Add(item.gameObject);
                }
            }

            return gameObjects;
        }

        private GameObject FindObjectInChilds(GameObject gameObject, string gameObjectName)
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

        internal void ChangeCardColumnAmountMenus(int amount)
        {
            Vector2 cellSize = new Vector2(221, 312);
            // Vector3 localScale = new Vector3(0.25f, 0.25f, 1);

            if (amount > 3)
            {
                switch (amount)
                {
                    case 4:
                        {
                            cellSize = new Vector2(170, 240);
                            // localScale = new Vector3(0.18f, 0.18f, 1);
                            break;
                        }
                    case 5:
                        {
                            cellSize = new Vector2(136, 192);
                            // localScale = new Vector3(0.145f, 0.145f, 1);
                            break;
                        }
                    default:
                    case 6:
                        {
                            cellSize = new Vector2(112, 158);
                            break;
                        }
                    case 7:
                        {
                            cellSize = new Vector2(97, 137);
                            break;
                        }
                    case 8:
                        {
                            cellSize = new Vector2(85, 120);
                            break;
                        }
                    case 9:
                        {
                            cellSize = new Vector2(75, 106);
                            break;
                        }
                    case 10:
                        {
                            cellSize = new Vector2(68, 96);
                            break;
                        }
                }
            }

            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = scrollViews[category].Find("Viewport/Content");
                var gridLayout = categoryMenu.gameObject.GetComponent<GridLayoutGroup>();
                gridLayout.cellSize = cellSize;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = amount;
                gridLayout.childAlignment = TextAnchor.UpperCenter;

                List<Transform> cardsInMenu = new List<Transform>() { };
                cardsInMenu.AddRange(categoryMenu.Cast<Transform>());

                // foreach (Transform cardTransform in cardsInMenu)
                // {
                //     cardTransform.GetChild(0).localScale = localScale;
                // }
                //
                // List<Transform> sorted = alph ? cardsInMenu.OrderBy(t => t.name).ToList() : cardsInMenu.OrderBy(t => CardManager.cards[t.name].cardInfo.rarity).ThenBy(t => t.name).ToList();
                //
                // int i = 0;
                // foreach (Transform cardInMenu in sorted)
                // {
                //     cardInMenu.SetSiblingIndex(i);
                //     i++;
                // }
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
                foreach (var card in cardObjs)
                {
                    UpdateVisualsCardObj(card.Key, CardManager.cards[card.Key.name].enabled);
                }
            });

            // trans.localScale = active ? Vector3.one : new Vector3(0.0001f, 0.0001f, 0.0001f);
            // if (trans.GetComponent<LayoutElement>())
            // {
            //     trans.GetComponent<LayoutElement>().ignoreLayout = false;
            // }
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
                    if (obj == null) throw new ArgumentNullException("obj", '"' + card + '"' + " is not a valid card name");
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
            foreach (var card in cardObjs)
            {
                UpdateVisualsCardObj(card.Key, CardManager.cards[card.Key.name].enabled);
            }
            foreach (Transform trans in toggleCardsCanvas.transform.Find(
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
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (var cardName in cardNames)
            {
                gameObjects.Add(GetCardObj(cardName));
            }

            return gameObjects.ToArray();
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
            // // Activate and deactivate the menu
            // if (Input.GetKeyDown(KeyCode.F3))
            // {
            //     SetActive(toggleCardsCanvas.transform.Find("CardMenu"),!IsActive(toggleCardsCanvas.transform.Find("CardMenu")));
            // }

            if (!menuOpenFromOutside)
            {
                if (GameManager.instance.isPlaying && !disabled)
                {
                    DisableButtonsMethod();
                }
                else if (!GameManager.instance.isPlaying && disabled)
                {
                    EnableButtonsMethod();
                }
            }
        }
    }
}