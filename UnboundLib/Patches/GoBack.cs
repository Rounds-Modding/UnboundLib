// using HarmonyLib;
// using UnboundLib.Utils.UI;
// using UnityEngine;
//
// namespace UnboundLib.Patches
// {
//     [HarmonyPatch(typeof(GoBack))]
//     public class GoBackPatch
//     {
//         [HarmonyPatch("Update")]
//         [HarmonyPrefix]
//         private static bool Update()
//         {
//             if (Input.GetKeyDown(KeyCode.Escape))
//             {
//                 if (ToggleLevelMenuHandler.instance.mapMenuCanvas.transform.Find("MapMenu/InfoMenu").gameObject.activeInHierarchy)
//                 {
//                     ToggleLevelMenuHandler.instance.mapMenuCanvas.transform.Find("MapMenu/InfoMenu").gameObject
//                         .SetActive(false);
//                     return false;
//                 }
//                 
//                 if (ToggleCardsMenuHandler.cardMenuCanvas.transform.Find("CardMenu/InfoMenu").gameObject.activeInHierarchy)
//                 {
//                     ToggleCardsMenuHandler.cardMenuCanvas.transform.Find("CardMenu/InfoMenu").gameObject
//                         .SetActive(false);
//                     if(ToggleCardsMenuHandler.menuOpenFromOutside) ToggleCardsMenuHandler.Close();
//                     return false;
//                 }
//
//                 if (ToggleLevelMenuHandler.instance.mapMenuCanvas.activeInHierarchy)
//                 {
//                     ToggleLevelMenuHandler.instance.mapMenuCanvas.SetActive (false);
//                     return false;
//                 }
//                 
//                 if (!ToggleCardsMenuHandler.disableEscapeButton && ToggleCardsMenuHandler.cardMenuCanvas.activeInHierarchy)
//                 {
//                     ToggleCardsMenuHandler.SetActive(ToggleCardsMenuHandler.cardMenuCanvas.transform, false);
//                     if(ToggleCardsMenuHandler.menuOpenFromOutside) ToggleCardsMenuHandler.Close();
//                     return false;
//                 }
//             }
//
//             if (ModOptions.inPauseMenu)
//             {
//                 return false;
//             }
//             
//
//             return true;
//         }
//     }
// }