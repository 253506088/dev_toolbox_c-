# DevToolbox Development Script
# Usage: .\dev.ps1 [command]
# Commands: run, test, build, clean, watch

param(
    [Parameter(Position=0)]
    [ValidateSet("run", "test", "build", "clean", "watch", "help")]
    [string]$Command = "run"
)

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Title)
    Write-Host "`n==================================" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host "==================================`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "[>>] $Message" -ForegroundColor Yellow
}

# Main switch
switch ($Command) {
    "run" {
        Write-Header "Run DevToolbox"
        Write-Info "Starting application..."

        # Clean old logs (optional)
        if (Test-Path "logs") {
            $oldLogs = Get-ChildItem "logs" -Filter "*.log" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) }
            if ($oldLogs) {
                $oldLogs | Remove-Item -Force
                Write-Success "Cleaned $($oldLogs.Count) old log files"
            }
        }

        dotnet run --project src/DevToolbox.App
    }

    "test" {
        Write-Header "Run Tests"

        if (!(Test-Path "tests/DevToolbox.Tests")) {
            Write-Host "Test project not found, creating..." -ForegroundColor Yellow

            dotnet new xunit -n DevToolbox.Tests -o tests/DevToolbox.Tests -f net10.0
            dotnet sln add tests/DevToolbox.Tests/DevToolbox.Tests.csproj

            Push-Location tests/DevToolbox.Tests
            dotnet add reference ../../src/DevToolbox.Core/DevToolbox.Core.csproj
            dotnet add package Moq
            dotnet add package FluentAssertions
            Pop-Location

            Write-Success "Test project created"
        }

        Write-Info "Running unit tests..."
        dotnet test --verbosity normal

        if ($LASTEXITCODE -eq 0) {
            Write-Success "All tests passed"
        } else {
            Write-Host "[FAIL] Some tests failed" -ForegroundColor Red
            exit 1
        }
    }

    "build" {
        Write-Header "Build Project"

        Write-Info "Cleaning build..."
        dotnet clean --verbosity quiet

        Write-Info "Restoring dependencies..."
        dotnet restore --verbosity quiet

        Write-Info "Compiling project..."
        dotnet build -c Debug --no-restore

        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build succeeded"

            # Show output path
            $outputPath = "src/DevToolbox.App/bin/Debug/net10.0-windows/DevToolbox.App.exe"
            if (Test-Path $outputPath) {
                Write-Info "Executable: $outputPath"
            }
        } else {
            Write-Host "[FAIL] Build failed" -ForegroundColor Red
            exit 1
        }
    }

    "clean" {
        Write-Header "Clean Project"

        Write-Info "Cleaning build artifacts..."
        dotnet clean --verbosity quiet

        # Remove bin and obj directories
        $dirsToRemove = @("bin", "obj")
        Get-ChildItem -Path . -Recurse -Directory | Where-Object { $_.Name -in $dirsToRemove } | ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }

        # Clean logs
        if (Test-Path "logs") {
            Remove-Item "logs" -Recurse -Force
            Write-Success "Cleaned logs directory"
        }

        # Clean publish directory
        if (Test-Path "publish") {
            Remove-Item "publish" -Recurse -Force
            Write-Success "Cleaned publish directory"
        }

        # Clean zip files
        Get-ChildItem "*.zip" -ErrorAction SilentlyContinue | Remove-Item -Force

        Write-Success "Clean completed"
    }

    "watch" {
        Write-Header "Hot Reload Mode"
        Write-Info "Starting application with hot reload..."
        Write-Host "Modify XAML or C# files to automatically reload`n" -ForegroundColor Gray

        dotnet watch run --project src/DevToolbox.App
    }

    "help" {
        Write-Host "`nDevToolbox Development Script" -ForegroundColor Cyan
        Write-Host "=======================`n" -ForegroundColor Cyan

        Write-Host "Usage:" -ForegroundColor Yellow
        Write-Host "  .\dev.ps1 [command]`n"

        Write-Host "Available commands:" -ForegroundColor Yellow
        Write-Host "  run     - Run the application (default)"
        Write-Host "  test    - Run unit tests"
        Write-Host "  build   - Build the project"
        Write-Host "  clean   - Clean build artifacts and temporary files"
        Write-Host "  watch   - Run with hot reload"
        Write-Host "  help    - Show this help message`n"

        Write-Host "Examples:" -ForegroundColor Yellow
        Write-Host "  .\dev.ps1              # Run app"
        Write-Host "  .\dev.ps1 test         # Run tests"
        Write-Host "  .\dev.ps1 watch        # Hot reload mode`n"

        Write-Host "Tips:" -ForegroundColor Yellow
        Write-Host "  - Use watch command for development with auto-reload"
        Write-Host "  - Log files are in logs/ directory"
        Write-Host "  - For publishing use .\publish.ps1 script`n"
    }

    default {
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Write-Host "Use '.\dev.ps1 help' for usage`n" -ForegroundColor Yellow
        exit 1
    }
}
