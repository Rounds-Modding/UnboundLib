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

namespace UnboundLib.Utils.UI
{
    public class ModOptions
    {

        internal static List<ModMenu> modMenus = new List<ModMenu>();
        internal static Dictionary<string, GUIListener> GUIListeners = new Dictionary<string, GUIListener>();

        internal GameObject modOptionsMenu;

        internal static bool showModUi = false;
        internal static bool noDeprecatedMods = false;

        public static ModOptions Instance = new ModOptions();

        private ModOptions()
        {
            // singleton first time setup

            ModOptions.Instance = this;
        }
        public static void RegisterGUI(string modName, Action guiAction)
        {
            GUIListeners.Add(modName, new GUIListener(modName, guiAction));
        }
        public void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null)
        {
            if (parent == null)
            {
                parent = ModOptions.Instance.modOptionsMenu;
            }
            ModOptions.modMenus.Add(new ModMenu(name, buttonAction, guiAction, parent));
        }
        public void CreateModOptions(bool firstTime)
        {
            // create mod options
            Unbound.Instance.ExecuteAfterSeconds(firstTime ? 0.2f : 0, () =>
            {
                // Create mod options menu
                modOptionsMenu = MenuHandler.Instance.CreateMenu("MOD OPTIONS", null, MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main").gameObject, 60, true, false, true, 4);


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

                // Create toggle cards button
                MenuHandler.Instance.CreateButton("Toggle Cards", modOptionsMenu, () => { CardToggleMenuHandler.Instance.Show(); });

                // Create menu's for mods with new UI
                foreach (var menu in modMenus)
                {
                    var mmenu = MenuHandler.Instance.CreateMenu(menu.menuName, menu.buttonAction, modOptionsMenu, 60, true, true);
                    void disableOldMenu()
                    {
                        if (GUIListeners.ContainsKey(menu.menuName))
                        {
                            GUIListeners[menu.menuName].guiEnabled = false;
                            showModUi = false;
                        }
                    }
                    mmenu.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(disableOldMenu);
                    mmenu.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(disableOldMenu);

                    menu.guiAction.Invoke(mmenu);
                }

                // Create menu's for mods that do not use the new UI
                if (GUIListeners.Count != 0) { MenuHandler.Instance.CreateText(" ", modOptionsMenu, out _); }
                foreach (var modMenu in GUIListeners.Keys)
                {
                    var menu = MenuHandler.Instance.CreateMenu(modMenu, () =>
                    {
                        foreach (var list in GUIListeners.Values.Where(list => list.guiEnabled))
                        {
                            list.guiEnabled = false;
                        }
                        GUIListeners[modMenu].guiEnabled = true;
                        showModUi = true;
                    }, modOptionsMenu,
                            75,
                            true, false);
                    void disableOldMenu()
                    {
                        if (GUIListeners.ContainsKey(menu.name))
                        {
                            GUIListeners[menu.name].guiEnabled = false;
                            showModUi = false;
                        }
                    }
                    menu.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(disableOldMenu);
                    menu.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(disableOldMenu);
                    MenuHandler.Instance.CreateText(
                        "This mod has not yet been updated to the new UI system.\nPlease use the old UI system in the top left.", menu, out _, 60, false);
                }

                // check if there are no deprecated ui's and disable the f1 menu
                if (GUIListeners.Count == 0) noDeprecatedMods = true;
            });
        }
        internal class ModMenu
        {
            public string menuName;
            public UnityAction buttonAction;
            public Action<GameObject> guiAction;
            public GameObject parent;

            public ModMenu(string menuName, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent)
            {
                this.menuName = menuName;
                this.buttonAction = buttonAction;
                this.guiAction = guiAction;
                this.parent = parent;
            }
        }

        internal class GUIListener
        {
            public bool guiEnabled = false;
            public string modName;
            public Action guiAction;
            public GUIListener(string modName, Action guiAction)
            {
                this.modName = modName;
                this.guiAction = guiAction;
            }
        }
    }
}
