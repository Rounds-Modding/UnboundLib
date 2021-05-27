using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UnboundLib
{
    public static class NetworkingManager
    {
        private static bool initialized = false;

        private static RaiseEventOptions raiseEventOptionsAll = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All,
            CachingOption = EventCaching.AddToRoomCache
        };
        private static RaiseEventOptions raiseEventOptionsOthers = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };
        private static SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };

        public delegate void PhotonEvent(object[] objects);
        private static Dictionary<string, PhotonEvent> events = new Dictionary<string, PhotonEvent>();

        private static byte ModEventCode = 69;

        static NetworkingManager()
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        public static void RegisterEvent(string eventName, PhotonEvent handler)
        {
            if (events.ContainsKey(eventName))
            {
                throw new Exception($"An event handler is already registered with the event name: {eventName}");
            }

            events.Add(eventName, handler);
        }
        public static void RaiseEvent(string eventName, params object[] data)
        {
            if (data == null) data = new object[0];
            var allData = new List<object>();
            allData.Add(eventName);
            allData.AddRange(data);
            PhotonNetwork.RaiseEvent(ModEventCode, allData.ToArray(), raiseEventOptionsAll, sendOptions);
        }
        public static void RaiseEventOthers(string eventName, params object[] data)
        {
            if (data == null) data = new object[0];
            var allData = new List<object>();
            allData.Add(eventName);
            allData.AddRange(data);
            PhotonNetwork.RaiseEvent(ModEventCode, allData.ToArray(), raiseEventOptionsOthers, sendOptions);
        }

        public static void OnEvent(EventData photonEvent)
        {
            object[] data = null;

            try
            {
                data = (object[])photonEvent.CustomData;
            }
            catch (Exception e)
            {
                return;
            }

            if (photonEvent.Code != ModEventCode) return;

            try
            {
                if (events.TryGetValue((string)data[0], out PhotonEvent handler))
                {
                    handler?.Invoke(data.Skip(1).ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Network Error: \n" + e.ToString());
            }
        }
    }
}
