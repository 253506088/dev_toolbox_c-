# DevToolbox 发布脚本
# 使用方法: .\publish.ps1 -Version "1.0.0"

param(
    [string]$Version = "1.0.0",
    [switch]$SkipTests,
    [switch]$IncludeArm64
)

$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# 开始发布
Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "   DevToolbox 发布工具 v$Version" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta

# 1. 检查环境
Write-Step "检查构建环境..."

if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK 未安装或不在 PATH 中"
    exit 1
}

$dotnetVersion = dotnet --version
Write-Success ".NET SDK 版本: $dotnetVersion"

# 2. 清理旧的构建
Write-Step "清理旧的构建文件..."

if (Test-Path "publish") {
    Remove-Item "publish" -Recurse -Force
    Write-Success "已清理 publish 目录"
}

if (Test-Path "*.zip") {
    Remove-Item "*.zip" -Force
    Write-Success "已清理旧的 ZIP 文件"
}

# 3. 运行测试（可选）
if (!$SkipTests) {
    Write-Step "运行单元测试..."

    if (Test-Path "tests/DevToolbox.Tests") {
        try {
            dotnet test --verbosity quiet --nologo
            Write-Success "所有测试通过"
        } catch {
            Write-Error "测试失败，发布中止"
            exit 1
        }
    } else {
        Write-Warning "未找到测试项目，跳过测试"
    }
} else {
    Write-Warning "已跳过测试（--SkipTests）"
}

# 4. 发布 x64 版本
Write-Step "发布 Windows x64 版本..."

dotnet publish src/DevToolbox.App -c Release -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o publish/win-x64 `
    --nologo `
    -v quiet

if ($LASTEXITCODE -eq 0) {
    Write-Success "x64 版本发布成功"
} else {
    Write-Error "x64 版本发布失败"
    exit 1
}

# 5. 发布 ARM64 版本（可选）
if ($IncludeArm64) {
    Write-Step "发布 Windows ARM64 版本..."

    dotnet publish src/DevToolbox.App -c Release -r win-arm64 `
        --self-contained `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -o publish/win-arm64 `
        --nologo `
        -v quiet

    if ($LASTEXITCODE -eq 0) {
        Write-Success "ARM64 版本发布成功"
    } else {
        Write-Warning "ARM64 版本发布失败（继续）"
    }
}

# 6. 复制额外资源
Write-Step "复制额外资源..."

# 复制 FFmpeg（如果存在）
$ffmpegPath = "assets/ffmpeg/ffmpeg.exe"
if (Test-Path $ffmpegPath) {
    Copy-Item $ffmpegPath "publish/win-x64/" -Force
    Write-Success "已复制 FFmpeg"

    if ($IncludeArm64 -and (Test-Path "publish/win-arm64")) {
        $ffmpegArmPath = "assets/ffmpeg/ffmpeg-arm64.exe"
        if (Test-Path $ffmpegArmPath) {
            Copy-Item $ffmpegArmPath "publish/win-arm64/ffmpeg.exe" -Force
            Write-Success "已复制 FFmpeg (ARM64)"
        }
    }
} else {
    Write-Warning "未找到 FFmpeg，请手动下载并复制到发布目录"
}

# 创建 README.txt
$readmeContent = @"
DevToolbox v$Version
========================================

这是一个 Windows 桌面开发工具箱，提供文本处理、文件管理、图片视频处理等功能。

## 运行方法

直接双击 DevToolbox.App.exe 即可运行，无需安装。

## 系统要求

- Windows 10 或更高版本
- 无需额外安装 .NET 运行时（已打包）

## 文件说明

- DevToolbox.App.exe  主程序
- ffmpeg.exe          视频处理工具（可选）
- logs/               日志目录（首次运行后生成）

## 卸载方法

直接删除整个文件夹即可，程序不会在系统中留下任何残留。

## 数据位置

用户数据存储在：
%LOCALAPPDATA%\DevToolbox\

包含：
- devtoolbox.db      数据库（便签、配置等）
- logs/              应用日志

## 问题反馈

如遇到问题，请查看 logs 目录下的日志文件。

版本：$Version
发布日期：$(Get-Date -Format "yyyy-MM-dd")
"@

Set-Content -Path "publish/win-x64/README.txt" -Value $readmeContent -Encoding UTF8
Write-Success "已生成 README.txt"

if ($IncludeArm64 -and (Test-Path "publish/win-arm64")) {
    Set-Content -Path "publish/win-arm64/README.txt" -Value $readmeContent -Encoding UTF8
}

# 7. 创建压缩包
Write-Step "创建压缩包..."

# x64 压缩包
$zipName = "DevToolbox-v$Version-win-x64.zip"
Compress-Archive -Path "publish/win-x64/*" -DestinationPath $zipName -Force
$x64Size = (Get-Item $zipName).Length / 1MB
Write-Success "已创建 $zipName ($([Math]::Round($x64Size, 2)) MB)"

# ARM64 压缩包
if ($IncludeArm64 -and (Test-Path "publish/win-arm64")) {
    $zipNameArm = "DevToolbox-v$Version-win-arm64.zip"
    Compress-Archive -Path "publish/win-arm64/*" -DestinationPath $zipNameArm -Force
    $armSize = (Get-Item $zipNameArm).Length / 1MB
    Write-Success "已创建 $zipNameArm ($([Math]::Round($armSize, 2)) MB)"
}

# 8. 生成校验和
Write-Step "生成文件校验和..."

function Get-FileHashSHA256 {
    param([string]$FilePath)
    $hash = Get-FileHash -Path $FilePath -Algorithm SHA256
    return $hash.Hash
}

$checksumFile = "DevToolbox-v$Version-checksums.txt"
$checksums = @()

Get-ChildItem "*.zip" | ForEach-Object {
    $hash = Get-FileHashSHA256 $_.FullName
    $checksums += "$hash  $($_.Name)"
    Write-Host "  $($_.Name): $hash" -ForegroundColor Gray
}

Set-Content -Path $checksumFile -Value ($checksums -join "`n")
Write-Success "已生成校验和文件: $checksumFile"

# 9. 显示摘要
Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "   发布完成！" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta

Write-Host "发布文件：" -ForegroundColor Yellow
Get-ChildItem "*.zip" | ForEach-Object {
    $size = $_.Length / 1MB
    Write-Host "  • $($_.Name) - $([Math]::Round($size, 2)) MB" -ForegroundColor White
}

Write-Host "`n校验和文件：" -ForegroundColor Yellow
Write-Host "  • $checksumFile" -ForegroundColor White

Write-Host "`n发布目录：" -ForegroundColor Yellow
Write-Host "  • publish/win-x64/" -ForegroundColor White
if ($IncludeArm64 -and (Test-Path "publish/win-arm64")) {
    Write-Host "  • publish/win-arm64/" -ForegroundColor White
}

Write-Host "`n下一步：" -ForegroundColor Yellow
Write-Host "  1. 在干净的 Windows 系统上测试压缩包" -ForegroundColor White
Write-Host "  2. 验证所有功能正常工作" -ForegroundColor White
Write-Host "  3. 更新 GitHub Release 或发布说明" -ForegroundColor White
Write-Host "  4. 分享校验和以便用户验证文件完整性`n" -ForegroundColor White
