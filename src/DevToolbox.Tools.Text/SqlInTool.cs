using DevToolbox.Core.Interfaces;

namespace DevToolbox.Tools.Text;

public class SqlInTool : ITool
{
    public string Id => "sql-in";
    public string Name => "SQL IN";
    public string Description => "将列表数据转换为 SQL IN 语句格式";
    public string Icon => "Database";
    public ToolCategory Category => ToolCategory.Text;
    public int Order => 5;
}
