using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using OpenCodeStudio.Services;

namespace OpenCodeStudio
{
    /// <summary>
    /// First-run wizard. Detects the OpenCode environments/apps on the machine
    /// and offers to import settings, commands, themes, credentials and chat
    /// history into the environment used by the embedded server.
    /// </summary>
    public partial class FirstRunWindow : Window
    {
        private readonly ImportService _service;
        private readonly string _solutionDir;
        private readonly Action _onFinished;
        private readonly Services.SpendSettings _settings;
        private readonly Dictionary<ImportItem, CheckBox> _checks = new Dictionary<ImportItem, CheckBox>();
        private string _customPath;

        private System.Drawing.Color _bg = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x1e);
        private System.Drawing.Color _panel = System.Drawing.Color.FromArgb(0x25, 0x25, 0x26);
        private System.Drawing.Color _text = System.Drawing.Color.FromArgb(0xf1, 0xf1, 0xf1);
        private System.Drawing.Color _muted = System.Drawing.Color.FromArgb(0x9a, 0x9a, 0x9a);
        private System.Drawing.Color _accent = System.Drawing.Color.FromArgb(0x00, 0x78, 0xd4);

        private static SolidColorBrush Brush(System.Drawing.Color c)
            => new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));

        public FirstRunWindow(ImportService service, string solutionDir,
            Services.SpendSettings settings = null, Action onFinished = null)
        {
            InitializeComponent();
            _service = service ?? new ImportService();
            _solutionDir = solutionDir;
            _onFinished = onFinished;
            _settings = settings;

            usageCheck.IsChecked = _settings?.EnableInjection ?? true;
            if (_settings == null) usageCard.Visibility = Visibility.Collapsed;

            ApplyTheme();
            Loaded += (_, __) => Rebuild();
        }

        private void ApplyTheme()
        {
            try
            {
                _bg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                _panel = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTabGradientBeginColorKey);
                _text = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
                _muted = VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarTextInactiveColorKey);
                _accent = VSColorTheme.GetThemedColor(EnvironmentColors.AccentBorderColorKey);
            }
            catch { }

            Background = Brush(_bg);
            titleText.Foreground = Brush(_text);
            subtitleText.Foreground = Brush(_muted);
            statusText.Foreground = Brush(_muted);
            foreach (var b in new[] { browseButton, skipButton, importButton })
            {
                b.Foreground = Brush(_text);
                b.Background = Brush(_panel);
                b.BorderBrush = Brush(_muted);
            }
            importButton.Foreground = Brush(_text);
        }

        private void Rebuild()
        {
            sourcesPanel.Children.Clear();
            _checks.Clear();
            statusText.Text = "";

            var sources = _service.DetectSources(_customPath, _solutionDir);
            if (sources.Count == 0)
            {
                sourcesPanel.Children.Add(new TextBlock
                {
                    Text = "No OpenCode data was found on this machine yet. " +
                           "You can start using OpenCode Studio right away - settings will be created automatically.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush(_muted),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                importButton.IsEnabled = false;
                return;
            }

            importButton.IsEnabled = true;
            foreach (var source in sources)
                sourcesPanel.Children.Add(BuildSourceCard(source));
        }

        private UIElement BuildSourceCard(ImportSource source)
        {
            var card = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = source.Name,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brush(_text)
            });
            if (source.IsActive)
            {
                header.Children.Add(new Border
                {
                    Margin = new Thickness(8, 0, 0, 0),
                    Padding = new Thickness(6, 1, 6, 1),
                    CornerRadius = new CornerRadius(3),
                    Background = Brush(_accent),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "in use",
                        FontSize = 10,
                        Foreground = Brushes.White
                    }
                });
            }
            card.Children.Add(header);

            card.Children.Add(new TextBlock
            {
                Text = source.Summary,
                FontSize = 11,
                Foreground = Brush(_muted),
                Margin = new Thickness(0, 2, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });

            foreach (var item in source.Items)
            {
                var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var check = new CheckBox
                {
                    Content = item.Label,
                    IsChecked = item.Selected,
                    IsEnabled = item.Importable,
                    Foreground = Brush(item.Found ? _text : _muted),
                    VerticalAlignment = VerticalAlignment.Center
                };
                if (item.Importable) _checks[item] = check;
                Grid.SetColumn(check, 0);
                row.Children.Add(check);

                var detail = new TextBlock
                {
                    Text = item.Detail,
                    FontSize = 11,
                    Foreground = Brush(_muted),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 0, 0),
                    MaxWidth = 320,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                if (source.IsActive) detail.Text = item.Found ? item.Detail + "  (already active)" : item.Detail;
                Grid.SetColumn(detail, 1);
                row.Children.Add(detail);

                card.Children.Add(row);
            }

            return card;
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select a folder that contains OpenCode data (config, auth.json, opencode.db...)";
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _customPath = dialog.SelectedPath;
                    Rebuild();
                }
            }
        }

        private void OnImportClick(object sender, RoutedEventArgs e)
        {
            var selected = _checks.Where(kv => kv.Value.IsChecked == true).Select(kv => kv.Key).ToList();
            if (selected.Count == 0)
            {
                statusText.Text = "Nothing selected to import.";
                return;
            }

            importButton.IsEnabled = false;
            try
            {
                var result = _service.Import(null, selected);

                var msg = $"Imported {result.Copied.Count} item(s).";
                if (result.Backups.Count > 0) msg += $" {result.Backups.Count} backup(s) created.";
                if (result.Errors.Count > 0)
                {
                    msg += $" {result.Errors.Count} error(s): " + string.Join("; ", result.Errors.Take(3));
                    Log.Warn("Import errors: " + string.Join(" | ", result.Errors));
                }
                Log.Info("First-run import completed. " + msg);
                statusText.Text = msg;
                Finish();
            }
            catch (Exception ex)
            {
                Log.Error("Import failed", ex);
                statusText.Text = "Import failed: " + ex.Message;
                importButton.IsEnabled = true;
            }
        }

        private void OnSkipClick(object sender, RoutedEventArgs e) => Finish();

        private void Finish()
        {
            if (_settings != null)
            {
                try
                {
                    _settings.EnableInjection = usageCheck.IsChecked == true;
                    _settings.SaveSettingsToStorage();
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to save spend settings", ex);
                }
            }

            Onboarding.MarkCompleted();
            _onFinished?.Invoke();
            Close();
        }
    }
}
