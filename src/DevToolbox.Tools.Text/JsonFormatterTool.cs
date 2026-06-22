using DevToolbox.Core.Interfaces;

namespace DevToolbox.Tools.Text;

public class JsonFormatterTool : ITool
{
    public string Id => "json-formatter";
    public string Name => "JSON 格式化";
    public string Description => "格式化和验证 JSON 数据";
    public string Icon => "CodeJson";
    public ToolCategory Category => ToolCategory.Text;
    public int Order => 1;
}
