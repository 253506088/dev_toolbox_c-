using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.IO;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using DevToolbox.Tools.Text.Helpers;

namespace DevToolbox.Tools.Text.Views;

public partial class UrlCodecView : UserControl
{
    private const string FontSizeKey = "UrlEditor_FontSize";
    private double _currentFontSize = 17;
    private List<SearchResult> _searchResults = new();
    private int _currentSearchIndex = -1;
    private SearchHighlightRenderer? _highlightRenderer;

    public UrlCodecView()
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
            // 如果加载失败，使用默认值
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

            // 读取现有设置
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

            // 更新字号设置
            settings[FontSizeKey] = _currentFontSize;

            // 保存
            var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, jsonString);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    private void UrlEditor_Loaded(object sender, RoutedEventArgs e)
    {
        // 应用保存的字号
        UrlEditor.FontSize = _currentFontSize;

        // 安装 AvalonEdit 内置的搜索面板
        ICSharpCode.AvalonEdit.Search.SearchPanel.Install(UrlEditor);

        // 初始化高亮渲染器
        _highlightRenderer = new SearchHighlightRenderer();
        UrlEditor.TextArea.TextView.BackgroundRenderers.Add(_highlightRenderer);

        UpdateStatus();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 文本改变时重新搜索
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

        var text = UrlEditor.Text;
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
                UrlEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
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
            UrlEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
        }
    }

    private void UrlEditor_TextChanged(object sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void UrlEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ctrl + 滚轮缩放字体
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

            UrlEditor.FontSize = _currentFontSize;
            SaveFontSize(); // 保存字号
            UpdateStatusMessage($"字体大小: {_currentFontSize}pt", true);
        }
    }

    private void UrlEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+F - 搜索
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            ShowSearchPanel();
        }
        // F4 - 编码
        else if (e.Key == Key.F4)
        {
            e.Handled = true;
            EncodeUrl();
        }
        // F5 - 解码
        else if (e.Key == Key.F5)
        {
            e.Handled = true;
            DecodeUrl();
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
        EditorBorder.Margin = new Thickness(0, 90, 0, 30);
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    private void CloseSearchPanel()
    {
        SearchPanel.Visibility = Visibility.Collapsed;
        EditorBorder.Margin = new Thickness(0, 50, 0, 30);
        UrlEditor.Focus();
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

        // 如果还没搜索过，先执行搜索
        if (_searchResults.Count == 0)
        {
            PerformSearch();
            if (_searchResults.Count == 0)
                return;
        }

        // 移动到下一个结果
        _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
        var result = _searchResults[_currentSearchIndex];

        // 选中并滚动到当前结果
        UrlEditor.Select(result.Start, result.Length);
        UrlEditor.ScrollTo(UrlEditor.Document.GetLineByOffset(result.Start).LineNumber, 0);

        // 更新状态和高亮
        SearchResultText.Text = $"{_currentSearchIndex + 1} / {_searchResults.Count}";

        if (HighlightAllCheckBox.IsChecked == true && _highlightRenderer != null)
        {
            _highlightRenderer.CurrentIndex = _currentSearchIndex;
            UrlEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
        }
    }

    private void FindPrevious()
    {
        var searchText = SearchTextBox.Text;
        if (string.IsNullOrEmpty(searchText))
            return;

        // 如果还没搜索过，先执行搜索
        if (_searchResults.Count == 0)
        {
            PerformSearch();
            if (_searchResults.Count == 0)
                return;
        }

        // 移动到上一个结果
        _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) % _searchResults.Count;
        var result = _searchResults[_currentSearchIndex];

        // 选中并滚动到当前结果
        UrlEditor.Select(result.Start, result.Length);
        UrlEditor.ScrollTo(UrlEditor.Document.GetLineByOffset(result.Start).LineNumber, 0);

        // 更新状态和高亮
        SearchResultText.Text = $"{_currentSearchIndex + 1} / {_searchResults.Count}";

        if (HighlightAllCheckBox.IsChecked == true && _highlightRenderer != null)
        {
            _highlightRenderer.CurrentIndex = _currentSearchIndex;
            UrlEditor.TextArea.TextView.InvalidateLayer(_highlightRenderer.Layer);
        }
    }

    private void UpdateStatus()
    {
        // 更新字符数
        var charCount = UrlEditor.Text.Length;
        CharCountText.Text = $"{charCount} 字符";

        // 更新行列信息
        var line = UrlEditor.Document.GetLineByOffset(UrlEditor.CaretOffset);
        var column = UrlEditor.CaretOffset - line.Offset + 1;

        LineColumnText.Text = $"第 {line.LineNumber} 行，第 {column} 列";
    }

    private void UpdateStatusMessage(string message, bool isSuccess = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = new SolidColorBrush(
            isSuccess
                ? Color.FromRgb(34, 197, 94)
                : Color.FromRgb(220, 38, 38));

        // 3秒后恢复默认状态
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            StatusText.Text = "就绪";
            StatusText.Foreground = new SolidColorBrush(
                Color.FromRgb(107, 112, 119));
            timer.Stop();
        };
        timer.Start();
    }

    private void EncodeButton_Click(object sender, RoutedEventArgs e)
    {
        EncodeUrl();
    }

    private void DecodeButton_Click(object sender, RoutedEventArgs e)
    {
        DecodeUrl();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyAll();
    }

    private void CopyAll()
    {
        if (!string.IsNullOrEmpty(UrlEditor.Text))
        {
            Clipboard.SetText(UrlEditor.Text);
            UpdateStatusMessage("已复制到剪贴板", true);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        UrlEditor.Text = "";
        UpdateStatusMessage("已清空", true);
    }

    private void EncodeUrl()
    {
        try
        {
            var input = UrlEditor.Text;
            if (string.IsNullOrEmpty(input))
            {
                UpdateStatusMessage("请输入要编码的 URL");
                return;
            }

            // 保存光标位置
            var caretOffset = UrlEditor.CaretOffset;

            var encoded = Uri.EscapeDataString(input);

            UrlEditor.Text = encoded;

            // 尝试恢复光标位置
            UrlEditor.CaretOffset = Math.Min(caretOffset, encoded.Length);

            UpdateStatusMessage($"编码成功 - {encoded.Length} 字符", true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"编码错误: {ex.Message}");
        }
    }

    private void DecodeUrl()
    {
        try
        {
            var input = UrlEditor.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                UpdateStatusMessage("请输入要解码的 URL");
                return;
            }

            // 保存光标位置
            var caretOffset = UrlEditor.CaretOffset;

            var decoded = Uri.UnescapeDataString(input.Trim());

            UrlEditor.Text = decoded;

            // 尝试恢复光标位置
            UrlEditor.CaretOffset = Math.Min(caretOffset, decoded.Length);

            UpdateStatusMessage($"解码成功 - {decoded.Length} 字符", true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"解码错误: {ex.Message}");
        }
    }
}
