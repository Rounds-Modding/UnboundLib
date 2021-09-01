using System.Collections.Generic;
using UnboundLib.Extensions;

namespace UnboundLib.Stats
{
    public static class StatDictionary
    {
        private static Dictionary<string, StatInfo> data = new Dictionary<string, StatInfo>();

        public static string[] GetStats()
        {
            if (data.Count > 0)
            {
                return (new List<string>(data.Keys)).ToArray();
            }

            return null;
        }

        public static void AddStat(string statName, StatInfo statInfo)
        {
            if (data.TryGetValue(statName, out StatInfo statdata))
            {
                // Throw error, show pre-existing data in log.
            }
            else
            {
                data.Add(statName, statInfo);
            }
        }

        public static void Clear()
        {
            data.Clear();
        }

        public static Dictionary<string, StatInfo> GetRaw()
        {
            return data;
        }

        public static void CopyCustomStats(Player player, Gun gun, CharacterData characterdata, HealthHandler healthHandler, Gravity gravity, Block block, GunAmmo gunAmmo, CharacterStatModifiers statModifiers, Gun copyFromGun, CharacterStatModifiers copyFromCSM, Block copyFromBlock)
        {
            Dictionary<string, List<StatOperation>> customStatChanges = new Dictionary<string, List<StatOperation>>();
            StatOperation value;
            foreach (var key in copyFromGun.GetAdditionalData().customStats)
            {
                if (key.Value.GetType() == typeof(StatOperation))
                {
                    value = key.Value;
                    if (!value.reroute)
                    {
                        value.reroute = true;
                        value.destination = StatOperation.Destination.Gun;
                    }
                    if (customStatChanges.TryGetValue(key.Key, out List<StatOperation> list))
                    {
                        list.Add(value);
                    }
                    else
                    {
                        customStatChanges.Add(key.Key, new List<StatOperation>() { value });
                    }
                }
            }
            foreach (var key in copyFromBlock.GetAdditionalData().customStats)
            {
                if (key.Value.GetType() == typeof(StatOperation))
                {
                    value = key.Value;
                    if (!value.reroute)
                    {
                        value.reroute = true;
                        value.destination = StatOperation.Destination.Block;
                    }
                    if (customStatChanges.TryGetValue(key.Key, out List<StatOperation> list))
                    {
                        list.Add(value);
                    }
                    else
                    {
                        customStatChanges.Add(key.Key, new List<StatOperation>() { value });
                    }
                }
            }
            foreach (var key in copyFromCSM.GetAdditionalData().customStats)
            {
                if (key.Value.GetType() == typeof(StatOperation))
                {
                    value = key.Value;
                    if (!value.reroute)
                    {
                        value.reroute = true;
                        value.destination = StatOperation.Destination.CharacterStatModifier;
                    }
                    if (customStatChanges.TryGetValue(key.Key, out List<StatOperation> list))
                    {
                        list.Add(value);
                    }
                    else
                    {
                        customStatChanges.Add(key.Key, new List<StatOperation>() { value });
                    }
                }
            }

            foreach (var key in customStatChanges)
            { 
                foreach(var operation in key.Value)
                {
                    switch (operation.destination)
                    {
                        case StatOperation.Destination.Gun:
                            PerformStatOperation(key.Key, gun.GetAdditionalData().customStats, operation);
                            break;
                        case StatOperation.Destination.Block:
                            PerformStatOperation(key.Key, block.GetAdditionalData().customStats, operation);
                            break;
                        //case StatOperation.Destination.CharacterData:
                        //    PerformStatOperation(key.Key, characterdata.GetAdditionalData().customStats, operation);
                        //    break;
                        case StatOperation.Destination.CharacterStatModifier:
                            PerformStatOperation(key.Key, statModifiers.GetAdditionalData().customStats, operation);
                            break;
                        //case StatOperation.Destination.Gravity:
                        //    PerformStatOperation(key.Key, gravity.GetAdditionalData().customStats, operation);
                        //    break;
                        //case StatOperation.Destination.HealthHandler:
                        //    PerformStatOperation(key.Key, healthHandler.GetAdditionalData().customStats, operation);
                        //    break;
                        //case StatOperation.Destination.Player:
                        //    PerformStatOperation(key.Key, player.GetAdditionalData().customStats, operation);
                        //    break;
                        case StatOperation.Destination.GunAmmo:
                            PerformStatOperation(key.Key, gunAmmo.GetAdditionalData().customStats, operation);
                            break;
                    }
                }
            }
        }

        private static void PerformStatOperation(string key, Dictionary<string, dynamic> to, StatOperation from)
        {
            // If the stat doesn't exist yet, but we have a registered default value, set it to that.
            if (!to.TryGetValue(key, out dynamic value) && StatDictionary.GetRaw().TryGetValue(key, out StatInfo statInfo))
            {
                to.Add(key, statInfo.defaultValue);
            }
            // If the stat exists already, operate on it 
            if (to.TryGetValue(key, out dynamic statVal))
            {
                // If the operation is a replace, we don't care about previous typing.
                if (from.operation == StatOperation.Operation.Replace)
                {
                    statVal = from.value;
                }
                // If we're adding to a list, and it is a list, we can add to it.
                if (from.operation == StatOperation.Operation.AddToList && statVal.GetType().IsGenericType && statVal.GetType().GetGenericTypeDefinition() == typeof(List<>))
                {
                    statVal.Add(from.value);
                }
                // We cannot perform math with values of different types, so we check to make sure they're the same here.
                if (statVal.GetType() == from.value.GetType())
                {
                    switch (from.operation)
                    {
                        case (StatOperation.Operation.Add):
                            statVal += from.value;
                            break;
                        case (StatOperation.Operation.Multiply):
                            statVal *= from.value;
                            break;
                        case (StatOperation.Operation.Divide):
                            statVal /= from.value;
                            break;
                        case (StatOperation.Operation.Subtract):
                            statVal -= from.value;
                            break;
                    }
                }
                else
                {
                    // Throw error about types not being the same
                }
            }
            else // If the stat doesn't exist yet (and therefore not registered, or it would be set already), we can only perform a replace operation.
            {
                if (from.operation == StatOperation.Operation.Replace)
                {
                    to.Add(key, from.value);
                }
            }

        }
    }
}
