Write-Host "============================================"
Write-Host ".NET AOT发布脚本"
Write-Host "============================================"

# 清理旧构建
Write-Host "正在清理旧构建文件..."
dotnet clean -c Release

# 发布项目
Write-Host "正在发布项目..."
dotnet publish -c Release

# 检查结果
if ($LASTEXITCODE -eq 0) {
    Write-Host "√√√ 发布成功！"
} else {
    Write-Host "xxx 发布失败！请检查错误信息"
}

# 退出提示
Read-Host "按 Enter 键退出..."