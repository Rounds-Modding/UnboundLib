using HarmonyLib;
using System;
using System.Runtime.Serialization;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Patches
{
    [Serializable]
    [HarmonyPatch(typeof(CardInfo), "Awake")]
    public class CardInfoPatch
    {
        static Exception Finalizer(Exception __exception)
        {
            return __exception is NullReferenceException ? null : __exception;
        }
    }
}