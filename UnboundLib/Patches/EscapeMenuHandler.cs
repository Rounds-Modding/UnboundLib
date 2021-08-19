using HarmonyLib;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(EscapeMenuHandler))]
    public class EscapeMenuHandlerPath
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool Update(EscapeMenuHandler __instance)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group").gameObject.activeInHierarchy)
                {
                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group").gameObject.SetActive(false);
                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject.SetActive(true);
                    return false;
                }
                foreach (Transform child in __instance.transform)
                {
                    if (child.Find("Group") && child.Find("Group").gameObject.activeInHierarchy)
                    {
                        return child.name == "Main";
                    }
                }
            }

            return true;
        }
    }
}