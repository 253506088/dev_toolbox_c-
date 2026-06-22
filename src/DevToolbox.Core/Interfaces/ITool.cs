namespace DevToolbox.Core.Interfaces;

/// <summary>
/// 工具模块接口，所有工具都需要实现此接口
/// </summary>
public interface ITool
{
    /// <summary>
    /// 工具唯一标识符
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 工具显示名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 工具图标（Material Design Icons 名称或路径）
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// 工具分类
    /// </summary>
    ToolCategory Category { get; }

    /// <summary>
    /// 排序优先级（数字越小越靠前）
    /// </summary>
    int Order { get; }
}

/// <summary>
/// 工具分类枚举
/// </summary>
public enum ToolCategory
{
    Text,       // 文本工具
    File,       // 文件工具
    Image,      // 图片工具
    Video,      // 视频工具
    Note,       // 便签工具
    Utility     // 实用工具
}
