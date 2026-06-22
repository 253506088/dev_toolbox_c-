using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevToolbox.Core.Interfaces;

/// <summary>
/// 后台任务接口
/// </summary>
public interface IBackgroundTask
{
    /// <summary>
    /// 任务唯一标识符
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 任务名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 任务状态
    /// </summary>
    TaskStatus Status { get; }

    /// <summary>
    /// 任务进度（0-100）
    /// </summary>
    double Progress { get; }

    /// <summary>
    /// 任务消息
    /// </summary>
    string Message { get; }

    /// <summary>
    /// 执行任务
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 进度变化事件
    /// </summary>
    event EventHandler<TaskProgressEventArgs> ProgressChanged;
}

/// <summary>
/// 任务进度事件参数
/// </summary>
public class TaskProgressEventArgs : EventArgs
{
    public double Progress { get; set; }
    public string Message { get; set; }

    public TaskProgressEventArgs(double progress, string message)
    {
        Progress = progress;
        Message = message;
    }
}

/// <summary>
/// 后台任务队列服务接口
/// </summary>
public interface ITaskQueueService
{
    /// <summary>
    /// 添加任务到队列
    /// </summary>
    Task<string> EnqueueAsync(IBackgroundTask task);

    /// <summary>
    /// 取消任务
    /// </summary>
    Task CancelAsync(string taskId);

    /// <summary>
    /// 获取所有任务
    /// </summary>
    IReadOnlyList<IBackgroundTask> GetAllTasks();

    /// <summary>
    /// 获取指定任务
    /// </summary>
    IBackgroundTask GetTask(string taskId);
}
