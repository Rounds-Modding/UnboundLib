using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using BepInEx.Configuration;
using Photon.Pun;
using UnboundLib.Cards;
using UnboundLib.Networking;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Utils
{
    public class CardManager : MonoBehaviour
    {
        public CardManager instance;
        
        // A string array of all cardInfos
        internal static CardInfo[] allCards
        {
            get
            {
                List<CardInfo> _allCards = new List<CardInfo>();
                _allCards.AddRange(activeCards);
                _allCards.AddRange(inactiveCards);
                _allCards.Sort((x, y) => string.CompareOrdinal(x.gameObject.name, y.gameObject.name));
                return _allCards.ToArray();
            }
        }
        
        internal static CardInfo[] defaultCards;
        internal static ObservableCollection<CardInfo> activeCards;
        //internal static ObservableCollection<CardInfo> previousActiveCards = new ObservableCollection<CardInfo>();
        internal static List<CardInfo> inactiveCards = new List<CardInfo>();
        //internal static List<CardInfo> previousInactiveCards = new List<CardInfo>();
        
        // List of all categories
        public static readonly List<string> categories = new List<string>();
        // Dictionary of category name against if it is enabled
        internal static readonly Dictionary<string, ConfigEntry<bool>> categoryBools = new Dictionary<string, ConfigEntry<bool>>();
        
        public static Dictionary<string, Card> cards = new Dictionary<string, Card>();

        private static readonly List<Action<CardInfo[]>> FirstStartCallbacks = new List<Action<CardInfo[]>>();

        public void Start()
        {
            instance = this;
            
            // store default cardInfos
            defaultCards = (CardInfo[]) CardChoice.instance.cards.Clone();
            
            // Make activeCardsCollection and add defaultCards to it
            activeCards = new ObservableCollection<CardInfo>(defaultCards);
            
            // Set activeCards CollectionChanged event
            activeCards.CollectionChanged += CardsChanged;
        }

        public static void FirstTimeStart()
        {
            // Sort cardInfos
            cards = cards.Keys.OrderBy(k => k).ToDictionary(k => k, k => cards[k]);
            
            // Set categories
            foreach (var card in cards.Where(card => !categories.Contains(card.Value.category)))
            {
                categories.Add(card.Value.category);
            }

            // Populate the categoryBools dictionary
            foreach (var category in categories)
            {
                categoryBools.Add(category, Unbound.config.Bind("Card categories", category, true));
            }

            foreach (Action<CardInfo[]> callback in FirstStartCallbacks)
            {
                callback(allCards);
            }

        }

        public static void RestoreCardToggles()
        {
            foreach (Card card in cards.Values)
            {
                if (card.config.Value)
                {
                    EnableCard(card.cardInfo, true);
                }
                else
                {
                    DisableCard(card.cardInfo, true);
                }
            }
        }

        public static void AddAllCardsCallback(Action<CardInfo[]> callback)
        {
            FirstStartCallbacks.Add(callback);
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

        public static void EnableCards(CardInfo[] cardInfos, bool saved = true)
        {
            foreach (var card in cardInfos)
            {
                EnableCard(card, saved);
            }
        }

        public static void EnableCard(CardInfo cardInfo, bool saved = true)
        {
            if (!activeCards.Contains(cardInfo))
            {
                activeCards.Add(cardInfo);
                activeCards = new ObservableCollection<CardInfo>(activeCards.OrderBy(i => i.gameObject.name));
                activeCards.CollectionChanged += CardsChanged;
            }
            if (inactiveCards.Contains(cardInfo))
            {
                inactiveCards.Remove(cardInfo);
            }

            string cardName = cardInfo.gameObject.name;
            if (!cards.ContainsKey(cardName)) return;
            
            cards[cardName].enabled = true;

            if (saved)
            {
                cards[cardName].config.Value = true;
            }
        }

        public static void DisableCards(CardInfo[] cardInfos, bool saved = true)
        {
            foreach (var card in cardInfos)
            {
                DisableCard(card, saved);
            }
        }
        
        public static void DisableCard(CardInfo cardInfo, bool saved = true)
        {
            if (activeCards.Contains(cardInfo))
            {
                activeCards.Remove(cardInfo);
            }
            if (!inactiveCards.Contains(cardInfo))
            {
                inactiveCards.Add(cardInfo);
                inactiveCards.Sort((x, y) => string.CompareOrdinal(x.gameObject.name, y.gameObject.name));
            }

            string cardName = cardInfo.gameObject.name;
            if (!cards.ContainsKey(cardName)) return;

            cards[cardName].enabled = false;

            if (saved)
            {
                cards[cardName].config.Value = false;
            }
        }

        public static void EnableCategory(string categoryName)
        {
            if(categoryBools.ContainsKey(categoryName)) categoryBools[categoryName].Value = true;
            foreach (string cardname in GetCardsInCategory(categoryName))
            {
                EnableCard(cards[cardname].cardInfo, true);
            }
        }

        public static void DisableCategory(string categoryName)
        {
            if(categoryBools.ContainsKey(categoryName)) categoryBools[categoryName].Value = false;
            foreach (string cardname in GetCardsInCategory(categoryName))
            {
                DisableCard(cards[cardname].cardInfo, true);
            }
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

        #region Syncing

        public static void OnLeftRoomAction()
        {
            RestoreCardToggles();
            ToggleCardsMenuHandler.RestoreCardToggleVisuals();
        }

        public static void OnJoinedRoomAction()
        {
            CardChoice.instance.cards = activeCards.ToArray();

            Unbound.Instance.ExecuteAfterFrames(45, () =>
            {
                // send available cardInfo pool to the master client
                if (!PhotonNetwork.IsMasterClient)
                {
                    NetworkingManager.RPC_Others(typeof(CardManager), nameof(RPC_CardHandshake), (object) cards.Keys.ToArray());
                }
            });
        }
        
        // This gets executed only on master client
        [UnboundRPC]
        private static void RPC_CardHandshake(string[] cardsArray)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            List<string> disabledCards = new List<string>();

            // disable any cardInfos which aren't shared by other players
            foreach (var cardName in cards.Keys.Where(cardName => !cardsArray.Contains(cardName)))
            {
                DisableCard(cards[cardName].cardInfo, false);
                disabledCards.Add(cardName);
                foreach (var obj in ToggleCardsMenuHandler.cardObjs.Where(c => c.Key.name == cardName))
                {
                    ToggleCardsMenuHandler.UpdateVisualsCardObj(obj.Key);
                }
            }

            if (disabledCards.Count != 0)
            {
                Unbound.BuildModal()
                    .Title("These cards have been disabled because someone didn't have them")
                    .Message(string.Join(", ", disabledCards.ToArray()))
                    .CancelButton("Copy", () =>
                    {
                        Unbound.BuildInfoPopup("Copied Message!");
                        GUIUtility.systemCopyBuffer = string.Join(", ", disabledCards.ToArray());
                    })
                    .CancelButton("Cancel", () => { })
                    .Show();
            }

            // reply to all users with new list of valid cardInfos
            NetworkingManager.RPC_Others(typeof(CardManager), nameof(RPC_HostCardHandshakeResponse), (object) activeCards.Select(c => c.gameObject.name).ToArray());
        }

        // This gets executed on everyone except the master client
        [UnboundRPC]
        private static void RPC_HostCardHandshakeResponse(string[] cardsArray)
        {
            // enable and disable only cardInfos that the host has specified are allowed
            foreach (var card in cards.Values.Select(card => card.cardInfo))
            {
                string cardObjectName = card.gameObject.name;
                if (cardsArray.Contains(cardObjectName))
                {
                    EnableCard(card, false);
                    foreach (var obj in ToggleCardsMenuHandler.cardObjs.Where(c => c.Key.name == cardObjectName))
                    {
                        ToggleCardsMenuHandler.UpdateVisualsCardObj(obj.Key);
                    }
                }
                else
                {
                    DisableCard(card, false);
                    foreach (var obj in ToggleCardsMenuHandler.cardObjs.Where(c => c.Key.name == cardObjectName))
                    {
                        ToggleCardsMenuHandler.UpdateVisualsCardObj(obj.Key);
                    }
                }
            }
        }

        #endregion
    }
    
    public class Card
    {
        public bool enabled;
        public string category;
        public CardInfo cardInfo;
        public ConfigEntry<bool> config;

        public Card(string category, ConfigEntry<bool> config, CardInfo cardInfo)
        {
            this.category = category;
            enabled = config.Value;
            this.cardInfo = cardInfo;
            this.config = config;
        }
    }
}