using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnboundLib.GameModes;
using UnityEngine;
using UnboundLib.Extensions;

namespace UnboundLib.Patches
{
    internal static class ArmsRacePatchUtils
    {
        internal static Type GetMethodNestedType(string method)
        {
            var nestedTypes = typeof(GM_ArmsRace).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            return nestedTypes.FirstOrDefault(type => type.Name.Contains(method));
        }

        internal static IEnumerator TriggerPlayerPickStart()
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);
        }

        internal static IEnumerator TriggerPlayerPickEnd()
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);
        }

        internal static IEnumerator TriggerPickStart()
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickStart);
        }

        internal static IEnumerator TriggerPickEnd()
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPickEnd);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "Start")]

    public class GM_ArmsRace_Patch_Start
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Remove the default player joined and died -hooks. We'll add them back through the GameMode abstraction layer.
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_pmInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var m_playerDied = ExtensionMethods.GetMethodInfo(typeof(GM_ArmsRace), "PlayerDied");
            var m_addPlayerDied = ExtensionMethods.GetMethodInfo(typeof(PlayerManager), "AddPlayerDiedAction");
            var m_getPlayerJoinedAction = ExtensionMethods.GetPropertyInfo(typeof(PlayerManager), "PlayerJoinedAction").GetGetMethod();
            var m_setPlayerJoinedAction = ExtensionMethods.GetPropertyInfo(typeof(PlayerManager), "PlayerJoinedAction").GetSetMethod(true);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_pmInstance) &&
                    list[i + 2].OperandIs(m_playerDied) &&
                    list[i + 4].Calls(m_addPlayerDied))
                {
                    i += 4;
                }
                else if (
                    list[i].LoadsField(f_pmInstance) &&
                    list[i + 2].Calls(m_getPlayerJoinedAction) &&
                    list[i + 8].Calls(m_setPlayerJoinedAction))
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
    [HarmonyPatch(typeof(GM_ArmsRace), "StartGame")]
    class GM_ArmsRace_Patch_StartGame
    {
        // Postfix to reset previousRound/PointWinners
        static void Postfix(GM_ArmsRace __instance)
        {
            __instance.GetAdditionalData().previousPointWinners = new int[] { };
            __instance.GetAdditionalData().previousRoundWinners = new int[] { };
        }
    }
    [HarmonyPatch(typeof(GM_ArmsRace), "DoStartGame")]
    class GM_ArmsRace_Patch_DoStartGame
    {
        static IEnumerator Postfix(IEnumerator e)
        {
            // rebuild the cardbar first

            CardBarHandler.instance.Rebuild();

            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);

            // We need to iterate through yields like this to get the postfix in the correct place
            while (e.MoveNext())
            {
                yield return e.Current;
            }

            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);
        }
    }

    [HarmonyPatch]
    class GM_ArmsRace_TranspilerPatch_DoStartGame
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(ArmsRacePatchUtils.GetMethodNestedType("DoStartGame"), "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
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
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickStart));
                    gen.AddYieldReturn(newInstructions);
                    i++;
                    continue;
                }

                if (list[i].LoadsField(f_cardChoiceVisualsInstance) && list[i + 4].Calls(m_cardChoiceVisualsShow))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickStart));
                    gen.AddYieldReturn(newInstructions);
                }

                if (list[i].Calls(m_cardChoiceInstancePick))
                {
                    newInstructions.AddRange(list.GetRange(i, 10));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickEnd));
                    gen.AddYieldReturn(newInstructions);
                    i += 9;
                    continue;
                }

                if (list[i].Calls(m_cardChoiceVisualsHide))
                {
                    newInstructions.Add(list[i]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickEnd));
                    gen.AddYieldReturn(newInstructions);
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

        static void ResetPoints()
        {
            GM_ArmsRace.instance.p1Points = 0;
            GM_ArmsRace.instance.p2Points = 0;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_cardChoiceInstance = AccessTools.Field(typeof(CardChoice), "instance");
            var m_cardChoiceInstancePick = ExtensionMethods.GetMethodInfo(typeof(CardChoice), "DoPick");
            var f_pickPhase = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "pickPhase");
            var f_playerManagerInstance = AccessTools.Field(typeof(PlayerManager), "instance");
            var m_showPlayers = ExtensionMethods.GetMethodInfo(typeof(PlayerManager), "SetPlayersVisible");
            var m_winSequence = ExtensionMethods.GetMethodInfo(typeof(PointVisualizer), "DoWinSequence");
            var m_startCoroutine = ExtensionMethods.GetMethodInfo(typeof(MonoBehaviour), "StartCoroutine", new Type[] { typeof(IEnumerator) });

            var m_triggerPickStart = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPickStart");
            var m_triggerPickEnd = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPickEnd");
            var m_triggerPlayerPickStart = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPlayerPickStart");
            var m_triggerPlayerPickEnd = ExtensionMethods.GetMethodInfo(typeof(ArmsRacePatchUtils), "TriggerPlayerPickEnd");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Calls(m_winSequence) &&
                    list[i + 1].Calls(m_startCoroutine) &&
                    list[i + 2].opcode == OpCodes.Pop)
                {
                    newInstructions.AddRange(list.GetRange(i, 3));
                    newInstructions.Add(CodeInstruction.Call(typeof(GM_ArmsRace_TranspilerPatch_RoundTransition), "ResetPoints"));
                    i += 2;
                    continue;
                }

                if (list[i].LoadsField(f_pickPhase))
                {
                    newInstructions.Add(list[i]);
                    newInstructions.Add(list[i + 1]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickStart));
                    gen.AddYieldReturn(newInstructions);
                    i++;
                    continue;
                }

                if (list[i].opcode == OpCodes.Ldarg_0 &&
                    list[i + 1].LoadsField(f_cardChoiceInstance) &&
                    list[i + 10].Calls(m_cardChoiceInstancePick))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickStart));
                    gen.AddYieldReturn(newInstructions);

                    newInstructions.AddRange(list.GetRange(i, 20));

                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPlayerPickEnd));
                    gen.AddYieldReturn(newInstructions);

                    i += 19;
                    continue;
                }

                if (list[i].LoadsField(f_playerManagerInstance) &&
                    list[i + 1].opcode == OpCodes.Ldc_I4_1 &&
                    list[i + 2].Calls(m_showPlayers))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_triggerPickEnd));
                    gen.AddYieldReturn(newInstructions);
                }

                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "PointTransition")]
    class GM_ArmsRace_Patch_PointTransition
    {
        static IEnumerator Postfix(IEnumerator e)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);

            // We need to iterate through yields like this to get the postfix in the correct place
            while (e.MoveNext())
            {
                yield return e.Current;
            }

            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "RoundTransition")]
    class GM_ArmsRace_Patch_RoundTransition
    {
        static IEnumerator Postfix(IEnumerator e, GM_ArmsRace __instance, int winningTeamID)
        {
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointEnd);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundEnd);

            // Check game over after round end trigger to allow more control in triggers
            if (__instance.p1Rounds >= __instance.roundsToWinGame || __instance.p2Rounds >= __instance.roundsToWinGame)
            {
                __instance.InvokeMethod("GameOver", winningTeamID);
                yield break;
            }

            // We need to iterate through yields like this to get the postfix in the correct place
            while (e.MoveNext())
            {
                yield return e.Current;
            }

            yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookPointStart);
            yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "GameOverTransition")]
    class GM_ArmsRace_Patch_GameOverTransition
    {
        static IEnumerator Postfix(IEnumerator e)
        {
            // We're really adding a prefix, but we get access to the IEnumerator in the postfix
            yield return GameModeManager.TriggerHook(GameModeHooks.HookGameEnd);

            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }
    [HarmonyPatch(typeof(GM_ArmsRace), "PointOver")]
    class GM_ArmsRace_Patch_PointOver
    {
        static void Postfix(GM_ArmsRace __instance, int winningTeamID)
        {
            __instance.GetAdditionalData().previousPointWinners = new int[] { winningTeamID };
        }
    }
    [HarmonyPatch(typeof(GM_ArmsRace), "RoundOver")]
    class GM_ArmsRace_Patch_RoundOver
    {
        static void Postfix(GM_ArmsRace __instance, int winningTeamID)
        {
            __instance.GetAdditionalData().previousRoundWinners = new int[] { winningTeamID };
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            // Do not set p1Points and p2Points to zero in RoundOver. We want to do it only after we've displayed them in RoundTransition.
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_p1Points = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "p1Points");
            var f_p2Points = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "p2Points");

            for (int i = 0; i < list.Count; i++)
            {
                if (i < list.Count - 2 && (list[i + 2].StoresField(f_p1Points) || list[i + 2].StoresField(f_p2Points)))
                {
                    i += 2;
                    continue;
                }

                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(GM_ArmsRace), "RPCA_NextRound")]
    class GM_ArmsRace_Patch_RPCA_NextRound
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            // Do not call GameOver in RPCA_NextRound. We move game over check to RoundTransition to handle triggers better.
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();
            var mGameOver = ExtensionMethods.GetMethodInfo(typeof(GM_ArmsRace), "GameOver");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Ldarg_2 && list[i + 1].Calls(mGameOver))
                {
                    newInstructions.Add(list[i]);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
                    newInstructions.Add(CodeInstruction.Call(typeof(GM_ArmsRace), "RoundOver"));
                    i++;
                    continue;
                }

                newInstructions.Add(list[i]);
            }

            return newInstructions;
        }
    }
}
