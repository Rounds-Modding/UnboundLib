using HarmonyLib;
using UnboundLib.Utils;
using UnityEngine;

namespace UnboundLib.Patches
{

    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkin")]
    class PlayerSkinBank_Patch_GetPlayerSkin
    {
        static bool Prefix(int team, ref PlayerSkinBank.PlayerSkinInstance __result)
        {
            __result = new PlayerSkinBank.PlayerSkinInstance() { currentPlayerSkin = PlayerSkinBank.GetPlayerSkinColors(team) };

            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerSkinBank), "GetPlayerSkinColors")]
    class PlayerSkinBank_Patch_GetPlayerSkinColors
    {
        static bool Prefix(int team, ref PlayerSkin __result)
        {

            __result = ExtraPlayerSkins.GetPlayerSkinColors(team);

            return false;
        }

        static void SetPlayerSkinColor(Player player, Color colorMaxToSet, Color colorMinToSet)
        {
            if (player.gameObject.GetComponentInChildren<PlayerSkinHandler>().simpleSkin)
            {
                SpriteMask[] sprites = player.gameObject.GetComponentInChildren<SetPlayerSpriteLayer>().transform.root.GetComponentsInChildren<SpriteMask>();
                foreach (var sprite in sprites)
                {
                    sprite.GetComponent<SpriteRenderer>().color = colorMaxToSet;
                }
                return;
            }

            PlayerSkinParticle[] playerSkinParticles = player.gameObject.GetComponentsInChildren<PlayerSkinParticle>();
            foreach (var playerSkinParticle in playerSkinParticles)
            {
                ParticleSystem particleSystem2 = (ParticleSystem) playerSkinParticle.GetFieldValue("part");
                ParticleSystem.MainModule main2 = particleSystem2.main;
                ParticleSystem.MinMaxGradient startColor2 = particleSystem2.main.startColor;
                startColor2.colorMin = colorMinToSet;
                startColor2.colorMax = colorMaxToSet;
                main2.startColor = startColor2;
            }

            SetTeamColor[] teamColors = player.transform.root.GetComponentsInChildren<SetTeamColor>();
            foreach (var teamColor in teamColors)
            {
                teamColor.Set(new PlayerSkin
                {
                    color = colorMaxToSet,
                    backgroundColor = colorMaxToSet,
                    winText = colorMaxToSet,
                    particleEffect = colorMaxToSet
                });
            }
        }
    }

}
