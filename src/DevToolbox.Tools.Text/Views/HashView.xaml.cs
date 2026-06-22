using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using DevToolbox.Tools.Text.Helpers;

namespace DevToolbox.Tools.Text.Views;

public partial class HashView : UserControl
{
    private const string FontSizeKey = "HashEditor_FontSize";
    private double _currentFontSize = 17;
    private List<SearchResult> _searchResults = new();
    private int _currentSearchIndex = -1;
    private SearchHighlightRenderer? _highlightRenderer;

    public HashView()
    {
        InitializeComponent();
        LoadFontSize();
    }

    private void LoadFontSize()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DevToolbox");
            var settingsFile = Path.Combine(appDataPath, "settings.json");

            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (settings != null && settings.TryGetValue(FontSizeKey, out var fontSizeElement))
                {
                    _currentFontSize = fontSizeElement.GetDouble();
                }
            }
        }
        catch
        {
            _currentFontSize = 17;
        }
    }

    private void SaveFontSize()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DevToolbox");

            Directory.CreateDirectory(appDataPath);
            var settingsFile = Path.Combine(appDataPath, "settings.json");

            Dictionary<string, object> settings = new();

            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var existingSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (existingSettings != null)
                {
                    foreach (var kvp in existingSettings)
                    {
                        settings[kvp.Key] = kvp.Value;
                    }
                }
            }

            settings[FontSizeKey] = _currentFontSize;

            var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, jsonString);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    private void InputEditor_Loaded(object sender, RoutedEventArgs e)
    {
        InputEditor.FontSize = _currentFontSize;

        _highlightRenderer = new SearchHighlightRenderer();
        InputEditor.TextArea.TextView.BackgroundRenderers.Add(_highlightRenderer);

        UpdateStatus();
    }

    private void InputEditor_TextChanged(object sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void InputEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                _currentFontSize = Math.Min(_currentFontSize + 1, 36);
            }
            else
            {
                _currentFontSize = Math.Max(_currentFontSize - 1, 8);
            }

            InputEditor.FontSize = _currentFontSize;
            SaveFontSize();
            UpdateStatusMessage($"字体大小: {_currentFontSize}pt", true);
        }
    }

    private void InputEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+F - 搜索
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            ShowSearchPanel();
        }
        // F4 - 计算哈希
        else if (e.Key == Key.F4)
        {
            e.Handled = true;
            CalculateHash();
        }
        // Ctrl+Shift+C - 复制全部
        else if (e.Key == Key.C && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            e.Handled = true;
            CopyAll();
        }
        // Esc - 关闭搜索框
        else if (e.Key == Key.Escape && SearchPanel.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            CloseSearchPanel();
        }
    }

    private void ShowSearchPanel()
    {
        SearchPanel.Visibility = Visibility.Visible;
        MainContent.Margin = new Thickness(0, 90, 0, 30);
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    private void CloseSearchPanel()
    {
        SearchPanel.Visibility = Visibility.Collapsed;
        MainContent.Margin = new Thickness(0, 50, 0, 30);
        InputEditor.Focus();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            FindNext();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseSearchPanel();
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (HighlightAllCheckBox.IsChecked == true)
        {
            PerformSearch();
        }
    }

    private void HighlightAll_Changed(object sender, RoutedEventArgs e)
    {
        if (HighlightAllCheckBox.IsChecked == true)
        {
            PerformSearch();
        }
        else
        {
            ClearHighlights();
        }
    }

    private void PerformSearch()
    {
        _searchResults.Clear();
        _currentSearchIndex = -1;

        var searchText = SearchTextBox.Text;
        if (string.IsNullOrEmpty(searchText))
        {
            ClearHighlights();
            SearchResultText.Text = "";
            return;
        }

        var text = InputEditor.Text;
        var index = 0;

        while ((index = text.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            _searchResults.Add(new SearchResult { Start = index, Length = searchText.Length });
            index += searchText.Length;
        }

        if (_searchResults.Count > 0)
        {
            SearchResultText.Text = $"找到 {_searchResults.Count} 个匹配项";

            if (HighlightAllCheckBox.IsChecked == true && _highlightRenderer != null)
            {
                _highlightRenderer.Results = _searchResults;
                _highlightRenderer.CurrentIndex = -1;
                InputEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
            }
        }
        else
        {
            SearchResultText.Text = "未找到";
            ClearHighlights();
        }
    }

    private void ClearHighlights()
    {
        if (_highlightRenderer != null)
        {
            _highlightRenderer.Results = new List<SearchResult>();
            _highlightRenderer.CurrentIndex = -1;
            InputEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void FindPrevious_Click(object sender, RoutedEventArgs e)
    {
        FindPrevious();
    }

    private void CloseSearch_Click(object sender, RoutedEventArgs e)
    {
        CloseSearchPanel();
    }

    private void FindNext()
    {
        var searchText = SearchTextBox.Text;
        if (string.IsNullOrEmpty(searchText))
            return;

        if (_searchResults.Count == 0)
        {
            PerformSearch();
            if (_searchResults.Count == 0)
                return;
        }

        _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
        var result = _searchResults[_currentSearchIndex];

        InputEditor.Select(result.Start, result.Length);
        InputEditor.ScrollTo(InputEditor.Document.GetLineByOffset(result.Start).LineNumber, 0);

        SearchResultText.Text = $"{_currentSearchIndex + 1} / {_searchResults.Count}";

        if (HighlightAllCheckBox.IsChecked == true && _highlightRenderer != null)
        {
            _highlightRenderer.CurrentIndex = _currentSearchIndex;
            InputEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
        }
    }

    private void FindPrevious()
    {
        var searchText = SearchTextBox.Text;
        if (string.IsNullOrEmpty(searchText))
            return;

        if (_searchResults.Count == 0)
        {
            PerformSearch();
            if (_searchResults.Count == 0)
                return;
        }

        _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) % _searchResults.Count;
        var result = _searchResults[_currentSearchIndex];

        InputEditor.Select(result.Start, result.Length);
        InputEditor.ScrollTo(InputEditor.Document.GetLineByOffset(result.Start).LineNumber, 0);

        SearchResultText.Text = $"{_currentSearchIndex + 1} / {_searchResults.Count}";

        if (HighlightAllCheckBox.IsChecked == true && _highlightRenderer != null)
        {
            _highlightRenderer.CurrentIndex = _currentSearchIndex;
            InputEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
        }
    }

    private void UpdateStatus()
    {
        var charCount = InputEditor.Text.Length;
        CharCountText.Text = $"{charCount} 字符";

        var line = InputEditor.Document.GetLineByOffset(InputEditor.CaretOffset);
        var column = InputEditor.CaretOffset - line.Offset + 1;

        LineColumnText.Text = $"第 {line.LineNumber} 行，第 {column} 列";
    }

    private void UpdateStatusMessage(string message, bool isSuccess = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = new SolidColorBrush(
            isSuccess
                ? Color.FromRgb(34, 197, 94)
                : Color.FromRgb(220, 38, 38));

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            StatusText.Text = "就绪";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 112, 119));
            timer.Stop();
        };
        timer.Start();
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        CalculateHash();
    }

    private void CalculateHash()
    {
        try
        {
            var input = InputEditor.Text;
            if (string.IsNullOrEmpty(input))
            {
                UpdateStatusMessage("请输入要计算哈希的文本");
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(input);

            // MD5
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                Md5TextBox.Text = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // SHA256
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                Sha256TextBox.Text = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // SHA512
            using (var sha512 = SHA512.Create())
            {
                var hash = sha512.ComputeHash(bytes);
                Sha512TextBox.Text = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            UpdateStatusMessage("计算成功", true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"计算错误: {ex.Message}");
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        InputEditor.Text = "";
        Md5TextBox.Text = "";
        Sha256TextBox.Text = "";
        Sha512TextBox.Text = "";
        UpdateStatusMessage("已清空", true);
    }

    private void CopyAllButton_Click(object sender, RoutedEventArgs e)
    {
        CopyAll();
    }

    private void CopyAll()
    {
        if (!string.IsNullOrEmpty(InputEditor.Text))
        {
            Clipboard.SetText(InputEditor.Text);
            UpdateStatusMessage("已复制到剪贴板", true);
        }
    }

    private void CopyMd5Button_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Md5TextBox.Text))
        {
            Clipboard.SetText(Md5TextBox.Text);
            UpdateStatusMessage("MD5 已复制到剪贴板", true);
        }
    }

    private void CopySha256Button_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Sha256TextBox.Text))
        {
            Clipboard.SetText(Sha256TextBox.Text);
            UpdateStatusMessage("SHA256 已复制到剪贴板", true);
        }
    }

    private void CopySha512Button_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Sha512TextBox.Text))
        {
            Clipboard.SetText(Sha512TextBox.Text);
            UpdateStatusMessage("SHA512 已复制到剪贴板", true);
        }
    }
}
