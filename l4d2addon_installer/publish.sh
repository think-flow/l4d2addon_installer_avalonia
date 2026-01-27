#!/bin/bash

echo "============================================"
echo ".NET 单文件发布脚本 (Release | 自包含)"
echo "============================================"

# 清理旧构建
echo "正在清理旧构建文件..."
dotnet clean -c Release

# 发布项目
echo "正在发布项目..."
dotnet publish -c Release \
    --self-contained true \
    -p:PublishAot=false \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

# 检查结果
if [  $? -eq 0 ]; then
    echo "√√√ 发布成功！"
else
    echo "xxx 发布失败！请检查错误信息"
fi

# 退出提示（按回车继续）
read -p "按 Enter 键退出..."