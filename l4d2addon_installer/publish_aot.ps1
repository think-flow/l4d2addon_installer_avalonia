Write-Host "============================================"
Write-Host ".NET AOT发布脚本"
Write-Host "============================================"

# 清理旧构建
Write-Host "正在清理旧构建文件..."
dotnet clean -c Release

# 发布项目  如果不需要静态链接av_libglesv2.lib libHarfBuzzSharp.lib libSkiaSharp.lib 则将EnableStaticNativeLibraries设为false
Write-Host "正在发布项目..."
dotnet publish -c Release -p:PublishAot=true -p:EnableStaticNativeLibraries=true

# 检查结果
if ($LASTEXITCODE -eq 0) {
    Write-Host "√√√ 发布成功！"
} else {
    Write-Host "xxx 发布失败！请检查错误信息"
}

# 退出提示
Read-Host "按 Enter 键退出..."