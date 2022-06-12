using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnboundLib.Extensions
{
    [Serializable]
    public class GM_ArmsRaceAdditionalData
    {
        public int[] previousRoundWinners;
        public int[] previousPointWinners;

        public GM_ArmsRaceAdditionalData()
        {
            previousRoundWinners = new int[] { };
            previousPointWinners = new int[] { };
        }
    }
    public static class GM_ArmsRaceExtensions
    {
        public static readonly ConditionalWeakTable<GM_ArmsRace, GM_ArmsRaceAdditionalData> data =
            new ConditionalWeakTable<GM_ArmsRace, GM_ArmsRaceAdditionalData>();

        public static GM_ArmsRaceAdditionalData GetAdditionalData(this GM_ArmsRace instance)
        {
            return data.GetOrCreateValue(instance);
        }

        public static void AddData(this GM_ArmsRace instance, GM_ArmsRaceAdditionalData value)
        {
            try
            {
                data.Add(instance, value);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
