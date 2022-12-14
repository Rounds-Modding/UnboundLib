using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnboundLib.Utils.UI
{

    public class Credits
    {

        internal Dictionary<string, ModCredits> modCredits = new Dictionary<string, ModCredits>();
        private Dictionary<string, GameObject> creditsMenus = new Dictionary<string, GameObject>();

        public static Credits Instance = new Credits();

        private static readonly ModCredits roundsCredits = new ModCredits("ROUNDS", new string[] { "Landfall Games (Publisher)", "Wilhelm Nulynd (Game design, programming and art)", "Karl Flodin (Music)", "Pontus Ullbors (Card and Face Art)", "Natalia Martinsson (Face Art)", "Sonigon (Sound design)"}, "Landfall.se", "https://landfall.se/rounds");

        private GameObject CreditsMenu;
        private GameObject RoundsCredits;
        private GameObject UnboundCredits;

        private Credits()
        {
            // singleton first time setup
            Instance = this;

        }
        internal void RegisterModCredits(ModCredits modCredits)
        {
            this.modCredits[modCredits.modName] = modCredits;
        }
        internal void CreateCreditsMenu(bool firstTime)
        {
            Unbound.Instance.ExecuteAfterSeconds(firstTime ? 0.1f : 0f, () =>
            {
                // Create credits menu
                CreditsMenu = MenuHandler.CreateMenu("CREDITS", null, MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main").gameObject, 60, true, false, null,true, 5);

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
                RoundsCredits = MenuHandler.CreateMenu("ROUNDS", null, CreditsMenu, 30);
                creditsMenus["ROUNDS"] = RoundsCredits;
                MenuHandler.CreateText(" ", CreditsMenu, out TextMeshProUGUI _, 30);
                // Create Unbound Credits
                UnboundCredits = MenuHandler.CreateMenu("UNBOUND", null, CreditsMenu, 30);
                creditsMenus["UNBOUND"] = UnboundCredits;
                MenuHandler.CreateText(" ", CreditsMenu, out TextMeshProUGUI _, 30);

                // Create credits for mods that have registered them
                foreach (string modName in modCredits.Keys)
                {
                    creditsMenus[modName] = MenuHandler.CreateMenu(modName, null, CreditsMenu, 30);
                }

                // add link to modding discord
                MenuHandler.CreateText(" ", CreditsMenu, out TextMeshProUGUI _, 60);
                MenuHandler.CreateText("<link=\"https://discord.gg/Fyr3YnWduJ\">" + "ROUNDS MODDING COMMUNITY" + "</link>", CreditsMenu, out TextMeshProUGUI _, 30, false).AddComponent<OpenHyperlinks>();
                // add link to Thunderstore
                MenuHandler.CreateText("<link=\"https://rounds.thunderstore.io/?ordering=most-downloaded\"> " + "THUNDERSTORE.IO" + "</link>", CreditsMenu, out TextMeshProUGUI _, 30, false).AddComponent<OpenHyperlinks>();

                // add credits for each mod

                // ROUNDS
                AddModCredits(roundsCredits, RoundsCredits);
                // UNBOUND
                AddModCredits(Unbound.modCredits, UnboundCredits);

                foreach (string modName in modCredits.Keys)
                {
                    AddModCredits(modCredits[modName], creditsMenus[modName]);
                }

            });
        }
        internal void AddModCredits(ModCredits credits, GameObject parentMenu)
        {
            MenuHandler.CreateText(credits.modName, parentMenu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" \nby\n ", parentMenu, out TextMeshProUGUI _, 30);
            if (credits.credits != null)
            {
                foreach (string line in credits.credits)
                {
                    MenuHandler.CreateText(line, parentMenu, out TextMeshProUGUI _, 30);
                }
            }
            if (credits.linkTexts.Length > 0) { MenuHandler.CreateText(" \n ", parentMenu, out TextMeshProUGUI _, 60); }
            for (int i = 0; i < credits.linkTexts.Length; i++)
            {
                string linkText = credits.linkTexts[i];
                string linkURL = "";
                if (i < credits.linkURLs.Length) { linkURL = credits.linkURLs[i]; }
                if (linkText != "") 
                {
                    MenuHandler.CreateText("<link=\"" + linkURL + "\">" + linkText.ToUpper() + "</link>", parentMenu, out TextMeshProUGUI _, 30, false).AddComponent<OpenHyperlinks>();
                }
            }

        }
    }

    public class ModCredits
    {
        public string modName;
        public string[] credits = null;
        public string[] linkTexts;
        public string[] linkURLs;

        public ModCredits(string modName = "", string[] credits = null,  string[] linkTexts = null, string[] linkURLs = null)
        {
            this.modName = modName;
            this.credits = credits;
            this.linkTexts = linkTexts ?? new[] { "" };
            this.linkURLs = linkURLs ?? new[] { "" };
        }
        public ModCredits(string modName = "", string[] credits = null, string linkText = "", string linkURL = "")
        {
            this.modName = modName;
            this.credits = credits;
            linkTexts = new[] { linkText };
            linkURLs = new[] { linkURL };
        }
    }
}
