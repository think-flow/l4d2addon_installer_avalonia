#!/bin/bash

echo "============================================"
echo ".NET AOT发布脚本"
echo "============================================"

# 清理旧构建
echo "正在清理旧构建文件..."
dotnet clean -c Release

# 发布项目
echo "正在发布项目..."
dotnet publish -c Release \
    -p:PublishAot=true 

# 检查结果
if [  $? -eq 0 ]; then
    echo "√√√ 发布成功！"
else
    echo "xxx 发布失败！请检查错误信息"
fi

# 退出提示（按回车继续）
read -p "按 Enter 键退出..."