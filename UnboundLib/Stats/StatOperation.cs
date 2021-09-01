using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using TMPro;

namespace UnboundLib.Stats
{
    public class StatOperation
    {
        public dynamic value;
        public Operation operation;
        public bool reroute;
        public Destination destination;

        public StatOperation(dynamic statValue, Operation operationType, bool routeToDifferentStat = false, Destination otherStat = Destination.CharacterStatModifier)
        {
            value = statValue;
            operation = operationType;
            reroute = routeToDifferentStat;
            destination = otherStat;
        }

        public enum Operation
        {
            Replace,
            Multiply,
            Add,
            Divide,
            Subtract,
            AddToList
        }

        public enum Destination
        {
            Gun,
            CharacterStatModifier,
            //Player,
            //CharacterData,
            //HealthHandler,
            //Gravity,
            GunAmmo,
            Block
        }
    }
}
