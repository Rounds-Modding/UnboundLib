using HarmonyLib;
using UnboundLib.Cards;

namespace UnboundLib.Patches
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
}
