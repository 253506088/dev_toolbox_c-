using DevToolbox.Core.Interfaces;

namespace DevToolbox.Tools.Text;

public class Base64Tool : ITool
{
    public string Id => "base64-codec";
    public string Name => "Base64 编解码";
    public string Description => "Base64 编码和解码";
    public string Icon => "CodeTags";
    public ToolCategory Category => ToolCategory.Text;
    public int Order => 2;
}
