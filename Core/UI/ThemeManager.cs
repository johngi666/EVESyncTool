using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVESyncTool.Core.UI
{
    /// <summary>
    /// 明暗主题管理器
    /// </summary>
    public static class ThemeManager
    {
        public static bool IsDarkMode { get; private set; } = false;

        // 亮色主题
        public static readonly Color Light_Bg = Color.FromArgb(240, 248, 255);
        public static readonly Color Light_Panel = Color.White;
        public static readonly Color Light_TitleBar = Color.FromArgb(70, 130, 180);
        public static readonly Color Light_Text = Color.Black;
        public static readonly Color Light_Accent = Color.FromArgb(70, 130, 180);
        public static readonly Color Light_Separator = Color.FromArgb(200, 200, 200);
        public static readonly Color Light_GridBg = Color.White;
        public static readonly Color Light_GridHeader = Color.FromArgb(240, 248, 255);
        public static readonly Color Light_SelectionBg = Color.FromArgb(70, 130, 180);
        public static readonly Color Light_SelectionFg = Color.White;
        public static readonly Color Light_TitleBtn = Color.White;

        // 暗色主题
        public static readonly Color Dark_Bg = Color.FromArgb(30, 30, 30);
        public static readonly Color Dark_Panel = Color.FromArgb(45, 45, 48);
        public static readonly Color Dark_TitleBar = Color.FromArgb(37, 37, 40);
        public static readonly Color Dark_Text = Color.FromArgb(220, 220, 220);
        public static readonly Color Dark_Accent = Color.FromArgb(100, 160, 210);
        public static readonly Color Dark_Separator = Color.FromArgb(70, 70, 74);
        public static readonly Color Dark_GridBg = Color.FromArgb(37, 37, 40);
        public static readonly Color Dark_GridHeader = Color.FromArgb(55, 55, 60);
        public static readonly Color Dark_SelectionBg = Color.FromArgb(62, 95, 130);
        public static readonly Color Dark_SelectionFg = Color.FromArgb(220, 220, 220);
        public static readonly Color Dark_TitleBtn = Color.FromArgb(200, 200, 200);

        public static Color Bg => IsDarkMode ? Dark_Bg : Light_Bg;
        public static Color Panel => IsDarkMode ? Dark_Panel : Light_Panel;
        public static Color TitleBar => IsDarkMode ? Dark_TitleBar : Light_TitleBar;
        public static Color Text => IsDarkMode ? Dark_Text : Light_Text;
        public static Color Accent => IsDarkMode ? Dark_Accent : Light_Accent;
        public static Color Separator => IsDarkMode ? Dark_Separator : Light_Separator;
        public static Color GridBg => IsDarkMode ? Dark_GridBg : Light_GridBg;
        public static Color GridHeader => IsDarkMode ? Dark_GridHeader : Light_GridHeader;
        public static Color SelectionBg => IsDarkMode ? Dark_SelectionBg : Light_SelectionBg;
        public static Color SelectionFg => IsDarkMode ? Dark_SelectionFg : Light_SelectionFg;
        public static Color TitleBtn => IsDarkMode ? Dark_TitleBtn : Light_TitleBtn;

        public static event Action<bool> ThemeChanged;

        public static void SetDarkMode(bool dark)
        {
            if (IsDarkMode == dark) return;
            IsDarkMode = dark;
            ThemeChanged?.Invoke(dark);
        }

        public static void Toggle()
        {
            SetDarkMode(!IsDarkMode);
        }
    }
}
