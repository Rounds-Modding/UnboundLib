using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnboundLib.Utils.UI
{
    public class ToggleLevelMenuHandler : MonoBehaviour
    {
        public static ToggleLevelMenuHandler instance;

        // Draw level
        public bool isDrawingLevels;

        // lvl canvas GameObject
        public GameObject mapMenuCanvas;

        // Dictionary of scrollView names(category name) compared with the transforms of the scroll views
        private static readonly Dictionary<string, Transform> ScrollViews = new Dictionary<string, Transform>();

        //List of buttons and toggles to disable when not host
        private readonly List<Button> buttonsToDisable = new List<Button>();
        private readonly List<Toggle> togglesToDisable = new List<Toggle>();

        // Content obj in category scroll view
        private Transform categoryContent;
        // Transform of root scroll views obj
        private Transform scrollViewTrans;

        // guiStyle for waiting text
        private GUIStyle guiStyle;

        // Loaded assets
        private GameObject mapObj;
        private GameObject categoryButton;
        private GameObject scrollView;
        private GameObject rightClickMenu;

        // A list of levelNames that need to redraw their art
        private readonly List<string> levelsThatNeedToRedrawn = new List<string>();
        private readonly List<string> levelsThatHaveBeenRedrawn = new List<string>();

        // List of every mapObject
        public readonly List<GameObject> lvlObjs = new List<GameObject>();

        // Right click menu variables
        private Vector2 mousePosOnRightClickMenu = Vector2.zero;
        private bool justRightClicked;
        private Vector2 staticMousePos;

        private TextMeshProUGUI redrawAllText;

        private bool disabled;
        private bool redrawDisabled;
        private bool manualRedraw;

        private string CurrentCategory => (from scroll in ScrollViews where scroll.Value.gameObject.activeInHierarchy select scroll.Key).FirstOrDefault();

        // if need to toggle all on or off
        private bool toggledAll;

        private static TextMeshProUGUI mapAmountText;

        public void Start()
        {
            instance = this;

            var mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();

            // Load assets
            var mapsMenuCanvas = Unbound.toggleUI.LoadAsset<GameObject>("MapMenuCanvas");
            mapObj = Unbound.toggleUI.LoadAsset<GameObject>("MapObj");
            categoryButton = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");
            scrollView = Unbound.toggleUI.LoadAsset<GameObject>("MapScrollView");
            rightClickMenu = Unbound.toggleUI.LoadAsset<GameObject>("RightClickMenu");

            // Create guiStyle for waiting text
            guiStyle = new GUIStyle { fontSize = 100, normal = { textColor = Color.black } };

            // // Clear all lists
            //     currentLevelsInMenu.Clear();
            //     currentCategories.Clear();
            //     scrollViews.Clear();
            //     levelsThatNeedToRedrawn.Clear();
            //     lvlObjs.Clear();

            // Create mapMenuCanvas
            mapMenuCanvas = Instantiate(mapsMenuCanvas);
            DontDestroyOnLoad(mapMenuCanvas);

            var canvas = mapMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            mapMenuCanvas.SetActive(false);

            // Set important root objects
            categoryContent = mapMenuCanvas.transform.Find("MapMenu/Top/Categories/ButtonsScroll/Viewport/Content");
            scrollViewTrans = mapMenuCanvas.transform.Find("MapMenu/ScrollViews");

            // Create and set searchbar
            var searchBar = mapMenuCanvas.transform.Find("MapMenu/Top/InputField").gameObject;
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                foreach (var level in ScrollViews.SelectMany(scrollViewPair => scrollViewPair.Value.GetComponentsInChildren<Button>(true)))
                {
                    if (value == "")
                    {
                        level.gameObject.SetActive(true);
                        continue;
                    }

                    level.gameObject.SetActive(level.name.ToUpper().Contains(value.ToUpper()));
                }
            });

            Transform mapAmountObject = mapMenuCanvas.transform.Find("MapMenu/Top/MapAmount");
            mapAmountText = mapAmountObject.GetComponentInChildren<TextMeshProUGUI>();

            var cardAmountSlider = mapAmountObject.GetComponentsInChildren<Slider>();
            foreach (Slider slider in cardAmountSlider)
            {
                slider.onValueChanged.AddListener(amount =>
                {
                    int integerAmount = (int) amount;
                    ChangeMapColumnAmountMenus(integerAmount);
                });
            }

            // Create and set toggle all button
            var toggleAllButton = mapMenuCanvas.transform.Find("MapMenu/Top/ToggleAll").GetComponent<Button>();
            buttonsToDisable.Add(toggleAllButton);
            toggleAllButton.onClick.AddListener(() =>
            {
                if (CurrentCategory == null) return;

                toggledAll = !toggledAll;

                var levelsInCategory = LevelManager.GetLevelsInCategory(CurrentCategory);
                if (toggledAll)
                {
                    LevelManager.DisableLevels(levelsInCategory);

                    foreach (var lvlObj in lvlObjs.Where(lvlObj => levelsInCategory.Contains(lvlObj.name)))
                    {
                        UpdateVisualsLevelObj(lvlObj);
                    }
                }
                else
                {
                    LevelManager.EnableLevels(levelsInCategory);

                    foreach (var lvlObj in lvlObjs.Where(lvlObj => levelsInCategory.Contains(lvlObj.name)))
                    {
                        UpdateVisualsLevelObj(lvlObj);
                    }
                }
            });

            // get and set the redraw all button
            var redrawAllButton = mapMenuCanvas.transform.Find("MapMenu/Top/RedrawAll").GetComponent<Button>();
            buttonsToDisable.Add(redrawAllButton);
            redrawAllText = redrawAllButton.GetComponentInChildren<TextMeshProUGUI>();
            redrawAllButton.onClick.AddListener(() =>
            {
                mapMenuCanvas.SetActive(true);
                levelsThatNeedToRedrawn.AddRange(redrawAllText.text == "Draw Thumbnails"
                    ? LevelManager.levels.Select(lvlObj => lvlObj.Key)
                    : LevelManager.GetLevelsInCategory(CurrentCategory));

                manualRedraw = true;
                StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });

            // get and set info button
            var infoButton = mapMenuCanvas.transform.Find("MapMenu/Top/Help").GetComponent<Button>();
            var infoMenu = mapMenuCanvas.transform.Find("MapMenu/InfoMenu").gameObject;
            infoButton.onClick.AddListener(() =>
            {
                infoMenu.SetActive(!infoMenu.activeInHierarchy);
            });

            this.ExecuteAfterSeconds(0.5f, () =>
            {
                mapMenuCanvas.SetActive(true);

                // Create category scrollViews
                foreach (var category in LevelManager.categories)
                {
                    var newScrollView = Instantiate(scrollView, scrollViewTrans);
                    newScrollView.name = category;
                    ScrollViews.Add(category, newScrollView.transform);
                    if (category == "Vanilla")
                    {
                        newScrollView.SetActive(true);
                    }

                }
                // Create lvlObjs
                foreach (var level in LevelManager.levels)
                {
                    if (!File.Exists(Path.Combine("./LevelImages", LevelManager.GetVisualName(level.Key) + ".png")))
                    {
                        levelsThatNeedToRedrawn.Add(level.Key);
                    }

                    var parentScroll = ScrollViews[level.Value.category].Find("Viewport/Content");
                    var mapObject = Instantiate(mapObj, parentScroll);
                    mapObject.SetActive(true);

                    mapObject.name = level.Key;

                    mapObject.GetComponentInChildren<TextMeshProUGUI>().text = LevelManager.GetVisualName(level.Value.name);
                    mapObject.AddComponent<LvlObj>();
                    mapObject.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            level.Value.selected = !level.Value.selected;

                            UpdateVisualsLevelObj(mapObject);
                            return;
                        }

                        if (level.Value.enabled)
                        {
                            LevelManager.DisableLevel(level.Key);
                            level.Value.enabled = false;
                            UpdateVisualsLevelObj(mapObject);
                        }
                        else
                        {
                            LevelManager.EnableLevel(level.Key);
                            level.Value.enabled = true;
                            UpdateVisualsLevelObj(mapObject);
                        }
                    });
                    buttonsToDisable.Add(mapObject.GetComponent<Button>());

                    lvlObjs.Add(mapObject);
                    UpdateVisualsLevelObj(mapObject);
                    if (!Unbound.config.Bind("Levels: " + level.Value.category, LevelManager.GetVisualName(level.Key), true).Value)
                    {
                        LevelManager.DisableLevel(level.Key);
                        UpdateVisualsLevelObj(mapObject);
                    }
                    UpdateImage(mapObject, Path.Combine("./LevelImages", LevelManager.GetVisualName(level.Key) + ".png"));
                }

                var viewingText = mapMenuCanvas.transform.Find("MapMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

                // Create category buttons
                List<string> sortedCategories = new[] { "Vanilla", "Default physics" }.Concat(LevelManager.categories.OrderBy(c => c).Except(new[] { "Vanilla", "Default physics" })).ToList();
                foreach (var category in sortedCategories)
                {
                    var categoryObj = Instantiate(categoryButton, categoryContent);
                    categoryObj.SetActive(true);
                    categoryObj.name = category;
                    categoryObj.GetComponentInChildren<TextMeshProUGUI>().text = category;
                    categoryObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        foreach (var scroll in ScrollViews)
                        {
                            scroll.Value.gameObject.SetActive(false);
                        }

                        ScrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        ScrollViews[category].gameObject.SetActive(true);
                        
                        viewingText.text = "Viewing: " + category;
                    });
                    var toggle = categoryObj.GetComponentInChildren<Toggle>();
                    togglesToDisable.Add(toggle);
                    toggle.onValueChanged.AddListener(UpdateCategoryVisuals);

                    void UpdateCategoryVisuals(bool enabledVisuals)
                    {
                        foreach (var obj in ScrollViews.Where(obj => obj.Key == category))
                        {
                            obj.Value.Find("Darken").gameObject.SetActive(!enabledVisuals);
                            if (enabledVisuals)
                            {
                                LevelManager.categoryBools[category].Value = true;
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (trs.name != "MapObj" && trs.GetComponentsInChildren<Image>()[1].color == Color.white)
                                    {
                                        LevelManager.EnableLevel(trs.name, false);
                                    }
                                }
                            }
                            else
                            {
                                LevelManager.categoryBools[category].Value = false;
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (trs.name != "MapObj" &&
                                        trs.GetComponentsInChildren<Image>()[1].color == Color.white)
                                    {
                                        LevelManager.DisableLevel(trs.name, false);
                                    }
                                }
                            }
                        }

                        toggle.isOn = LevelManager.IsCategoryActive(category);
                    }

                    UpdateCategoryVisuals(LevelManager.IsCategoryActive(category));
                }

                mapMenuCanvas.SetActive(false);

                // Detect which levels need to redraw
                //if(levelsThatNeedToRedrawn.Count != 0) StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });
        }

        // Update the visuals of a mapObject
        public static void UpdateVisualsLevelObj(GameObject lvlObj)
        {
            if (!LevelManager.levels.ContainsKey(lvlObj.name)) return;
            if (LevelManager.levels[lvlObj.name].enabled)
            {
                lvlObj.transform.Find("Image").GetComponent<Image>().color = Color.white;
                lvlObj.transform.Find("Background").GetComponent<Image>().color = new Color(0.2352941f, 0.2352941f, 0.2352941f, 0.8470588f);
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            }
            else
            {
                lvlObj.transform.Find("Image").GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f);
                lvlObj.transform.Find("Background").GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.25f, 0.25f, 0.25f);
            }
        }

        // Update the image of a mapObject
        private static void UpdateImage(GameObject mapObject, string imagePath)
        {
            if (!File.Exists(imagePath)) return;

            var image = mapObject.transform.Find("Image").gameObject;
            var fileData = File.ReadAllBytes(imagePath);
            var img = new Texture2D(1, 1);
            img.LoadImage(fileData);
            image.GetComponent<Image>().sprite = Sprite.Create(img, new Rect(0, 0, img.width, img.height), new Vector2(0.5f, 0.5f));
        }

        private IEnumerator LoadScenesForRedrawing(IEnumerable<string> sceneNames)
        {
            isDrawingLevels = true;

            foreach (var sceneName in sceneNames)
            {
                ArtHandler.instance.NextArt();
                yield return LoadScene(sceneName);
                levelsThatHaveBeenRedrawn.Add(sceneName);
            }
            levelsThatNeedToRedrawn.Clear();
            levelsThatHaveBeenRedrawn.Clear();
            NetworkConnectionHandler.instance.NetworkRestart();
            isDrawingLevels = false;

            foreach (var map in LevelManager.levels.Where(lvl => lvl.Value.selected))
            {
                map.Value.selected = false;
            }

            foreach (var mapObject in lvlObjs)
            {
                UpdateVisualsLevelObj(mapObject);
                UpdateImage(mapObject, Path.Combine("./LevelImages", LevelManager.GetVisualName(mapObject.name) + ".png"));
            }

            foreach (var obj in mapMenuCanvas.scene.GetRootGameObjects())
            {
                if (obj.name == "UnboundLib Canvas")
                {
                    obj.SetActive(false);
                }
            }

            RemoveAllRightClickMenus();

            if (manualRedraw)
            {
                this.ExecuteAfterFrames(5, () =>
                {
                    SetActive(true);
                });
            }

            manualRedraw = false;
        }

        private static void ChangeMapColumnAmountMenus(int amount)
        {
            Vector2 cellSize = new Vector2(158, 115);
            float localScale;
            switch (amount)
            {
                case 3:
                    {
                        localScale = 1.4f;
                        break;
                    }
                default:
                    {
                        localScale = 1f;
                        break;
                    }
                case 5:
                    {
                        localScale = 0.85f;
                        break;
                    }
                case 6:
                    {
                        localScale = 0.7f;
                        break;
                    }
                case 7:
                    {
                        localScale = 0.6f;
                        break;
                    }
                case 8:
                    {
                        localScale = 0.525f;
                        break;
                    }
            }
            cellSize *= localScale;
            
            mapAmountText.text = "Maps Per Line: " + amount;
            foreach (GridLayoutGroup gridLayout in from category in LevelManager.categories select ScrollViews[category].Find("Viewport/Content") into categoryMenu where categoryMenu != null select categoryMenu.gameObject.GetComponent<GridLayoutGroup>())
            {
                gridLayout.cellSize = cellSize;
                gridLayout.constraintCount = amount;
                gridLayout.spacing = new Vector2(0, 20 * localScale);
            }
        }

        private IEnumerator LoadScene(string sceneName)
        {
            bool isDone = false;

            void NewSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= NewSceneLoaded;

                scene.GetRootGameObjects()[0].transform.position = Vector3.zero;

                this.ExecuteAfterSeconds(0.05f, () =>
                {
                    TakeScreenshot(sceneName);
                    var unloadSceneAsync = SceneManager.UnloadSceneAsync(scene);
                    scene.GetRootGameObjects()[0].transform.position = new Vector3(100, 100, 0);
                    unloadSceneAsync.completed += operation =>
                    {
                        this.ExecuteAfterFrames(30, () =>
                        {
                            isDone = true;
                        });
                    };
                });
            }

            var sceneLoadedMethod = AccessTools.Method(typeof(MapManager), "OnLevelFinishedLoading");
            var existingSceneLoaded = (UnityAction<Scene, LoadSceneMode>) Delegate.CreateDelegate(typeof(UnityAction<Scene, LoadSceneMode>), MapManager.instance, sceneLoadedMethod);

            MapManager.instance.RPCA_LoadLevel(sceneName);
            SceneManager.sceneLoaded -= existingSceneLoaded;
            SceneManager.sceneLoaded += NewSceneLoaded;

            while (!isDone)
            {
                yield return null;
            }
        }

        private static void TakeScreenshot(string levelName)
        {
            // Get camera to take picture from
            var camObj = MainMenuHandler.instance.gameObject.transform.parent.parent.GetComponentInChildren<MainCam>().gameObject;
            var camera = camObj.GetComponent<Camera>();
            // set resolution
            const int resWidth = 640;
            const int resHeight = 360;

            var rt = new RenderTexture(resWidth, resHeight, 24);
            camera.targetTexture = rt;
            var screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            camera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null;
            // Destroy render texture to avoid null errors
            Destroy(rt);

            // Get camera to take picture from
            var lighObj = camObj.transform.parent.Find("Lighting/LightCamera").gameObject;
            var lightCam = lighObj.GetComponent<Camera>();

            var rt1 = new RenderTexture(resWidth, resHeight, 24);
            lightCam.targetTexture = rt1;
            var screenShot1 = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
            lightCam.Render();
            RenderTexture.active = rt1;
            screenShot1.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            lightCam.targetTexture = null;
            RenderTexture.active = null;
            // Destroy render texture to avoid null errors
            Destroy(rt1);

            // Combine the two screenshots if alpha is zero on screenshot 1
            var pixels = screenShot.GetPixels(0, 0, screenShot.width, screenShot.height);
            var pixels1 = screenShot1.GetPixels(0, 0, screenShot.width, screenShot.height);
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a != 0)
                {
                    pixels[i].a = 1;
                }
                if (pixels1[i].a != 0)
                {
                    pixels1[i].a = 1;
                }

                if (pixels[i].a == 0)
                {
                    pixels[i] = pixels1[i];
                }
            }
            screenShot.SetPixels(pixels);

            // Write the screenshot to disk
            var bytes = screenShot.EncodeToPNG();
            var dir = Directory.CreateDirectory("./LevelImages");
            var filename = Path.Combine(dir.FullName, LevelManager.GetVisualName(levelName) + ".png");
            File.WriteAllBytes(filename, bytes);
#if DEBUG
            UnityEngine.Debug.Log($"Took screenshot to: {filename}");
#endif
        }

        // This is executed when right Clicking on a mapObject
        public void RightClickedAt(Vector2 position, GameObject obj)
        {
            if (GameManager.instance.isPlaying) return;
            RemoveAllRightClickMenus();
            mousePosOnRightClickMenu = position;
            justRightClicked = true;
            var rightMenu = Instantiate(rightClickMenu, position, Quaternion.identity, mapMenuCanvas.transform.Find("MapMenu"));
            var levelKey = obj.name;

            var selectedCount = LevelManager.levels.Count(lvl => lvl.Value.selected);

            // get and set redraw button
            var redrawButton = rightMenu.transform.Find("Redraw").GetComponent<Button>();
            if (selectedCount > 0)
            {
                redrawButton.GetComponentInChildren<TextMeshProUGUI>().text = "Redraw Selected Map Thumbnails";
            }
            redrawButton.onClick.AddListener(() =>
            {
                if (selectedCount > 0)
                {
                    levelsThatNeedToRedrawn.AddRange(from lvl in LevelManager.levels where lvl.Value.selected select lvl.Key);
                }
                else
                {
                    levelsThatHaveBeenRedrawn.Add(levelKey);
                }

                manualRedraw = true;
                StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });

        }

        private void RemoveAllRightClickMenus()
        {
            mousePosOnRightClickMenu = Vector2.zero;
            foreach (Transform obj in mapMenuCanvas.transform.Find("MapMenu").transform)
            {
                if (obj.name.Contains("RightClickMenu"))
                {
                    Destroy(obj.gameObject);
                }
            }
        }

        public void SetActive(bool active)
        {
            // Main camera changes when going back to menu and glow disappears if we don't se the camera again to the canvas
            Camera mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
            Canvas canvas = mapMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

            //if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
            mapMenuCanvas.SetActive(true);
        }

        private void Update()
        {
            // // Activate and deactivate the menu
            // if (Input.GetKeyDown(KeyCode.F2))
            // {
            //     SetActive(!mapMenuCanvas.activeInHierarchy);
            // }

            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient && !disabled)
            {
                foreach (var button in buttonsToDisable)
                {
                    button.interactable = false;
                }
                foreach (var toggle in togglesToDisable)
                {
                    toggle.interactable = false;
                }
                disabled = true;
            }
            if ((!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) && disabled)
            {
                foreach (var button in buttonsToDisable)
                {
                    button.interactable = true;
                }
                foreach (var toggle in togglesToDisable)
                {
                    toggle.interactable = true;
                }
                disabled = false;
            }

            if (isDrawingLevels)
            {
                mapMenuCanvas.SetActive(false);
            }

            switch (GameManager.instance.isPlaying)
            {
                case true when !redrawDisabled:
                    mapMenuCanvas.transform.Find("MapMenu/Top/RedrawAll").gameObject.SetActive(false);
                    redrawDisabled = true;
                    break;
                case false when redrawDisabled:
                    mapMenuCanvas.transform.Find("MapMenu/Top/RedrawAll").gameObject.SetActive(true);
                    redrawDisabled = false;
                    break;
            }

            if (redrawAllText.text != "Draw Thumbnails" &&
                !Directory.Exists("./LevelImages"))
            {
                redrawAllText.text = "Draw Thumbnails";
            }
            else if (redrawAllText.text == "Draw Thumbnails" &&
                     Directory.Exists("./LevelImages"))
            {
                redrawAllText.text = "Redraw All";
            }

            // Remove right click menu when mouse gets too far away
            if (mousePosOnRightClickMenu != Vector2.zero)
            {
                //staticMousePos = Vector2.zero;
                if (justRightClicked) { staticMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y); }
                var trueMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (trueMousePos.x < staticMousePos.x - 5 * (Screen.width / 50) ||
                    trueMousePos.x > staticMousePos.x + 10 * (Screen.width / 50) ||
                    trueMousePos.y < staticMousePos.y - 4 * (Screen.width / 50) ||
                    trueMousePos.y > staticMousePos.y + 8 * (Screen.width / 50))
                {
                    RemoveAllRightClickMenus();
                }

                justRightClicked = false;
            }

            // Remove right click menu when scrolling
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0 || Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            {
                RemoveAllRightClickMenus();
            }
        }

        private void OnGUI()
        {
            if (!isDrawingLevels) return;

            var boxStyle = guiStyle;
            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, Color.gray);
            background.Apply();
            boxStyle.normal.background = background;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);
            GUI.Label(new Rect(Screen.width / 3.8f, Screen.height / 2.5f, 300, 300), "Drawing map thumbnails.\nThis may take a while.\n " + (levelsThatHaveBeenRedrawn.Count / (float) levelsThatNeedToRedrawn.Count).ToString("P1"), guiStyle);
        }
    }

    public class LvlObj : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ToggleLevelMenuHandler.instance.RightClickedAt(eventData.position, gameObject);
            }
        }
    }
}