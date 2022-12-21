using HarmonyLib;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CardChoice))]
    public class CardChoicePatchGetSourceCard
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CardChoice.GetSourceCard))]
        private static bool CheckHiddenCards(CardChoice __instance, CardInfo info, ref CardInfo __result)
        {
            for (int i = 0; i < __instance.cards.Length; i++)
            {
                if ((__instance.cards[i].gameObject.name + "(Clone)") == info.gameObject.name)
                {
                    __result = __instance.cards[i];
                    return false;
                }
            }
            __result = null;

            return false;
        }
    }
}
