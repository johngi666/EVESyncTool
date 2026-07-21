using EVESyncTool.Core.Utils;
using EVESyncTool.Core.Config;
using System;
using System.Collections.Generic;

namespace EVESyncTool.Data

{
    /// <summary>
    /// 同步配置文件 - 保存用户选择的同步方案
    /// </summary>
    public class SyncProfile
    {
        /// <summary>
        /// 配置文件唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 配置文件名称（用户自定义）
        /// </summary>
        public string Name { get; set; } = "默认方案";

        /// <summary>
        /// 配置文件描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 源服务器（同步来源）
        /// </summary>
        public string SourceServer { get; set; } = "曙光服 (Infinity)";

        /// <summary>
        /// 源文件夹路径
        /// </summary>
        public string SourceFolder { get; set; } = string.Empty;

        /// <summary>
        /// 目标文件夹路径（可以多个）
        /// </summary>
        public List<string> TargetFolders { get; set; } = new List<string>();

        /// <summary>
        /// 同步设置（引用 SyncSettings）
        /// </summary>
        public SyncSettings Settings { get; set; } = new SyncSettings();

        /// <summary>
        /// 是否启用此配置文件
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 是否自动执行同步
        /// </summary>
        public bool AutoSync { get; set; } = false;

        /// <summary>
        /// 创建配置文件的副本
        /// </summary>
        public SyncProfile Clone()
        {
            return new SyncProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{Name} (副本)",
                Description = Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SourceServer = SourceServer,
                SourceFolder = SourceFolder,
                TargetFolders = new List<string>(TargetFolders),
                Settings = new SyncSettings
                {
                    OverrideChatConfig = Settings.OverrideChatConfig,
                    OverridePublicChannelNames = Settings.OverridePublicChannelNames,
                    OverrideGroupChatTitles = Settings.OverrideGroupChatTitles,
                    OverrideOtherWindowTitles = Settings.OverrideOtherWindowTitles,
                    OverrideOverviewTabs = Settings.OverrideOverviewTabs,
                    OverrideCustomCommands = Settings.OverrideCustomCommands,
                    OverrideBookmarkFolders = Settings.OverrideBookmarkFolders,
                    OverrideFittingNames = Settings.OverrideFittingNames
                },
                IsEnabled = IsEnabled,
                AutoSync = AutoSync
            };
        }

        /// <summary>
        /// 验证配置文件是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) &&
                   !string.IsNullOrEmpty(SourceFolder) &&
                   TargetFolders != null &&
                   TargetFolders.Count > 0;
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public string GetSummary()
        {
            return $@"═══════════════════════════════════════
同步方案: {Name}
{(string.IsNullOrEmpty(Description) ? "" : $"描述: {Description}")}
源服务器: {SourceServer}
源文件夹: {SourceFolder}
目标文件夹: {TargetFolders.Count} 个
启用状态: {(IsEnabled ? "✔ 已启用" : "✘ 已禁用")}
自动同步: {(AutoSync ? "✔ 开启" : "✘ 关闭")}
创建时间: {CreatedAt:yyyy-MM-dd HH:mm}
最后更新: {UpdatedAt:yyyy-MM-dd HH:mm}
═══════════════════════════════════════";
        }

        /// <summary>
        /// 获取简短的显示名称
        /// </summary>
        public string GetDisplayName()
        {
            string status = IsEnabled ? "" : "[已禁用] ";
            string targets = TargetFolders != null ? $"(→ {TargetFolders.Count}个目标)" : "";
            return $"{status}{Name} {targets}";
        }
    }

    /// <summary>
    /// 同步配置文件管理器（内存缓存 + 文件持久化）
    /// </summary>
    public class SyncProfileManager
    {
        private const string ProfilesFile = "sync_profiles.json";
        private List<SyncProfile> _profiles = new List<SyncProfile>();
        private readonly object _lock = new object();

        public SyncProfileManager()
        {
            Load();
        }

        /// <summary>
        /// 获取所有配置文件
        /// </summary>
        public List<SyncProfile> GetAll()
        {
            lock (_lock)
            {
                return new List<SyncProfile>(_profiles);
            }
        }

        /// <summary>
        /// 获取已启用的配置文件
        /// </summary>
        public List<SyncProfile> GetEnabled()
        {
            lock (_lock)
            {
                return _profiles.FindAll(p => p.IsEnabled);
            }
        }

        /// <summary>
        /// 根据 ID 获取配置文件
        /// </summary>
        public SyncProfile GetById(string id)
        {
            lock (_lock)
            {
                return _profiles.Find(p => p.Id == id);
            }
        }

        /// <summary>
        /// 根据名称获取配置文件
        /// </summary>
        public SyncProfile GetByName(string name)
        {
            lock (_lock)
            {
                return _profiles.Find(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// 添加配置文件
        /// </summary>
        public bool Add(SyncProfile profile)
        {
            if (profile == null || !profile.IsValid())
                return false;

            // 检查名称是否重复
            if (GetByName(profile.Name) != null)
                return false;

            lock (_lock)
            {
                profile.CreatedAt = DateTime.Now;
                profile.UpdatedAt = DateTime.Now;
                _profiles.Add(profile);
                Save();
                return true;
            }
        }

        /// <summary>
        /// 更新配置文件
        /// </summary>
        public bool Update(SyncProfile profile)
        {
            if (profile == null || !profile.IsValid())
                return false;

            lock (_lock)
            {
                var existing = GetById(profile.Id);
                if (existing == null)
                    return false;

                // 检查名称是否被其他配置占用
                var duplicate = _profiles.Find(p =>
                    p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase) &&
                    p.Id != profile.Id);
                if (duplicate != null)
                    return false;

                existing.Name = profile.Name;
                existing.Description = profile.Description;
                existing.SourceServer = profile.SourceServer;
                existing.SourceFolder = profile.SourceFolder;
                existing.TargetFolders = new List<string>(profile.TargetFolders);
                existing.Settings = profile.Settings;
                existing.IsEnabled = profile.IsEnabled;
                existing.AutoSync = profile.AutoSync;
                existing.UpdatedAt = DateTime.Now;

                Save();
                return true;
            }
        }

        /// <summary>
        /// 删除配置文件
        /// </summary>
        public bool Remove(string id)
        {
            lock (_lock)
            {
                var profile = GetById(id);
                if (profile == null)
                    return false;

                _profiles.Remove(profile);
                Save();
                return true;
            }
        }

        /// <summary>
        /// 启用/禁用配置文件
        /// </summary>
        public bool SetEnabled(string id, bool enabled)
        {
            lock (_lock)
            {
                var profile = GetById(id);
                if (profile == null)
                    return false;

                profile.IsEnabled = enabled;
                profile.UpdatedAt = DateTime.Now;
                Save();
                return true;
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (System.IO.File.Exists(ProfilesFile))
                    {
                        string json = System.IO.File.ReadAllText(ProfilesFile);
                        _profiles = JsonHelper.Deserialize<List<SyncProfile>>(json) ?? new List<SyncProfile>();
                    }
                    else
                    {
                        // 创建默认配置文件
                        _profiles = CreateDefaultProfiles();
                        Save();
                    }
                }
                catch
                {
                    _profiles = CreateDefaultProfiles();
                }
            }
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonHelper.Serialize(_profiles);
                    System.IO.File.WriteAllText(ProfilesFile, json);
                }
                catch
                {
                    // 忽略保存失败
                }
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private List<SyncProfile> CreateDefaultProfiles()
        {
            return new List<SyncProfile>
            {
                new SyncProfile
                {
                    Name = "完整同步",
                    Description = "同步所有配置（聊天、窗口、UI、快捷键等）",
                    IsEnabled = true,
                    Settings = new SyncSettings
                    {
                        OverrideChatConfig = true,
                        OverridePublicChannelNames = true,
                        OverrideGroupChatTitles = true,
                        OverrideOtherWindowTitles = true,
                        OverrideOverviewTabs = true,
                        OverrideCustomCommands = true,
                        OverrideBookmarkFolders = true,
                        OverrideFittingNames = true
                    }
                },
                new SyncProfile
                {
                    Name = "仅UI设置",
                    Description = "仅同步窗口位置、总览、快捷键等UI设置，不涉及聊天",
                    IsEnabled = false,
                    Settings = new SyncSettings
                    {
                        OverrideChatConfig = false,
                        OverridePublicChannelNames = false,
                        OverrideGroupChatTitles = false,
                        OverrideOtherWindowTitles = true,
                        OverrideOverviewTabs = true,
                        OverrideCustomCommands = true,
                        OverrideBookmarkFolders = true,
                        OverrideFittingNames = false
                    }
                },
                new SyncProfile
                {
                    Name = "仅聊天设置",
                    Description = "仅同步公共频道和群聊设置",
                    IsEnabled = false,
                    Settings = new SyncSettings
                    {
                        OverrideChatConfig = true,
                        OverridePublicChannelNames = true,
                        OverrideGroupChatTitles = true,
                        OverrideOtherWindowTitles = false,
                        OverrideOverviewTabs = false,
                        OverrideCustomCommands = false,
                        OverrideBookmarkFolders = false,
                        OverrideFittingNames = false
                    }
                }
            };
        }
    }
}