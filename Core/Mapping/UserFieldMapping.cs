using System.Collections.Generic;

namespace EVESyncTool.Core.Mapping
{
    public static class UserFieldMapping
    {
        public static Dictionary<string, string> WindowTitleMapping = new Dictionary<string, string>();
        public static Dictionary<string, string> ChatChannelMapping = new Dictionary<string, string>();
        public static Dictionary<string, string> OverviewTabMapping = new Dictionary<string, string>();
        public static Dictionary<string, string> CustomCommandMapping = new Dictionary<string, string>();
        public static Dictionary<string, string> BookmarkFolderMapping = new Dictionary<string, string>();

        public static void BuildWindowTitleMapping(Dictionary<string, string> windowData)
        {
            if (windowData == null)
            {
                WindowTitleMapping = new Dictionary<string, string>();
                return;
            }

            var filtered = new Dictionary<string, string>();
            foreach (var kvp in windowData)
            {
                if (kvp.Value.Contains("私聊(") || kvp.Value.Contains("私聊（"))
                    continue;
                filtered[kvp.Key] = kvp.Value;
            }
            WindowTitleMapping = filtered;
        }

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

        public static void BuildOverviewTabMapping(Dictionary<string, string> overviewData)
        {
            OverviewTabMapping = overviewData ?? new Dictionary<string, string>();
        }

        public static void BuildCustomCommandMapping(Dictionary<string, string> commandData)
        {
            CustomCommandMapping = commandData ?? new Dictionary<string, string>();
        }

        public static void BuildBookmarkFolderMapping(Dictionary<string, string> bookmarkData)
        {
            BookmarkFolderMapping = bookmarkData ?? new Dictionary<string, string>();
        }

        public static void BuildAll(
            Dictionary<string, string> windowData,
            Dictionary<string, string> channelData,
            Dictionary<string, string> overviewData,
            Dictionary<string, string> commandData,
            Dictionary<string, string> bookmarkData)
        {
            BuildWindowTitleMapping(windowData);
            BuildChatChannelMapping(channelData);
            BuildOverviewTabMapping(overviewData);
            BuildCustomCommandMapping(commandData);
            BuildBookmarkFolderMapping(bookmarkData);
        }

        public static void Clear()
        {
            WindowTitleMapping = new Dictionary<string, string>();
            ChatChannelMapping = new Dictionary<string, string>();
            OverviewTabMapping = new Dictionary<string, string>();
            CustomCommandMapping = new Dictionary<string, string>();
            BookmarkFolderMapping = new Dictionary<string, string>();
        }

        public static string GetWindowTitle(string key)
        {
            return WindowTitleMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static string GetChatChannelName(string key)
        {
            return ChatChannelMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static string GetOverviewTabName(string key)
        {
            return OverviewTabMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static string GetCustomCommand(string key)
        {
            return CustomCommandMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static string GetBookmarkFolderName(string key)
        {
            return BookmarkFolderMapping.TryGetValue(key, out string value) ? value : key;
        }

        public static bool IsPrivateChatTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;
            return title.Contains("私聊(") || title.Contains("私聊（");
        }

        public static bool IsLocalChannelTitle(string title)
        {
            return title == "本地";
        }

        public static bool IsGroupChatTitle(string title)
        {
            return title.StartsWith("群聊(");
        }

        public static bool IsPublicChannelTitle(string title)
        {
            return ChatChannelMapping.ContainsValue(title);
        }
    }
}