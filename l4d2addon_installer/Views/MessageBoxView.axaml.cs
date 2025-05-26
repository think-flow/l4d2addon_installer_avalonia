using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace l4d2addon_installer.Views;

public partial class MessageBoxView : Message
{
    //消息框存活时长 （毫秒）
    private const int LifeTime = 3 * 1000;

    private readonly Dictionary<Border, CancellationTokenSource> _tokens = new();

    public MessageBoxView()
    {
        InitializeComponent();
        Initialize(this);
    }

    /*
    /// <summary>
    /// 弹出Success样式的消息框
    /// </summary>
    public void Success(string message) => ShowMsg("success", message);

    /// <summary>
    /// 弹出Error样式的消息框
    /// </summary>
    public void Error(string message) => ShowMsg("error", message);
    */

    protected override void ShowMsg(string @class, string message)
    {
        var border = new Border
        {
            Classes = {@class},
            Child = new StackPanel
            {
                Children =
                {
                    new Path(),
                    new SelectableTextBlock
                    {
                        Text = message
                    }
                }
            }
        };
        //为border添加事件
        border.PointerEntered += Border_PointerEntered;
        border.PointerExited += Border_PointerExited;
        _tokens.Add(border, new CancellationTokenSource());

        MsgBox.Children.Add(border);

        // 触发opacity的过渡动画
        border.Opacity = 1;
        border.RenderTransform = TransformOperations.Parse("translateY(0px)");

        RemoveBorderAfterLiveTime(border);
    }

    private void Border_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border) return;

        var tokenSource = _tokens[border];
        tokenSource.Cancel();
    }

    private void Border_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border) return;

        var tokenSource = _tokens[border];
        if (!tokenSource.IsCancellationRequested) return;
        _tokens[border] = new CancellationTokenSource();
        RemoveBorderAfterLiveTime(border);
    }

    //在指定的生命周期后，移出消息框
    private void RemoveBorderAfterLiveTime(Border border)
    {
        var tokenSource = _tokens[border];
        Task.Run(async () =>
        {
            await Task.Delay(LifeTime, tokenSource.Token);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                tokenSource.Token.ThrowIfCancellationRequested();
                border.Opacity = 0;
                border.IsVisible = false;
                border.RenderTransform = TransformOperations.Parse("translateY(-20px)");
            }, default, tokenSource.Token);
            await Task.Delay(1000, tokenSource.Token);
            //将失活的border移出，已施放内存
            Dispatcher.UIThread.Post(() => MsgBox.Children.Remove(border));
        }, tokenSource.Token);
    }
}

public abstract class Message : UserControl
{
    private static Message? _source;

    protected static void Initialize(Message source)
    {
        _source = source;
    }

    /// <summary>
    /// 弹出Success样式的消息框
    /// </summary>
    public static void Success(string message)
    {
        if (_source is null) throw new InvalidOperationException("MessageBoxView is not initialized");
        Dispatcher.UIThread.Post(() => _source.ShowMsg("success", message));
    }

    /// <summary>
    /// 弹出Error样式的消息框
    /// </summary>
    public static void Error(string message)
    {
        if (_source is null) throw new InvalidOperationException("MessageBoxView is not initialized");
        Dispatcher.UIThread.Post(() => _source.ShowMsg("error", message));
    }

    protected abstract void ShowMsg(string @class, string message);
}
