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
            Credits.Instance = this;

        }
        internal void RegisterModCredits(ModCredits modCredits)
        {
            this.modCredits[modCredits.modName] = modCredits;
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

                // add link to modding discord
                MenuHandler.Instance.CreateText(" ", CreditsMenu, out TextMeshProUGUI _, 60);
                MenuHandler.Instance.CreateText("<link=\"https://discord.gg/Fyr3YnWduJ\">" + "ROUNDS MODDING COMMUNITY" + "</link>", CreditsMenu, out TextMeshProUGUI _, 30, false).AddComponent<OpenHyperlinks>();
                // add link to Thunderstore
                MenuHandler.Instance.CreateText("<link=\"https://rounds.thunderstore.io/?ordering=most-downloaded\"> " + "THUNDERSTORE.IO" + "</link>", CreditsMenu, out TextMeshProUGUI _, 30, false).AddComponent<OpenHyperlinks>();

                // add credits for each mod

                // ROUNDS
                this.AddModCredits(Credits.roundsCredits, RoundsCredits);
                // UNBOUND
                this.AddModCredits(Unbound.modCredits, UnboundCredits);

                foreach (string modName in this.modCredits.Keys)
                {
                    this.AddModCredits(this.modCredits[modName], this.creditsMenus[modName]);
                }

            });
        }
        internal void AddModCredits(ModCredits credits, GameObject parentMenu)
        {
            MenuHandler.Instance.CreateText(credits.modName, parentMenu, out TextMeshProUGUI _, 60);
            MenuHandler.Instance.CreateText(" \nby\n ", parentMenu, out TextMeshProUGUI _, 30);
            if (credits.credits != null)
            {
                foreach (string line in credits.credits)
                {
                    MenuHandler.Instance.CreateText(line, parentMenu, out TextMeshProUGUI _, 30);
                }
            }
            if (credits.linkText != "") 
            {
                MenuHandler.Instance.CreateText(" \n ", parentMenu, out TextMeshProUGUI _, 60);
                MenuHandler.Instance.CreateText("<link=\"" + credits.linkURL + "\">" + credits.linkText.ToUpper() + "</link>", parentMenu, out TextMeshProUGUI _, 30, false).AddComponent<OpenHyperlinks>();
            }
        }
    }

    public class ModCredits
    {
        public string modName = "";
        public string[] credits = null;
        public string linkText = "";
        public string linkURL = "";

        public ModCredits(string modName = "", string[] credits = null,  string linkText = "", string linkURL = "")
        {
            this.modName = modName;
            this.credits = credits;
            this.linkText = linkText;
            this.linkURL = linkURL;
        }
    }

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class OpenHyperlinks : MonoBehaviour, IPointerClickHandler
    {
        public bool doesColorChangeOnHover = true;
        public Color hoverColor = new Color(60f / 255f, 120f / 255f, 1f);

        private TextMeshProUGUI pTextMeshPro;
        private Canvas pCanvas;
        private Camera pCamera;

        public bool isLinkHighlighted { get { return pCurrentLink != -1; } }

        private int pCurrentLink = -1;
        private List<Color32[]> pOriginalVertexColors = new List<Color32[]>();

        protected virtual void Awake()
        {
            pTextMeshPro = GetComponent<TextMeshProUGUI>();
            pCanvas = GetComponentInParent<Canvas>();

            // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
            if (pCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                pCamera = null;
            else
                pCamera = pCanvas.worldCamera;
        }

        void LateUpdate()
        {
            // is the cursor in the correct region (above the text area) and furthermore, in the link region?
            var isHoveringOver = TMP_TextUtilities.IsIntersectingRectTransform(pTextMeshPro.rectTransform, Input.mousePosition, pCamera);
            int linkIndex = isHoveringOver ? TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, Input.mousePosition, pCamera)
                : -1;

            // Clear previous link selection if one existed.
            if (pCurrentLink != -1 && linkIndex != pCurrentLink)
            {
                // Debug.Log("Clear old selection");
                SetLinkToColor(pCurrentLink, (linkIdx, vertIdx) => pOriginalVertexColors[linkIdx][vertIdx]);
                pOriginalVertexColors.Clear();
                pCurrentLink = -1;
            }

            // Handle new link selection.
            if (linkIndex != -1 && linkIndex != pCurrentLink)
            {
                // Debug.Log("New selection");
                pCurrentLink = linkIndex;
                if (doesColorChangeOnHover)
                    pOriginalVertexColors = SetLinkToColor(linkIndex, (_linkIdx, _vertIdx) => hoverColor);
            }

            // Debug.Log(string.Format("isHovering: {0}, link: {1}", isHoveringOver, linkIndex));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Debug.Log("Click at POS: " + eventData.position + "  World POS: " + eventData.worldPosition);

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, Input.mousePosition, pCamera);
            if (linkIndex != -1)
            { // was a link clicked?
                TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

                // Debug.Log(string.Format("id: {0}, text: {1}", linkInfo.GetLinkID(), linkInfo.GetLinkText()));
                // open the link id as a url, which is the metadata we added in the text field
                Application.OpenURL(linkInfo.GetLinkID());
            }
        }

        List<Color32[]> SetLinkToColor(int linkIndex, Func<int, int, Color32> colorForLinkAndVert)
        {
            TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

            var oldVertColors = new List<Color32[]>(); // store the old character colors

            for (int i = 0; i < linkInfo.linkTextLength; i++)
            { // for each character in the link string
                int characterIndex = linkInfo.linkTextfirstCharacterIndex + i; // the character index into the entire text
                var charInfo = pTextMeshPro.textInfo.characterInfo[characterIndex];
                int meshIndex = charInfo.materialReferenceIndex; // Get the index of the material / sub text object used by this character.
                int vertexIndex = charInfo.vertexIndex; // Get the index of the first vertex of this character.

                Color32[] vertexColors = pTextMeshPro.textInfo.meshInfo[meshIndex].colors32; // the colors for this character
                oldVertColors.Add(vertexColors.ToArray());

                if (charInfo.isVisible)
                {
                    vertexColors[vertexIndex + 0] = colorForLinkAndVert(i, vertexIndex + 0);
                    vertexColors[vertexIndex + 1] = colorForLinkAndVert(i, vertexIndex + 1);
                    vertexColors[vertexIndex + 2] = colorForLinkAndVert(i, vertexIndex + 2);
                    vertexColors[vertexIndex + 3] = colorForLinkAndVert(i, vertexIndex + 3);
                }
            }

            // Update Geometry
            pTextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

            return oldVertColors;
        }
    }

}
