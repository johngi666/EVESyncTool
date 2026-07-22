using EVESyncTool.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EVESyncTool.Core.Config
{
    /// <summary>
    /// 统一配置管理（所有配置汇总到一个文件）
    /// </summary>
    public class ConfigManager
    {
        private const string ConfigFile = "evesync_config.json";
        private AppConfig _config;

        public AppConfig Config => _config;

        public ConfigManager()
        {
            Load();
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string json = File.ReadAllText(ConfigFile);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

                    // 如果 CharacterNames 有数据，同步到 CharacterCacheManager
                    if (_config.CharacterNames != null && _config.CharacterNames.Count > 0)
                    {
                        CharacterCacheManager.LoadFromDictionary(_config.CharacterNames);
                    }
                }
                else
                {
                    _config = new AppConfig();
                }
            }
            catch (Exception)
            {
                _config = new AppConfig();
            }
        }

        /// <summary>
        /// 保存配置（统一写入一个文件）
        /// </summary>
        public void Save()
        {
            try
            {
                // 从 CharacterCacheManager 获取最新角色名
                var charNames = CharacterCacheManager.GetAllCachedNames();
                _config.CharacterNames = charNames ?? new Dictionary<string, string>();

                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 获取缓存的路径
        /// </summary>
        public string GetCachedPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(_config.CachedPath) && Directory.Exists(_config.CachedPath))
                    return _config.CachedPath;
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// 保存路径缓存
        /// </summary>
        public void SaveCachedPath(string folder)
        {
            _config.CachedPath = folder;
            Save();
        }

        /// <summary>
        /// 获取最后使用的服务器
        /// </summary>
        public string GetLastServer()
        {
            return _config.LastServer ?? "曙光服 (Infinity)";
        }

        /// <summary>
        /// 保存最后使用的服务器
        /// </summary>
        public void SaveLastServer(string server)
        {
            _config.LastServer = server;
            Save();
        }

        /// <summary>
        /// 获取同步设置
        /// </summary>
        public SyncSettings GetSyncSettings()
        {
            return _config.SyncSettings ?? new SyncSettings();
        }

        /// <summary>
        /// 保存同步设置
        /// </summary>
        public void SaveSyncSettings(SyncSettings settings)
        {
            _config.SyncSettings = settings;
            Save();
        }

        /// <summary>
        /// 获取所有用户备注
        /// </summary>
        public Dictionary<string, string> GetUserRemarks()
        {
            return _config.UserRemarks ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// 保存单个用户备注
        /// </summary>
        public void SaveUserRemark(string userId, string remark)
        {
            if (_config.UserRemarks == null)
                _config.UserRemarks = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(remark))
            {
                // 如果备注为空，删除该条目
                if (_config.UserRemarks.ContainsKey(userId))
                    _config.UserRemarks.Remove(userId);
            }
            else
            {
                _config.UserRemarks[userId] = remark;
            }
            Save();
        }

        /// <summary>
        /// 获取用户备注显示名（有备注显示备注，无备注显示数字ID）
        /// </summary>
        public string GetUserDisplayName(string userId)
        {
            var remarks = GetUserRemarks();
            if (remarks != null && remarks.TryGetValue(userId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                return remark;
            return userId;
        }

        /// <summary>
        /// 获取用户备注显示名（带原ID后缀，用于同步对话框）
        /// </summary>
        public string GetUserDisplayNameWithId(string userId)
        {
            var remarks = GetUserRemarks();
            if (remarks != null && remarks.TryGetValue(userId, out string remark) && !string.IsNullOrWhiteSpace(remark))
                return $"{remark} ({userId})";
            return userId;
        }
    }

    /// <summary>
    /// 应用配置数据模型
    /// </summary>
    public class AppConfig
    {
        public string LastServer { get; set; } = "曙光服 (Infinity)";
        public string CachedPath { get; set; }
        public Dictionary<string, string> CharacterNames { get; set; } = new Dictionary<string, string>();
        public List<ConfigScheme> ConfigSchemes { get; set; } = new List<ConfigScheme>();
        public SyncSettings SyncSettings { get; set; } = new SyncSettings();

        /// <summary>
        /// 用户备注字典 Key: 用户数字ID, Value: 备注文字
        /// </summary>
        public Dictionary<string, string> UserRemarks { get; set; } = new Dictionary<string, string>();
    }
}