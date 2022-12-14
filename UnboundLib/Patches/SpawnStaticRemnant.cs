﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnboundLib.Extensions;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(SpawnStaticRemnant), "Start")]
    class SpawnStaticRemnant_Patch_Start
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

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
    [HarmonyPatch(typeof(SpawnStaticRemnant), "Go")]
    class SpawnStaticRemnant_Patch_Go
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

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
}
