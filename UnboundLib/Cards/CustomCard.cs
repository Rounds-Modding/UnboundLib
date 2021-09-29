using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Linq;
using UnboundLib.Utils;

namespace UnboundLib.Cards
{
    public abstract class CustomCard : MonoBehaviour
    {
        public static List<CardInfo> cards = new List<CardInfo>();

        public CardInfo cardInfo;
        public Gun gun;
        public ApplyCardStats cardStats;
        public CharacterStatModifiers statModifiers;
        public Block block;
        private bool isPrefab = false;

        void Awake()
        {
            cardInfo = GetComponent<CardInfo>();
            gun = GetComponent<Gun>();
            cardStats = GetComponent<ApplyCardStats>();
            statModifiers = GetComponent<CharacterStatModifiers>();
            block = gameObject.GetOrAddComponent<Block>();
            SetupCard(cardInfo, gun, cardStats, statModifiers, block);
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
        public virtual void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {

        }
        public virtual void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            SetupCard(cardInfo, gun, cardStats, statModifiers);
        }
        public abstract void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats);
        public virtual void OnRemoveCard()
        { }
        public virtual void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            OnRemoveCard();
        }
        public virtual bool GetEnabled()
        {
            return true;
        }
        public virtual string GetModName()
        {
            return "Modded";
        }

        public static void BuildCard<T>() where T : CustomCard
        {
            BuildCard<T>(null);
        }
        public static void BuildCard<T>(Action<CardInfo> callback) where T : CustomCard
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
                newCardInfo.cardArt = customCard.GetCardArt();

                // add mod name text
                // create blank object for text, and attach it to the canvas
                GameObject modNameObj = new GameObject("ModNameText");
                // find bottom left edge object
                RectTransform[] allChildrenRecursive = newCard.gameObject.GetComponentsInChildren<RectTransform>();
                GameObject BottomLeftCorner = allChildrenRecursive.Where(obj => obj.gameObject.name == "EdgePart (2)").FirstOrDefault().gameObject;
                modNameObj.gameObject.transform.SetParent(BottomLeftCorner.transform);
                TextMeshProUGUI modText = modNameObj.gameObject.AddComponent<TextMeshProUGUI>();
                modText.text = customCard.GetModName();
                modNameObj.transform.Rotate(new Vector3(0f, 0f, 1f), 45f);
                modNameObj.transform.Rotate(new Vector3(0f, 1f, 0f), 180f);
                modNameObj.transform.localScale = new Vector3(1f, 1f, 1f);
                modNameObj.AddComponent<SetLocalPos>();
                modText.alignment = TextAlignmentOptions.Bottom;
                modText.alpha = 0.1f;
                modText.fontSize = 54;


                // Fix sort order issue
                newCardInfo.cardBase.transform.position -= Camera.main.transform.forward * 0.5f;

                // Reset stats
                newCard.GetComponent<CharacterStatModifiers>().health = 1;

                // Finish initializing
                newCardInfo.SendMessage("Awake");
                PhotonNetwork.PrefabPool.RegisterPrefab(newCard.gameObject.name, newCard);

                // If the card is enabled
                if (customCard.GetEnabled())
                {
                    // Add this card to the list of all custom cards
                    CardManager.activeCards.Add(newCardInfo);
                    CardManager.activeCards = new ObservableCollection<CardInfo>(CardManager.activeCards.OrderBy(i => i.cardName));
                    CardManager.activeCards.CollectionChanged += CardManager.CardsChanged;
                    // Register card with the toggle menu
                    CardManager.cards.Add(newCardInfo.cardName,
                        new Card(customCard.GetModName(), Unbound.config.Bind("Cards: " + customCard.GetModName(), newCardInfo.cardName, true).Value, newCardInfo));
                }

                

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

                    callback?.Invoke(newCardInfo);

                });
            });
        }

        private static void DestroyChildren(GameObject t)
        {
            while (t.transform.childCount > 0)
            {
                DestroyImmediate(t.transform.GetChild(0).gameObject);
            }
        }

        class SetLocalPos : MonoBehaviour
        {
            private readonly Vector3 localpos = new Vector3(-50f, -50f, 0f);
            void Update()
            {
                if (gameObject.transform.localPosition != localpos)
                {
                    gameObject.transform.localPosition = localpos;
                    Destroy(this,1f);
                }
            }
        }

    }
}
