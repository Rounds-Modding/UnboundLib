using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace UnboundLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class UnboundRPC : Attribute
    {
        public string Key { get; private set; }

        public UnboundRPC(string key)
        {
            this.Key = key;
        }
    }

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
        private static Dictionary<string, MethodInfo> rpcHandlers = new Dictionary<string, MethodInfo>();

        private static byte ModEventCode = 69;

        static NetworkingManager()
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        public static void RegisterRPCHandlers()
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyName = assembly.GetName();

            // Gather all methods in all classes, in the calling assembly, that have the UnboundRPC attribute
            var methods = assembly.GetTypes()
                                  .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                  .Where(m => m.GetCustomAttributes(typeof(UnboundRPC), false).Length > 0)
                                  .ToArray();

            foreach (var method in methods)
            {
                foreach (var rpcAttribute in method.GetCustomAttributes<UnboundRPC>())
                {
                    var eventName = $"{assemblyName}_RPC_{rpcAttribute.Key}";

                    if (!rpcHandlers.ContainsKey(eventName))
                    {
                        NetworkingManager.RegisterRPCHandler(eventName, method);
                    }
                }
            }
        }

        public static void RPC(string key, params object[] args)
        {
            var assembly = Assembly.GetCallingAssembly();
            var eventName = $"{assembly.GetName()}_RPC_{key}";

            if (!rpcHandlers.ContainsKey(eventName))
            {
                throw new Exception($"Could not find RPC method registered with the key: {key}");
            }

            NetworkingManager.RaiseEvent(eventName, args);
        }

        public static void RegisterEvent(string eventName, PhotonEvent handler)
        {
            if (events.ContainsKey(eventName))
            {
                throw new Exception($"An event handler is already registered with the event name: {eventName}");
            }

            events.Add(eventName, handler);
        }

        public static void RegisterRPCHandler(string key, MethodInfo handler)
        {
            if (rpcHandlers.ContainsKey(key))
            {
                throw new Exception($"An RPC handler is already registered with the key: {key}");
            }

            rpcHandlers.Add(key, handler);
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
                if (events.TryGetValue((string)data[0], out PhotonEvent eventHandler))
                {
                    eventHandler?.Invoke(data.Skip(1).ToArray());
                }

                if (rpcHandlers.TryGetValue((string)data[0], out MethodInfo rpcHandler))
                {
                    rpcHandler?.Invoke(null, data.Skip(1).ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Network Error: \n" + e.ToString());
            }
        }
    }
}
