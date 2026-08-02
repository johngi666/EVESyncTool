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
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 新增暗色模式支持（标题栏 🌙 切换）\n" +
            "   - 备份路径支持自定义配置\n" +
            "   - 优化磁盘扫描速度（注册表+Steam路径）\n" +
            "   - FFI 加载保护（DLL缺失不再崩溃）\n" +
            "   - ESI 角色名查询加超时重试\n" +
            "   - 自动更新检查（启动时后台检测）\n" +
            "   - 配置保存防抖（减少磁盘写入）";

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
