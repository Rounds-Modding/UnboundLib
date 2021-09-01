using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

//From PCE
namespace UnboundLib.Extensions
{
    // ADD FIELDS TO GUN
    [Serializable]
    public class PlayerAdditionalData
    {
        public Dictionary<string, dynamic> customStats;

        public PlayerAdditionalData()
        {
            customStats = new Dictionary<string, dynamic>();
        }
    }
    public static class PlayerExtension
    {
        public static readonly ConditionalWeakTable<Player, PlayerAdditionalData> data =
            new ConditionalWeakTable<Player, PlayerAdditionalData>();

        public static PlayerAdditionalData GetAdditionalData(this Player player)
        {
            return data.GetOrCreateValue(player);
        }

        public static void AddData(this Player player, PlayerAdditionalData value)
        {
            try
            {
                data.Add(player, value);
            }
            catch (Exception) { }
        }
    }
    // reset extra player attributes when resetstats is called
    [HarmonyPatch(typeof(Player), "FullReset")]
    class PlayerPatchResetStats
    {
        private static void Prefix(Player __instance)
        {
            __instance.GetAdditionalData().customStats.Clear();
        }
    }
}