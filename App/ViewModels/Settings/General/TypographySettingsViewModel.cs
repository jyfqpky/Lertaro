using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;
using Lertaro.App.Services;
using Lertaro.Core;

namespace Lertaro.App.ViewModels.Settings.General;

public sealed record FontOption(string Value, string Label);
public sealed record FontSizeOption(double Value, string Label);

/// <summary>Per-window font/size pickers for the Appearance page. Applies live (no Apply/staging),
/// matching the theme cards above it -- a font choice is exactly as visual as a theme.</summary>
public class TypographySettingsViewModel : ViewModelBase
{
    private readonly UserSettings _userSettings;
    private readonly PropertyChangedEventHandler _translationHandler;
    private IReadOnlyList<FontOption>? _fontOptions;
    private IReadOnlyList<FontSizeOption>? _sizeOptions;

    public TypographySettingsViewModel(UserSettings userSettings)
    {
        _userSettings = userSettings;
        var profiles = userSettings.Typography;
        QuickSearch = new FontProfileViewModel(profiles.QuickSearch, Apply);
        SearchWindow = new FontProfileViewModel(profiles.SearchWindow, Apply);
        InlineSearch = new FontProfileViewModel(profiles.InlineSearch, Apply);
        Settings = new FontProfileViewModel(profiles.Settings, Apply);
        Default = new FontProfileViewModel(profiles.Default, Apply);

        // Only the "Default" option's label is a translation lookup; family names are system data,
        // so a rebuild is all the refresh a language change needs.
        _translationHandler = (s, e) =>
        {
            _fontOptions = null;
            _sizeOptions = null;
            OnPropertyChanged(nameof(FontOptions));
            OnPropertyChanged(nameof(SizeOptions));
        };
        TranslationManager.Instance.PropertyChanged += _translationHandler;
    }

    public FontProfileViewModel QuickSearch { get; }
    public FontProfileViewModel SearchWindow { get; }
    public FontProfileViewModel InlineSearch { get; }
    public FontProfileViewModel Settings { get; }
    public FontProfileViewModel Default { get; }

    public IReadOnlyList<FontOption> FontOptions => _fontOptions ??= BuildFontOptions();

    public IReadOnlyList<FontSizeOption> SizeOptions => _sizeOptions ??=
    [
        new FontSizeOption(0, TranslationManager.Instance["Appearance_FontDefaultLabel"]),
        .. new[] { 9d, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 28 }
            .Select(size => new FontSizeOption(size, size.ToString("0", CultureInfo.InvariantCulture)))
    ];

    private static IReadOnlyList<FontOption> BuildFontOptions()
    {
        // Current-UI-culture family names when the font carries them ("微软雅黑"), the source
        // otherwise. No symbol-font filtering: which families count as text is the user's call.
        var language = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.Name);
        var options = new List<FontOption> { new("", TranslationManager.Instance["Appearance_FontDefaultLabel"]) };
        options.AddRange(Fonts.SystemFontFamilies
            .Select(family => new FontOption(family.Source,
                family.FamilyNames.TryGetValue(language, out var label) ? label : family.Source))
            .OrderBy(option => option.Label, StringComparer.CurrentCulture));
        return options;
    }

    private void Apply()
    {
        AppTypography.UpdateTypography(_userSettings.Typography);
        _userSettings.Save();
    }

    internal void Cleanup() => TranslationManager.Instance.PropertyChanged -= _translationHandler;
}

/// <summary>One window's family + size pair. Writes straight through to the settings model and
/// applies live; the "Default" option (""/0) falls back to the shared composite UI font.</summary>
public class FontProfileViewModel : ViewModelBase
{
    private readonly FontProfileSettings _profile;
    private readonly Action _apply;

    public FontProfileViewModel(FontProfileSettings profile, Action apply)
    {
        _profile = profile;
        _apply = apply;
    }

    public string SelectedFamilyKey
    {
        get => _profile.Family;
        set
        {
            if (value is null || _profile.Family == value) return;
            _profile.Family = value;
            _apply();
            OnPropertyChanged();
        }
    }

    public double SelectedSize
    {
        get => _profile.Size;
        set
        {
            // ComboBox SelectedValue arithmetic needs a tolerance, not equality.
            if (Math.Abs(_profile.Size - value) < 0.01) return;
            _profile.Size = value;
            _apply();
            OnPropertyChanged();
        }
    }
}
