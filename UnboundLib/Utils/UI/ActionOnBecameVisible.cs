using System;
using UnityEngine;

namespace UnboundLib.Utils.UI
{
    public class ActionOnBecameVisible : MonoBehaviour
    {
        public Action visibleAction = () => { };

        private void OnBecameVisible()
        {
            visibleAction.Invoke();
        }
    }
}