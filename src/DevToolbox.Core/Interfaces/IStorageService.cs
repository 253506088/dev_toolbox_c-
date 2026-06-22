using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevToolbox.Core.Interfaces;

/// <summary>
/// 数据存储服务接口
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// 初始化数据库
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 执行查询
    /// </summary>
    Task<List<T>> QueryAsync<T>(string sql, object parameters = null) where T : new();

    /// <summary>
    /// 执行非查询命令
    /// </summary>
    Task<int> ExecuteAsync(string sql, object parameters = null);

    /// <summary>
    /// 执行标量查询
    /// </summary>
    Task<T> ExecuteScalarAsync<T>(string sql, object parameters = null);

    /// <summary>
    /// 事务执行
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
}

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    T Get<T>(string key, T defaultValue = default);

    /// <summary>
    /// 设置配置值
    /// </summary>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// 删除配置
    /// </summary>
    Task DeleteAsync(string key);

    /// <summary>
    /// 配置是否存在
    /// </summary>
    bool Exists(string key);
}
