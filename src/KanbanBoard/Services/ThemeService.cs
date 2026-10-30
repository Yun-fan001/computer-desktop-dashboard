using System;
using System.Windows;
using Microsoft.Win32;

namespace KanbanBoard.Services;

/// <summary>主题切换：动态合并 Light/Dark 资源字典。</summary>
public static class ThemeService
{
    public static void Apply(string theme)
    {
        string source = theme switch
        {
            "Light" => "Resources/Themes/Light.xaml",
            "System" => IsSystemDark() ? "Resources/Themes/Dark.xaml" : "Resources/Themes/Light.xaml",
            _ => "Resources/Themes/Dark.xaml",
        };

        var dict = new ResourceDictionary { Source = new Uri(source, UriKind.RelativeOrAbsolute) };
        var merged = Application.Current.Resources.MergedDictionaries;

        // 移除旧主题字典，保留 BaseStyles。
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            var src = merged[i].Source?.OriginalString ?? "";
            if (src.Contains("Light.xaml") || src.Contains("Dark.xaml"))
                merged.RemoveAt(i);
        }
        merged.Add(dict);
    }

    private static bool IsSystemDark()
    {
        try
        {
            var v = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);
            return v is int i && i == 0;
        }
        catch { return false; }
    }
}
