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
            if (Input.GetKeyDown(KeyCode.Escape) && __instance.togglers[1].activeInHierarchy)
            {
                if (ToggleLevelMenuHandler.instance.levelMenuCanvas.transform.Find("LevelMenu/InfoMenu").gameObject
                    .activeInHierarchy)
                {
                    ToggleLevelMenuHandler.instance.levelMenuCanvas.transform.Find("LevelMenu/InfoMenu").gameObject
                        .SetActive(false);
                    return false;
                }
                
                if (UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group").gameObject.activeInHierarchy)
                {
                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group").gameObject.SetActive(false);
                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject.SetActive(true);
                    return false;
                }
                
                if (ToggleLevelMenuHandler.instance.levelMenuCanvas.activeInHierarchy)
                {
                    ToggleLevelMenuHandler.instance.levelMenuCanvas.SetActive (false);
                    return false;
                }
            }

            return true;
        }
    }
}