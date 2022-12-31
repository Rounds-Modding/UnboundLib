using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

//From PCE
namespace UnboundLib.Extensions
{
    // ADD FIELDS TO GUN
    [Serializable]
    public class GunAmmoAdditionalData
    {
        public Dictionary<string, dynamic> customStats;

        public GunAmmoAdditionalData()
        {
            customStats = new Dictionary<string, dynamic>();
        }
    }
    public static class GunAmmoExtension
    {
        public static readonly ConditionalWeakTable<GunAmmo, GunAmmoAdditionalData> data =
            new ConditionalWeakTable<GunAmmo, GunAmmoAdditionalData>();

        public static GunAmmoAdditionalData GetAdditionalData(this GunAmmo gunAmmo)
        {
            return data.GetOrCreateValue(gunAmmo);
        }

        public static void AddData(this GunAmmo gunAmmo, GunAmmoAdditionalData value)
        {
            try
            {
                data.Add(gunAmmo, value);
            }
            catch (Exception) { }
        }
    }
    // reset extra gunAmmo attributes when resetstats is called via gun
}