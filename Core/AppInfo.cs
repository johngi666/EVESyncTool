namespace EVESyncTool.Core
{
    /// <summary>
    /// 应用版本信息和更新配置
    /// </summary>
    public static class AppInfo
    {
        /// <summary>
        /// 当前版本号（发布时修改此处即可）
        /// </summary>
        public const string Version = "v5.4";

        /// <summary>
        /// 远端版本检查 URL（GitHub 仓库根目录下的 version.json 原始文件）
        /// </summary>
        public const string UpdateCheckUrl =
            "https://raw.githubusercontent.com/johngi666/EVESyncTool/main/version.json";

        /// <summary>
        /// 发布页面 URL
        /// </summary>
        public const string ReleasesUrl =
            "https://github.com/johngi666/EVESyncTool/releases/latest";
    }
}
