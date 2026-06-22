using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using DevToolbox.Core.Interfaces;
using DevToolbox.Tools.Text;

namespace DevToolbox.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/devtoolbox-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // 配置服务容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // 显示主窗口
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 日志
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // 主窗口
        services.AddSingleton<MainWindow>();

        // 基础设施服务（后续实现）
        // services.AddSingleton<IStorageService, StorageService>();
        // services.AddSingleton<IConfigService, ConfigService>();
        // services.AddSingleton<ITaskQueueService, TaskQueueService>();

        // 工具模块
        services.AddTransient<ITool, JsonFormatterTool>();
        services.AddTransient<ITool, Base64Tool>();
        services.AddTransient<ITool, UrlCodecTool>();
        services.AddTransient<ITool, HashTool>();
        services.AddTransient<ITool, SqlInTool>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

