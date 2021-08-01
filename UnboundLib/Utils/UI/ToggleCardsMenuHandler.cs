using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnboundLib.Utils.UI
{
    public class ToggleCardsMenuHandler : MonoBehaviour
    {
        public static ToggleCardsMenuHandler instance;
        
        private readonly Dictionary<string, Transform> scrollViews = new Dictionary<string, Transform>();

        public static readonly List<GameObject> cardObjs = new List<GameObject>();

        public static GameObject toggleCardsCanvas;

        private GameObject cardObj;
        private GameObject scrollView;
        private GameObject categoryButton;

        private Transform scrollViewTrans;
        private Transform categoryContent;

        private bool disabled;

        private string currentCategory
        {
            get
            {
                foreach (var scroll in scrollViews)
                {
                    if (IsActive(scroll.Value))
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
            cardObj = Unbound.toggleUI.LoadAsset<GameObject>("CardObj");
            scrollView = Unbound.toggleUI.LoadAsset<GameObject>("ScrollView2");
            categoryButton = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");

            toggleCardsCanvas = Instantiate(_toggleCardsCanvas);
            DontDestroyOnLoad(toggleCardsCanvas);
            toggleCardsCanvas.GetComponent<Canvas>().worldCamera = Camera.current;
            toggleCardsCanvas.SetActive(true);
            toggleCardsCanvas.transform.Find("CardMenu").localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);

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
            
            // Create and set toggle all button
            var toggleAllButton = toggleCardsCanvas.transform.Find("CardMenu/Top/Toggle all").GetComponent<Button>();
            toggleAllButton.onClick.AddListener(() =>
            {
                if (currentCategory == null) return;
                
                toggledAll = !toggledAll;

                var cardsInCategory = CardManager.GetCardsInCategory(currentCategory);
                if (toggledAll)
                {
                    CardManager.DisableCards(CardManager.GetCardsInfoWithNames(cardsInCategory));

                    foreach (var crdObj in cardObjs.Where(cardObj => cardsInCategory.Contains(cardObj.name)))
                    {
                        UpdateVisualsCardObj(crdObj, false);
                    }
                }
                else
                {
                    CardManager.EnableCards(CardManager.GetCardsInfoWithNames(cardsInCategory));

                    foreach (var crdObj in cardObjs.Where(cardObj => cardsInCategory.Contains(cardObj.name)))
                    {
                        UpdateVisualsCardObj(crdObj, true);
                    }
                }
            });

            this.ExecuteAfterSeconds(0.5f, () =>
            {

                // Create category scrollViews
                foreach (var category in CardManager.categories)
                {
                    var _scrollView = Instantiate(scrollView, scrollViewTrans);
                    _scrollView.SetActive(true);
                    SetActive(_scrollView.transform, false);
                    _scrollView.name = category;
                    scrollViews.Add(category, _scrollView.transform);
                    if (category == "Default")
                    {
                        SetActive(_scrollView.transform, true);
                    }

                }
                
                // Create cardObjs
                foreach (var card in CardManager.cards)
                {
                    var parentScroll = scrollViews[card.Value.category].Find("Viewport/Content");
                    var crdObj = Instantiate(cardObj, parentScroll);

                    crdObj.name = card.Key;

                    cardObjs.Add(crdObj);
                    
                    crdObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (card.Value.enabled)
                        {
                            CardManager.DisableCard(card.Value.cardInfo);
                            card.Value.enabled = false;
                            UpdateVisualsCardObj(crdObj, card.Value.enabled);
                        }
                        else
                        {
                            CardManager.EnableCard(card.Value.cardInfo);
                            card.Value.enabled = true;
                            UpdateVisualsCardObj(crdObj, card.Value.enabled);
                        }
                    });
                        
                    crdObj.transform.GetComponentInChildren<TextMeshProUGUI>().text = card.Key;
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
                    
                    if (!Unbound.config.Bind("Cards: " + card.Value.category, card.Key, true).Value)
                    {
                        CardManager.DisableCard(card.Value.cardInfo);
                    }
                    UpdateVisualsCardObj(crdObj, card.Value.enabled);
                }
                
                // Create category buttons
                foreach (var category in CardManager.categories)
                {
                    var categoryObj = Instantiate(categoryButton, categoryContent);
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
                    toggle.onValueChanged.AddListener(value =>
                    {
                        if (!value)
                        {
                            UpdateCategoryVisuals(false);
                        }
                        else
                        {
                            UpdateCategoryVisuals(true);
                        }
                    });
            
                    void UpdateCategoryVisuals(bool enabled)
                    {
                        foreach (var obj in scrollViews.Where(obj => obj.Key == category))
                        {
                            obj.Value.Find("Darken").gameObject.SetActive(!enabled);
                            if (enabled)
                            {
                                CardManager.categoryBools[category].Value = true;
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (!trs.Find("Darken/Darken").gameObject.activeInHierarchy)
                                    {
                                        CardManager.EnableCard(CardManager.GetCardInfoWithName(trs.name), false);
                                    }
                                }
                            }
                            else
                            {
                                CardManager.categoryBools[category].Value = false;
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (!trs.Find("Darken/Darken").gameObject.activeInHierarchy)
                                    {
                                        CardManager.DisableCard(CardManager.GetCardInfoWithName(trs.name), false);
                                    }
                                }
                            }
                        }
            
                        toggle.isOn = CardManager.IsCategoryActive(category);
                    }
            
                    UpdateCategoryVisuals(CardManager.IsCategoryActive(category));
                }
                
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

            foreach (var category in CardManager.categories)
            {
                if (CardManager.GetCardsInCategory(category).All(card => !CardManager.IsCardActive(CardManager.GetCardInfoWithName(card))))
                {
                    CardManager.DisableCategory(category);
                }
            }
        }

        public static void SetActive(Transform trans, bool active)
        {
            trans.localScale = active ? Vector3.one : new Vector3(0.0001f, 0.0001f, 0.0001f);
            if (trans.GetComponent<LayoutElement>())
            {
                trans.GetComponent<LayoutElement>().ignoreLayout = false;
            }
        }

        public static bool IsActive(Transform trans)
        {
            return trans.localScale == Vector3.one;
        }

        public static GameObject GetCardObj(string cardName)
        {
            return cardObjs.FirstOrDefault(obj => obj.name == cardName);
        }

        private void Update()
        {
            // // Activate and deactivate the menu
            // if (Input.GetKeyDown(KeyCode.F3))
            // {
            //     SetActive(toggleCardsCanvas.transform.Find("CardMenu"),!IsActive(toggleCardsCanvas.transform.Find("CardMenu")));
            // }

            if (GameManager.instance.isPlaying && !disabled)
            {
                UnityEngine.Debug.LogWarning("Started playing");
                toggleCardsCanvas.SetActive(false);
                disabled = true;
            }
            if (!GameManager.instance.isPlaying && disabled)
            {
                UnityEngine.Debug.LogWarning("Stopped playing");
                toggleCardsCanvas.SetActive(true);
                disabled = false;
            }
        }
    }
}