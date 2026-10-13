using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using Lertaro.App.Views.InlineSearchWindow;
using Lertaro.App.Views.QuickSearchWindow;
using Lertaro.App.Views.SearchWindow;
using Lertaro.App.Views.Settings;
using Lertaro.Core;
using FontFamily = System.Windows.Media.FontFamily;
using ContextMenu = System.Windows.Controls.ContextMenu;
using ToolTip = System.Windows.Controls.ToolTip;

namespace Lertaro.App.Services;

internal static class AppTypography
{
    internal const string UiFontKey = "AppUiFontFamily";
    internal const string MonospaceFontKey = "AppMonospaceFontFamily";
    internal const string IconFontKey = "AppIconFontFamily";
    internal const string LanguageKey = "AppUiLanguage";
    internal const string UiFontWeightKey = "AppUiFontWeight";

    // WPF's built-in composite selects Windows fonts by script AND Language, including regional
    // CJK glyphs. Keep that maintained fallback table instead of ordering CJK families ourselves.
    internal static FontFamily UiFont { get; } = new("Segoe UI, Global User Interface");
    internal static FontFamily MonospaceFont { get; } = new("Consolas, Global User Interface");
    // Prefer Windows 11 icons; WPF falls back to the Windows 10 font when unavailable.
    internal static FontFamily IconFont { get; } = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    private static bool _registered;

    // Per-window user font profiles (Appearance settings page). Null until Initialize runs.
    private static TypographySettings? _typography;

    // Marks roots where a profile wrote LOCAL FontFamily/FontSize values. Switching a profile back
    // to default needs to know those local values are ours to remove -- OnRootLoaded's
    // plugin-protective "leave local values alone" rule can't tell ours from a plugin's.
    private static readonly DependencyProperty FamilyWrittenProperty = DependencyProperty.RegisterAttached(
        "FamilyWritten", typeof(bool), typeof(AppTypography), new PropertyMetadata(false));
    private static readonly DependencyProperty SizeWrittenProperty = DependencyProperty.RegisterAttached(
        "SizeWritten", typeof(bool), typeof(AppTypography), new PropertyMetadata(false));

    internal static void Initialize(ResourceDictionary resources, string? culture, TypographySettings? typography = null)
    {
        _typography = typography;
        resources[UiFontKey] = UiFont;
        resources[MonospaceFontKey] = MonospaceFont;
        resources[IconFontKey] = IconFont;
        UpdateLanguage(resources, culture);
        if (_registered) return;
        _registered = true;

        // Popups have their own visual roots; covering Window alone misses menus/tooltips and
        // plugin-created popups. A root resource reference also updates already-open windows.
        foreach (var type in new[] { typeof(Window), typeof(Popup), typeof(ContextMenu), typeof(ToolTip) })
            EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRootLoaded));
    }

    // Re-applies every profile to all open windows. Menus/tooltips popups pick the new values up on
    // their next open, so refreshing the windows themselves is enough. Called by the Appearance
    // page after each per-window font change.
    internal static void UpdateTypography(TypographySettings typography)
    {
        _typography = typography;
        if (System.Windows.Application.Current is null) return;
        foreach (Window window in System.Windows.Application.Current.Windows)
            ApplyProfile(window);
    }

    internal static void UpdateLanguage(ResourceDictionary resources, string? culture)
    {
        var language = ResolveLanguage(culture);
        resources[LanguageKey] = language;
        // Windows' message font can be lighter than 400 (305 on the affected system).
        // Regular and Normal both parse as 400, so anchor the Chinese UI's inherited weight explicitly.
        var isChinese = language.IetfLanguageTag == "zh" || language.IetfLanguageTag.StartsWith("zh-", StringComparison.Ordinal);
        resources[UiFontWeightKey] = isChinese ? FontWeights.Normal : System.Windows.SystemFonts.MessageFontWeight;
    }

    internal static XmlLanguage ResolveLanguage(string? culture)
    {
        try
        {
            var name = CultureInfo.GetCultureInfo(culture ?? "").Name;
            return XmlLanguage.GetLanguage(name.Length == 0 ? "en-US" : name);
        }
        catch (ArgumentException)
        {
            return XmlLanguage.GetLanguage("en-US");
        }
    }

    private static void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        var root = (FrameworkElement)sender;
        if (!ReferenceEquals(root, e.OriginalSource)) return;
        // Preserve an explicit content language or font supplied by a plugin. Descendant icon and
        // code fonts likewise take precedence over the inherited UI font.
        if (root.ReadLocalValue(TextElement.FontFamilyProperty) == DependencyProperty.UnsetValue)
            root.SetResourceReference(TextElement.FontFamilyProperty, UiFontKey);
        if (root.ReadLocalValue(FrameworkElement.LanguageProperty) == DependencyProperty.UnsetValue)
            root.SetResourceReference(FrameworkElement.LanguageProperty, LanguageKey);
        var weightSource = DependencyPropertyHelper.GetValueSource(root, TextElement.FontWeightProperty).BaseValueSource;
        if (weightSource is BaseValueSource.Default or BaseValueSource.Inherited
            or BaseValueSource.DefaultStyle or BaseValueSource.DefaultStyleTrigger)
            root.SetResourceReference(TextElement.FontWeightProperty, UiFontWeightKey);
        ApplyProfile(root);
    }

    // Menus, tooltips and plugin popups are separate visual roots that never inherit from the
    // window they belong to, so they resolve their host window themselves to get its profile.
    // ponytail: a ContextMenu/ToolTip whose PlacementTarget is still null when it loads falls
    // back to the Default profile; switching those to per-window tracking means hooking Opened
    // per instance, which this never needed.
    private static Window? HostOf(FrameworkElement root) => root switch
    {
        Window window => window,
        Popup { PlacementTarget: { } target } => Window.GetWindow(target),
        ContextMenu { PlacementTarget: { } menuTarget } => Window.GetWindow(menuTarget),
        ToolTip { PlacementTarget: { } tipTarget } => Window.GetWindow(tipTarget),
        _ => null
    };

    private static FontProfileSettings? ProfileFor(FrameworkElement root)
    {
        if (_typography is null || HostOf(root) is not { } host) return null;
        return host switch
        {
            QuickSearchWindow => _typography.QuickSearch,
            SearchWindow => _typography.SearchWindow,
            InlineSearchWindow => _typography.InlineSearch,
            SettingsWindow => _typography.Settings,
            _ => _typography.Default
        };
    }

    private static void ApplyProfile(FrameworkElement root)
    {
        var profile = ProfileFor(root);
        if (profile is { Family.Length: > 0 })
        {
            root.SetValue(TextElement.FontFamilyProperty, new FontFamily(profile.Family));
            root.SetValue(FamilyWrittenProperty, true);
        }
        else if ((bool)root.GetValue(FamilyWrittenProperty))
        {
            // Restore the shared per-language fallback reference the profile value replaced.
            root.SetResourceReference(TextElement.FontFamilyProperty, UiFontKey);
            root.SetValue(FamilyWrittenProperty, false);
        }
        if (profile is { Size: > 0 })
        {
            root.SetValue(TextElement.FontSizeProperty, profile.Size);
            root.SetValue(SizeWrittenProperty, true);
        }
        else if ((bool)root.GetValue(SizeWrittenProperty))
        {
            // UnsetValue clears the local value (InvalidateProperty would keep it); back to WPF's
            // inherited system-default size.
            // ponytail: also clears a local FontSize a plugin root set itself; per-window sizes and
            // plugin-set root sizes never legitimately coexist, so the collision is acceptable.
            root.SetValue(TextElement.FontSizeProperty, DependencyProperty.UnsetValue);
            root.SetValue(SizeWrittenProperty, false);
        }
    }
}
