﻿using HarmonyLib;
using UnboundLib.GameModes;
using System.Linq;
using System.Collections.Generic;

namespace UnboundLib.Patches
{

    [HarmonyPatch(typeof(GM_Test), "Start")]
    class GM_Test_Patch_Start
    {
        static void Prefix()
        {
            GameModeManager.TriggerHook(GameModeHooks.HookInitStart);
            GameModeManager.TriggerHook(GameModeHooks.HookInitEnd);
            GameModeManager.TriggerHook(GameModeHooks.HookGameStart);
        }

        static void Postfix()
        {
            GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Remove the default player joined and died -hooks. We'll add them back through the GameMode abstraction layer.
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_pmInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var m_playerDied = ExtensionMethods.GetMethodInfo(typeof(GM_Test), "PlayerDied");
            var m_addPlayerDied = ExtensionMethods.GetMethodInfo(typeof(PlayerManager), "AddPlayerDiedAction");
            var m_getPlayerJoinedAction = ExtensionMethods.GetPropertyInfo(typeof(PlayerManager), "PlayerJoinedAction").GetGetMethod();
            var m_setPlayerJoinedAction = ExtensionMethods.GetPropertyInfo(typeof(PlayerManager), "PlayerJoinedAction").GetSetMethod(true);

            for (int i = 0; i < list.Count; i++)
            {
                if (
                    list[i].LoadsField(f_pmInstance) &&
                    list[i + 2].OperandIs(m_playerDied) &&
                    list[i + 4].Calls(m_addPlayerDied)
                )
                {
                    i += 4;
                }
                else if (
                    list[i].LoadsField(f_pmInstance) &&
                    list[i + 2].Calls(m_getPlayerJoinedAction) &&
                    list[i + 8].Calls(m_setPlayerJoinedAction)
                )
                {
                    i += 8;
                }
                else
                {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }
}
