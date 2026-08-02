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
        public const string Version = "v5.43";

        /// <summary>
        /// 更新内容（每行用 \n 换行）
        /// </summary>
        public const string ReleaseNotes =
            "   - 优化：MainForm 瘦身，同步逻辑下沉服务层\n" +
            "   - 优化：清理死代码（删除未使用功能）\n" +
            "   - 优化：服务器信息集中管理（新增服务器只需改一处）\n" +
            "   - 优化：弹窗标题栏统一基类，去掉重复代码\n" +
            "   - 优化：新增 32 个单元测试\n" +
            "   - 优化：服务目录命名统一";

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
