using System;
using System.Runtime.CompilerServices;

namespace UnboundLib.Cards
{
    // add fields to CardInfo - accessible via CardInfo.GetAdditionalData().<field>
    [Serializable]
    public class CardInfoAdditionalData
    {
        public bool CardEnabled; // if false, the card will not be added to the deck(s) or toggle menu when built. default is true

        public CardInfoAdditionalData()
        {
            CardEnabled = true;
        }
    }
    public static class CardInfoExtension
    {
        public static readonly ConditionalWeakTable<CardInfo, CardInfoAdditionalData> data =
            new ConditionalWeakTable<CardInfo, CardInfoAdditionalData>();

        public static CardInfoAdditionalData GetAdditionalData(this CardInfo cardInfo)
        {
            return data.GetOrCreateValue(cardInfo);
        }

        public static void AddData(this CardInfo cardInfo, CardInfoAdditionalData value)
        {
            try
            {
                data.Add(cardInfo, value);
            }
            catch (Exception) { }
        }
    }
}
