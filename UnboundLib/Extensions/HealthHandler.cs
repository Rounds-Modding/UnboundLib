using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

//From PCE
namespace UnboundLib.Extensions
{
    // ADD FIELDS TO GUN
    [Serializable]
    public class HealthHandlerAdditionalData
    {
        public Dictionary<string, dynamic> customStats;

        public HealthHandlerAdditionalData()
        {
            customStats = new Dictionary<string, dynamic>();
        }
    }
    public static class HealthHandlerExtension
    {
        public static readonly ConditionalWeakTable<HealthHandler, HealthHandlerAdditionalData> data =
            new ConditionalWeakTable<HealthHandler, HealthHandlerAdditionalData>();

        public static HealthHandlerAdditionalData GetAdditionalData(this HealthHandler healthHandler)
        {
            return data.GetOrCreateValue(healthHandler);
        }

        public static void AddData(this HealthHandler healthHandler, HealthHandlerAdditionalData value)
        {
            try
            {
                data.Add(healthHandler, value);
            }
            catch (Exception) { }
        }
    }
    // reset extra healthHandler attributes when resetstats is called
    //[HarmonyPatch(typeof(HealthHandler), "ResetStats")]
    //class HealthHandlerPatchResetStats
    //{
    //    private static void Prefix(HealthHandler __instance)
    //    {
    //        __instance.GetAdditionalData().customStats.Clear();
    //    }
    //}
}