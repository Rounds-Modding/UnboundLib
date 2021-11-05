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
            gun.objectsToSpawn = new ObjectsToSpawn[0];
            gun.projectileColor = Color.black;
        }

        private static void ResetOnlyCharacterStatModifiersStats(CharacterStatModifiers characterStatModifiers)
        {
            for (int i = 0; i < characterStatModifiers.objectsAddedToPlayer.Count; i++)
            {
                GameObject.Destroy(characterStatModifiers.objectsAddedToPlayer[i]);
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
