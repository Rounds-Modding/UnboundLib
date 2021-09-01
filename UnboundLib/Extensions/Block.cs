using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

//From PCE
namespace UnboundLib.Extensions
{
    // ADD FIELDS TO GUN
    [Serializable]
    public class BlockAdditionalData
    {
        public Dictionary<string, dynamic> customStats;

        public BlockAdditionalData()
        {
            customStats = new Dictionary<string, dynamic>();
        }
    }
    public static class BlockExtension
    {
        public static readonly ConditionalWeakTable<Block, BlockAdditionalData> data =
            new ConditionalWeakTable<Block, BlockAdditionalData>();

        public static BlockAdditionalData GetAdditionalData(this Block block)
        {
            return data.GetOrCreateValue(block);
        }

        public static void AddData(this Block block, BlockAdditionalData value)
        {
            try
            {
                data.Add(block, value);
            }
            catch (Exception) { }
        }
    }
    // reset extra block attributes when resetstats is called
    [HarmonyPatch(typeof(Block), "ResetStats")]
    class BlockPatchResetStats
    {
        private static void Prefix(Block __instance)
        {
            __instance.GetAdditionalData().customStats.Clear();
        }
    }
}