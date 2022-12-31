using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

//From PCE
namespace UnboundLib.Extensions
{
    // ADD FIELDS TO GUN
    [Serializable]
    public class CharacterDataAdditionalData
    {
        public Dictionary<string, dynamic> customStats;

        public CharacterDataAdditionalData()
        {
            customStats = new Dictionary<string, dynamic>();
        }
    }
    public static class CharacterDataExtension
    {
        public static readonly ConditionalWeakTable<CharacterData, CharacterDataAdditionalData> data =
            new ConditionalWeakTable<CharacterData, CharacterDataAdditionalData>();

        public static CharacterDataAdditionalData GetAdditionalData(this CharacterData characterData)
        {
            return data.GetOrCreateValue(characterData);
        }

        public static void AddData(this CharacterData characterData, CharacterDataAdditionalData value)
        {
            try
            {
                data.Add(characterData, value);
            }
            catch (Exception) { }
        }
    }
    // reset extra characterData attributes when resetstats is called
    //[HarmonyPatch(typeof(CharacterData), "ResetStats")]
    //class CharacterDataPatchResetStats
    //{
    //    private static void Prefix(CharacterData __instance)
    //    {
    //        __instance.GetAdditionalData().customStats.Clear();
    //    }
    //}
}