# DevToolbox 性能测试脚本
# 使用方法: .\performance-test.ps1

param(
    [switch]$CreateTestFiles,
    [switch]$CleanupTestFiles,
    [int]$FileCount = 100000,
    [int]$ImageCount = 1000
)

$ErrorActionPreference = "Stop"

function Write-TestHeader {
    param([string]$Title)
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "  $Title" -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta
}

function Write-TestResult {
    param(
        [string]$TestName,
        [double]$Duration,
        [bool]$Passed,
        [string]$Details = ""
    )

    $status = if ($Passed) { "✓ PASS" } else { "✗ FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }

    Write-Host "$status $TestName" -ForegroundColor $color
    Write-Host "     耗时: $([Math]::Round($Duration, 2))s" -ForegroundColor Gray
    if ($Details) {
        Write-Host "     $Details" -ForegroundColor Gray
    }
}

# 创建测试文件
if ($CreateTestFiles) {
    Write-TestHeader "创建性能测试文件"

    # 1. 创建大量小文件目录
    $testFilesDir = "D:\DevToolbox_TestFiles"
    if (Test-Path $testFilesDir) {
        Write-Host "清理旧的测试目录..." -ForegroundColor Yellow
        Remove-Item $testFilesDir -Recurse -Force
    }

    Write-Host "创建测试目录: $testFilesDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $testFilesDir -Force | Out-Null

    Write-Host "生成 $FileCount 个测试文件..." -ForegroundColor Cyan
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    1..$FileCount | ForEach-Object {
        $fileName = "file_$_.txt"
        $content = "Test file $_`n" + ("x" * 100)
        Set-Content -Path "$testFilesDir\$fileName" -Value $content -NoNewline

        if ($_ % 10000 -eq 0) {
            Write-Host "  已创建 $_ 个文件..." -ForegroundColor Gray
        }
    }

    $sw.Stop()
    Write-Host "✓ 完成！耗时: $($sw.Elapsed.TotalSeconds)s" -ForegroundColor Green

    # 2. 创建测试图片
    $testImagesDir = "D:\DevToolbox_TestImages"
    if (Test-Path $testImagesDir) {
        Remove-Item $testImagesDir -Recurse -Force
    }

    Write-Host "`n创建测试图片目录: $testImagesDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $testImagesDir -Force | Out-Null

    Write-Host "生成 $ImageCount 个测试图片（需要安装 ImageMagick）..." -ForegroundColor Cyan

    if (Get-Command "magick" -ErrorAction SilentlyContinue) {
        $sw.Restart()
        1..$ImageCount | ForEach-Object {
            $imagePath = "$testImagesDir\test_$_.png"
            # 生成 1920x1080 的随机颜色图片
            magick -size 1920x1080 "xc:#$(Get-Random -Minimum 100000 -Maximum 999999)" $imagePath 2>$null

            if ($_ % 100 -eq 0) {
                Write-Host "  已创建 $_ 张图片..." -ForegroundColor Gray
            }
        }
        $sw.Stop()
        Write-Host "✓ 完成！耗时: $($sw.Elapsed.TotalSeconds)s" -ForegroundColor Green
    } else {
        Write-Host "⚠ 未安装 ImageMagick，跳过图片生成" -ForegroundColor Yellow
        Write-Host "  下载地址: https://imagemagick.org/script/download.php" -ForegroundColor Gray
    }

    Write-Host "`n测试文件已准备好！" -ForegroundColor Green
    Write-Host "文件目录: $testFilesDir" -ForegroundColor White
    Write-Host "图片目录: $testImagesDir" -ForegroundColor White
    exit 0
}

# 清理测试文件
if ($CleanupTestFiles) {
    Write-TestHeader "清理测试文件"

    $testFilesDir = "D:\DevToolbox_TestFiles"
    $testImagesDir = "D:\DevToolbox_TestImages"

    if (Test-Path $testFilesDir) {
        Write-Host "删除 $testFilesDir..." -ForegroundColor Cyan
        Remove-Item $testFilesDir -Recurse -Force
        Write-Host "✓ 已删除文件目录" -ForegroundColor Green
    }

    if (Test-Path $testImagesDir) {
        Write-Host "删除 $testImagesDir..." -ForegroundColor Cyan
        Remove-Item $testImagesDir -Recurse -Force
        Write-Host "✓ 已删除图片目录" -ForegroundColor Green
    }

    Write-Host "`n清理完成！" -ForegroundColor Green
    exit 0
}

# 运行性能测试
Write-TestHeader "DevToolbox 性能测试套件"

Write-Host "测试环境:" -ForegroundColor Yellow
Write-Host "  操作系统: $([System.Environment]::OSVersion.VersionString)" -ForegroundColor Gray
Write-Host "  处理器数: $([System.Environment]::ProcessorCount)" -ForegroundColor Gray
Write-Host "  内存: $([Math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)) GB" -ForegroundColor Gray
Write-Host "  .NET 版本: $(dotnet --version)" -ForegroundColor Gray

# 测试 1: 启动性能
Write-TestHeader "测试 1: 应用启动时间"

$exePath = "src\DevToolbox.App\bin\Debug\net10.0-windows\DevToolbox.App.exe"

if (!(Test-Path $exePath)) {
    Write-Host "应用未构建，正在构建..." -ForegroundColor Yellow
    dotnet build -c Debug --verbosity quiet
}

Write-Host "测量冷启动时间（首次启动）..." -ForegroundColor Cyan

$coldStartTime = Measure-Command {
    $process = Start-Process -FilePath $exePath -PassThru
    Start-Sleep -Seconds 2
    $process.Kill()
    $process.WaitForExit()
}

Write-TestResult `
    -TestName "冷启动时间" `
    -Duration $coldStartTime.TotalSeconds `
    -Passed ($coldStartTime.TotalSeconds -lt 5) `
    -Details "目标: < 5秒"

# 测试 2: 内存占用
Write-TestHeader "测试 2: 内存占用"

Write-Host "启动应用并测量内存占用..." -ForegroundColor Cyan

$process = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 3

$initialMemory = (Get-Process -Id $process.Id).WorkingSet64 / 1MB
Write-Host "  初始内存: $([Math]::Round($initialMemory, 2)) MB" -ForegroundColor Gray

Start-Sleep -Seconds 5
$stableMemory = (Get-Process -Id $process.Id).WorkingSet64 / 1MB
Write-Host "  稳定内存: $([Math]::Round($stableMemory, 2)) MB" -ForegroundColor Gray

$process.Kill()
$process.WaitForExit()

Write-TestResult `
    -TestName "内存占用" `
    -Duration 0 `
    -Passed ($stableMemory -lt 300) `
    -Details "内存: $([Math]::Round($stableMemory, 2)) MB (目标: < 300 MB)"

# 测试 3: 构建性能
Write-TestHeader "测试 3: 构建性能"

Write-Host "清理构建..." -ForegroundColor Cyan
dotnet clean --verbosity quiet | Out-Null

Write-Host "测量完整构建时间..." -ForegroundColor Cyan
$buildTime = Measure-Command {
    dotnet build -c Debug --verbosity quiet
}

Write-TestResult `
    -TestName "完整构建时间" `
    -Duration $buildTime.TotalSeconds `
    -Passed ($buildTime.TotalSeconds -lt 30) `
    -Details "目标: < 30秒"

Write-Host "测量增量构建时间..." -ForegroundColor Cyan
$rebuildTime = Measure-Command {
    dotnet build -c Debug --verbosity quiet --no-restore
}

Write-TestResult `
    -TestName "增量构建时间" `
    -Duration $rebuildTime.TotalSeconds `
    -Passed ($rebuildTime.TotalSeconds -lt 10) `
    -Details "目标: < 10秒"

# 测试 4: 发布包大小
Write-TestHeader "测试 4: 发布包大小"

Write-Host "生成单文件发布..." -ForegroundColor Cyan

$publishDir = "publish\win-x64-perf-test"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish src/DevToolbox.App -c Release -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir `
    --verbosity quiet | Out-Null

$exeSize = (Get-Item "$publishDir\DevToolbox.App.exe").Length / 1MB
Write-Host "  可执行文件大小: $([Math]::Round($exeSize, 2)) MB" -ForegroundColor Gray

# 压缩测试
Compress-Archive -Path "$publishDir\*" -DestinationPath "$publishDir.zip" -Force
$zipSize = (Get-Item "$publishDir.zip").Length / 1MB
Write-Host "  压缩包大小: $([Math]::Round($zipSize, 2)) MB" -ForegroundColor Gray

# 清理
Remove-Item $publishDir -Recurse -Force
Remove-Item "$publishDir.zip" -Force

Write-TestResult `
    -TestName "发布包大小" `
    -Duration 0 `
    -Passed ($zipSize -lt 100) `
    -Details "压缩后: $([Math]::Round($zipSize, 2)) MB (目标: < 100 MB)"

# 测试摘要
Write-TestHeader "测试摘要"

Write-Host "所有性能测试已完成！" -ForegroundColor Green
Write-Host "`n建议:" -ForegroundColor Yellow
Write-Host "  1. 如果启动时间过长，检查是否有阻塞的初始化代码"
Write-Host "  2. 如果内存占用过高，使用内存分析器查找泄漏"
Write-Host "  3. 如果构建时间过长，考虑拆分项目或优化依赖"
Write-Host "  4. 如果发布包过大，启用 PublishTrimmed 或移除不必要的依赖`n"

Write-Host "生成测试文件:" -ForegroundColor Yellow
Write-Host "  .\performance-test.ps1 -CreateTestFiles" -ForegroundColor White
Write-Host "  .\performance-test.ps1 -CreateTestFiles -FileCount 50000 -ImageCount 500" -ForegroundColor Gray
Write-Host "`n清理测试文件:" -ForegroundColor Yellow
Write-Host "  .\performance-test.ps1 -CleanupTestFiles`n" -ForegroundColor White
