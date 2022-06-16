using HarmonyLib;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(SetTeamColor), "TeamColorThis")]
    class SetTeamColor_Patch_TeamColorThis
    {
        // replace go.GetComponentsInChildren<SetTeamColor>()
        // with
        // go.GetComponentsInChildren<SetTeamColor>(true)
        // to ensure that disabled components have their colors set properly
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m_getCompGeneric = ExtensionMethods.GetMethodInfo(typeof(GameObject), nameof(GameObject.GetComponentsInChildren), new System.Type[] { });
            var m_getAllGeneric = ExtensionMethods.GetMethodInfo(typeof(GameObject), nameof(GameObject.GetComponentsInChildren), new System.Type[] {typeof(bool)});
            var m_getSetTeamColor = m_getCompGeneric.MakeGenericMethod(typeof(SetTeamColor));
            var m_getAllSetTeamColor = m_getAllGeneric.MakeGenericMethod(typeof(SetTeamColor));

            foreach (var code in instructions)
            {
                if (code.Calls(m_getSetTeamColor))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_getAllSetTeamColor);
                }
                else
                {
                    yield return code;
                }
            }

        }
        static void Postfix(GameObject go, PlayerSkin teamColor)
        {
            // if the object to color is a player, make sure any unparented objects (smoke effects) are colored properly as well
            if (go?.GetComponent<PlayerJump>() != null && go.GetComponent<PlayerJump>().jumpPart.Any(j => j?.gameObject != null))
            {
                SetTeamColor.TeamColorThis(go.GetComponent<PlayerJump>().jumpPart.First(j => j?.gameObject != null).gameObject.transform.parent.gameObject, teamColor);
            }
        }
    }
}
