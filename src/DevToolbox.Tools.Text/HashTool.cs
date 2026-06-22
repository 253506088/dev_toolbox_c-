using DevToolbox.Core.Interfaces;

namespace DevToolbox.Tools.Text;

public class HashTool : ITool
{
    public string Id => "hash-tool";
    public string Name => "哈希计算";
    public string Description => "MD5/SHA256/SHA512 哈希计算";
    public string Icon => "Fingerprint";
    public ToolCategory Category => ToolCategory.Text;
    public int Order => 4;
}
