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

        private static readonly List<Button> buttonsToDisable = new List<Button>();
        private static readonly List<Toggle> togglesToDisable = new List<Toggle>();

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

        internal static Color commonColor = new Color(1f, 1f, 1f, 0.5f);
        internal static Color uncommonColor = new Color(0.1745f, 0.6782f, 1f, 0.5f);
        internal static Color rareColor = new Color(1f, 0.1765f, 0.7567f, 0.5f);

        private string currentCategory
        {
            get
            {
                foreach (var scroll in scrollViews)
                {
                    if (scroll.Value.gameObject.activeInHierarchy)
                    {
                        return scroll.Key;
                    }
                }

                return null;
            }
        }
        
        // if need to toggle all on or off
        private bool toggledAll;

        private void Start()
        {
            instance = this;
            var _toggleCardsCanvas = Unbound.toggleUI.LoadAsset<GameObject>("ToggleCardsCanvas");
            cardObjAsset = Unbound.toggleUI.LoadAsset<GameObject>("CardObj");
            scrollViewAsset = Unbound.toggleUI.LoadAsset<GameObject>("ScrollView2");
            categoryButtonAsset = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");

            toggleCardsCanvas = Instantiate(_toggleCardsCanvas);
            DontDestroyOnLoad(toggleCardsCanvas);
            toggleCardsCanvas.GetComponent<Canvas>().worldCamera = Camera.current;
            toggleCardsCanvas.SetActive(false);

            scrollViewTrans = toggleCardsCanvas.transform.Find("CardMenu/ScrollViews");
            categoryContent = toggleCardsCanvas.transform.Find("CardMenu/Top/Categories/ButtonsScroll/Viewport/Content");
            

            // Create and set searchbar
            var searchBar = toggleCardsCanvas.transform.Find("CardMenu/Top/InputField").gameObject;
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                foreach (var _scrollView in scrollViews)
                {
                    foreach (var card in _scrollView.Value.GetComponentsInChildren<Button>(true))
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
                }
            });

            // create and set sort button (making use of the unused "Switch profile" button)
            toggleCardsCanvas.transform.Find("CardMenu/Top/Switch profile").gameObject.SetActive(true);
            toggleCardsCanvas.transform.Find("CardMenu/Top/Switch profile").GetComponentInChildren<TextMeshProUGUI>().text = "Sort by: " + (sortedByName ? "Name" : "Rarity");
            var sortButton = toggleCardsCanvas.transform.Find("CardMenu/Top/Switch profile").GetComponent<Button>();
            sortButton.transform.localPosition = new Vector3(245f, 19.33f, 0f);
            sortButton.onClick.AddListener(() =>
            {
                sortedByName = !sortedByName;
                toggleCardsCanvas.transform.Find("CardMenu/Top/Switch profile").GetComponentInChildren<TextMeshProUGUI>().text = "Sort by: " + (sortedByName ? "Name" : "Rarity");

                SortCardMenus(sortedByName);

            });


            // Create and set toggle all button
            var toggleAllButton = toggleCardsCanvas.transform.Find("CardMenu/Top/Toggle all").GetComponent<Button>();
            buttonsToDisable.Add(toggleAllButton);
            toggleAllButton.onClick.AddListener(() =>
            {
                if (currentCategory == null) return;
                
                toggledAll = !toggledAll;

                var cardsInCategory = CardManager.GetCardsInCategory(currentCategory);
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
            var infoButton = toggleCardsCanvas.transform.Find("CardMenu/Top/Help").GetComponent<Button>();
            var infoMenu = toggleCardsCanvas.transform.Find("CardMenu/InfoMenu").gameObject;
            infoButton.onClick.AddListener(() =>
            {
                infoMenu.SetActive(!infoMenu.activeInHierarchy);
            });

            this.ExecuteAfterSeconds(0.5f, () =>
            {
                toggleCardsCanvas.SetActive(true);
                // Create category scrollViews
                foreach (var category in CardManager.categories)
                {
                    var _scrollView = Instantiate(scrollViewAsset, scrollViewTrans);
                    _scrollView.SetActive(true);
                    SetActive(_scrollView.transform, false);
                    _scrollView.name = category;
                    scrollViews.Add(category, _scrollView.transform);
                    if (category == "Vanilla")
                    {
                        SetActive(_scrollView.transform, true);
                    }

                }



                // Create cardObjs
                foreach (var card in CardManager.cards)
                {
                    var parentScroll = scrollViews[card.Value.category].Find("Viewport/Content");
                    var crdObj = Instantiate(cardObjAsset, parentScroll);

                    crdObj.name = card.Key;

                    void cardAction()
                    {
                        if (card.Value.enabled)
                        {
                            CardManager.DisableCard(card.Value.cardInfo);
                            //card.Value.enabled = false;
                            UpdateVisualsCardObj(crdObj, false);
                        }
                        else
                        {
                            CardManager.EnableCard(card.Value.cardInfo);
                            //card.Value.enabled = true;
                            UpdateVisualsCardObj(crdObj, true);
                        }
                    }

                    cardObjs[crdObj] = cardAction;
                    defaultCardActions.Add(cardAction);

                    buttonsToDisable.Add(crdObj.GetComponent<Button>());
                        
                    crdObj.transform.GetComponentInChildren<TextMeshProUGUI>().text = card.Key;

                    // add rarity border
                    GameObject background = crdObj.transform.GetComponentInChildren<TextMeshProUGUI>().transform.parent.gameObject;
                    GameObject rarity = UnityEngine.GameObject.Instantiate(background, background.transform);
                    UnityEngine.GameObject.DestroyImmediate(rarity.transform.GetChild(0).gameObject);
                    background.AddComponent<Mask>();
                    Image image = rarity.GetComponent<Image>();
                    image.type = Image.Type.Filled;
                    image.fillAmount = 0.5f;
                    image.fillMethod = Image.FillMethod.Radial90;
                    image.fillOrigin = 3;
                    image.preserveAspect = true;
                    rarity.transform.localPosition = new Vector3(-30f, -105f, 0f);
                    switch (card.Value.cardInfo.rarity)
                    {
                        case CardInfo.Rarity.Common:
                            image.color = commonColor;
                            break;
                        case CardInfo.Rarity.Uncommon:
                            image.color = uncommonColor;
                            break;
                        case CardInfo.Rarity.Rare:
                            image.color = rareColor;
                            break;
                        
                    }

                    crdObj.transform.GetComponentsInChildren<TextMeshProUGUI>()[1].text = card.Value.cardInfo.cardDestription;
                    
                    var statsText = "";
                    foreach (var stat in card.Value.cardInfo.cardStats)
                    {
                        var amount = stat.positive
                            ? "<color=green>" + stat.amount + "</color>"
                            : "<color=red>" + stat.amount + "</color>";
                        statsText += amount + " " + stat.stat + "\n";
                    }
                    
                    crdObj.transform.GetComponentsInChildren<TextMeshProUGUI>()[2].text = statsText;
                    
                    if (card.Value.config.Value)
                    {
                        CardManager.EnableCard(card.Value.cardInfo);
                    }
                    else
                    {
                        CardManager.DisableCard(card.Value.cardInfo);
                    }
                    UpdateVisualsCardObj(crdObj, card.Value.config.Value);
                }

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
                toggleCardsCanvas.SetActive(false);
            });
        }
        
        public static void UpdateVisualsCardObj(GameObject cardObj, bool cardEnabled)
        {
            if (cardEnabled)
            {
                cardObj.transform.Find("Darken/Darken").gameObject.SetActive(false);
            }
            else
            {
                cardObj.transform.Find("Darken/Darken").gameObject.SetActive(true);
            }
        }

        internal void SortCardMenus(bool alph)
        {
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = scrollViews[category].Find("Viewport/Content");

                List<Transform> cardsInMenu = new List<Transform>() { };

                foreach (Transform child in categoryMenu)
                {
                    cardsInMenu.Add(child);
                }

                List<Transform> sorted = new List<Transform>() { };

                if (alph)
                {
                    sorted = cardsInMenu.OrderBy(t => t.name).ToList();
                }
                else
                {
                    sorted = cardsInMenu.OrderBy(t => CardManager.cards[t.name].cardInfo.rarity).ThenBy(t => t.name).ToList();
                }

                int i = 0;
                foreach (Transform cardInMenu in sorted)
                {
                    cardInMenu.SetSiblingIndex(i);
                    i++;
                }


            }
        }


        /// <summary> This is used for opening and closing menus </summary>
        public static void SetActive(Transform trans, bool active)
        {
            trans.gameObject.SetActive(active);
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
        public static void Open(bool escape,bool toggleAll, Action[] buttonActions = null, string[] interactionDisabledCards = null)
        {
            menuOpenFromOutside = true;
            SetActive(toggleCardsCanvas.transform, true);
            disableEscapeButton = escape;
            enableButtonsMethod();
            toggleCardsCanvas.transform.Find("CardMenu/Top/Help")?.gameObject.SetActive(false);
            
            if (toggleAll) toggleCardsCanvas.transform.Find("CardMenu/Top/Toggle all").gameObject.SetActive(false);
            
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
            
            enableButtonsMethod();
            
            if (interactionDisabledCards != null)
            {
                foreach (var card in interactionDisabledCards)
                {
                    var obj = GetCardObj(card);
                    if (obj == null) throw new ArgumentNullException("obj", '"' +card+'"' + " is not a valid card name");
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
            disableButtonsMethod();
            toggleCardsCanvas.transform.Find("CardMenu/Top/Help").gameObject.SetActive(true);
            toggleCardsCanvas.transform.Find("CardMenu/Top/Toggle all").gameObject.SetActive(true);
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
        
        private static void enableButtonsMethod()
        {
            foreach (var button in buttonsToDisable)
            {
                button.interactable = true;
            }
            foreach (var toggle in togglesToDisable)
            {
                toggle.interactable = true;
            }
            disabled = false;
        }

        private static void disableButtonsMethod()
        {
            foreach (var button in buttonsToDisable)
            {
                button.interactable = false;
            }
            foreach (var toggle in togglesToDisable)
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
                    disableButtonsMethod();
                } else if (!GameManager.instance.isPlaying && disabled)
                {
                    enableButtonsMethod();
                }
            }
        }
    }
}