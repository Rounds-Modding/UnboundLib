using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnboundLib.Cards;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Utils;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(Player), "Start")]
    class Player_Patch_Start
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /* Removes
             *   GM_ArmsRace instance = GM_ArmsRace.instance;
			 *   instance.StartGameAction += this.GetFaceOffline;
			 * from Player::Start
             */
            var f_gmInstance = AccessTools.Field(typeof(GM_ArmsRace), "instance");
            var f_startGameAction = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "StartGameAction");
            var m_getFace = ExtensionMethods.GetMethodInfo(typeof(Player), "GetFaceOffline");

            var list = instructions.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_gmInstance) &&
                    list[i + 2].LoadsField(f_startGameAction) &&
                    list[i + 4].OperandIs(m_getFace) &&
                    list[i + 8].LoadsField(f_startGameAction))
                {
                    i += 8;
                }
                else
                {
                    yield return list[i];
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    class Player_Patch_Awake
    {
        static void Postfix(Player __instance)
        {
            if (__instance.data.view.IsMine)
            {
                GameModeManager.AddOnceHook(GameModeHooks.HookGameStart, gm => OnGameStart(gm, __instance));
            }
        }

        static IEnumerator OnGameStart(IGameModeHandler gm, Player player)
        {
            if (gm.Name != "Sandbox")
            {
                player.GetFaceOffline();
            }
            yield break;
        }
    }
    [HarmonyPatch(typeof(Player), "SetColors")]
    class Player_Patch_SetColors
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // we want colorID instead of teamID
                    yield return new CodeInstruction(OpCodes.Call, m_colorID); // call the colorID method, which pops the player instance off the stack and leaves the result [colorID, ...]
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
    [HarmonyPatch(typeof(Player), "GetTeamColors")]
    class Player_Patch_GetTeamColors
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // we want colorID instead of teamID
                    yield return new CodeInstruction(OpCodes.Call, m_colorID); // call the colorID method, which pops the player instance off the stack and leaves the result [colorID, ...]
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
    [HarmonyPatch(typeof(Player), "FullReset")]
    class Player_Patch_FullReset
    {
        static void Postfix(Player __instance)
        {
            foreach (CardInfo currentCard in __instance.data.currentCards)
            {
                if(currentCard.GetComponent<CustomCard>() is CustomCard customCard)
                {
                    try
                    {
                        Gun gun = __instance.GetComponent<Holding>().holdable.GetComponent<Gun>();
                        CharacterData characterData = __instance.GetComponent<CharacterData>();
                        HealthHandler healthHandler = __instance.GetComponent<HealthHandler>();
                        Gravity gravity = __instance.GetComponent<Gravity>();
                        Block block = __instance.GetComponent<Block>();
                        GunAmmo gunAmmo = gun.GetComponentInChildren<GunAmmo>();
                        CharacterStatModifiers characterStatModifiers = __instance.GetComponent<CharacterStatModifiers>();
                        customCard.OnRemoveCard(__instance, gun, gunAmmo, characterData, healthHandler, gravity, block, characterStatModifiers);
                    }
                    catch (NotImplementedException)
                    { }
                    catch (Exception exception)
                    {
                        UnityEngine.Debug.LogError($"{exception.GetType()}\nThrown by: {customCard.GetModName()} - {currentCard.cardName} - OnRemoveCard()");
                        UnityEngine.Debug.LogException(exception);
                    }
                }
            }
            __instance.data.currentCards.Clear();
        }
    }
}
