using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace UnboundLib.Utils
{
    public class CardManager : MonoBehaviour
    {
        public CardManager instance;
        
        // A string array of all cards
        internal static CardInfo[] allCards
        {
            get
            {
                List<CardInfo> _allCards = new List<CardInfo>();
                _allCards.AddRange(activeCards);
                _allCards.AddRange(inactiveCards);
                _allCards.Sort((x, y) => string.CompareOrdinal(x.cardName, y.cardName));
                return _allCards.ToArray();
            }
        }
        
        internal static CardInfo[] defaultCards;
        internal static ObservableCollection<CardInfo> activeCards;
        internal static List<CardInfo> inactiveCards = new List<CardInfo>();
        
        // List of all categories
        public static readonly List<string> categories = new List<string>();
        // Dictionary of category name against if it is enabled
        internal static readonly Dictionary<string, ConfigEntry<bool>> categoryBools = new Dictionary<string, ConfigEntry<bool>>();
        
        public static Dictionary<string, Card> cards = new Dictionary<string, Card>();

        public void Start()
        {
            instance = this;
            
            // store default cards
            defaultCards = (CardInfo[]) CardChoice.instance.cards.Clone();
            
            // Make activeCardsCollection and add defaultCards to it
            activeCards = new ObservableCollection<CardInfo>(defaultCards);
            
            // Set activeCards CollectionChanged event
            activeCards.CollectionChanged += CardsChanged;
        }

        public static void FirstTimeStart()
        {
            // Sort cards
            cards = cards.Keys.OrderBy(k => k).ToDictionary(k => k, k => cards[k]);
            
            // Set categories
            foreach (var card in cards)
            {
                if(!categories.Contains(card.Value.category))
                {
                    categories.Add(card.Value.category);
                }
            }
                    
            // Populate the categoryBools dictionary
            foreach (var category in categories)
            {
                categoryBools.Add(category, Unbound.config.Bind("Card categories", category, true));
            }
        }
        
        internal static void CardsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (CardChoice.instance)
            {
                CardChoice.instance.cards = activeCards.ToArray();
            }
        }

        public static CardInfo[] GetCardsInfoWithNames(string[] cardNames)
        {
            return cardNames.Select(GetCardInfoWithName).ToArray();
        }

        public static CardInfo GetCardInfoWithName(string cardName)
        {
            return cards.ContainsKey(cardName) ? cards[cardName].cardInfo : null;
        }

        public static void EnableCards(CardInfo[] cards, bool saved = true)
        {
            foreach (var card in cards)
            {
                EnableCard(card, saved);
            }
        }

        public static void EnableCard(CardInfo card, bool saved = true)
        {
            if (activeCards.Contains(card)) return;
            if (!activeCards.Contains(card))
            {
                activeCards.Add(card);
                activeCards = new ObservableCollection<CardInfo>(activeCards.OrderBy(i => i.cardName));
                activeCards.CollectionChanged += CardsChanged;
            }
            if (inactiveCards.Contains(card))
            {
                inactiveCards.Remove(card);
            }

            
            if (saved)
            {
                cards[card.cardName].enabled = true;
                Unbound.config.Bind("Cards: " + cards[card.cardName].category, card.cardName, true).Value = true;
            }
        }
        
        public static void DisableCards(CardInfo[] cards, bool saved = true)
        {
            foreach (var card in cards)
            {
                DisableCard(card, saved);
            }
        }
        
        public static void DisableCard(CardInfo card, bool saved = true)
        {
            if (activeCards.Contains(card))
            {
                activeCards.Remove(card);
            }
            if (!inactiveCards.Contains(card))
            {
                inactiveCards.Add(card);
                inactiveCards.Sort((x, y) => string.Compare(x.cardName, y.cardName));
            }
            
            
            if (saved)
            {
                cards[card.cardName].enabled = false;
                Unbound.config.Bind("Cards: " + cards[card.cardName].category, card.cardName, true).Value = false;
            }
        }
        
        public static void EnableCategory(string categoryName)
        {
            if(categoryBools.ContainsKey(categoryName)) categoryBools[categoryName].Value = true;
        }

        public static void DisableCategory(string categoryName)
        {
            if(categoryBools.ContainsKey(categoryName)) categoryBools[categoryName].Value = false;
        }
        
        public static bool IsCardActive(CardInfo card)
        {
            return activeCards.Contains(card);
        }
        
        public static bool IsCategoryActive(string categoryName)
        {
            return categoryBools.ContainsKey(categoryName) && categoryBools[categoryName].Value;
        }
        public static string[] GetCardsInCategory(string category)
        {
            return (from card in cards where card.Value.category.Contains(category) select card.Key).ToArray();
        }
        
    }
    
    public class Card
    {
        public bool enabled;
        public string category;
        public CardInfo cardInfo;

        public Card(string category, bool enabled, CardInfo cardInfo)
        {
            this.category = category;
            this.enabled = enabled;
            this.cardInfo = cardInfo;
        }
    }
}