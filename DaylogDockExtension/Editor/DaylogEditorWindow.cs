using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace DaylogDockExtension;

internal sealed partial class DaylogEditorWindow : Window, IDisposable
{
    private const int DockGap = 6;
    private const int FallbackTopOffset = 28;
    private const int MaxWindowWidth = 540;
    private const int MinWindowWidth = 420;
    private const int MaxWindowHeight = 600;
    private const int MinWindowHeight = 420;
    private const int ShowWindowMinimize = 6;
    private const int ShowWindowRestore = 9;
    private readonly DaylogState _state = new();
    private readonly DispatcherTimer _autosaveTimer = new();
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly TextBox _editor = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _date = new();
    private readonly List<Button> _textButtons = [];
    private readonly List<TextBlock> _softTextBlocks = [];
    private Grid? _root;
    private Grid? _utilityRail;
    private Border? _paperSurface;
    private Border? _calendarPanel;
    private Button? _themeButton;
    private Button? _fontSizeButton;
    private Button? _latoButton;
    private Button? _systemFontButton;
    private Button? _serifButton;
    private Button? _randomFontButton;
    private bool _loading;
    private DateOnly _loadedDate;
    private DateOnly _calendarMonth;
    private bool _initialized;
    private bool _hiddenByDockToggle;
    private bool _iconApplied;
    private bool _isDarkMode;
    private string _selectedFontKey = "Lato";
    private int _editVersion;
    private int _lastSavedVersion;

    public DaylogEditorWindow()
    {
        Title = "Daylog";
        Content = BuildContent();
        Closed += (_, _) => Dispose();

        _autosaveTimer.Interval = TimeSpan.FromMilliseconds(650);
        _autosaveTimer.Tick += async (_, _) => await SaveNowAsync();
    }

    public void Dispose()
    {
        _autosaveTimer.Stop();
        _saveGate.Dispose();
    }

    internal void BringToFront()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, ShowWindowRestore);
            PlaceWindowNearDock();
            ShowWindow(hwnd, ShowWindowRestore);
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }

        _hiddenByDockToggle = false;
        Activate();
        _editor.Focus(FocusState.Programmatic);
    }

    internal void FinishStartupSession()
    {
        _hiddenByDockToggle = true;
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, ShowWindowMinimize);
        }
    }

    internal void ToggleFromDockActivation()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd != IntPtr.Zero && !_hiddenByDockToggle && !IsIconic(hwnd))
        {
            SaveNow();
            _hiddenByDockToggle = true;
            ShowWindow(hwnd, ShowWindowMinimize);
            return;
        }

        _hiddenByDockToggle = false;
        BringToFront();
    }

    private Grid BuildContent()
    {
        var root = new Grid
        {
            Background = new SolidColorBrush(Colors.White),
            Padding = new Thickness(18, 8, 18, 12),
            RowSpacing = 8,
            RequestedTheme = ElementTheme.Light,
        };
        _root = root;
        root.Loaded += (_, _) => InitializeEditor();

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildHeader());

        ConfigureEditor();
        _paperSurface = new Border
        {
            CornerRadius = new CornerRadius(6),
            Child = _editor,
        };
        Grid.SetRow(_paperSurface, 1);
        root.Children.Add(_paperSurface);

        _calendarPanel = BuildCalendarPanel();
        _calendarPanel.Visibility = Visibility.Collapsed;
        Grid.SetRow(_calendarPanel, 1);
        Canvas.SetZIndex(_calendarPanel, 8);
        root.Children.Add(_calendarPanel);

        _utilityRail = BuildUtilityRail();
        Grid.SetRow(_utilityRail, 2);
        root.Children.Add(_utilityRail);

        _status.FontSize = 12;
        _status.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 110, 110, 110));
        _status.Visibility = Visibility.Collapsed;
        Grid.SetRow(_status, 2);
        root.Children.Add(_status);

        return root;
    }

    private Grid BuildHeader()
    {
        var header = new Grid
        {
            ColumnSpacing = 10,
            Margin = new Thickness(0, 0, 0, 2),
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var previous = HeaderButton("<");
        previous.Click += (_, _) => MoveDay(-1);
        Grid.SetColumn(previous, 0);
        header.Children.Add(previous);

        _date.FontFamily = new FontFamily("Georgia");
        _date.FontSize = 14;
        _date.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 28, 28, 28));
        _date.HorizontalAlignment = HorizontalAlignment.Center;
        _date.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_date, 1);
        header.Children.Add(_date);

        var today = HeaderButton("today");
        today.Click += (_, _) => SelectToday();
        Grid.SetColumn(today, 2);
        header.Children.Add(today);

        var next = HeaderButton(">");
        next.Click += (_, _) => MoveDay(1);
        Grid.SetColumn(next, 3);
        header.Children.Add(next);

        var folder = HeaderButton("folder");
        folder.Click += async (_, _) => await ChooseFolderAsync();
        Grid.SetColumn(folder, 4);
        header.Children.Add(folder);

        Grid.SetRow(header, 0);
        return header;
    }

    private Grid BuildUtilityRail()
    {
        var rail = new Grid
        {
            ColumnSpacing = 10,
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(8, 0, 8, 0),
            Opacity = 0.72,
        };
        rail.PointerEntered += (_, _) => rail.Opacity = 1.0;
        rail.PointerExited += (_, _) => rail.Opacity = 0.72;
        rail.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rail.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rail.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var typography = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _fontSizeButton = FooterButton("17px", () => SetFontSize(17));
        typography.Children.Add(_fontSizeButton);
        typography.Children.Add(Separator());
        _latoButton = FooterButton("Lato", () => SetFont("Lato", "Lato, Segoe UI, Consolas"));
        typography.Children.Add(_latoButton);
        typography.Children.Add(Separator());
        _systemFontButton = FooterButton("System", () => SetFont("System", "Segoe UI, Arial"));
        typography.Children.Add(_systemFontButton);
        typography.Children.Add(Separator());
        _serifButton = FooterButton("Serif", () => SetFont("Serif", "Georgia, Cambria, Times New Roman"));
        typography.Children.Add(_serifButton);
        typography.Children.Add(Separator());
        _randomFontButton = FooterButton("Random", SetRandomFont);
        typography.Children.Add(_randomFontButton);
        Grid.SetColumn(typography, 0);
        rail.Children.Add(typography);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _themeButton = FooterButton("Dark Mode", ToggleTheme);
        actions.Children.Add(_themeButton);
        actions.Children.Add(Separator());
        actions.Children.Add(FooterButton("History", ToggleCalendar));
        Grid.SetColumn(actions, 2);
        rail.Children.Add(actions);

        return rail;
    }

    private static Border BuildCalendarPanel()
    {
        return new Border
        {
            Width = 310,
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 8),
            Child = new StackPanel(),
        };
    }

    private void ConfigureEditor()
    {
        _editor.AcceptsReturn = true;
        _editor.IsSpellCheckEnabled = false;
        _editor.IsTextPredictionEnabled = false;
        _editor.TextWrapping = TextWrapping.Wrap;
        _editor.BorderThickness = new Thickness(0);
        _editor.Background = new SolidColorBrush(Colors.White);
        _editor.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 30, 30, 30));
        _editor.FontFamily = new FontFamily("Lato, Segoe UI, Consolas");
        _editor.FontSize = 17;
        _editor.PlaceholderText = "Start with one sentence.";
        _editor.PlaceholderForeground = new SolidColorBrush(ColorHelper.FromArgb(255, 187, 187, 187));
        _editor.MinHeight = 340;
        _editor.IsTabStop = true;
        _editor.VerticalAlignment = VerticalAlignment.Stretch;
        _editor.HorizontalAlignment = HorizontalAlignment.Stretch;
        _editor.VerticalContentAlignment = VerticalAlignment.Top;
        _editor.TextChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }

            _editVersion++;
            _autosaveTimer.Stop();
            _autosaveTimer.Start();
        };
    }

    private Button HeaderButton(string text)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(7, 3, 7, 3),
            MinWidth = 0,
            MinHeight = 0,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 28, 28, 28)),
        };
        _textButtons.Add(button);
        return button;
    }

    private Button FooterButton(string text, Action action)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(2, 0, 2, 0),
            MinWidth = 0,
            MinHeight = 0,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 118, 118, 118)),
            FontSize = 13,
            FontFamily = new FontFamily("Lato, Segoe UI, Arial"),
        };
        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        };
        _textButtons.Add(button);
        return button;
    }

    private TextBlock Separator()
    {
        var separator = new TextBlock
        {
            Text = " \u2022 ",
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 145, 145, 145)),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
        };
        separator.FontFamily = new FontFamily("Lato, Segoe UI, Arial");
        _softTextBlocks.Add(separator);
        return separator;
    }

    private void InitializeEditor()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            PlaceWindowNearDock();
            LoadSelectedDate();
            if (DaylogLaunchOptions.Startup)
            {
                FinishStartupSession();
            }
            else
            {
                BringToFront();
            }
        }
        catch (Exception ex)
        {
            _status.Text = $"startup failed: {ex.Message}";
            BringToFront();
        }
    }

    private void MoveDay(int days)
    {
        SaveNow();
        if (!_state.TryMoveSelectedDate(days, out var message))
        {
            _status.Text = message;
            return;
        }

        LoadSelectedDate();
        _editor.Focus(FocusState.Programmatic);
    }

    private void SelectToday()
    {
        SaveNow();
        _state.SelectToday();
        LoadSelectedDate();
        _editor.Focus(FocusState.Programmatic);
    }

    private void SetFontSize(double size)
    {
        _editor.FontSize = size;
        if (_fontSizeButton is not null)
        {
            _fontSizeButton.Content = $"{size:0}px";
        }

        _editor.Focus(FocusState.Programmatic);
    }

    private void SetFont(string key, string fontFamily)
    {
        _selectedFontKey = key;
        _editor.FontFamily = new FontFamily(fontFamily);
        UpdateFooterSelectionStyles();
        _editor.Focus(FocusState.Programmatic);
    }

    private void SetRandomFont()
    {
        var fonts = new[]
        {
            "Lato, Segoe UI, Consolas",
            "Georgia, Cambria, Times New Roman",
            "Segoe UI, Arial",
            "Consolas, Cascadia Mono, Courier New",
        };
        _selectedFontKey = "Random";
        _editor.FontFamily = new FontFamily(fonts[Random.Shared.Next(fonts.Length)]);
        UpdateFooterSelectionStyles();
        _editor.Focus(FocusState.Programmatic);
    }

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        _state.Settings.SaveColorScheme(_isDarkMode ? "dark" : "light");
        ApplyTheme();
        _editor.Focus(FocusState.Programmatic);
    }

    private void ApplyTheme()
    {
        var chromeBackground = _isDarkMode
            ? ColorHelper.FromArgb(255, 30, 30, 30)
            : Colors.White;
        var chromeForeground = _isDarkMode
            ? ColorHelper.FromArgb(255, 224, 224, 224)
            : ColorHelper.FromArgb(255, 28, 28, 28);
        var chromeSoft = _isDarkMode
            ? ColorHelper.FromArgb(255, 140, 140, 140)
            : ColorHelper.FromArgb(255, 118, 118, 118);
        var chromeSeparator = _isDarkMode
            ? ColorHelper.FromArgb(255, 100, 100, 100)
            : ColorHelper.FromArgb(255, 145, 145, 145);
        var footerBackground = _isDarkMode
            ? chromeBackground
            : Colors.White;
        var surfaceBackground = _isDarkMode ? chromeBackground : Colors.White;
        var surfaceForeground = _isDarkMode
            ? chromeForeground
            : ColorHelper.FromArgb(255, 30, 30, 30);
        var placeholder = _isDarkMode
            ? ColorHelper.FromArgb(255, 100, 100, 100)
            : ColorHelper.FromArgb(255, 187, 187, 187);

        if (_root is not null)
        {
            _root.Background = new SolidColorBrush(chromeBackground);
            _root.RequestedTheme = _isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
        }

        if (_utilityRail is not null)
        {
            _utilityRail.Background = new SolidColorBrush(footerBackground);
            _utilityRail.Padding = new Thickness(8, 0, 8, 0);
        }

        if (_paperSurface is not null)
        {
            _paperSurface.Background = new SolidColorBrush(surfaceBackground);
            _paperSurface.BorderBrush = _isDarkMode
                ? new SolidColorBrush(surfaceBackground)
                : new SolidColorBrush(ColorHelper.FromArgb(255, 230, 230, 230));
            _paperSurface.BorderThickness = _isDarkMode ? new Thickness(0) : new Thickness(0);
            _paperSurface.Padding = new Thickness(12, 10, 12, 10);
        }

        _editor.Background = new SolidColorBrush(surfaceBackground);
        _editor.Foreground = new SolidColorBrush(surfaceForeground);
        _editor.PlaceholderForeground = new SolidColorBrush(placeholder);
        ApplyEditorThemeResources(surfaceBackground, surfaceForeground, placeholder);
        _date.Foreground = new SolidColorBrush(chromeForeground);

        foreach (var button in _textButtons)
        {
            if (ReferenceEquals(button, _themeButton))
            {
                continue;
            }

            button.Foreground = new SolidColorBrush(chromeSoft);
            button.Background = new SolidColorBrush(Colors.Transparent);
            button.FontWeight = FontWeights.Normal;
        }

        foreach (var text in _softTextBlocks)
        {
            text.Foreground = new SolidColorBrush(chromeSeparator);
        }

        if (_themeButton is not null)
        {
            _themeButton.Content = _isDarkMode ? "Light Mode" : "Dark Mode";
            _themeButton.Foreground = new SolidColorBrush(chromeSoft);
            _themeButton.Background = new SolidColorBrush(Colors.Transparent);
            _themeButton.FontWeight = FontWeights.Normal;
            _themeButton.Padding = new Thickness(2, 0, 2, 0);
        }

        UpdateFooterSelectionStyles();
        RenderCalendar();
    }

    private void UpdateFooterSelectionStyles()
    {
        var soft = new SolidColorBrush(_isDarkMode
            ? ColorHelper.FromArgb(255, 140, 140, 140)
            : ColorHelper.FromArgb(255, 118, 118, 118));
        var active = new SolidColorBrush(_isDarkMode
            ? ColorHelper.FromArgb(255, 245, 245, 245)
            : ColorHelper.FromArgb(255, 34, 34, 34));

        SetFooterButtonState(_latoButton, _selectedFontKey == "Lato", soft, active);
        SetFooterButtonState(_systemFontButton, _selectedFontKey == "System", soft, active);
        SetFooterButtonState(_serifButton, _selectedFontKey == "Serif", soft, active);
        SetFooterButtonState(_randomFontButton, _selectedFontKey == "Random", soft, active);
    }

    private static void SetFooterButtonState(Button? button, bool isSelected, Brush soft, Brush active)
    {
        if (button is null)
        {
            return;
        }

        button.Foreground = isSelected ? active : soft;
        button.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
    }

    private void ApplyEditorThemeResources(Color background, Color foreground, Color placeholder)
    {
        var backgroundBrush = new SolidColorBrush(background);
        var foregroundBrush = new SolidColorBrush(foreground);
        var placeholderBrush = new SolidColorBrush(placeholder);
        var transparentBrush = new SolidColorBrush(Colors.Transparent);

        _editor.Resources["TextControlBackground"] = backgroundBrush;
        _editor.Resources["TextControlBackgroundPointerOver"] = backgroundBrush;
        _editor.Resources["TextControlBackgroundFocused"] = backgroundBrush;
        _editor.Resources["TextControlBackgroundDisabled"] = backgroundBrush;
        _editor.Resources["TextControlForeground"] = foregroundBrush;
        _editor.Resources["TextControlForegroundPointerOver"] = foregroundBrush;
        _editor.Resources["TextControlForegroundFocused"] = foregroundBrush;
        _editor.Resources["TextControlPlaceholderForeground"] = placeholderBrush;
        _editor.Resources["TextControlPlaceholderForegroundFocused"] = placeholderBrush;
        _editor.Resources["TextControlBorderBrush"] = transparentBrush;
        _editor.Resources["TextControlBorderBrushPointerOver"] = transparentBrush;
        _editor.Resources["TextControlBorderBrushFocused"] = transparentBrush;
        _editor.Resources["TextControlBorderBrushDisabled"] = transparentBrush;
    }

    private void ToggleCalendar()
    {
        if (_calendarPanel is null)
        {
            return;
        }

        if (_calendarPanel.Visibility == Visibility.Visible)
        {
            _calendarPanel.Visibility = Visibility.Collapsed;
            _editor.Focus(FocusState.Programmatic);
            return;
        }

        _calendarMonth = new DateOnly(_state.SelectedDate.Year, _state.SelectedDate.Month, 1);
        RenderCalendar();
        _calendarPanel.Visibility = Visibility.Visible;
    }

    private void RenderCalendar()
    {
        if (_calendarPanel?.Child is not StackPanel panel)
        {
            return;
        }

        panel.Children.Clear();
        var background = _isDarkMode ? ColorHelper.FromArgb(255, 34, 34, 34) : ColorHelper.FromArgb(255, 250, 249, 246);
        var foreground = _isDarkMode ? ColorHelper.FromArgb(255, 244, 244, 244) : ColorHelper.FromArgb(255, 30, 30, 30);
        var soft = _isDarkMode ? ColorHelper.FromArgb(255, 155, 155, 155) : ColorHelper.FromArgb(255, 112, 112, 112);
        var disabled = _isDarkMode ? ColorHelper.FromArgb(255, 82, 82, 82) : ColorHelper.FromArgb(255, 194, 194, 194);
        var border = _isDarkMode ? ColorHelper.FromArgb(255, 66, 66, 66) : ColorHelper.FromArgb(255, 225, 225, 225);

        _calendarPanel.Background = new SolidColorBrush(background);
        _calendarPanel.BorderBrush = new SolidColorBrush(border);

        var header = new Grid { ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var prev = CalendarNavButton("<", () =>
        {
            _calendarMonth = _calendarMonth.AddMonths(-1);
            RenderCalendar();
        });
        Grid.SetColumn(prev, 0);
        header.Children.Add(prev);

        var title = new TextBlock
        {
            Text = _calendarMonth.ToDateTime(default).ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 14,
            Foreground = new SolidColorBrush(foreground),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var next = CalendarNavButton(">", () =>
        {
            _calendarMonth = _calendarMonth.AddMonths(1);
            RenderCalendar();
        });
        Grid.SetColumn(next, 2);
        header.Children.Add(next);
        panel.Children.Add(header);

        var days = new Grid { ColumnSpacing = 2, RowSpacing = 2 };
        for (var column = 0; column < 7; column++)
        {
            days.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var row = 0; row < 7; row++)
        {
            days.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var labels = new[] { "S", "M", "T", "W", "T", "F", "S" };
        for (var i = 0; i < labels.Length; i++)
        {
            var label = new TextBlock
            {
                Text = labels[i],
                FontSize = 11,
                Foreground = new SolidColorBrush(soft),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4),
            };
            Grid.SetColumn(label, i);
            Grid.SetRow(label, 0);
            days.Children.Add(label);
        }

        var startOffset = (int)_calendarMonth.DayOfWeek;
        var daysInMonth = DateTime.DaysInMonth(_calendarMonth.Year, _calendarMonth.Month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var index = startOffset + day - 1;
            var row = index / 7 + 1;
            var column = index % 7;
            var date = new DateOnly(_calendarMonth.Year, _calendarMonth.Month, day);
            var selectable = _state.IsCalendarSelectable(date);
            var selected = date == _state.SelectedDate;
            var button = new Button
            {
                Content = day.ToString(CultureInfo.InvariantCulture),
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                Height = 26,
                FontSize = 12,
                BorderThickness = selected ? new Thickness(1) : new Thickness(0),
                BorderBrush = new SolidColorBrush(foreground),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(selectable ? foreground : disabled),
                IsEnabled = selectable,
            };
            button.Click += (_, _) => SelectCalendarDate(date);
            Grid.SetColumn(button, column);
            Grid.SetRow(button, row);
            days.Children.Add(button);
        }

        panel.Children.Add(days);
    }

    private Button CalendarNavButton(string text, Action action)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(4, 0, 4, 0),
            MinWidth = 0,
            MinHeight = 0,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            FontSize = 13,
            Foreground = new SolidColorBrush(_isDarkMode
                ? ColorHelper.FromArgb(255, 230, 230, 230)
                : ColorHelper.FromArgb(255, 35, 35, 35)),
        };
        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        };
        return button;
    }

    private void SelectCalendarDate(DateOnly date)
    {
        SaveNow();
        if (!_state.TrySelectDate(date, out var message))
        {
            _status.Text = message;
            return;
        }

        if (_calendarPanel is not null)
        {
            _calendarPanel.Visibility = Visibility.Collapsed;
        }

        LoadSelectedDate();
        _editor.Focus(FocusState.Programmatic);
    }

    private AppWindow? CurrentAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        return AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
    }

    private async Task ChooseFolderAsync()
    {
        SaveNow();

        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                _status.Text = "folder unchanged";
                return;
            }

            _state.Settings.SaveRootFolder(folder.Path);
            _state.SelectToday();
            LoadSelectedDate();
            BringToFront();
        }
        catch (Exception ex)
        {
            _status.Text = $"folder failed: {ex.Message}";
        }
    }

    private void LoadSelectedDate()
    {
        _loading = true;
        _loadedDate = _state.SelectedDate;
        _calendarMonth = new DateOnly(_loadedDate.Year, _loadedDate.Month, 1);
        _date.Text = _loadedDate.ToDateTime(default).ToString("dddd, MMMM d", CultureInfo.CurrentCulture);

        if (!_state.Settings.Current.IsConfigured)
        {
            _editor.Text = string.Empty;
            _status.Text = "choose folder to start";
            _loading = false;
            return;
        }

        _state.Markdown.EnsureDailyFile(_loadedDate);
        _editor.Text = _state.Markdown.ReadDailyText(_loadedDate);
        _editVersion = 0;
        _lastSavedVersion = 0;
        _status.Text = $"saved - {_state.Markdown.FilePathForSelectedDate(_loadedDate)}";
        _loading = false;
        RenderCalendar();
    }

    private void SaveNow()
    {
        _autosaveTimer.Stop();
        try
        {
            if (!_state.Settings.Current.IsConfigured)
            {
                _status.Text = "choose folder first";
                return;
            }

            _saveGate.Wait();
            try
            {
                _state.Markdown.SaveDailyText(_loadedDate, _editor.Text);
                _lastSavedVersion = _editVersion;
            }
            finally
            {
                _saveGate.Release();
            }

            _status.Text = $"saved {DateTime.Now:h:mm tt}";
        }
        catch (Exception ex)
        {
            _status.Text = $"save failed: {ex.Message}";
        }
    }

    private async Task SaveNowAsync()
    {
        _autosaveTimer.Stop();
        var version = _editVersion;
        var date = _loadedDate;
        var text = _editor.Text;

        try
        {
            if (!_state.Settings.Current.IsConfigured)
            {
                _status.Text = "choose folder first";
                return;
            }

            if (version == _lastSavedVersion)
            {
                _status.Text = $"saved {DateTime.Now:h:mm tt}";
                return;
            }

            await _saveGate.WaitAsync();
            try
            {
                await _state.Markdown.SaveDailyTextAsync(date, text);
                if (date == _loadedDate && version >= _lastSavedVersion)
                {
                    _lastSavedVersion = version;
                }
            }
            finally
            {
                _saveGate.Release();
            }

            if (date == _loadedDate && version == _editVersion)
            {
                _status.Text = $"saved {DateTime.Now:h:mm tt}";
            }
        }
        catch (Exception ex)
        {
            _status.Text = $"save failed: {ex.Message}";
        }
        finally
        {
            if (date == _loadedDate && version != _editVersion)
            {
                _autosaveTimer.Stop();
                _autosaveTimer.Start();
            }
        }
    }

    private void PlaceWindowNearDock()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        ApplyWindowIcon(appWindow);
        var workArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary).WorkArea;
        var width = Math.Min(MaxWindowWidth, Math.Max(MinWindowWidth, workArea.Width - 24));
        var height = Math.Min(MaxWindowHeight, Math.Max(MinWindowHeight, workArea.Height - 56));
        var anchor = TryFindDockAnchor();
        var x = anchor is null
            ? workArea.X + (workArea.Width - width) / 2
            : anchor.Value.X - width / 2;
        var y = anchor is null
            ? workArea.Y + FallbackTopOffset
            : anchor.Value.Y;

        x = Math.Max(workArea.X + 4, Math.Min(x, workArea.X + workArea.Width - width - 4));
        y = Math.Max(workArea.Y + 4, Math.Min(y, workArea.Y + workArea.Height - height - 4));
        appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void ApplyWindowIcon(AppWindow appWindow)
    {
        if (_iconApplied)
        {
            return;
        }

        var baseDir = AppContext.BaseDirectory;
        string? iconPath = null;
        foreach (var relative in new[] { "Daylog.ico", Path.Combine("Assets", "Daylog.ico") })
        {
            var candidate = Path.Combine(baseDir, relative);
            if (File.Exists(candidate))
            {
                iconPath = candidate;
                break;
            }
        }

        if (iconPath is null)
        {
            return;
        }

        appWindow.SetIcon(iconPath);
        _iconApplied = true;
    }

    private static PointInt32? TryFindDockAnchor()
    {
        var powerDock = FindWindow(null, "PowerDock");
        if (powerDock == IntPtr.Zero)
        {
            powerDock = FindPowerDockByShape();
        }

        if (powerDock != IntPtr.Zero && GetWindowRect(powerDock, out var dockRect))
        {
            var dockCenterX = dockRect.Left + (dockRect.Right - dockRect.Left) / 2;
            return new PointInt32(dockCenterX, dockRect.Bottom + DockGap);
        }

        return null;
    }

    private static IntPtr FindPowerDockByShape()
    {
        var best = IntPtr.Zero;
        var bestWidth = 0;

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || !GetWindowRect(hwnd, out var rect))
            {
                return true;
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width < 320 || height is < 16 or > 80 || rect.Top > 8)
            {
                return true;
            }

            var className = GetWindowClassName(hwnd);
            if (!string.Equals(className, "WinUIDesktopWin32WindowClass", StringComparison.Ordinal))
            {
                return true;
            }

            if (width > bestWidth)
            {
                best = hwnd;
                bestWidth = width;
            }

            return true;
        }, IntPtr.Zero);

        return best;
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        const int maxClassNameLength = 256;
        var buffer = new char[maxClassNameLength];
        var length = GetClassName(hwnd, buffer, buffer.Length);
        return length <= 0 ? string.Empty : new string(buffer, 0, length);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);
}
