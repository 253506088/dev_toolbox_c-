using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text;
using System.IO;
using System.Text.Json;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace DevToolbox.Tools.Text.Views;

public partial class SqlInView : UserControl
{
    private const string FontSizeKey = "SqlInEditor_FontSize";
    private double _currentFontSize = 17;

    public SqlInView()
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
        InputEditor.Options.ConvertTabsToSpaces = true;
        InputEditor.Options.IndentationSize = 4;
        UpdateInputStatus();
    }

    private void OutputEditor_Loaded(object sender, RoutedEventArgs e)
    {
        OutputEditor.FontSize = _currentFontSize;

        // 设置 SQL 语法高亮
        try
        {
            OutputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("SQL");
        }
        catch
        {
            // 如果加载失败，不使用语法高亮
        }
    }

    private void Editor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

            InputEditor.FontSize = _currentFontSize;
            OutputEditor.FontSize = _currentFontSize;
            SaveFontSize();
            UpdateStatusMessage($"字体大小: {_currentFontSize}pt", true);
        }
    }

    private void OutputEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // F4 - 转换
        if (e.Key == Key.F4)
        {
            e.Handled = true;
            ConvertToSqlIn();
        }
        // Ctrl+Shift+C - 复制全部
        else if (e.Key == Key.C && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            e.Handled = true;
            CopyOutput();
        }
    }

    private void InputEditor_TextChanged(object sender, EventArgs e)
    {
        UpdateInputStatus();
    }

    private void UpdateInputStatus()
    {
        var lines = InputEditor.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Count();
        InputCountText.Text = $"{nonEmptyLines} 行";
    }

    private void UpdateStatusMessage(string message, bool isSuccess = false)
    {
        OutputStatusText.Text = message;
        OutputStatusText.Foreground = new SolidColorBrush(
            isSuccess
                ? Color.FromRgb(34, 197, 94)
                : Color.FromRgb(220, 38, 38));

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            OutputStatusText.Text = "就绪";
            OutputStatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 112, 119));
            timer.Stop();
        };
        timer.Start();
    }

    private void QuoteTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 当引号类型改变时，如果输出区域有内容，自动重新转换
        if (OutputEditor != null && !string.IsNullOrWhiteSpace(OutputEditor.Text))
        {
            ConvertToSqlIn();
        }
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        ConvertToSqlIn();
    }

    private void ConvertToSqlIn()
    {
        try
        {
            var input = InputEditor.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                UpdateStatusMessage("请输入列表内容");
                return;
            }

            // 按行分割并去除空行
            var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
            {
                UpdateStatusMessage("没有有效的输入内容");
                return;
            }

            // 获取引号类型
            var quoteType = (QuoteTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string quote = "";

            if (quoteType?.Contains("单引号") == true)
            {
                quote = "'";
            }
            else if (quoteType?.Contains("双引号") == true)
            {
                quote = "\"";
            }

            // 构建 SQL IN 语句
            var sb = new StringBuilder();
            sb.Append("(");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                // 如果使用引号，需要转义内容中的引号
                if (!string.IsNullOrEmpty(quote))
                {
                    var escapedValue = lines[i].Replace(quote, quote + quote);
                    sb.Append($"{quote}{escapedValue}{quote}");
                }
                else
                {
                    sb.Append(lines[i]);
                }
            }

            sb.Append(")");

            OutputEditor.Text = sb.ToString();
            OutputCountText.Text = $"{sb.Length} 字符";

            UpdateStatusMessage($"转换成功 - {lines.Count} 个值", true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"转换失败: {ex.Message}");
        }
    }

    private void UnformatButton_Click(object sender, RoutedEventArgs e)
    {
        UnformatFromSqlIn();
    }

    /// <summary>
    /// 去格式化：将 SQL IN 格式还原为多行文本
    /// 支持格式：('value1', 'value2', 'value3') 或 'value1','value2' 等
    /// </summary>
    private void UnformatFromSqlIn()
    {
        try
        {
            var input = InputEditor.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                UpdateStatusMessage("请输入 SQL IN 格式内容");
                return;
            }

            input = input.Trim();

            // 移除外层括号（如果有）
            if (input.StartsWith("(") && input.EndsWith(")"))
            {
                input = input.Substring(1, input.Length - 2);
            }

            // 按逗号分割
            var values = input.Split(',')
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v =>
                {
                    // 移除引号（单引号或双引号）
                    if ((v.StartsWith("'") && v.EndsWith("'")) ||
                        (v.StartsWith("\"") && v.EndsWith("\"")))
                    {
                        return v.Substring(1, v.Length - 2);
                    }
                    return v;
                })
                .ToList();

            if (values.Count == 0)
            {
                UpdateStatusMessage("没有有效的值");
                return;
            }

            // 每行一个值
            var result = string.Join("\n", values);
            OutputEditor.Text = result;
            OutputCountText.Text = $"{result.Length} 字符";

            UpdateStatusMessage($"还原成功 - {values.Count} 个值", true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"还原失败: {ex.Message}");
        }
    }

    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        SwapInputOutput();
    }

    /// <summary>
    /// 交换输入和输出区域的内容
    /// </summary>
    private void SwapInputOutput()
    {
        try
        {
            var temp = InputEditor.Text;
            InputEditor.Text = OutputEditor.Text;
            OutputEditor.Text = temp;

            UpdateInputStatus();
            UpdateStatusMessage("已交换输入输出", true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"交换失败: {ex.Message}");
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyOutput();
    }

    private void CopyOutput()
    {
        if (!string.IsNullOrEmpty(OutputEditor.Text))
        {
            Clipboard.SetText(OutputEditor.Text);
            UpdateStatusMessage("已复制到剪贴板", true);
        }
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        InputEditor.Text = "";
        OutputEditor.Text = "";
        UpdateInputStatus();
        OutputCountText.Text = "0 字符";
        UpdateStatusMessage("已清空所有内容", true);
    }
}
