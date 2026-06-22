using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;

namespace DevToolbox.Tools.Text.Helpers;

// 搜索结果类
public class SearchResult
{
    public int Start { get; set; }
    public int Length { get; set; }
}

// 搜索高亮渲染器
public class SearchHighlightRenderer : IBackgroundRenderer
{
    public List<SearchResult> Results { get; set; } = new();
    public int CurrentIndex { get; set; } = -1;

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (Results == null || Results.Count == 0 || textView.Document == null)
            return;

        for (int i = 0; i < Results.Count; i++)
        {
            var result = Results[i];

            try
            {
                var start = result.Start;
                var end = result.Start + result.Length;

                // 确保位置有效
                if (start < 0 || end > textView.Document.TextLength)
                    continue;

                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, new TextSegment { StartOffset = start, Length = result.Length }))
                {
                    // 当前焦点项 - 橙色高亮
                    // 其他匹配项 - 黄色高亮
                    var color = i == CurrentIndex
                        ? Color.FromArgb(180, 255, 140, 0)  // 橙色，半透明
                        : Color.FromArgb(100, 255, 255, 0); // 黄色，更透明

                    drawingContext.DrawRectangle(
                        new SolidColorBrush(color),
                        null,
                        new Rect(rect.Location, rect.Size));
                }
            }
            catch
            {
                // 忽略无效的位置
            }
        }
    }
}
