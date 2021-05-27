using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Photon.Pun;

namespace UnboundLib
{
    public class NetworkEventCallbacks : MonoBehaviourPunCallbacks
    {
        public delegate void NetworkEvent();
        public event NetworkEvent OnJoinedRoomEvent, OnLeftRoomEvent;

        public override void OnJoinedRoom()
        {
            OnJoinedRoomEvent?.Invoke();
        }
        public override void OnLeftRoom()
        {
            OnLeftRoomEvent?.Invoke();
        }
    }
}