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
        public const string Version = "v5.44";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 自动更新改为运行期间持续检测（每分钟一次）\n" +
            "   - 清理重复 AppConfig 与死代码链\n" +
            "   - ConfigScheme 移入 Core.Config 统一管理\n" +
            "   - DataGridViewHandler 归入 Services.Grid\n" +
            "   - 版本检查失败时记录到操作日志";

        /// <summary>
        /// 更新日期
        /// </summary>
        public const string ReleaseDate = "2026年8月3日";

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
