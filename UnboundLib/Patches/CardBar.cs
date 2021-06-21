using HarmonyLib;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CardBar), "OnHover")]
    class CardBar_Patch
    {
        static void Postfix(CardBar __instance, CardInfo card, Vector3 hoverPos, GameObject ___currentCard)
        {
            ___currentCard.SetActive(true);
        }
    }
}
