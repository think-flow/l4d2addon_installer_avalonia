using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using l4d2addon_installer.ViewModels;

namespace l4d2addon_installer;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        string name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control) Activator.CreateInstance(type)!;
        }

        return new TextBlock {Text = "Not Found: " + name};
    }

    public bool Match(object? data) => data is ViewModelBase;
}
