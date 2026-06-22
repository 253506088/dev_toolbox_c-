using System.Windows;
using System.Windows.Controls;
using DevToolbox.Core.Interfaces;
using DevToolbox.Tools.Text.Views;

namespace DevToolbox.App;

public partial class MainWindow : Window
{
    private readonly IEnumerable<ITool> _tools;
    private readonly Dictionary<string, UserControl> _viewCache = new();

    public MainWindow(IEnumerable<ITool> tools)
    {
        InitializeComponent();
        _tools = tools;
        LoadTools();
    }

    private void LoadTools()
    {
        var toolList = _tools
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Order)
            .ToList();

        ToolsItemsControl.ItemsSource = toolList;
    }

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ITool tool)
        {
            // 检查缓存中是否已有视图
            if (!_viewCache.TryGetValue(tool.Id, out var view))
            {
                // 根据工具 ID 创建对应的视图（仅创建一次）
                view = tool.Id switch
                {
                    "json-formatter" => new JsonFormatterView(),
                    "base64-codec" => new Base64View(),
                    "url-codec" => new UrlCodecView(),
                    "hash-tool" => new HashView(),
                    "sql-in" => new SqlInView(),
                    _ => null
                };

                // 缓存视图
                if (view != null)
                {
                    _viewCache[tool.Id] = view;
                }
            }

            if (view != null)
            {
                ToolContentControl.Content = view;
            }
            else
            {
                // 显示工具信息（未实现的工具）
                var infoText = new TextBlock
                {
                    Text = $"工具：{tool.Name}\n\n描述：{tool.Description}\n\n分类：{tool.Category}\n\n此工具的界面尚未实现，敬请期待。",
                    FontSize = 16,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E2227")),
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(20)
                };

                ToolContentControl.Content = infoText;
            }
        }
    }
}