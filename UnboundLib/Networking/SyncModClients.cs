using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Photon.Pun;
using TMPro;
using UnboundLib.GameModes;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UnboundLib.Networking
{
    internal static class SyncModClients
    {
        internal static readonly float timeoutTime = 5f;

        private static List<string> clientSideGUIDs = new List<string>();

        private static Dictionary<int, string[]> extra = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> missing = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> mismatch = new Dictionary<int, string[]>();

        private static Dictionary<int, string[]> clientsServerSideMods = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> clientsServerSideGUIDs = new Dictionary<int, string[]>();
        private static Dictionary<int, string[]> clientsModVersions = new Dictionary<int, string[]>();

        private static List<string> hostsServerSideMods = new List<string>();
        private static List<string> hostsServerSideGUIDs = new List<string>();
        private static List<string> hostsModVersions = new List<string>();

        private static Dictionary<string, PluginInfo> loadedMods = new Dictionary<string, PluginInfo>();
        private static List<string> loadedGUIDs = new List<string>();
        private static List<string> loadedModNames = new List<string>();
        private static List<string> loadedVersions = new List<string>();

        internal static void RequestSync()
        {
            if (PhotonNetwork.OfflineMode) return;

            NetworkingManager.RPC(typeof(SyncModClients), "SyncLobby", new object[] { });
        }

        [UnboundRPC]
        internal static void SyncLobby()
        {
            Reset();
            LocalSetup();
            if (!PhotonNetwork.IsMasterClient) return;
            
            CheckLobby();
            Unbound.Instance.StartCoroutine(Check());
        }

        internal static IEnumerator Check()
        {
            float startTime = Time.time;
            while (clientsServerSideGUIDs.Keys.Count < PhotonNetwork.PlayerList.Except(new List<Photon.Realtime.Player> { PhotonNetwork.LocalPlayer }).ToList().Count)
            {
                if (Time.time > startTime + timeoutTime)
                {
                    break;
                }

                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            FindDifferences();
            MakeFlags();
        }

        internal static void LocalSetup()
        {
            loadedMods = BepInEx.Bootstrap.Chainloader.PluginInfos;

            foreach (var modId in loadedMods.Keys.Where(id => !clientSideGUIDs.Contains(loadedMods[id].Metadata.GUID)))
            {
                loadedGUIDs.Add(loadedMods[modId].Metadata.GUID);
                loadedModNames.Add(loadedMods[modId].Metadata.Name);
                loadedVersions.Add(loadedMods[modId].Metadata.Version.ToString());
            }
        }

        internal static void CheckLobby()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(SyncModClients), "SendModList", new object[] {  });
            }
        }

        internal static void Reset()
        {
            clientsServerSideMods = new Dictionary<int, string[]>();
            clientsServerSideGUIDs = new Dictionary<int, string[]>();
            clientsModVersions = new Dictionary<int, string[]>();

            extra = new Dictionary<int, string[]>();
            missing = new Dictionary<int, string[]>();
            mismatch = new Dictionary<int, string[]>();

            hostsServerSideMods = new List<string>();
            hostsServerSideGUIDs = new List<string>();
            hostsModVersions = new List<string>();
            loadedGUIDs = new List<string>();
            loadedModNames = new List<string>();
            loadedVersions = new List<string>();
        }

        internal static void FindDifferences()
        {
            foreach (int actorId in clientsServerSideGUIDs.Keys)
            {
                missing[actorId] = hostsServerSideGUIDs.Except(clientsServerSideGUIDs[actorId]).ToArray();
                extra[actorId] = clientsServerSideGUIDs[actorId].Except(hostsServerSideGUIDs).ToArray();
                mismatch[actorId] = clientsServerSideGUIDs[actorId].Except(extra[actorId]).Except(missing[actorId]).Where(guid => HostVersionFromGUID(guid) != VersionFromGUID(actorId, guid)).ToArray();
            }

        }
        private static string HostVersionFromGUID(string GUID)
        {
            return hostsModVersions.Where((v, i) => hostsServerSideGUIDs[i] == GUID).FirstOrDefault();
        }
        private static string ModIDFromGUID(int actorID, string GUID)
        {
            return clientsServerSideMods[actorID].Where((v, i) => clientsServerSideGUIDs[actorID][i] == GUID).FirstOrDefault();
        }
        private static string VersionFromGUID(int actorID, string GUID)
        {
            return clientsModVersions[actorID].Where((v, i) => clientsServerSideGUIDs[actorID][i] == GUID).FirstOrDefault();
        }

        internal static void RegisterClientSideMod(string GUID)
        {
            if (!clientSideGUIDs.Contains(GUID)) { clientSideGUIDs.Add(GUID); }
        }

        internal static void MakeFlags()
        {
            if (!PhotonNetwork.IsMasterClient) { return; }
            //UnityEngine.Debug.Log("MAKING FLAGS...");

            // add a host flag for the host
            NetworkingManager.RPC(typeof(SyncModClients), nameof(AddFlags), new object[] { PhotonNetwork.LocalPlayer.ActorNumber, new string[] { "✓ " + PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.LocalPlayer.ActorNumber).NickName, "HOST"}, false });

            // detect unmodded clients
            foreach (int actorId in PhotonNetwork.CurrentRoom.Players.Values.Select(p => p.ActorNumber).Except(clientsServerSideGUIDs.Keys).Except(new int[] { PhotonNetwork.LocalPlayer.ActorNumber }).ToArray())
            {
                NetworkingManager.RPC(typeof(SyncModClients), nameof(AddFlags), new object[] { actorId, new string[] { "✗ " + PhotonNetwork.CurrentRoom.GetPlayer(actorId).NickName, "UNMODDED" }, true });
            }

            foreach (int actorId in clientsServerSideGUIDs.Keys.Intersect(PhotonNetwork.CurrentRoom.Players.Select(kv => kv.Value.ActorNumber)))
            {
                List<string> flags = new List<string>();

                if (missing[actorId].Length == 0 && extra[actorId].Length == 0 && mismatch[actorId].Length == 0)
                {
                    flags.Add("✓ " + PhotonNetwork.CurrentRoom.GetPlayer(actorId).NickName);
                    flags.Add("ALL MODS SYNCED");
                    //UnityEngine.Debug.Log(PhotonNetwork.CurrentRoom.GetPlayer(actorID).NickName + " is synced!");

                    NetworkingManager.RPC(typeof(SyncModClients), nameof(AddFlags), new object[] {actorId, flags.ToArray(), false});
                    continue;
                }

                flags.Add("✗ " + PhotonNetwork.CurrentRoom.GetPlayer(actorId).NickName);
                flags.AddRange(missing[actorId].Select(missingGUID => "MISSING: " + ModIDFromGUID(actorId, missingGUID) + " (" + missingGUID + ")"));
                flags.AddRange(mismatch[actorId].Select(versionGUID => "VERSION: " + ModIDFromGUID(actorId, versionGUID) + " (" + versionGUID + ") Version: " + VersionFromGUID(actorId, versionGUID) + " <b>Host has: " + HostVersionFromGUID(versionGUID) + "</b>"));
                flags.AddRange(extra[actorId].Select(extraGUID => "EXTRA: " + ModIDFromGUID(actorId, extraGUID) + " (" + extraGUID + ") Version: " + VersionFromGUID(actorId, extraGUID)));
                NetworkingManager.RPC(typeof(SyncModClients), nameof(AddFlags), new object[] { actorId, flags.ToArray(), true });
            }


        }

        public static GameObject uiParent;
        [UnboundRPC]
        private static void AddFlags(int actorID, string[] flags, bool error)
        {
            //UnityEngine.Debug.Log("ADDING FLAGS");
            // display the sync status of each player here
            // each player has a unique actorID, which is tied to their Nickname (displayed in the lobby) by PhotonNetwork.CurrentLobby.GetPlayer(actorID).NickName
            // AddFlags adds an array of strings for one actorID when called. the array of strings are the status and warning messages for syncing mods
            // the first entry in the array is a simple "Good" "Bad" (checkmark or X) and ideally would always be shown next to the player's name in the lobby
            // if (error=true) then the text should ideally be red
            // when a player hovers over (with mouse) the green/red check/X it should display a textbox or something with the full error/warning messages - each entry on a new line
            var nickName = PhotonNetwork.CurrentRoom.GetPlayer(actorID).NickName;
            var objName = actorID.ToString();

            var _uiHolder = MenuHandler.modOptionsUI.LoadAsset<GameObject>("uiHolder");
            var _checkmark =  MenuHandler.modOptionsUI.LoadAsset<GameObject>("checkmark");
            var _redx = MenuHandler.modOptionsUI.LoadAsset<GameObject>("redx");
            
            // Check if uiHolder has already been made
            if (!UIHandler.instance.transform.Find("Canvas/UIHolder"))
            {
                uiParent = Object.Instantiate(_uiHolder,UIHandler.instance.transform.Find("Canvas"));
                uiParent.name = "UIHolder";
                uiParent.GetComponent<RectTransform>().localPosition = new Vector3(-975, 486, 2565);
                uiParent.GetOrAddComponent<DetectUnmodded>();
                uiParent.GetOrAddComponent<DetectMissingPlayers>();
                uiParent.GetComponent<VerticalLayoutGroup>().spacing = -60;
            }
            else
            {
                uiParent = UIHandler.instance.transform.Find("Canvas/UIHolder").gameObject;
            }
            
            GameObject playerObj;
            if (!uiParent.transform.Find(objName))
            {
                playerObj = new GameObject();
                playerObj.AddComponent<RectTransform>();
                playerObj.transform.SetParent(uiParent.transform, true);
                playerObj.transform.localScale = Vector3.one;
                playerObj.name = objName;
            }
            else
            {
                playerObj = uiParent.transform.Find(objName).gameObject;
            }

            // destroy sync object and remake it
            while (playerObj.transform.childCount > 0)
            {
                Object.DestroyImmediate(playerObj.transform.GetChild(0).gameObject);
            }

            if (!playerObj.transform.Find(nickName))
            {
                var flag = flags[0];
                if (flag.Contains("✓ "))
                {
                    var check = Object.Instantiate(_checkmark, playerObj.transform);
                    var _hover = check.AddComponent<CheckHover>();
                    _hover.texts = flags;
                    check.transform.localPosition = new Vector3(-15, 25, 0);
                } else if (flag.Contains("✗ "))
                {
                    var redcheck = Object.Instantiate(_redx, playerObj.transform);
                    var _hover = redcheck.AddComponent<CheckHover>();
                    _hover.texts = flags;
                    redcheck.transform.localPosition = new Vector3(-15, 25, 0);
                }
                var text = MenuHandler.CreateText(nickName, playerObj, out var uGUI, 20, false, error ? Color.red : new Color(0.902f, 0.902f, 0.902f, 1f), null, null, TextAlignmentOptions.MidlineLeft );
                text.name = nickName;
                var ping = text.AddComponent<PingUpdater>();
                ping.actorId = actorID;
                var hover = text.AddComponent<CheckHover>();
                hover.texts = flags;
                hover.actorId = actorID;
                uGUI.fontSizeMin = 25;
                var layout = text.AddComponent<LayoutElement>();
                layout.preferredWidth = 300;
                layout.preferredHeight = 100;
                layout.minWidth = 300;
                layout.minHeight = 100;
                
                var rectTrans = text.GetComponent<RectTransform>();
                rectTrans.pivot = Vector2.zero;
                text.transform.localPosition = Vector3.zero;
            }

            Unbound.Instance.ExecuteAfterFrames(5, () =>
            {
                uiParent.GetComponent<VerticalLayoutGroup>().SetLayoutVertical();
            });
        }

        public static IEnumerator DisableSyncModUi(GameObject parent)
        {
            Object.Destroy(parent);
            yield break;
        } 

        [UnboundRPC]
        private static void SendModList()
        {
            NetworkingManager.RPC(typeof(SyncModClients), "ReceiveModList", new object[] { loadedGUIDs.ToArray(), loadedModNames.ToArray(), loadedVersions.ToArray(), PhotonNetwork.LocalPlayer.ActorNumber });
        }

        [UnboundRPC]
        private static void ReceiveModList(string[] serverSideGUIDs, string[] serverSideMods, string[] versions, int actorID)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            if (PhotonNetwork.LocalPlayer.ActorNumber == actorID)
            {
                hostsServerSideGUIDs = serverSideGUIDs.ToList();
                hostsServerSideMods = serverSideMods.ToList();
                hostsModVersions = versions.ToList();
            }
            else
            {
                clientsServerSideGUIDs[actorID] = serverSideGUIDs;
                clientsServerSideMods[actorID] = serverSideMods;
                clientsModVersions[actorID] = versions;
            }
        }
    }

    internal class PingUpdater : MonoBehaviour
    {
        public int actorId;

        private TextMeshProUGUI textBox;
        private string text = null;

        private void Start()
        {
            PingMonitor.instance.PingUpdateAction += OnPingUpdate;
        }

        private void OnPingUpdate(int updatedActorId, int ping)
        {
            if (!textBox)
            {
                textBox = gameObject.GetComponent<TextMeshProUGUI>();
                text = textBox.text;
            }

            var color = PingMonitor.instance.GetPingColors(ping);

            if (textBox.color == Color.red)
            {
                color = PingMonitor.instance.GetPingColors(5000);
            }

            if (updatedActorId == actorId)
            {
                textBox.text = $"{text} - <color={color.HTMLCode}>{ping}ms</color>";
            }
        }

        private void OnDestroy()
        {
            PingMonitor.instance.PingUpdateAction -= OnPingUpdate;
        }
    }

    internal class DetectUnmodded : MonoBehaviour
    {
        private const float BaseDelay = 1f;
        private float delay = 1f;
        private float startTime;
        private int prevPlayers;
        private bool update;

        private void Start()
        {
            startTime = Time.time;
            prevPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            delay = BaseDelay;
        }

        private void Update()
        {
            if(Time.time <= startTime + delay) return;
            
            if (update)
            {
                SyncModClients.MakeFlags();
                update = false;
                delay = BaseDelay;
            }
            else
            {
                startTime = Time.time;
                if (prevPlayers == PhotonNetwork.CurrentRoom.PlayerCount) return;
                
                prevPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                delay = SyncModClients.timeoutTime + 2f;
                update = true;
            }
        }
    }

    internal class DetectMissingPlayers : MonoBehaviour
    {
        private const float BaseDelay = 1f;
        private float delay = BaseDelay;

        private void Start()
        {
            delay = BaseDelay;
        }

        private void Update()
        {
            if (delay <= 0f)
            {
                delay = BaseDelay;

                // look through all of the mod syncing objects and make sure there is a photon client with the same actor number
                List<int> toDestroy = new List<int>();
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    GameObject playerObj = gameObject.transform.GetChild(i).gameObject;
                    if (!PhotonNetwork.CurrentRoom.Players.Keys.Select(aID => aID.ToString()).Contains(playerObj.name))
                    {
                        toDestroy.Add(i);
                    }
                }
                foreach (int i in toDestroy)
                {
                    Destroy(gameObject.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                delay -= TimeHandler.deltaTime;
            }
        }
    }

    internal class CheckHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string[] texts;
        public int actorId;

        private GUIStyle guiStyleFore;
        private string pingString = "";

        private bool inBounds;

        private void Start()
        {
            guiStyleFore = new GUIStyle
            {
                richText = true,
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                stretchWidth = true
            };
            var background = new Texture2D(1, 1);
            background.SetPixel(0,0, Color.gray);
            background.Apply();
            guiStyleFore.normal.background = background;
            guiStyleFore.fontSize = 20;

            PingMonitor.instance.PingUpdateAction += OnPingUpdate;
        }
        private void OnGUI()
        {
            if (!inBounds || texts == Array.Empty<string>() || !(Input.mousePosition.x < Screen.width / 4f)) return;
            
            Vector2 size = guiStyleFore.CalcSize(new GUIContent(string.Join("\n",texts)));
            GUILayout.BeginArea(new Rect(Input.mousePosition.x + 25, Screen.height - Input.mousePosition.y + 25, size.x + 10, size.y+10));
            GUILayout.BeginVertical();
            foreach (var t in texts)
            {
                GUILayout.Label (t, guiStyleFore);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            inBounds = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            inBounds = false;
        }

        private void OnPingUpdate(int updatedActorId, int ping)
        {
            if (updatedActorId != actorId) return;

            if (pingString == "")
            {
                pingString = texts[0];
            }

            var color = PingMonitor.instance.GetPingColors(ping);

            texts[0] = $"{pingString} - <color={color.HTMLCode}>{ping}ms</color>";
        }

        private void OnDestroy()
        {
            PingMonitor.instance.PingUpdateAction -= OnPingUpdate;
        }
    }
}
