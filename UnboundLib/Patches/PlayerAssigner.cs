using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using UnboundLib.Extensions;

namespace UnboundLib.Patches
{
    [HarmonyPatch]
    class PlayerAssigner_Patch_CreatePlayer
    {
        static MethodBase TargetMethod()
        {
            var nestedTypes = typeof(PlayerAssigner).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedCreatePlayerType = null;

            foreach (var type in nestedTypes)
            {
                if (type.Name.Contains("CreatePlayer"))
                {
                    nestedCreatePlayerType = type;
                }
            }

            return AccessTools.Method(nestedCreatePlayerType, "MoveNext");
        }

        static void AssignColorID(CharacterData characterData)
        {
            characterData.player.AssignColorID(characterData.player.teamID);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // add player.AssignColorID right after RegisterPlayer
            // this way everything gets colored properly

            var m_registerPlayer = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerAssigner), "RegisterPlayer");
            var m_assignColorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerAssigner_Patch_CreatePlayer), nameof(PlayerAssigner_Patch_CreatePlayer.AssignColorID));

            foreach (var ins in instructions)
            {
                if (ins.Calls(m_registerPlayer))
                {
                    yield return ins;
                    // load the newly created character data onto the stack (local variable in slot 3) [characterData, ...]
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    // call assignColorID which takes the character data off the stack [...]
                    yield return new CodeInstruction(OpCodes.Call,m_assignColorID);
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
}
