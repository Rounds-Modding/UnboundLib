using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Photon.Pun;

namespace UnboundLib.Stats
{
    public class StatInfo
    {
        public dynamic type;
        public dynamic defaultValue;
        public string sourceMod;
        public string description;

        public StatInfo(dynamic statDefaultValue, string statSourceMod = "com.mod.id", string statDescription = "If only this stat had a description.")
        {
            type = statDefaultValue.GetType();
            defaultValue = statDefaultValue;
            sourceMod = statSourceMod;
            description = statDescription;
        }
    }
}
