using System;
using System.Windows;
using System.Windows.Controls;
using KanbanBoard.Models;
using KanbanBoard.Services;

namespace KanbanBoard.Controls;

public partial class SettingsWindow : Window
{
    public event Action? Saved;

    private readonly AppSettings _s;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _s = settings;
        HeaderText.Text = L10n.T("settings.title");
        Title = L10n.T("settings.title");
        PopulateCombos();
        LoadSettings();
    }

    public static void Open()
    {
        var w = new SettingsWindow(AppServices.Settings.Current);
        w.Saved += () => AppServices.Settings.Save();
        w.ShowDialog();
    }

    private void PopulateCombos()
    {
        AddCombo(ThemeCombo, ("Dark", L10n.T("settings.theme.dark")), ("Light", L10n.T("settings.theme.light")), ("System", L10n.T("settings.theme.system")));
        AddCombo(MaterialCombo, ("None", "None"), ("Transparent", "Transparent"), ("Acrylic", "Acrylic"), ("Mica", "Mica"));
        AddCombo(LanguageCombo, ("zh-CN", "简体中文"), ("zh-TW", "繁體中文"), ("en", "English"));
        AddCombo(FontScaleCombo, ("0.8", "80%"), ("0.9", "90%"), ("1.0", "100%"), ("1.1", "110%"), ("1.2", "120%"), ("1.4", "140%"), ("1.6", "160%"));
    }

    private static void AddCombo(ComboBox combo, params (string Tag, string Text)[] items)
    {
        foreach (var (tag, text) in items)
            combo.Items.Add(new ComboBoxItem { Content = text, Tag = tag });
    }

    private static int IndexOfTag(ComboBox combo, string tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
            if (combo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tag) return i;
        return 0;
    }

    private void LoadSettings()
    {
        ThemeCombo.SelectedIndex = IndexOfTag(ThemeCombo, _s.Theme);
        MaterialCombo.SelectedIndex = IndexOfTag(MaterialCombo, _s.Material);
        LanguageCombo.SelectedIndex = IndexOfTag(LanguageCombo, _s.Language);
        FontScaleCombo.SelectedIndex = IndexOfTag(FontScaleCombo, _s.FontScale.ToString("0.0"));
        OpacitySlider.Value = _s.Opacity;
        ExpandDelayBox.Text = _s.HoverExpandDelayMs.ToString();
        CollapseDelayBox.Text = _s.HoverCollapseDelayMs.ToString();
        QuickAddHotkeyBox.Text = _s.QuickAddHotkey;
        ToggleHotkeyBox.Text = _s.ToggleHotkey;
        CommandPaletteHotkeyBox.Text = _s.CommandPaletteHotkey;
        AutoStartCheck.IsChecked = _s.AutoStart;
        AutoStartDelayCheck.IsChecked = _s.AutoStartDelayed;
        AutoStartDelayBox.Text = _s.AutoStartDelaySeconds.ToString();
        BackupDaysBox.Text = _s.BackupDays.ToString();
        AutoBackupCheck.IsChecked = _s.AutoBackup;
        ReportCheck.IsChecked = _s.AnonymousReporting;
        Opacity_Changed(this, new RoutedPropertyChangedEventArgs<double>(0, _s.Opacity));
    }

    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        OpacityText.Text = $"{(int)(OpacitySlider.Value * 100)}%";

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _s.Theme = TagOf(ThemeCombo, "Dark");
        _s.Material = TagOf(MaterialCombo, "Acrylic");
        _s.Language = TagOf(LanguageCombo, "zh-CN");
        if (double.TryParse(TagOf(FontScaleCombo, "1.0"), out var fs)) _s.FontScale = fs;
        _s.Opacity = OpacitySlider.Value;
        _s.HoverExpandDelayMs = ParseInt(ExpandDelayBox.Text, _s.HoverExpandDelayMs);
        _s.HoverCollapseDelayMs = ParseInt(CollapseDelayBox.Text, _s.HoverCollapseDelayMs);
        _s.QuickAddHotkey = QuickAddHotkeyBox.Text.Trim();
        _s.ToggleHotkey = ToggleHotkeyBox.Text.Trim();
        _s.CommandPaletteHotkey = CommandPaletteHotkeyBox.Text.Trim();
        _s.AutoStart = AutoStartCheck.IsChecked == true;
        _s.AutoStartDelayed = AutoStartDelayCheck.IsChecked == true;
        _s.AutoStartDelaySeconds = ParseInt(AutoStartDelayBox.Text, _s.AutoStartDelaySeconds);
        _s.BackupDays = Math.Clamp(ParseInt(BackupDaysBox.Text, _s.BackupDays), 7, 30);
        _s.AutoBackup = AutoBackupCheck.IsChecked == true;
        _s.AnonymousReporting = ReportCheck.IsChecked == true;

        Saved?.Invoke();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private static string TagOf(ComboBox combo, string fallback) =>
        combo.SelectedItem is ComboBoxItem item ? item.Tag?.ToString() ?? fallback : fallback;

    private static int ParseInt(string s, int fallback) =>
        int.TryParse(s?.Trim(), out var v) ? v : fallback;
}
