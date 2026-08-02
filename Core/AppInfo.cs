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
        public const string Version = "v5.42";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 所有弹窗适配暗色模式\n" +
            "   - 滚动条、进度条暗色配置\n" +
            "   - 服务器下拉框暗色适配\n" +
            "   - 修复暗色↔亮色双向切换恢复\n" +
            "   - 启动时自动启用上次的主题模式";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年8月2日";

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
