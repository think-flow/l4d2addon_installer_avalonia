## 该文件夹功能

avalonia Native AOT 发布时，会带上以下3个动态链接库文件

`av_libglesv2.dll `

`libHarfBuzzSharp.dll`

`libSkiaSharp.dll`

而本文件夹可以存放上面3个动态链接库的静态库文件lib，用于在本机发布时作为静态链接

这样发布的程序，就可以删掉以上3个dll，作为单文件使用



> 由于静态链接库 过大，所以本文件夹不存放真实文件
>
> 请根据下面的链接下载对应的lib文件



来源 https://github.com/peaceshi/Avalonia-NativeAOT-SingleFile

Copy:

https://github.com/2ndlab/SkiaSharp.Static/releases
https://github.com/2ndlab/ANGLE.Static/releases

here.