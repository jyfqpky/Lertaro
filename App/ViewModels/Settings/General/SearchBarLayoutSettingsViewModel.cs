using System.ComponentModel;
using System.Windows.Input;
using Lertaro.App.Helpers;
using Lertaro.App.Services;
using Lertaro.Core;

namespace Lertaro.App.ViewModels.Settings.General;

// The quick window's search bar layout. Save() commits _userSettings.SearchWindow.
public class SearchBarLayoutSettingsViewModel : ViewModelBase
{
    private readonly UserSettings _userSettings;
    private double _searchBarWidth;
    private double _searchBarHeight;
    private bool _showClock;
    private bool _reopenAsFullWindowOnRepeatHotkey;
    private bool _lockPosition;
    private string _popupPositionMode;
    private double _customHorizontalPercent;
    private double _customVerticalPercent;
    // Reset() clears the quick window's remembered screen position -- there's no bound field for it
    // (the window itself owns Left/Top), so this just stages the intent for Save() to commit.
    private bool _resetPosition;
    private readonly PropertyChangedEventHandler _translationHandler;

    public SearchBarLayoutSettingsViewModel(UserSettings userSettings)
    {
        _userSettings = userSettings;
        _searchBarWidth = userSettings.SearchWindow.SearchBarWidth;
        _searchBarHeight = userSettings.SearchWindow.SearchBarHeight;
        _showClock = userSettings.SearchWindow.ShowClock;
        _reopenAsFullWindowOnRepeatHotkey = userSettings.SearchWindow.ReopenAsFullWindowOnRepeatHotkey;
        _lockPosition = userSettings.SearchWindow.LockPosition;
        _popupPositionMode = userSettings.SearchWindow.NormalizedPopupPositionMode;
        _customHorizontalPercent = userSettings.SearchWindow.CustomRelativeLeft * 100.0;
        _customVerticalPercent = userSettings.SearchWindow.CustomRelativeTop * 100.0;

        // Stable-identity, mutate-in-place options -- see LabeledOption's own doc comment. The
        // SelectedValue binding matches by Value string, so no re-match pass is needed.
        PopupPositionOptions =
        [
            new LabeledOption("Remember", TranslationManager.Instance["General_PopupPositionRemember"]),
            new LabeledOption("Center", TranslationManager.Instance["General_PopupPositionCenter"]),
            new LabeledOption("FollowMouse", TranslationManager.Instance["General_PopupPositionFollowMouse"]),
            new LabeledOption("Custom", TranslationManager.Instance["General_PopupPositionCustom"])
        ];
        _translationHandler = (s, e) =>
        {
            PopupPositionOptions[0].Label = TranslationManager.Instance["General_PopupPositionRemember"];
            PopupPositionOptions[1].Label = TranslationManager.Instance["General_PopupPositionCenter"];
            PopupPositionOptions[2].Label = TranslationManager.Instance["General_PopupPositionFollowMouse"];
            PopupPositionOptions[3].Label = TranslationManager.Instance["General_PopupPositionCustom"];
        };
        TranslationManager.Instance.PropertyChanged += _translationHandler;
    }

    internal void Cleanup() => TranslationManager.Instance.PropertyChanged -= _translationHandler;

    public double SearchBarWidth
    {
        get => _searchBarWidth;
        set
        {
            if (value < 300.0 || value > 1200.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Width must be between 300 and 1200.");
            }
            SetProperty(ref _searchBarWidth, value);
        }
    }

    public double SearchBarHeight
    {
        get => _searchBarHeight;
        set
        {
            if (value < 45.0 || value > 120.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Height must be between 45 and 120.");
            }
            SetProperty(ref _searchBarHeight, value);
        }
    }

    public bool ShowClock
    {
        get => _showClock;
        set => SetProperty(ref _showClock, value);
    }

    public bool ReopenAsFullWindowOnRepeatHotkey
    {
        get => _reopenAsFullWindowOnRepeatHotkey;
        set => SetProperty(ref _reopenAsFullWindowOnRepeatHotkey, value);
    }

    public bool LockPosition
    {
        get => _lockPosition;
        set => SetProperty(ref _lockPosition, value);
    }

    public IReadOnlyList<LabeledOption> PopupPositionOptions { get; }

    public string PopupPositionMode
    {
        get => _popupPositionMode;
        set
        {
            if (SetProperty(ref _popupPositionMode, value))
                OnPropertyChanged(nameof(IsCustomPopupPosition));
        }
    }

    public bool IsCustomPopupPosition => _popupPositionMode == "Custom";

    // Percent-of-work-area, shown as 0-100 in the UI; Save() stores them back as fractions.
    public double CustomHorizontalPercent
    {
        get => _customHorizontalPercent;
        set
        {
            if (value < 0.0 || value > 100.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Horizontal position must be between 0 and 100.");
            }
            SetProperty(ref _customHorizontalPercent, value);
        }
    }

    public double CustomVerticalPercent
    {
        get => _customVerticalPercent;
        set
        {
            if (value < 0.0 || value > 100.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Vertical position must be between 0 and 100.");
            }
            SetProperty(ref _customVerticalPercent, value);
        }
    }

    public ICommand ResetCommand => new RelayCommand(Reset);

    private void Reset()
    {
        SearchBarWidth = 570;
        SearchBarHeight = 60;
        ShowClock = false;
        ReopenAsFullWindowOnRepeatHotkey = false;
        // Unlocked as well as re-centred: Reset exists to undo a layout you no longer want, and leaving
        // the lock on would hand back a window that cannot be moved off wherever it lands.
        LockPosition = false;
        PopupPositionMode = "Remember";
        CustomHorizontalPercent = 50;
        CustomVerticalPercent = 22;
        _resetPosition = true;
    }

    public void Save()
    {
        _userSettings.SearchWindow.SearchBarWidth = _searchBarWidth;
        _userSettings.SearchWindow.SearchBarHeight = _searchBarHeight;
        _userSettings.SearchWindow.ShowClock = _showClock;
        _userSettings.SearchWindow.ReopenAsFullWindowOnRepeatHotkey = _reopenAsFullWindowOnRepeatHotkey;
        _userSettings.SearchWindow.LockPosition = _lockPosition;
        _userSettings.SearchWindow.PopupPositionMode = _popupPositionMode;
        _userSettings.SearchWindow.CustomRelativeLeft = _customHorizontalPercent / 100.0;
        _userSettings.SearchWindow.CustomRelativeTop = _customVerticalPercent / 100.0;
        if (_resetPosition)
        {
            _userSettings.SearchWindow.RelativeLeft = null;
            _userSettings.SearchWindow.RelativeTop = null;
            _resetPosition = false;
        }
        UiMetrics.ApplyScaleFromSettings();
    }
}
