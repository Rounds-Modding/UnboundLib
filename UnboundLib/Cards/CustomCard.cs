using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Photon.Pun;

namespace UnboundLib.Cards
{
    public abstract class CustomCard : MonoBehaviour
    {
        public static List<CardInfo> cards = new List<CardInfo>();

        public CardInfo cardInfo;
        public Gun gun;
        public ApplyCardStats cardStats;
        public CharacterStatModifiers statModifiers;
        private bool isPrefab = false;

        void Awake()
        {
            cardInfo = GetComponent<CardInfo>();
            gun = GetComponent<Gun>();
            cardStats = GetComponent<ApplyCardStats>();
            statModifiers = GetComponent<CharacterStatModifiers>();
            SetupCard(cardInfo, gun, cardStats, statModifiers);
        }

        void Start()
        {
            if (!isPrefab)
            {
                Destroy(transform.GetChild(1).gameObject);
            }
        }
        
        protected abstract string GetTitle();
        protected abstract string GetDescription();
        protected abstract CardInfoStat[] GetStats();
        protected abstract CardInfo.Rarity GetRarity();
        protected abstract GameObject GetCardArt();
        protected abstract CardThemeColor.CardThemeColorType GetTheme();
        public abstract void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers);
        public abstract void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats);
        public abstract void OnRemoveCard();

        public static void BuildCard<T>() where T : CustomCard
        {
            Unbound.Instance.ExecuteAfterFrames(2, () =>
            {
                // Instantiate card and mark to avoid destruction on scene change
                var newCard = Instantiate(Unbound.templateCard.gameObject, Vector3.up * 100, Quaternion.identity);
                newCard.transform.SetParent(null, true);
                var newCardInfo = newCard.GetComponent<CardInfo>();
                DontDestroyOnLoad(newCard);

                // Add custom ability handler
                var customCard = newCard.AddComponent<T>();
                customCard.isPrefab = true;

                // Clear default card info
                DestroyChildren(newCardInfo.cardBase.GetComponent<CardInfoDisplayer>().grid);

                // Apply card data
                newCardInfo.cardStats = customCard.GetStats() ?? new CardInfoStat[0];
                newCard.gameObject.name = newCardInfo.cardName = customCard.GetTitle();
                newCardInfo.cardDestription = customCard.GetDescription();
                newCardInfo.sourceCard = newCardInfo;
                newCardInfo.rarity = customCard.GetRarity();
                newCardInfo.colorTheme = customCard.GetTheme();
                newCardInfo.allowMultiple = true;
                newCardInfo.cardArt = customCard.GetCardArt() ?? new GameObject();
                
                // Fix sort order issue
                newCardInfo.cardBase.transform.position -= Camera.main.transform.forward * 0.5f;

                // Reset stats
                newCard.GetComponent<CharacterStatModifiers>().health = 1;

                // Finish initializing
                newCardInfo.SendMessage("Awake");
                PhotonNetwork.PrefabPool.RegisterPrefab(newCard.gameObject.name, newCard);

                // Add this card to the list of all custom cards
                Unbound.activeCards.Add(newCardInfo);
                Unbound.activeCards.Sort((x, y) => string.Compare(x.cardName, y.cardName));

                // Register card with the toggle menu
                CardToggleMenuHandler.Instance.AddCardToggle(newCardInfo);

                // Post-creation clean up
                newCardInfo.ExecuteAfterFrames(5, () =>
                {
                    // Destroy extra card face
                    Destroy(newCard.transform.GetChild(0).gameObject);

                    // Destroy extra art object
                    var artContainer = newCard.transform.Find("CardBase(Clone)(Clone)/Canvas/Front/Background/Art");
                    if (artContainer != null && artContainer.childCount > 1)
                        Destroy(artContainer.GetChild(0).gameObject);

                    // Disable "prefab"
                    newCard.SetActive(false);
                });
            });
        }
        
        private static void DestroyChildren(GameObject t)
        {
            while (t.transform.childCount > 0)
            {
                GameObject.DestroyImmediate(t.transform.GetChild(0).gameObject);
            }
        }
    }
}
