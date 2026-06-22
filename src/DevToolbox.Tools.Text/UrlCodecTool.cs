using DevToolbox.Core.Interfaces;

namespace DevToolbox.Tools.Text;

public class UrlCodecTool : ITool
{
    public string Id => "url-codec";
    public string Name => "URL 编解码";
    public string Description => "URL 编码和解码";
    public string Icon => "Link";
    public ToolCategory Category => ToolCategory.Text;
    public int Order => 3;
}
