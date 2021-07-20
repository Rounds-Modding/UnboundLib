using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jotunn.Utils;
using TMPro;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnboundLib.UI
{

    public class Credits
    {

        internal Dictionary<string, string[]> modCredits = new Dictionary<string, string[]>();
        private Dictionary<string, GameObject> creditsMenus = new Dictionary<string, GameObject>();

        public static Credits Instance = new Credits();

        private static readonly string[] roundsCredits = new string[] { "Landfall Games (Publisher)", "Wilhelm Nulynd (Game design, programming and art)", "Karl Flodin (Music)", "Pontus Ullbors (Card and Face Art)", "Natalia Martinsson (Face Art)", "Sonigon (Sound design)" };

        private GameObject CreditsMenu;
        private GameObject RoundsCredits;
        private GameObject UnboundCredits;

        private Credits()
        {
            // singleton first time setup
            Credits.Instance = this;

        }
        internal void CreateCreditsMenu(bool firstTime)
        {
            // create mod options
            Unbound.Instance.ExecuteAfterSeconds(firstTime ? 0.2f : 0f, () =>
            {
                // Create mod options menu
                CreditsMenu = MenuHandler.Instance.CreateMenu("CREDITS", null, MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main").gameObject, 60, true, false, true, 5);

                // Fix main menu layout
                void fixMainMenuLayout()
                {
                    var mainMenu = MainMenuHandler.instance.transform.Find("Canvas/ListSelector");
                    var logo = mainMenu.Find("Main/Group/Rounds_Logo2_White").gameObject.AddComponent<LayoutElement>();
                    logo.GetComponent<RectTransform>().sizeDelta = new Vector2(logo.GetComponent<RectTransform>().sizeDelta.x, 80);
                    mainMenu.Find("Main").transform.position = new Vector3(0, 1.7f, mainMenu.Find("Main").transform.position.z);
                    mainMenu.Find("Main/Group").GetComponent<VerticalLayoutGroup>().spacing = 10;
                }

                var visibleObj = new GameObject("visible");
                var visible = visibleObj.AddComponent<ActionOnBecameVisible>();
                visibleObj.AddComponent<SpriteRenderer>();
                visible.visibleAction += fixMainMenuLayout;
                visibleObj.transform.parent = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main");

                // Create Rounds Credits
                RoundsCredits = MenuHandler.Instance.CreateMenu("ROUNDS", null, CreditsMenu, 30);
                creditsMenus["ROUNDS"] = RoundsCredits;
                MenuHandler.Instance.CreateText(" ", CreditsMenu, out TextMeshProUGUI _, 30);
                // Create Unbound Credits
                UnboundCredits = MenuHandler.Instance.CreateMenu("UNBOUND", null, CreditsMenu, 30);
                creditsMenus["UNBOUND"] = UnboundCredits;
                MenuHandler.Instance.CreateText(" ", CreditsMenu, out TextMeshProUGUI _, 30);

                // Create credits for mods that have registered them
                foreach (string modName in modCredits.Keys)
                {
                    creditsMenus[modName] = MenuHandler.Instance.CreateMenu(modName, null, CreditsMenu, 30);
                }

                // add credits for each mod

                // ROUNDS
                foreach (string line in roundsCredits)
                {
                    MenuHandler.Instance.CreateText(line, RoundsCredits, out TextMeshProUGUI _, 30);
                }
                // UNBOUND
                foreach (string line in Unbound.credits)
                {
                    MenuHandler.Instance.CreateText(line, UnboundCredits, out TextMeshProUGUI _, 30);
                }

            });
        }
    }
}
