using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnboundLib.Extensions;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(Gun), "ApplyProjectileStats")]
    class Gun_Patch_ApplyProjectileStats
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            List<CodeInstruction> ins = instructions.ToList();

            int idx = -1;

            for (int i = 0; i < ins.Count(); i++)
            {
                // we only want to change the first occurence here
                if (!ins[i].LoadsField(f_playerID)) continue;
                idx = i;
                break;
            }
            if (idx == -1)
            {
                throw new Exception("[ApplyProjectileStats PATCH] INSTRUCTION NOT FOUND");
            }
            // get colorID instead of playerID
            ins[idx] = new CodeInstruction(OpCodes.Call, m_colorID);

            return ins.AsEnumerable();
        }
    }
}

