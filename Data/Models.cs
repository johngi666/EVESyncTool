using System;
using System.Collections.Generic;
using EVESyncTool.Core.Config;

namespace EVESyncTool.Data
{
    /// <summary>
    /// 统一配置数据模型
    /// </summary>
    public class AppConfig
    {
        public string LastServer { get; set; } = "曙光服 (Infinity)";
        public string CachedPath { get; set; } = "";
        public Dictionary<string, string> CharacterNames { get; set; } = new Dictionary<string, string>();
        public List<ConfigScheme> ConfigSchemes { get; set; } = new List<ConfigScheme>();
        public SyncSettings SyncSettings { get; set; } = new SyncSettings();
    }

    /// <summary>
    /// 配置方案数据模型
    /// </summary>
    public class ConfigScheme
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime LastUsedTime { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
