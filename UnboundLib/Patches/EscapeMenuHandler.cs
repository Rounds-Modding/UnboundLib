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
            if (Input.GetKeyDown(KeyCode.Escape) && !ToggleCardsMenuHandler.instance.disableEscapeButton)
            {
                if (ToggleLevelMenuHandler.instance.mapMenuCanvas.transform.Find("MapMenu/InfoMenu").gameObject.activeInHierarchy)
                {
                    ToggleLevelMenuHandler.instance.mapMenuCanvas.transform.Find("MapMenu/InfoMenu").gameObject
                        .SetActive(false);
                    return false;
                }
                
                if (ToggleCardsMenuHandler.instance.cardMenuCanvas.transform.Find("CardMenu/InfoMenu").gameObject.activeInHierarchy)
                {
                    ToggleCardsMenuHandler.instance.cardMenuCanvas.transform.Find("CardMenu/InfoMenu").gameObject
                        .SetActive(false);
                    if(ToggleCardsMenuHandler.instance.menuOpenFromOutside) ToggleCardsMenuHandler.Close();
                    return false;
                }

                if (ToggleLevelMenuHandler.instance.mapMenuCanvas.activeInHierarchy)
                {
                    ToggleLevelMenuHandler.instance.mapMenuCanvas.SetActive (false);
                    return false;
                }
                
                if (!ToggleCardsMenuHandler.instance.disableEscapeButton && ToggleCardsMenuHandler.instance.cardMenuCanvas.activeInHierarchy)
                {
                    ToggleCardsMenuHandler.SetActive(ToggleCardsMenuHandler.instance.cardMenuCanvas.transform, false);
                    if(ToggleCardsMenuHandler.instance.menuOpenFromOutside) ToggleCardsMenuHandler.Close();
                    return false;
                }
                
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