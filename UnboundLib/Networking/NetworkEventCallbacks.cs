using Photon.Pun;
using UnboundLib.GameModes;

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
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            GameModeManager.CurrentHandler.OnPlayerLeftRoom(otherPlayer);
        }
    }
}