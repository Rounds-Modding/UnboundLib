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
        private bool isPrefab;

        private void Awake()
        {
            cardInfo = GetComponent<CardInfo>();
            gun = GetComponent<Gun>();
            cardStats = GetComponent<ApplyCardStats>();
            statModifiers = GetComponent<CharacterStatModifiers>();
            block = gameObject.GetOrAddComponent<Block>();
            SetupCard(cardInfo, gun, cardStats, statModifiers, block);
        }

        private void Start()
        {
            if (isPrefab) return;
            // add mod name text
            // create blank object for text, and attach it to the canvas
            GameObject modNameObj = new GameObject("ModNameText");
            // find bottom left edge object
            RectTransform[] allChildrenRecursive = gameObject.GetComponentsInChildren<RectTransform>();
            var edgeTransform = allChildrenRecursive.FirstOrDefault(obj => obj.gameObject.name == "EdgePart (2)");
            if (edgeTransform != null)
            {
                GameObject bottomLeftCorner = edgeTransform.gameObject;
                modNameObj.gameObject.transform.SetParent(bottomLeftCorner.transform);
            }

            TextMeshProUGUI modText = modNameObj.gameObject.AddComponent<TextMeshProUGUI>();
            modText.text = GetModName().Sanitize();
            modNameObj.transform.localEulerAngles = new Vector3(0f, 0f, 135f);

            modNameObj.transform.localScale = Vector3.one;
            modNameObj.AddComponent<SetLocalPos>();
            modText.alignment = TextAlignmentOptions.Bottom;
            modText.alpha = 0.1f;
            modText.fontSize = 54;

            Callback();
        }

        protected abstract string GetTitle();
        protected abstract string GetDescription();
        protected abstract CardInfoStat[] GetStats();
        protected abstract CardInfo.Rarity GetRarity();
        protected abstract GameObject GetCardArt();
        protected abstract CardThemeColor.CardThemeColorType GetTheme();
        protected virtual GameObject GetCardBase()
        {
            return Unbound.templateCard.cardBase;
        }

        public virtual void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers) { }

        public virtual void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            SetupCard(cardInfo, gun, cardStats, statModifiers);
        }

        public abstract void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats);

        public virtual void OnReassignCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            OnReassignCard();
        }
        public virtual void OnReassignCard()
        { }

        public virtual void OnRemoveCard() { }

        public virtual void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            OnRemoveCard();
        }

        /// <summary>
        /// Returns if the card should be enabled when built. Cards that are not enabled do not appear in the Toggle Cards menu, nor can be spawned in game by any regular means
        /// </summary>
        public virtual bool GetEnabled()
        {
            return true;
        }

        /// <summary>
        /// Returns the name of the mod this card is from. Should be unique.
        /// </summary>
        public virtual string GetModName()
        {
            return "Modded";
        }

        /// <summary>
        /// A callback method that is called each time the card is spawned in and fully instantiated
        /// </summary>
        public virtual void Callback()
        {

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
                DestroyImmediate(newCard.transform.GetChild(0).gameObject);
                newCard.transform.SetParent(null, true);
                var newCardInfo = newCard.GetComponent<CardInfo>();
                DontDestroyOnLoad(newCard);

                // Add custom ability handler
                var customCard = newCard.AddComponent<T>();
                customCard.isPrefab = true;
                newCardInfo.cardBase = customCard.GetCardBase();
                // Apply card data
                newCardInfo.cardStats = customCard.GetStats() ?? Array.Empty<CardInfoStat>();
                newCardInfo.cardName = customCard.GetTitle();
                newCard.gameObject.name = $"__{customCard.GetModName()}__{customCard.GetTitle()}".Sanitize();
                newCardInfo.cardDestription = customCard.GetDescription();
                newCardInfo.sourceCard = newCardInfo;
                newCardInfo.rarity = customCard.GetRarity();
                newCardInfo.colorTheme = customCard.GetTheme();
                newCardInfo.cardArt = customCard.GetCardArt();

                // Finish initializing
                PhotonNetwork.PrefabPool.RegisterPrefab(newCard.gameObject.name, newCard);

                // If the card is enabled
                if (customCard.GetEnabled())
                {
                    // Add this card to the list of all custom cards
                    CardManager.activeCards.Add(newCardInfo);
                    CardManager.activeCards = new ObservableCollection<CardInfo>(CardManager.activeCards.OrderBy(i => i.gameObject.name));
                    CardManager.activeCards.CollectionChanged += CardManager.CardsChanged;
                    // Register card with the toggle menu
                    CardManager.cards.Add(newCard.gameObject.name,
                        new Card(customCard.GetModName().Sanitize(), Unbound.config.Bind("Cards: " + customCard.GetModName().Sanitize(), newCard.gameObject.name, true), newCardInfo));
                }

                // Post-creation clean up
                newCardInfo.ExecuteAfterFrames(5, () =>
                {
                    callback?.Invoke(newCardInfo);
                });
            });
        }

        public static void BuildUnityCard<T>(GameObject cardPrefab, Action<CardInfo> callback) where T : CustomCard
        {
            CardInfo cardInfo = cardPrefab.GetComponent<CardInfo>();
            CustomCard customCard = cardPrefab.GetOrAddComponent<T>();

            cardInfo.cardBase = customCard.GetCardBase();
            cardInfo.cardStats = customCard.GetStats();
            cardInfo.cardName = customCard.GetTitle();
            cardInfo.gameObject.name = $"__{customCard.GetModName()}__{customCard.GetTitle()}".Sanitize();
            cardInfo.cardDestription = customCard.GetDescription();
            cardInfo.sourceCard = cardInfo;
            cardInfo.rarity = customCard.GetRarity();
            cardInfo.colorTheme = customCard.GetTheme();
            cardInfo.cardArt = customCard.GetCardArt();

            PhotonNetwork.PrefabPool.RegisterPrefab(cardInfo.gameObject.name, cardPrefab);

            if (customCard.GetEnabled())
            {
                CardManager.cards.Add(cardInfo.gameObject.name, new Card(customCard.GetModName().Sanitize(), Unbound.config.Bind("Cards: " + customCard.GetModName().Sanitize(), cardInfo.gameObject.name, true), cardInfo));
            }

            customCard.Awake();

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                callback?.Invoke(cardInfo);
            });
        }

        public void BuildUnityCard(Action<CardInfo> callback)
        {
            CardInfo cardInfo = this.gameObject.GetComponent<CardInfo>();
            CustomCard customCard = this;
            GameObject cardPrefab = this.gameObject;

            cardInfo.cardBase = customCard.GetCardBase();
            cardInfo.cardStats = customCard.GetStats();
            cardInfo.cardName = customCard.GetTitle();
            cardInfo.gameObject.name = $"__{customCard.GetModName()}__{customCard.GetTitle()}".Sanitize();
            cardInfo.cardDestription = customCard.GetDescription();
            cardInfo.sourceCard = cardInfo;
            cardInfo.rarity = customCard.GetRarity();
            cardInfo.colorTheme = customCard.GetTheme();
            cardInfo.cardArt = customCard.GetCardArt();

            PhotonNetwork.PrefabPool.RegisterPrefab(cardInfo.gameObject.name, cardPrefab);

            if (customCard.GetEnabled())
            {
                CardManager.cards.Add(cardInfo.gameObject.name, new Card(customCard.GetModName().Sanitize(), Unbound.config.Bind("Cards: " + customCard.GetModName().Sanitize(), cardInfo.gameObject.name, true), cardInfo));
            }

            this.Awake();

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                callback?.Invoke(cardInfo);
            });
        }

        public static void RegisterUnityCard<T>(GameObject cardPrefab, Action<CardInfo> callback) where T : CustomCard
        {
            CardInfo cardInfo = cardPrefab.GetComponent<CardInfo>();
            CustomCard customCard = cardPrefab.GetOrAddComponent<T>();

            cardInfo.gameObject.name = $"__{customCard.GetModName()}__{customCard.GetTitle()}".Sanitize();

            PhotonNetwork.PrefabPool.RegisterPrefab(cardInfo.gameObject.name, cardPrefab);

            if (customCard.GetEnabled())
            {
                CardManager.cards.Add(cardInfo.gameObject.name, new Card(customCard.GetModName().Sanitize(), Unbound.config.Bind("Cards: " + customCard.GetModName().Sanitize(), cardInfo.gameObject.name, true), cardInfo));
            }

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                callback?.Invoke(cardInfo);
            });
        }

        public static void RegisterUnityCard(GameObject cardPrefab, string modInitials, string cardname, bool enabled, Action<CardInfo> callback)
        {
            CardInfo cardInfo = cardPrefab.GetComponent<CardInfo>();

            cardInfo.gameObject.name = $"__{modInitials}__{cardname}".Sanitize();

            PhotonNetwork.PrefabPool.RegisterPrefab(cardInfo.gameObject.name, cardPrefab);

            if (enabled)
            {
                CardManager.cards.Add(cardInfo.gameObject.name, new Card(modInitials.Sanitize(), Unbound.config.Bind("Cards: " + cardname.Sanitize(), cardInfo.gameObject.name, true), cardInfo));
            }

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                callback?.Invoke(cardInfo);
            });
        }

        public void RegisterUnityCard(Action<CardInfo> callback)
        {
            CardInfo cardInfo = this.gameObject.GetComponent<CardInfo>();

            cardInfo.gameObject.name = $"__{this.GetModName()}__{this.GetTitle()}".Sanitize();

            PhotonNetwork.PrefabPool.RegisterPrefab(cardInfo.gameObject.name, this.gameObject);

            if (this.GetEnabled())
            {
                CardManager.cards.Add(cardInfo.gameObject.name, new Card(this.GetModName().Sanitize(), Unbound.config.Bind("Cards: " + this.GetModName().Sanitize(), cardInfo.gameObject.name, true), cardInfo));
            }

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                callback?.Invoke(cardInfo);
            });
        }

        private static void DestroyChildren(GameObject t)
        {
            while (t.transform.childCount > 0)
            {
                DestroyImmediate(t.transform.GetChild(0).gameObject);
            }
        }

        private static void ResetOnlyGunStats(Gun gun)
        {
            gun.isReloading = false;
            gun.damage = 1f;
            gun.reloadTime = 1f;
            gun.reloadTimeAdd = 0f;
            gun.recoilMuiltiplier = 1f;
            gun.knockback = 1f;
            gun.attackSpeed = 1f;
            gun.projectileSpeed = 1f;
            gun.projectielSimulatonSpeed = 1f;
            gun.gravity = 1f;
            gun.damageAfterDistanceMultiplier = 1f;
            gun.bulletDamageMultiplier = 1f;
            gun.multiplySpread = 1f;
            gun.shakeM = 1f;
            gun.ammo = 0;
            gun.ammoReg = 0f;
            gun.size = 0f;
            gun.overheatMultiplier = 0f;
            gun.timeToReachFullMovementMultiplier = 0f;
            gun.numberOfProjectiles = 1;
            gun.bursts = 0;
            gun.reflects = 0;
            gun.smartBounce = 0;
            gun.bulletPortal = 0;
            gun.randomBounces = 0;
            gun.timeBetweenBullets = 0f;
            gun.projectileSize = 0f;
            gun.speedMOnBounce = 1f;
            gun.dmgMOnBounce = 1f;
            gun.drag = 0f;
            gun.dragMinSpeed = 1f;
            gun.spread = 0f;
            gun.evenSpread = 0f;
            gun.percentageDamage = 0f;
            gun.cos = 0f;
            gun.slow = 0f;
            gun.chargeNumberOfProjectilesTo = 0f;
            gun.destroyBulletAfter = 0f;
            gun.forceSpecificAttackSpeed = 0f;
            gun.lockGunToDefault = false;
            gun.unblockable = false;
            gun.ignoreWalls = false;
            gun.currentCharge = 0f;
            gun.useCharge = false;
            gun.waveMovement = false;
            gun.teleport = false;
            gun.spawnSkelletonSquare = false;
            gun.explodeNearEnemyRange = 0f;
            gun.explodeNearEnemyDamage = 0f;
            gun.hitMovementMultiplier = 1f;
            gun.isProjectileGun = false;
            gun.defaultCooldown = 1f;
            gun.attackSpeedMultiplier = 1f;
            gun.objectsToSpawn = Array.Empty<ObjectsToSpawn>();
            gun.projectileColor = Color.black;
        }

        private static void ResetOnlyCharacterStatModifiersStats(CharacterStatModifiers characterStatModifiers)
        {
            for (int i = 0; i < characterStatModifiers.objectsAddedToPlayer.Count; i++)
            {
                Destroy(characterStatModifiers.objectsAddedToPlayer[i]);
            }
            characterStatModifiers.objectsAddedToPlayer.Clear();
            characterStatModifiers.sizeMultiplier = 1f;
            characterStatModifiers.health = 1f;
            characterStatModifiers.movementSpeed = 1f;
            characterStatModifiers.jump = 1f;
            characterStatModifiers.gravity = 1f;
            characterStatModifiers.slow = 0f;
            characterStatModifiers.slowSlow = 0f;
            characterStatModifiers.fastSlow = 0f;
            characterStatModifiers.secondsToTakeDamageOver = 0f;
            characterStatModifiers.numberOfJumps = 0;
            characterStatModifiers.regen = 0f;
            characterStatModifiers.lifeSteal = 0f;
            characterStatModifiers.respawns = 0;
            characterStatModifiers.refreshOnDamage = false;
            characterStatModifiers.automaticReload = true;
            characterStatModifiers.tasteOfBloodSpeed = 1f;
            characterStatModifiers.rageSpeed = 1f;
            characterStatModifiers.attackSpeedMultiplier = 1f;
        }

        private static void ResetOnlyBlockStats(Block block)
        {
            block.objectsToSpawn = new List<GameObject>();
            block.sinceBlock = 10f;
            block.cooldown = 4f;
            block.counter = 1000f;
            block.cdMultiplier = 1f;
            block.cdAdd = 0f;
            block.forceToAdd = 0f;
            block.forceToAddUp = 0f;
            block.autoBlock = false;
            block.blockedThisFrame = false;
            block.additionalBlocks = 0;
            block.healing = 0f;
            block.delayOtherActions = false;
        }

        private class SetLocalPos : MonoBehaviour
        {
            private readonly Vector3 localpos = new Vector3(-50f, -50f, 0f);

            private void Update()
            {
                if (gameObject.transform.localPosition == localpos) return;
                gameObject.transform.localPosition = localpos;
                Destroy(this, 1f);
            }
        }

    }
}
