using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnboundLib.GameModes;

namespace UnboundLib.Patches
{
    internal static class ArmsRacePatchUtils
    {
        internal static Type GetMethodNestedType(string method)
        {
            var nestedTypes = typeof(GM_ArmsRace).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedType = null;

            foreach (var type in nestedTypes)
            {
                if (type.Name.Contains(method))
                {
                    nestedType = type;
                    break;
                }
            }

            return nestedType;
        }

        internal static void TriggerPlayerPickStart()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPlayerPickStart);
        }

        internal static void TriggerPlayerPickEnd()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPlayerPickEnd);
        }

        internal static void TriggerPickStart()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPickStart);
        }

        internal static void TriggerPickEnd()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPickEnd);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "Start")]
    class GM_ArmsRace_Patch_Start
    {
        static void Prefix()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookInitStart);
        }

        static void Postfix()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookInitEnd);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "DoStartGame")]
    class GM_ArmsRace_Patch_DoStartGame
    {
        static void Prefix()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookGameStart);
        }

        static IEnumerator Postfix(IEnumerator e)
        {
            // We need to iterate through yields like this to get the postfix in the correct place
            while (e.MoveNext())
            {
                yield return e.Current;
            }

            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookRoundStart);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPointStart);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookBattleStart);
        }
    }

    [HarmonyPatch]
    class GM_ArmsRace_TranspilerPatch_DoStartGame
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(ArmsRacePatchUtils.GetMethodNestedType("DoStartGame"), "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_cardChoiceVisualsInstance = AccessTools.Field(typeof(CardChoiceVisuals), "instance");
            var m_cardChoiceVisualsShow = ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals), "Show");
            var m_cardChoiceVisualsHide = ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals), "Hide");
            var m_cardChoiceInstancePick = ExtensionMethods.GetMethodInfo(typeof(CardChoice), "DoPick");
            var f_pickPhase = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "pickPhase");

            var m_triggerPickStart = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPickStart");
            var m_triggerPickEnd = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPickEnd");
            var m_triggerPlayerPickStart = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPlayerPickStart");
            var m_triggerPlayerPickEnd = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPlayerPickEnd");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_pickPhase))
                {
                    newInstructions.Add(list[i]);
                    newInstructions.Add(list[i + 1]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickStart));
                    i++;
                    continue;
                }

                if (list[i].LoadsField(f_cardChoiceVisualsInstance) && list[i + 4].Calls(m_cardChoiceVisualsShow))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickStart));
                }

                if (list[i].Calls(m_cardChoiceInstancePick))
                {
                    newInstructions.AddRange(list.GetRange(i, 8));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickEnd));
                    i += 7;
                    continue;
                }

                if (list[i].Calls(m_cardChoiceVisualsHide))
                {
                    newInstructions.Add(list[i]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickEnd));
                    continue;
                }

                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }

    [HarmonyPatch]
    class GM_ArmsRace_TranspilerPatch_RoundTransition
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(ArmsRacePatchUtils.GetMethodNestedType("RoundTransition"), "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_cardChoiceInstance = AccessTools.Field(typeof(CardChoice), "instance");
            var m_cardChoiceInstancePick = ExtensionMethods.GetMethodInfo(typeof(CardChoice), "DoPick");
            var f_pickPhase = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "pickPhase");
            var f_playerManagerInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var m_showPlayers = ExtensionMethods.GetMethodInfo(typeof(PlayerManager), "SetPlayersVisible");

            var m_triggerPickStart = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPickStart");
            var m_triggerPickEnd = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPickEnd");
            var m_triggerPlayerPickStart = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPlayerPickStart");
            var m_triggerPlayerPickEnd = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPlayerPickEnd");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_pickPhase))
                {
                    newInstructions.Add(list[i]);
                    newInstructions.Add(list[i + 1]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickStart));
                    i++;
                    continue;
                }

                if (list[i].opcode == OpCodes.Ldarg_0 && 
                    list[i + 1].LoadsField(f_cardChoiceInstance) && 
                    list[i + 10].Calls(m_cardChoiceInstancePick))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickStart));
                    newInstructions.AddRange(list.GetRange(i, 19));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickEnd));
                    i += 18;
                    continue;
                }

                if (list[i].LoadsField(f_playerManagerInstance) && 
                    list[i + 1].opcode == OpCodes.Ldc_I4_1 && 
                    list[i + 2].Calls(m_showPlayers))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickEnd));
                }

                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "PointOver")]
    class GM_ArmsRace_Patch_PointOver
    {
        static void Prefix()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPointEnd);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "PointTransition")]
    class GM_ArmsRace_Patch_PointTransition
    {
        static IEnumerator Postfix(IEnumerator e)
        {
            // We need to iterate through yields like this to get the postfix in the correct place
            while (e.MoveNext())
            {
                yield return e.Current;
            }

            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPointStart);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookBattleStart);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "RoundTransition")]
    class GM_ArmsRace_Patch_RoundTransition
    {
        static IEnumerator Postfix(IEnumerator e)
        {
            // We need to iterate through yields like this to get the postfix in the correct place
            while (e.MoveNext())
            {
                yield return e.Current;
            }

            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookRoundStart);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPointStart);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookBattleStart);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "RoundOver")]
    class GM_ArmsRace_Patch_RoundOver
    {
        static void Prefix()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPointEnd);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookRoundEnd);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "GameOver")]
    class GM_ArmsRace_Patch_GameOver
    {
        static void Prefix()
        {
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookPointEnd);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookRoundEnd);
            GameModeHandler.TriggerHook("ArmsRace", GameModeHooks.HookGameEnd);
        }
    }
}
