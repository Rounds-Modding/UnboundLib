using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

//From PCE
namespace UnboundLib.Extensions
{
    // ADD FIELDS TO GUN
    [Serializable]
    public class GravityAdditionalData
    {
        public Dictionary<string, dynamic> customStats;

        public GravityAdditionalData()
        {
            customStats = new Dictionary<string, dynamic>();
        }
    }
    public static class GravityExtension
    {
        public static readonly ConditionalWeakTable<Gravity, GravityAdditionalData> data =
            new ConditionalWeakTable<Gravity, GravityAdditionalData>();

        public static GravityAdditionalData GetAdditionalData(this Gravity gravity)
        {
            return data.GetOrCreateValue(gravity);
        }

        public static void AddData(this Gravity gravity, GravityAdditionalData value)
        {
            try
            {
                data.Add(gravity, value);
            }
            catch (Exception) { }
        }
    }
    // reset extra gravity attributes when resetstats is called
    //[HarmonyPatch(typeof(Gravity), "ResetStats")]
    //class GravityPatchResetStats
    //{
    //    private static void Prefix(Gravity __instance)
    //    {
    //        __instance.GetAdditionalData().customStats.Clear();
    //    }
    //}
}