using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarmonyLib;

namespace UnboundLib.Utils.UI
{
    public class ToggleLevelMenuHandler : MonoBehaviour
    {
        public static ToggleLevelMenuHandler instance;
        
        // Draw level
        public bool IsDrawingLevels;

        // lvl canvas GameObject
        public GameObject levelMenuCanvas;

        // Dictionary of scrollView names(category name) compared with the transforms of the scroll views
        private readonly Dictionary<string, Transform> scrollViews = new Dictionary<string, Transform>();
        
        // Content obj in category scroll view
        private Transform categoryContent;
        // Transform of root scroll views obj
        private Transform scrollViewTrans;

        // guiStyle for waiting text
        private GUIStyle guiStyle;

        // Loaded assets
        private GameObject levelObj;
        private GameObject categoryButton;
        private GameObject scrollView;
        private GameObject rightClickMenu;

        // A list of levelNames that need to redraw their art
        private readonly List<string> levelsThatNeedToRedrawn = new List<string>();
        private readonly List<string> levelsThatHaveBeenRedrawn = new List<string>();

        // List of every lvlObj
        public readonly List<GameObject> lvlObjs = new List<GameObject>();

        // Right click menu variables
        private Vector2 mousePosOnRightClickMenu = Vector2.zero;
        private bool justRightClicked;
        private Vector2 staticMousePos;

        private TextMeshProUGUI redrawAllText;

        private bool disabled;
        private bool manualRedraw;

        private string currentCategory
        {
            get
            {
                foreach (var scroll in scrollViews)
                {
                    if (scroll.Value.gameObject.activeInHierarchy)
                    {
                        return scroll.Key;
                    }
                }

                return null;
            }
        }

        // if need to toggle all on or off
        private bool toggledAll;

        public void Start()
        {
            instance = this;
            
            // Load assets
            var _levelMenuCanvas = Unbound.toggleUI.LoadAsset<GameObject>("LevelMenuCanvas");
            levelObj = Unbound.toggleUI.LoadAsset<GameObject>("LevelObj");
            categoryButton = Unbound.toggleUI.LoadAsset<GameObject>("CategoryButton");
            scrollView = Unbound.toggleUI.LoadAsset<GameObject>("ScrollView");
            rightClickMenu = Unbound.toggleUI.LoadAsset<GameObject>("RightClickMenu");

            // Create guiStyle for waiting text
            guiStyle = new GUIStyle {fontSize = 100, normal = {textColor = Color.black}};
            
            // // Clear all lists
            //     currentLevelsInMenu.Clear();
            //     currentCategories.Clear();
            //     scrollViews.Clear();
            //     levelsThatNeedToRedrawn.Clear();
            //     lvlObjs.Clear();
                
            // Create levelMenuCanvas
            levelMenuCanvas = Instantiate(_levelMenuCanvas);
            DontDestroyOnLoad(levelMenuCanvas);
            levelMenuCanvas.GetComponent<Canvas>().worldCamera = Camera.current;
            levelMenuCanvas.SetActive(false);

            // Set important root objects
            categoryContent = levelMenuCanvas.transform.Find("LevelMenu/Top/Categories/ButtonsScroll/Viewport/Content");
            scrollViewTrans = levelMenuCanvas.transform.Find("LevelMenu/ScrollViews");

            // Create and set searchbar
            var searchBar = levelMenuCanvas.transform.Find("LevelMenu/Top/InputField").gameObject;
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                foreach (var _scrollView in scrollViews)
                {
                    foreach (var level in _scrollView.Value.GetComponentsInChildren<Button>(true))
                    {
                        if (value == "")
                        {
                            level.gameObject.SetActive(true);
                            continue;
                        }

                        if (level.name.ToUpper().Contains(value.ToUpper()))
                        {
                            level.gameObject.SetActive(true);
                        }
                        else
                        {
                            level.gameObject.SetActive(false);
                        }
                    }
                }
            });

            // Create and set toggle all button
            var toggleAllButton = levelMenuCanvas.transform.Find("LevelMenu/Top/Toggle all").GetComponent<Button>();
            toggleAllButton.onClick.AddListener(() =>
            {
                if (currentCategory == null) return;
                
                toggledAll = !toggledAll;

                var levelsInCategory = LevelManager.GetLevelsInCategory(currentCategory);
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
            var redrawAllButton = levelMenuCanvas.transform.Find("LevelMenu/Top/Redraw all").GetComponent<Button>();
            redrawAllText = redrawAllButton.GetComponentInChildren<TextMeshProUGUI>();
            redrawAllButton.onClick.AddListener(() =>
            {
                if (redrawAllText.text == "Draw all thumbnails")
                {
                    levelsThatNeedToRedrawn.AddRange(LevelManager.levels.Select(lvlObj => lvlObj.Key));
                }
                else
                {
                    levelsThatNeedToRedrawn.AddRange(LevelManager.GetLevelsInCategory(currentCategory));
                }

                manualRedraw = true;
                StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });
            
            // get and set info button
            var infoButton = levelMenuCanvas.transform.Find("LevelMenu/Top/Help").GetComponent<Button>();
            var infoMenu = levelMenuCanvas.transform.Find("LevelMenu/InfoMenu").gameObject;
            infoButton.onClick.AddListener(() =>
            {
                infoMenu.SetActive(!infoMenu.activeInHierarchy);
            });

            this.ExecuteAfterSeconds(0.5f, () =>
            {
                levelMenuCanvas.SetActive(true);
                
                // Create category scrollViews
                foreach (var category in LevelManager.categories)
                {
                    var _scrollView = Instantiate(scrollView, scrollViewTrans);
                    _scrollView.name = category;
                    scrollViews.Add(category, _scrollView.transform);
                    if (category == "Default")
                    {
                        _scrollView.SetActive(true);
                    }

                }
                // Create lvlObjs
                foreach (var level in LevelManager.levels)
                {
                    if (!File.Exists(Path.Combine(Path.Combine(Paths.ConfigPath, "LevelImages"), LevelManager.GetVisualName(level.Value.name) + ".png")))
                    {
                        levelsThatNeedToRedrawn.Add(level.Value.name);
                    }
                    
                    var parentScroll = scrollViews[level.Value.category].Find("Viewport/Content");
                    var lvlObj = Instantiate(levelObj, parentScroll);
                    lvlObj.SetActive(true);

                    lvlObj.name = level.Key;
            
                    lvlObj.GetComponentInChildren<TextMeshProUGUI>().text = LevelManager.GetVisualName(level.Key);
                    lvlObj.AddComponent<LvlObj>();
                    lvlObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            if (level.Value.selected)
                            {
                                level.Value.selected = false;
                            }
                            else
                            {
                                level.Value.selected = true;
                            }
                            
                            UpdateVisualsLevelObj(lvlObj);
                            return;
                        }
                        
                        if (level.Value.enabled)
                        {
                            LevelManager.DisableLevel(level.Key);
                            level.Value.enabled = false;
                            UpdateVisualsLevelObj(lvlObj);
                        }
                        else
                        {
                            LevelManager.EnableLevel(level.Key);
                            level.Value.enabled = true;
                            UpdateVisualsLevelObj(lvlObj);
                        }
                    });

                    lvlObjs.Add(lvlObj);
                    UpdateVisualsLevelObj(lvlObj);
                    if (!Unbound.config.Bind("Levels: " + level.Value.category, LevelManager.GetVisualName(level.Key), true).Value)
                    {
                        LevelManager.DisableLevel(level.Key);
                        UpdateVisualsLevelObj(lvlObj);
                    }
                    UpdateImage(lvlObj, Path.Combine(Path.Combine(Paths.ConfigPath, "LevelImages"), LevelManager.GetVisualName(level.Key) + ".png"));
                }
                
                // Create category buttons
                foreach (var category in LevelManager.categories)
                {
                    var categoryObj = Instantiate(categoryButton, categoryContent);
                    categoryObj.SetActive(true);
                    categoryObj.name = category;
                    categoryObj.GetComponentInChildren<TextMeshProUGUI>().text = category;
                    categoryObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        foreach (var scroll in scrollViews)
                        {
                            scroll.Value.gameObject.SetActive(false);
                        }
            
                        scrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        scrollViews[category].gameObject.SetActive(true);
                    });
                    var toggle = categoryObj.GetComponentInChildren<Toggle>();
                    toggle.onValueChanged.AddListener(value =>
                    {
                        if (!value)
                        {
                            UpdateCategoryVisuals(false);
                        }
                        else
                        {
                            UpdateCategoryVisuals(true);
                        }
                    });
            
                    void UpdateCategoryVisuals(bool enabled)
                    {
                        foreach (var obj in scrollViews.Where(obj => obj.Key == category))
                        {
                            obj.Value.Find("Darken").gameObject.SetActive(!enabled);
                            if (enabled)
                            {
                                LevelManager.categoryBools[category].Value = true;
                                foreach (Transform trs in obj.Value.Find("Viewport/Content"))
                                {
                                    if (trs.name != "LevelObj" &&
                                        trs.GetComponentsInChildren<Image>()[1].color == Color.white)
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
                                    if (trs.name != "LevelObj" &&
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
                
                levelMenuCanvas.SetActive(false);
                
                // Detect which levels need to redraw
                //if(levelsThatNeedToRedrawn.Count != 0) StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });
        }

        // Update the visuals of a lvlObj
        public static void UpdateVisualsLevelObj(GameObject lvlObj)
        {
            if (LevelManager.levels[lvlObj.name].enabled)
            {   
                lvlObj.transform.Find("ImageHolder").GetComponentInChildren<Image>().color = Color.white;
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            }
            else
            {
                lvlObj.transform.Find("ImageHolder").GetComponentInChildren<Image>().color = new Color(0.5f,0.5f,0.5f);
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5f,0.5f,0.5f);
            }
            
            if (LevelManager.levels[lvlObj.name].selected)
            {
                lvlObj.transform.Find("Glow").gameObject.SetActive(true);
            }
            else
            {
                lvlObj.transform.Find("Glow").gameObject.SetActive(false);
            }
            

            foreach (var category in LevelManager.categories)
            {
                if (LevelManager.GetLevelsInCategory(category).All(level => !LevelManager.IsLevelActive(level)))
                {
                    LevelManager.DisableCategory(category);
                }
            }
        }

        // Update the image of a lvlObj
        private static void UpdateImage(GameObject lvlObj, string imagePath)
        {
            if(!File.Exists(imagePath)) return; 
            
            var image = lvlObj.transform.Find("ImageHolder/Image").gameObject;
            var fileData = File.ReadAllBytes(imagePath);
            var img = new Texture2D(1, 1);
            img.LoadImage(fileData);
            image.GetComponent<Image>().sprite = Sprite.Create(img, new Rect(0,0, img.width, img.height), new Vector2(0.5f,0.5f));
        }

        private IEnumerator LoadScenesForRedrawing(string[] sceneNames)
        {
            IsDrawingLevels = true;

            foreach (var sceneName in sceneNames)
            {
                ArtHandler.instance.NextArt();
                yield return LoadScene(sceneName);
                levelsThatHaveBeenRedrawn.Add(sceneName);
            }
            levelsThatNeedToRedrawn.Clear();
            levelsThatHaveBeenRedrawn.Clear();
            NetworkConnectionHandler.instance.NetworkRestart();
            IsDrawingLevels = false;


            foreach (var lvl in LevelManager.levels.Where(lvl => lvl.Value.selected))
            {
                lvl.Value.selected = false;
            }
            
            foreach (var lvlObj in lvlObjs)
            {
                UpdateVisualsLevelObj(lvlObj);
                UpdateImage(lvlObj, Path.Combine(Path.Combine(Paths.ConfigPath, "LevelImages"), LevelManager.GetVisualName(lvlObj.name) + ".png"));
            }

            foreach (var obj in levelMenuCanvas.scene.GetRootGameObjects())
            {
                if (obj.name == "UnboundLib Canvas")
                {
                    //Destroy(obj.transform.Find("Unbound Text Object")?.gameObject);
                    obj.SetActive(false);
                }
            }

            RemoveAllRightClickMenus();

            if (manualRedraw)
            {
                this.ExecuteAfterSeconds(0.25f, () =>
                {
                    levelMenuCanvas.SetActive(true);
                });
            }

            manualRedraw = false;
        }

        private IEnumerator LoadScene(string sceneName)
        {
            bool isDone = false;

            void NewSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= NewSceneLoaded;
                
                scene.GetRootGameObjects()[0].transform.position = Vector3.zero;

                this.ExecuteAfterSeconds( 0.05f, () => {
                    TakeScreenshot(sceneName);
                    var unloadSceneAsync = SceneManager.UnloadSceneAsync(scene);
                    scene.GetRootGameObjects()[0].transform.position = new Vector3(100,100,0);
                    unloadSceneAsync.completed += operation =>
                    {
                        this.ExecuteAfterSeconds(0.1f, () =>
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
            var screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGBA32, false);
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
            var dir = Directory.CreateDirectory(Path.Combine(Paths.ConfigPath, "LevelImages"));
            var filename = Path.Combine(dir.FullName, LevelManager.GetVisualName(levelName) + ".png"); 
            File.WriteAllBytes(filename, bytes);
            #if DEBUG
            UnityEngine.Debug.Log($"Took screenshot to: {filename}");
            #endif
        }

        // This is executed when right Clicking on a lvlObj
        public void RightClickedAt(Vector2 position, GameObject obj)
        {
            if (GameManager.instance.isPlaying) return;
            RemoveAllRightClickMenus();
            mousePosOnRightClickMenu = position;
            justRightClicked = true;
            var rightMenu = Instantiate(rightClickMenu, position, Quaternion.identity, levelMenuCanvas.transform.Find("LevelMenu"));
            var levelKey = obj.name;

            var selectedCount = LevelManager.levels.Count(lvl => lvl.Value.selected);

            // get and set redraw button
            var redrawButton = rightMenu.transform.Find("Redraw").GetComponent<Button>();
            if (selectedCount > 0)
            {
                redrawButton.GetComponentInChildren<TextMeshProUGUI>().text = "Redraw selected level thumbnails";
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
            foreach (Transform obj in levelMenuCanvas.transform.Find("LevelMenu").transform)
            {
                if (obj.name.Contains("RightClickMenu"))
                {
                    Destroy(obj.gameObject);
                }
            }
        }

        public void SetActive(bool active)
        {
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
            levelMenuCanvas.SetActive(true);
        }

        private void Update()
        {
            // // Activate and deactivate the menu
            // if (Input.GetKeyDown(KeyCode.F2))
            // {
            //     SetActive(!levelMenuCanvas.activeInHierarchy);
            // }

            if (IsDrawingLevels)
            {
                levelMenuCanvas.SetActive(false);
            }

            if (GameManager.instance.isPlaying && !disabled)
            {
                levelMenuCanvas.transform.Find("LevelMenu/Top/Redraw all").gameObject.SetActive(false);
                disabled = true;
            }
            if (!GameManager.instance.isPlaying && disabled)
            {
                levelMenuCanvas.transform.Find("LevelMenu/Top/Redraw all").gameObject.SetActive(true);
                disabled = false;
            }

            if (redrawAllText.text != "Draw all thumbnails" &&
                !Directory.Exists(Path.Combine(Paths.ConfigPath, "LevelImages")))
            {
                redrawAllText.text = "Draw all thumbnails";
            }
            else if (redrawAllText.text == "Draw all thumbnails" &&
                     Directory.Exists(Path.Combine(Paths.ConfigPath, "LevelImages")))
            {
                redrawAllText.text = "Redraw all in category";
            }

            // Remove right click menu when mouse gets too far away
            if (mousePosOnRightClickMenu != Vector2.zero)
            {
                //staticMousePos = Vector2.zero;
                if (justRightClicked) { staticMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);}
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
            if(Input.GetAxisRaw("Mouse ScrollWheel") > 0 || Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            {
                RemoveAllRightClickMenus();
            }
        }

        private void OnGUI()
        {
            if (IsDrawingLevels)
            {
                var boxStyle = guiStyle;
                var background = new Texture2D(1, 1);
                background.SetPixel(0,0, Color.gray);
                background.Apply();
                boxStyle.normal.background = background;
                GUI.Box(new Rect(0,0, Screen.width, Screen.height), "", boxStyle);
                GUI.Label(new Rect(Screen.width / 3.8f, Screen.height / 2.5f, 300, 300), "Drawing level thumbnails.\nThis may take a while.\n " + ((float)levelsThatHaveBeenRedrawn.Count/(float)levelsThatNeedToRedrawn.Count).ToString("P1"), guiStyle );
            }
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