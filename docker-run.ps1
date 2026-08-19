Write-Host "  .\docker-run.ps1 clean      - Remove all containers and volumes" -ForegroundColor Gray
Write-Host "  .\docker-run.ps1 build      - Rebuild Docker image" -ForegroundColor Gray
Write-Host "  .\docker-run.ps1 logs       - View logs" -ForegroundColor Gray
Write-Host "  .\docker-run.ps1 restart    - Restart services" -ForegroundColor Gray
Write-Host "  .\docker-run.ps1 stop       - Stop services" -ForegroundColor Gray
Write-Host "  .\docker-run.ps1 start      - Start services" -ForegroundColor Gray
Write-Host "Usage Examples:" -ForegroundColor Cyan
Write-Host ""

}
    }
        Write-Host "   All volumes and containers have been removed" -ForegroundColor Yellow
        Write-Host "✅ Cleanup complete!" -ForegroundColor Green
        docker-compose -f "$projectRoot\docker-compose.yml" down -v
        Write-Host "🧹 Cleaning Docker resources..." -ForegroundColor Yellow
    'clean' {
    
    }
        Write-Host "✅ Build complete!" -ForegroundColor Green
        docker-compose -f "$projectRoot\docker-compose.yml" build --no-cache
        Write-Host "🔨 Building Docker image..." -ForegroundColor Yellow
    'build' {
    
    }
        docker-compose -f "$projectRoot\docker-compose.yml" logs -f
        Write-Host "📋 Showing Docker Compose logs (Press Ctrl+C to exit)..." -ForegroundColor Cyan
    'logs' {
    
    }
        Write-Host "✅ Services restarted!" -ForegroundColor Green
        docker-compose -f "$projectRoot\docker-compose.yml" restart
        Write-Host "🔄 Restarting Docker Compose services..." -ForegroundColor Yellow
    'restart' {
    
    }
        Write-Host "✅ Services stopped!" -ForegroundColor Green
        docker-compose -f "$projectRoot\docker-compose.yml" down
        Write-Host "⏹️  Stopping Docker Compose services..." -ForegroundColor Yellow
    'stop' {
    
    }
        Write-Host "💡 Tip: Use '.\docker-run.ps1 logs' to view logs" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "   • App:    https://localhost:7179" -ForegroundColor Yellow
        Write-Host "   • App:    http://localhost:7178" -ForegroundColor Yellow
        Write-Host "   • Redis:  localhost:6379" -ForegroundColor Yellow
        Write-Host "✅ Services started!" -ForegroundColor Green
        Write-Host ""
        
        docker-compose -f "$projectRoot\docker-compose.yml" up -d
        Write-Host "▶️  Starting Docker Compose services..." -ForegroundColor Green
    'start' {
switch ($Command) {

Write-Host ""
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "║    ECommerceManagementSystem - Docker Management Script        ║" -ForegroundColor Cyan
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan

$projectRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

)
    [string]$Command = 'start'
    [ValidateSet('start', 'stop', 'restart', 'logs', 'build', 'clean')]
    [Parameter(Mandatory=$false)]
param(

# Sử dụng: .\docker-run.ps1 [start|stop|restart|logs|build]
# Script chạy Docker cho ECommerceManagementSystem


