using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnboundLib.Cards;

namespace UnboundLib
{
    [HarmonyPatch(typeof(ApplyCardStats), "ApplyStats")]
    class ApplyCardStats_Patch
    {
        static void Prefix(ApplyCardStats __instance, Player ___playerToUpgrade)
        {
            var player = ___playerToUpgrade.GetComponent<Player>();
            var gun = ___playerToUpgrade.GetComponent<Holding>().holdable.GetComponent<Gun>();
            var characterData = ___playerToUpgrade.GetComponent<CharacterData>();
            var healthHandler = ___playerToUpgrade.GetComponent<HealthHandler>();
            var gravity = ___playerToUpgrade.GetComponent<Gravity>();
            var block = ___playerToUpgrade.GetComponent<Block>();
            var gunAmmo = gun.GetComponentInChildren<GunAmmo>();
            var characterStatModifiers = player.GetComponent<CharacterStatModifiers>();

            CustomCard customAbility = __instance.gameObject.GetComponent<CustomCard>();
            if (customAbility != null)
            {
                customAbility.OnAddCard(player, gun, gunAmmo, characterData, healthHandler, gravity, block, characterStatModifiers);
            }
        }
    }

    [HarmonyPatch(typeof(CardBarHandler), "AddCard")]
    class CardBarHandler_Patch
    {
        static void Prefix(int teamId, CardInfo card)
        {
            CardData.AddCard(teamId, card.cardName);
        }
    }

    [HarmonyPatch(typeof(CardBar), "OnHover")]
    class CardBar_Patch
    {
        static void Postfix(CardBar __instance, CardInfo card, Vector3 hoverPos, GameObject ___currentCard)
        {
            ___currentCard.SetActive(true);
        }
    }
}
