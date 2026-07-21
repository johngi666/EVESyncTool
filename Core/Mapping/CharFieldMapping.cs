using System.Collections.Generic;

namespace EVESyncTool.Core.Mapping
{
    public static class CharFieldMapping
    {
        public static Dictionary<string, string> ChatChannelMapping = new Dictionary<string, string>();
        public static Dictionary<string, string> FittingNameMapping = new Dictionary<string, string>();

        public static void BuildChatChannelMapping(Dictionary<string, string> channelData)
        {
            if (channelData == null)
            {
                ChatChannelMapping = new Dictionary<string, string>();
                return;
            }

            var filtered = new Dictionary<string, string>();
            foreach (var kvp in channelData)
            {
                if (kvp.Key.Contains("私聊") || kvp.Value.Contains("私聊"))
                    continue;
                filtered[kvp.Key] = kvp.Value;
            }
            ChatChannelMapping = filtered;
        }

        public static void BuildFittingNameMapping(Dictionary<string, string> fittingData)
        {
            FittingNameMapping = fittingData ?? new Dictionary<string, string>();
        }

        public static void Clear()
        {
            ChatChannelMapping = new Dictionary<string, string>();
            FittingNameMapping = new Dictionary<string, string>();
        }

        public static string GetChatChannelName(string key)
        {
            return ChatChannelMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static string GetFittingName(string key)
        {
            return FittingNameMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static bool IsPublicChannel(string key)
        {
            return ChatChannelMapping.ContainsKey(key);
        }

        public static bool IsFitting(string key)
        {
            return FittingNameMapping.ContainsKey(key);
        }
    }
}