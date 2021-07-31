using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Jotunn.Utils;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnboundLib.Utils.UI
{
    public class ToggleLevelMenuHandler : MonoBehaviour
    {
        public static ToggleLevelMenuHandler instance;
        
        // Draw level
        public bool IsDrawingLevels;

        // level ui asset bundle
        public AssetBundle levelMenuUI;
        
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

        // List of every lvlObj
        public readonly List<GameObject> lvlObjs = new List<GameObject>();

        // Right click menu variables
        private Vector2 mousePosOnRightClickMenu = Vector2.zero;
        private bool justRightClicked;
        private Vector2 staticMousePos;

        private bool justStartedPlaying;
        private bool justStoppedPlaying;

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
            
            // Load assetBundle
            levelMenuUI = AssetUtils.LoadAssetBundleFromResources("levelmenu ui", typeof(ToggleLevelMenuHandler).Assembly);
            // Load assets
            var _levelMenuCanvas = levelMenuUI.LoadAsset<GameObject>("LevelMenuCanvas");
            levelObj = levelMenuUI.LoadAsset<GameObject>("LevelObj");
            categoryButton = levelMenuUI.LoadAsset<GameObject>("CategoryButton");
            scrollView = levelMenuUI.LoadAsset<GameObject>("ScrollView");
            rightClickMenu = levelMenuUI.LoadAsset<GameObject>("RightClickMenu");

            // Create guiStyle for waiting text
            guiStyle = new GUIStyle {fontSize = 100, normal = {textColor = Color.white}};
            
            // // Clear all lists
            //     currentLevelsInMenu.Clear();
            //     currentCategories.Clear();
            //     scrollViews.Clear();
            //     levelsThatNeedToRedrawn.Clear();
            //     lvlObjs.Clear();
                
            // Create levelMenuCanvas
            levelMenuCanvas = Instantiate(_levelMenuCanvas);
            Object.DontDestroyOnLoad(levelMenuCanvas);
            levelMenuCanvas.GetComponent<Canvas>().worldCamera = Camera.current;
            SetActive(false);

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
                        UpdateVisualsLevelObj(lvlObj, false);
                    }
                }
                else
                {
                    LevelManager.EnableLevels(levelsInCategory);

                    foreach (var lvlObj in lvlObjs.Where(lvlObj => levelsInCategory.Contains(lvlObj.name)))
                    {
                        UpdateVisualsLevelObj(lvlObj, true);
                    }
                }
            });
            
            var redrawAllButton = levelMenuCanvas.transform.Find("LevelMenu/Top/Redraw all").GetComponent<Button>();    
            redrawAllButton.onClick.AddListener(() =>
            {
                foreach (var level in LevelManager.GetLevelsInCategory(currentCategory))
                {
                    levelsThatNeedToRedrawn.Add(level);
                }

                StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });

            this.ExecuteAfterSeconds(1, () =>
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
                // Create lvlObj's
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
                        if (level.Value.enabled)
                        {
                            LevelManager.DisableLevel(level.Key);
                            level.Value.enabled = false;
                            UpdateVisualsLevelObj(lvlObj, level.Value.enabled);
                        }
                        else
                        {
                            LevelManager.EnableLevel(level.Key);
                            level.Value.enabled = true;
                            UpdateVisualsLevelObj(lvlObj, level.Value.enabled);
                        }
                    });

                    lvlObjs.Add(lvlObj);
                    UpdateVisualsLevelObj(lvlObj, level.Value.enabled);
                    if (!Unbound.config.Bind("Levels: " + level.Value.category, LevelManager.GetVisualName(level.Key), true).Value)
                    {
                        LevelManager.DisableLevel(level.Key);
                        UpdateVisualsLevelObj(lvlObj, false);
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
                                obj.Value.Find("Darken").gameObject.SetActive(true);
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
                if(levelsThatNeedToRedrawn.Count != 0) StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });
        }

        // Update the visuals of a lvlObj
        public static void UpdateVisualsLevelObj(GameObject lvlObj, bool levelEnabled)
        {
            if (levelEnabled)
            {   
                lvlObj.transform.Find("ImageHolder").GetComponentInChildren<Image>().color = Color.white;
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            }
            else
            {
                lvlObj.transform.Find("ImageHolder").GetComponentInChildren<Image>().color = new Color(0.5f,0.5f,0.5f);
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5f,0.5f,0.5f);
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
                yield return new WaitForSeconds(0.3f);
            }
            NetworkConnectionHandler.instance.NetworkRestart();
            IsDrawingLevels = false;
            if (CardToggleMenuHandler.Instance.transform.parent.Find("Unbound Text Object"))
            {
                Destroy(CardToggleMenuHandler.Instance.transform.parent.Find("Unbound Text Object").gameObject);
            }

            foreach (var lvlObj in lvlObjs)
            {
                UpdateImage(lvlObj, Path.Combine(Path.Combine(Paths.ConfigPath, "LevelImages"), LevelManager.GetVisualName(lvlObj.name) + ".png"));
            }

            levelsThatNeedToRedrawn.Clear();
            RemoveAllRightClickMenus();
        }

        private IEnumerator LoadScene(string sceneName)
        {
            var isDone = false;
            UnityEngine.Debug.LogWarning("Loading: " + sceneName);
            var scene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            scene.completed += operation =>
            {
                TakeScreenshot(sceneName);
#pragma warning disable 618
                SceneManager.UnloadScene(sceneName);
#pragma warning restore 618
                UnityEngine.Debug.LogWarning("Done loading: " + sceneName);
                isDone = true;
            };

            if(isDone)
            {
                yield break;
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

        // This is executed when rightClicking on a lvlObj
        public void RightClickedAt(Vector2 position, GameObject obj)
        {
            if (GameManager.instance.isPlaying) return;
            RemoveAllRightClickMenus();
            mousePosOnRightClickMenu = position;
            justRightClicked = true;
            var rightMenu = Instantiate(rightClickMenu, position, Quaternion.identity, levelMenuCanvas.transform.Find("LevelMenu"));
            var levelKey = obj.name;
            
            // var toggleButton = rightMenu.transform.Find("Toggle").GetComponent<Button>();
            //
            // toggleButton.onClick.AddListener(() =>
            // {
            //     if (LevelManager.levels[obj.name].enabled)
            //     {
            //         LevelManager.DisableLevel(levelKey);
            //         LevelManager.levels[obj.name].enabled = false;
            //         UpdateVisualsLevelObj(obj, LevelManager.levels[obj.name].enabled);
            //     }
            //     else
            //     {
            //         LevelManager.EnableLevel(levelKey);
            //         LevelManager.levels[obj.name].enabled = true;
            //         UpdateVisualsLevelObj(obj, LevelManager.levels[obj.name].enabled);
            //     }
            // });
            
            // get and set redraw button
            var redrawButton = rightMenu.transform.Find("Redraw").GetComponent<Button>();
            redrawButton.onClick.AddListener(() =>
            {
                StartCoroutine(LoadScenesForRedrawing(new []{ levelKey}));
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
            // Activate and deactivate the menu
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SetActive(!levelMenuCanvas.activeInHierarchy);
            }

            if (IsDrawingLevels)
            {
                levelMenuCanvas.SetActive(false);
            }

            if (PhotonNetwork.IsConnected || (levelMenuCanvas.activeInHierarchy && GameManager.instance.isPlaying) && justStartedPlaying == false)
            {
                justStoppedPlaying = false;
                levelMenuCanvas.transform.Find("LevelMenu/Top/Redraw all").gameObject.SetActive(false);
                justStartedPlaying = true;
            }
            if (levelMenuCanvas.activeInHierarchy && !GameManager.instance.isPlaying && justStoppedPlaying == false)
            {
                justStartedPlaying = false;
                levelMenuCanvas.transform.Find("LevelMenu/Top/Redraw all").gameObject.SetActive(true);
                justStoppedPlaying = true;
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
                GUI.Label(new Rect(Screen.width / 3.8f, Screen.height / 2.5f, 300, 300), "Drawing level thumbnails.\nThis may take a while.", guiStyle );
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