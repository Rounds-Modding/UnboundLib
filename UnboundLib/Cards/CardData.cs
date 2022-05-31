using System.Collections.Generic;

namespace UnboundLib.Cards
{
    public static class CardData
    {
        private static Dictionary<int, List<string>> data = new Dictionary<int, List<string>>();

        public static string[] GetCards(int teamId)
        {
            return data.TryGetValue(teamId, out var list) ? list.ToArray() : null;
        }

        public static void AddCard(int teamId, string cardName)
        {
            if (data.TryGetValue(teamId, out var list))
            {
                list.Add(cardName);
            }
            else
            {
                data.Add(teamId, new List<string>{ cardName });
            }
        }

        public static void Clear()
        {
            foreach (var key in data.Keys)
            {
                data[key].Clear();
            }
        }

        public static Dictionary<int, List<string>> GetRaw()
        {
            return data;
        }
    }
}
