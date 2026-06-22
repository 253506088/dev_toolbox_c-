# DevToolbox - Windows 开发者工具箱

基于 .NET 10 + WPF 的 Windows 桌面开发工具集合，提供高性能的文本处理、文件管理、图片视频处理等功能。

## 技术栈

- **运行时**: .NET 10 LTS
- **UI框架**: WPF
- **架构模式**: MVVM (CommunityToolkit.Mvvm)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **日志**: Serilog
- **数据存储**: SQLite (Microsoft.Data.Sqlite)
- **图片处理**: SkiaSharp + OpenCvSharp
- **视频处理**: FFmpeg (内置)

## 项目结构

```
src/
  DevToolbox.App/              # WPF 主程序
  DevToolbox.Core/             # 核心接口和模型
  DevToolbox.Infrastructure/   # 基础设施（SQLite、日志、配置、文件系统）
  DevToolbox.Tools.Text/       # 文本工具（JSON、SQL、Base64、URL、MD5、Diff）
  DevToolbox.Tools.Notes/      # 便签和提醒
  DevToolbox.Tools.Files/      # Windows 文件管理器
  DevToolbox.Tools.Images/     # 图片处理（缩放、批量抠图）
  DevToolbox.Tools.Video/      # 视频处理（抽帧、视频抠图、FFmpeg 封装）
tests/
  DevToolbox.Tests/            # 单元测试
```

## 开发环境要求

- .NET SDK 10.0+
- Windows 10/11
- Visual Studio 2022 或 Rider

## 构建和运行

### 快速开始（推荐）

```powershell
# 运行应用
.\dev.ps1

# 或者使用热重载模式（自动重新加载代码更改）
.\dev.ps1 watch

# 运行测试
.\dev.ps1 test

# 查看所有命令
.\dev.ps1 help
```

### 手动构建

```powershell
# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行
dotnet run --project src/DevToolbox.App
```

### 发布打包（免安装版）

```powershell
# 使用发布脚本（推荐）
.\publish.ps1 -Version "1.0.0"

# 包含 ARM64 版本
.\publish.ps1 -Version "1.0.0" -IncludeArm64

# 跳过测试快速发布
.\publish.ps1 -Version "1.0.0" -SkipTests

# 手动发布
dotnet publish src/DevToolbox.App -c Release -r win-x64 --self-contained
```

### 性能测试

```powershell
# 运行性能测试套件
.\performance-test.ps1

# 创建测试文件（文件管理器测试）
.\performance-test.ps1 -CreateTestFiles

# 清理测试文件
.\performance-test.ps1 -CleanupTestFiles
```

## 迁移进度

根据《Windows重构技术选型方案.md》，迁移分为以下阶段：

- [x] 阶段0：冻结功能清单
- [x] 阶段1：搭建 .NET WPF 空壳
- [ ] 阶段2：建立工具模块协议
- [ ] 阶段3：迁移文本类工具
- [ ] 阶段4：迁移便签和 SQLite
- [ ] 阶段5：重做文件管理器
- [ ] 阶段6：迁移图片和视频工具
- [ ] 阶段7：性能压测和打包发布

## 核心设计原则

1. **模块化**：每个工具独立成模块，通过统一接口注册
2. **高性能**：后台任务队列、取消令牌、虚拟滚动
3. **可维护**：MVVM 分离、依赖注入、结构化日志
4. **原生优先**：直接使用 .NET 和 Win32 API，避免绕路

## 许可证

MIT License
